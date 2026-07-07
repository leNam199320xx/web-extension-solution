using System.Diagnostics;
using System.Text.Json;
using PluginRuntime.Core.Enums;
using PluginRuntime.Core.Interfaces;
using PluginRuntime.Core.ValueObjects;
using PluginRuntime.Sdk;

using Manifest = PluginRuntime.Core.Entities.Manifest;
using PluginVersion = PluginRuntime.Core.Entities.PluginVersion;

namespace PluginRuntime.Runtime.Pipeline;

public class ExecutionPipeline : IExecutionPipeline
{
    private readonly IManifestValidator _manifestValidator;
    private readonly ISignatureVerifier _signatureVerifier;
    private readonly IHashVerifier _hashVerifier;
    private readonly IRevocationChecker _revocationChecker;
    private readonly ICapabilityResolver _capabilityResolver;
    private readonly IPluginLoader _pluginLoader;
    private readonly IExecutionGovernor _executionGovernor;
    private readonly IObservabilityCollector _observabilityCollector;
    private readonly IPluginVersionRepository _pluginVersionRepository;
    private readonly IManifestRepository _manifestRepository;
    private readonly IObjectStorageService _objectStorageService;
    private readonly IAuditLogger _auditLogger;

    public ExecutionPipeline(
        IManifestValidator manifestValidator,
        ISignatureVerifier signatureVerifier,
        IHashVerifier hashVerifier,
        IRevocationChecker revocationChecker,
        ICapabilityResolver capabilityResolver,
        IPluginLoader pluginLoader,
        IExecutionGovernor executionGovernor,
        IObservabilityCollector observabilityCollector,
        IPluginVersionRepository pluginVersionRepository,
        IManifestRepository manifestRepository,
        IObjectStorageService objectStorageService,
        IAuditLogger auditLogger)
    {
        _manifestValidator = manifestValidator;
        _signatureVerifier = signatureVerifier;
        _hashVerifier = hashVerifier;
        _revocationChecker = revocationChecker;
        _capabilityResolver = capabilityResolver;
        _pluginLoader = pluginLoader;
        _executionGovernor = executionGovernor;
        _observabilityCollector = observabilityCollector;
        _pluginVersionRepository = pluginVersionRepository;
        _manifestRepository = manifestRepository;
        _objectStorageService = objectStorageService;
        _auditLogger = auditLogger;
    }

