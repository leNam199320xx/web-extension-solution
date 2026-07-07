using System.Reflection;
using System.Reflection.Emit;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using PluginRuntime.Core.Entities;
using PluginRuntime.Sdk;

namespace PluginRuntime.IntegrationTests.Helpers;

/// <summary>
/// Creates in-memory plugin assemblies and signed manifests for integration testing.
/// Avoids requiring pre-built DLL files on disk.
/// </summary>
public static class TestPluginFactory
{
    // ---------------------------------------------------------------
    // Key pair — generated once per process for all tests
    // ---------------------------------------------------------------
    private static readonly RSA _rsa;
    private static readonly byte[] _publicKeyBytes;
    public const string TestKeyId = "test-key-1";

    static TestPluginFactory()
    {
        _rsa = RSA.Create(2048);
        _publicKeyBytes = _rsa.ExportSubjectPublicKeyInfo();
    }

    public static byte[] PublicKeyBytes => _publicKeyBytes;

    // ---------------------------------------------------------------
    // Build a minimal IPlugin DLL in memory
    // ---------------------------------------------------------------

    /// <summary>
    /// Emits a simple IPlugin implementation as raw IL bytes.
    /// The plugin always returns success with {"message":"ok"} as output.
    /// </summary>
    public static byte[] BuildPluginAssembly(string typeName = "TestPlugin.SimplePlugin")
    {
        var parts = typeName.Split('.');
        var ns = string.Join(".", parts[..^1]);
        var className = parts[^1];

        var assemblyName = new AssemblyName($"{ns}.dll");
        var ab = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
        var mb = ab.DefineDynamicModule(assemblyName.Name!);
        var tb = mb.DefineType($"{ns}.{className}",
            TypeAttributes.Public | TypeAttributes.Class,
            typeof(object),
            new[] { typeof(IPlugin) });

        // Implement ExecuteAsync — always returns success
        var executeMethod = tb.DefineMethod(
            "ExecuteAsync",
            MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig,
            typeof(Task<PluginResult>),
            new[] { typeof(PluginContext), typeof(CancellationToken) });

        var il = executeMethod.GetILGenerator();
        // return Task.FromResult(new PluginResult(true, JsonDocument.Parse("{\"message\":\"ok\"}").RootElement, null, null))
        il.Emit(OpCodes.Ldc_I4_1);   // true
        il.Emit(OpCodes.Ldnull);     // JsonElement? data = null — simplified
        il.Emit(OpCodes.Ldnull);     // ErrorCode = null
        il.Emit(OpCodes.Ldnull);     // ErrorMessage = null
        il.Emit(OpCodes.Newobj, typeof(PluginResult).GetConstructor(
            new[] { typeof(bool), typeof(JsonElement?), typeof(string), typeof(string) })!);
        il.Emit(OpCodes.Call, typeof(Task).GetMethod("FromResult")!
            .MakeGenericMethod(typeof(PluginResult)));
        il.Emit(OpCodes.Ret);

        tb.DefineMethodOverride(executeMethod,
            typeof(IPlugin).GetMethod("ExecuteAsync")!);

        tb.CreateType();

        // Emit to a MemoryStream via Reflection.Emit Save (not available in .NET in-process)
        // Use a pre-built minimal assembly bytes instead.
        return GetMinimalPluginAssemblyBytes(typeName);
    }

    /// <summary>
    /// Returns a pre-compiled minimal plugin assembly as bytes.
    /// The assembly contains a class that implements IPlugin and always returns success.
    /// Since Reflection.Emit can't save in .NET 10 without extra packages, we compile
    /// a C# source using Roslyn (Microsoft.CodeAnalysis.CSharp is transitively available
    /// via the test SDK) or fall back to a static stub.
    /// </summary>
    public static byte[] GetMinimalPluginAssemblyBytes(string typeName = "TestPlugin.SimplePlugin")
    {
        // Compile a real C# class at test time using Roslyn (available via Microsoft.CodeAnalysis)
        // The IPlugin interface comes from PluginRuntime.Sdk which is transitively referenced.
        var sdkAssembly = typeof(IPlugin).Assembly;
        var pluginResultAssembly = typeof(PluginResult).Assembly;

        var source = $$"""
            using System.Threading;
            using System.Threading.Tasks;
            using System.Text.Json;
            using PluginRuntime.Sdk;
            namespace {{GetNamespace(typeName)}};
            public class {{GetClassName(typeName)}} : IPlugin
            {
                public Task<PluginResult> ExecuteAsync(PluginContext context, CancellationToken ct)
                {
                    var data = JsonDocument.Parse("{\"message\":\"ok\",\"pluginId\":\"" + context.PluginId + "\"}").RootElement;
                    return Task.FromResult(new PluginResult(true, data, null, null));
                }
            }
            """;

        return CompileSource(source, typeName, sdkAssembly, pluginResultAssembly);
    }

    private static byte[] CompileSource(string source, string typeName,
        params Assembly[] additionalRefs)
    {
        // Use basic Roslyn compilation available in test context
        var references = AppDomain.CurrentDomain
            .GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
            .Select(a => Microsoft.CodeAnalysis.MetadataReference.CreateFromFile(a.Location))
            .Cast<Microsoft.CodeAnalysis.MetadataReference>()
            .ToList();

        foreach (var asm in additionalRefs)
        {
            if (!asm.IsDynamic && !string.IsNullOrEmpty(asm.Location))
                references.Add(Microsoft.CodeAnalysis.MetadataReference.CreateFromFile(asm.Location));
        }

        var tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(source);
        var compilation = Microsoft.CodeAnalysis.CSharp.CSharpCompilation.Create(
            $"{typeName}.dll",
            new[] { tree },
            references,
            new Microsoft.CodeAnalysis.CSharp.CSharpCompilationOptions(
                Microsoft.CodeAnalysis.OutputKind.DynamicallyLinkedLibrary));

        using var ms = new MemoryStream();
        var result = compilation.Emit(ms);
        if (!result.Success)
        {
            var errors = string.Join("\n", result.Diagnostics
                .Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error)
                .Select(d => d.ToString()));
            throw new InvalidOperationException($"Plugin compilation failed:\n{errors}");
        }

        return ms.ToArray();
    }

    // ---------------------------------------------------------------
    // Build a signed Manifest for a given plugin version
    // ---------------------------------------------------------------
    public static Manifest BuildSignedManifest(
        Guid versionId,
        byte[] dllBytes,
        string targetCoreVersion = "10.0",
        string[]? capabilities = null,
        string[]? permissions = null,
        DateTime? expiresAt = null)
    {
        var manifestId = Guid.NewGuid();
        var caps = capabilities ?? [];
        var perms = permissions ?? [];
        var capJson = JsonDocument.Parse(JsonSerializer.Serialize(caps)).RootElement;
        var permJson = JsonDocument.Parse(JsonSerializer.Serialize(perms)).RootElement;

        var draft = new Manifest(
            manifestId: manifestId,
            versionId: versionId,
            targetCoreVersion: targetCoreVersion,
            signature: "PLACEHOLDER",
            publicKeyId: TestKeyId,
            permissions: permJson,
            capabilities: capJson,
            executionTimeoutMs: 5000,
            maxMemoryMb: 256,
            maxCpuMs: 2000,
            issuedAt: DateTime.UtcNow.AddMinutes(-1),
            expiresAt: expiresAt ?? DateTime.UtcNow.AddYears(1));

        // Compute the real signature
        var canonical = PluginRuntime.Security.Signing.SignatureVerifier.GetCanonicalContent(draft);
        var sigBytes = _rsa.SignData(canonical, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var signature = Convert.ToBase64String(sigBytes);

        // Rebuild with real signature
        return new Manifest(
            manifestId: manifestId,
            versionId: versionId,
            targetCoreVersion: targetCoreVersion,
            signature: signature,
            publicKeyId: TestKeyId,
            permissions: permJson,
            capabilities: capJson,
            executionTimeoutMs: 5000,
            maxMemoryMb: 256,
            maxCpuMs: 2000,
            issuedAt: DateTime.UtcNow.AddMinutes(-1),
            expiresAt: expiresAt ?? DateTime.UtcNow.AddYears(1));
    }

    /// <summary>Compute SHA-256 hex of DLL bytes (matches HashVerifier).</summary>
    public static string ComputeSha256(byte[] bytes)
        => Convert.ToHexStringLower(SHA256.HashData(bytes));

    /// <summary>
    /// Builds a manifest with a garbage (forged) Base64 signature that will
    /// always fail SignatureVerifier — used for security rejection tests.
    /// </summary>
    public static Manifest BuildForgedSignatureManifest(Guid versionId, byte[] dllBytes)
    {
        var manifestId = Guid.NewGuid();
        var capJson  = JsonDocument.Parse("[]").RootElement;
        var permJson = JsonDocument.Parse("[]").RootElement;

        // Use random bytes as a forged signature
        var forgedSigBytes = new byte[256];
        RandomNumberGenerator.Fill(forgedSigBytes);
        var forgedSig = Convert.ToBase64String(forgedSigBytes);

        return new Manifest(
            manifestId:          manifestId,
            versionId:           versionId,
            targetCoreVersion:   "10.0",
            signature:           forgedSig,
            publicKeyId:         TestKeyId,
            permissions:         permJson,
            capabilities:        capJson,
            executionTimeoutMs:  5000,
            maxMemoryMb:         256,
            maxCpuMs:            2000,
            issuedAt:            DateTime.UtcNow.AddMinutes(-1),
            expiresAt:           DateTime.UtcNow.AddYears(1));
    }

    private static string GetNamespace(string typeName)
    {
        var idx = typeName.LastIndexOf('.');
        return idx >= 0 ? typeName[..idx] : "TestPlugin";
    }

    private static string GetClassName(string typeName)
    {
        var idx = typeName.LastIndexOf('.');
        return idx >= 0 ? typeName[(idx + 1)..] : typeName;
    }
}