    public async Task<ExecutionResult> ProcessAsync(ExecutionRequest request, CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString("N");
        var executionId = Guid.NewGuid().ToString("N");
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Look up PluginVersion
            var pluginVersion = request.Version is not null
                ? await _pluginVersionRepository.GetByVersionAsync(request.PluginId, request.Version, cancellationToken)
                : await _pluginVersionRepository.GetLatestApprovedAsync(request.PluginId, cancellationToken);

            if (pluginVersion is null)
            {
                stopwatch.Stop();
                return CreateErrorResult(executionId, traceId, stopwatch, "PLUGIN_VERSION_NOT_FOUND",
                    "Plugin version not found.", "NotFound", "PluginVersionLookup");
            }

            // Look up Manifest
            var manifest = await _manifestRepository.GetByVersionIdAsync(pluginVersion.VersionId, cancellationToken);
            if (manifest is null)
            {
                stopwatch.Stop();
                return CreateErrorResult(executionId, traceId, stopwatch, "MANIFEST_NOT_FOUND",
                    "Manifest not found for the specified plugin version.", "NotFound", "ManifestLookup");
            }

            // Stage 1: ManifestValidator
            var validationResult = await _manifestValidator.ValidateAsync(manifest, cancellationToken);
            if (!validationResult.IsValid)
            {
                stopwatch.Stop();
                var errorMessage = string.Join("; ", validationResult.Errors.Select(e => e.Message));
                var errorCode = validationResult.Errors.FirstOrDefault()?.Code ?? "MANIFEST_INVALID";
                await LogSecurityFailureAsync(traceId, request.PluginId, "ManifestValidator", errorCode, errorMessage, cancellationToken);
                return CreateErrorResult(executionId, traceId, stopwatch, errorCode, errorMessage, "Security", "ManifestValidator");
            }

            // Stage 2: SignatureVerifier
            var signatureResult = await _signatureVerifier.VerifyAsync(manifest, cancellationToken);
            if (!signatureResult.IsValid)
            {
                stopwatch.Stop();
                await LogSecurityFailureAsync(traceId, request.PluginId, "SignatureVerifier",
                    signatureResult.ErrorCode ?? "SIGNATURE_INVALID", signatureResult.ErrorMessage ?? "Signature verification failed.", cancellationToken);
                return CreateErrorResult(executionId, traceId, stopwatch,
                    signatureResult.ErrorCode ?? "SIGNATURE_INVALID",
                    signatureResult.ErrorMessage ?? "Signature verification failed.",
                    "Security", "SignatureVerifier");
            }

            // Stage 3: HashVerifier
            var dllBytes = await _objectStorageService.GetPluginBinaryAsync(request.PluginId, pluginVersion.VersionId, cancellationToken);
            if (dllBytes is null)
            {
                stopwatch.Stop();
                await LogSecurityFailureAsync(traceId, request.PluginId, "HashVerifier", "BINARY_NOT_FOUND", "Plugin binary not found in storage.", cancellationToken);
                return CreateErrorResult(executionId, traceId, stopwatch, "BINARY_NOT_FOUND",
                    "Plugin binary not found in storage.", "Security", "HashVerifier");
            }

            var hashResult = await _hashVerifier.VerifyAsync(dllBytes, pluginVersion.Sha256, cancellationToken);
            if (!hashResult.IsValid)
            {
                stopwatch.Stop();
                await LogSecurityFailureAsync(traceId, request.PluginId, "HashVerifier",
                    hashResult.ErrorCode ?? "HASH_MISMATCH", hashResult.ErrorMessage ?? "Hash verification failed.", cancellationToken);
                return CreateErrorResult(executionId, traceId, stopwatch,
                    hashResult.ErrorCode ?? "HASH_MISMATCH",
                    hashResult.ErrorMessage ?? "Hash verification failed.",
                    "Security", "HashVerifier");
            }

            // Stage 4: RevocationChecker
            var isRevoked = await _revocationChecker.IsRevokedAsync(pluginVersion.VersionId, cancellationToken);
            if (isRevoked)
            {
                stopwatch.Stop();
                await LogSecurityFailureAsync(traceId, request.PluginId, "RevocationChecker", "PLUGIN_REVOKED", "Plugin version has been revoked.", cancellationToken);
                return CreateErrorResult(executionId, traceId, stopwatch, "PLUGIN_REVOKED",
                    "Plugin version has been revoked.", "Security", "RevocationChecker");
            }

            // Stage 5: CapabilityResolver
            var executionContext = new Core.ValueObjects.ExecutionContext(
                executionId, request.PluginId, request.Version,
                request.CorrelationId, request.UserId, request.TenantId);
            var capabilities = _capabilityResolver.Resolve(manifest, executionContext);

            // Stage 6: PluginLoader
            var plugin = await _pluginLoader.LoadAsync(pluginVersion, manifest, cancellationToken);

            // Stage 7: Execute plugin with resource governance via ExecutionGovernor
            var resourceLimits = new ResourceLimits(manifest.ExecutionTimeoutMs, manifest.MaxMemoryMb, manifest.MaxCpuMs);
            var pluginResult = await _executionGovernor.ExecuteWithLimitsAsync(
                async ct =>
                {
                    var context = new PluginContext
                    {
                        ExecutionId = executionId,
                        PluginId = request.PluginId.ToString(),
                        Version = pluginVersion.Version,
                        Input = request.Input,
                        Capabilities = capabilities.ToDictionary(kvp => kvp.Key, kvp => (object)kvp.Value),
                        CorrelationId = request.CorrelationId
                    };
                    return await plugin.ExecuteAsync(context, ct);
                },
                resourceLimits,
                cancellationToken);

            stopwatch.Stop();

            // Record successful execution via ObservabilityCollector
            var execution = new PluginRuntime.Core.Entities.Execution(
                executionId: Guid.Parse(executionId.PadLeft(32, '0').Insert(8, "-").Insert(13, "-").Insert(18, "-").Insert(23, "-")),
                pluginId: request.PluginId,
                versionId: pluginVersion.VersionId,
                traceId: traceId,
                status: pluginResult.Success ? ExecutionStatus.Completed : ExecutionStatus.Failed,
                correlationId: request.CorrelationId,
                tenantId: request.TenantId,
                userId: request.UserId,
                errorCode: pluginResult.ErrorCode,
                errorMessage: pluginResult.ErrorMessage,
                startTime: DateTime.UtcNow.AddMilliseconds(-stopwatch.ElapsedMilliseconds),
                endTime: DateTime.UtcNow,
                durationMs: (int)stopwatch.ElapsedMilliseconds);

            await _observabilityCollector.RecordExecutionAsync(execution, cancellationToken);

            return new ExecutionResult(
                Success: pluginResult.Success,
                Data: pluginResult.Data,
                ExecutionId: executionId,
                TraceId: traceId,
                DurationMs: (int)stopwatch.ElapsedMilliseconds,
                ErrorCode: pluginResult.ErrorCode,
                ErrorMessage: pluginResult.ErrorMessage,
                ErrorCategory: pluginResult.Success ? null : "Execution",
                FailingStage: pluginResult.Success ? null : "PluginExecutor");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            stopwatch.Stop();
            return CreateErrorResult(executionId, traceId, stopwatch, "EXECUTION_CANCELLED",
                "Execution was cancelled.", "Timeout", "PluginExecutor");
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            await LogSecurityFailureAsync(traceId, request.PluginId, "PluginExecutor",
                "EXECUTION_ERROR", ex.Message, cancellationToken);
            return CreateErrorResult(executionId, traceId, stopwatch, "EXECUTION_ERROR",
                ex.Message, "Execution", "PluginExecutor");
        }
    }

    private static ExecutionResult CreateErrorResult(
        string executionId, string traceId, Stopwatch stopwatch,
        string errorCode, string errorMessage, string errorCategory, string failingStage)
    {
        return new ExecutionResult(
            Success: false,
            Data: null,
            ExecutionId: executionId,
            TraceId: traceId,
            DurationMs: (int)stopwatch.ElapsedMilliseconds,
            ErrorCode: errorCode,
            ErrorMessage: errorMessage,
            ErrorCategory: errorCategory,
            FailingStage: failingStage);
    }

    private async Task LogSecurityFailureAsync(
        string traceId, Guid pluginId, string stage, string errorCode, string errorMessage,
        CancellationToken cancellationToken)
    {
        var entry = new AuditEntry(
            TraceId: traceId,
            ActorId: "system",
            ActorType: "System",
            Action: $"pipeline.{stage}.failed",
            ResourceType: "Plugin",
            ResourceId: pluginId.ToString(),
            Result: "Failure",
            IpAddress: null,
            Metadata: new Dictionary<string, object>
            {
                ["stage"] = stage,
                ["errorCode"] = errorCode,
                ["errorMessage"] = errorMessage
            });

        await _auditLogger.LogAsync(entry, cancellationToken);
    }
}
