using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using PluginRuntime.Core.Entities;
using PluginRuntime.Core.Enums;
using PluginRuntime.Infrastructure.Persistence.Entities;

namespace PluginRuntime.Infrastructure.Persistence;

public class PluginRuntimeDbContext : DbContext
{
    public DbSet<Plugin> Plugins => Set<Plugin>();
    public DbSet<PluginVersion> PluginVersions => Set<PluginVersion>();
    public DbSet<Manifest> Manifests => Set<Manifest>();
    public DbSet<CapabilityEntity> Capabilities => Set<CapabilityEntity>();
    public DbSet<Execution> Executions => Set<Execution>();
    public DbSet<AuditLogEntity> AuditLogs => Set<AuditLogEntity>();
    public DbSet<RevocationEntity> Revocations => Set<RevocationEntity>();
    public DbSet<ApprovalEntity> Approvals => Set<ApprovalEntity>();
    public DbSet<RuntimeNodeEntity> RuntimeNodes => Set<RuntimeNodeEntity>();
    public DbSet<ExtensionRegistryEntity> ExtensionRegistry => Set<ExtensionRegistryEntity>();
    public DbSet<ExtensionSubscriptionEntity> ExtensionSubscriptions => Set<ExtensionSubscriptionEntity>();
    public DbSet<PermissionReviewEntity> PermissionReviews => Set<PermissionReviewEntity>();
    public DbSet<DeclarativeConfigEntity> DeclarativeConfigs => Set<DeclarativeConfigEntity>();

    public PluginRuntimeDbContext(DbContextOptions<PluginRuntimeDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigurePlugins(modelBuilder);
        ConfigurePluginVersions(modelBuilder);
        ConfigureManifests(modelBuilder);
        ConfigureCapabilities(modelBuilder);
        ConfigureExecutions(modelBuilder);
        ConfigureAuditLogs(modelBuilder);
        ConfigureRevocations(modelBuilder);
        ConfigureApprovals(modelBuilder);
        ConfigureRuntimeNodes(modelBuilder);
        ConfigureExtensionRegistry(modelBuilder);
        ConfigureExtensionSubscriptions(modelBuilder);
        ConfigurePermissionReviews(modelBuilder);
        ConfigureDeclarativeConfigs(modelBuilder);
    }

    public override int SaveChanges()
    {
        ThrowIfAuditLogsModified();
        return base.SaveChanges();
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ThrowIfAuditLogsModified();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfAuditLogsModified();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        ThrowIfAuditLogsModified();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void ThrowIfAuditLogsModified()
    {
        var modifiedAuditLogs = ChangeTracker.Entries<AuditLogEntity>()
            .Where(e => e.State == EntityState.Modified || e.State == EntityState.Deleted);

        if (modifiedAuditLogs.Any())
        {
            throw new InvalidOperationException(
                "Audit logs are immutable. UPDATE and DELETE operations are not permitted on the audit_logs table.");
        }
    }

    private static void ConfigurePlugins(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Plugin>(entity =>
        {
            entity.ToTable("plugins");
            entity.HasKey(e => e.PluginId);
            entity.Property(e => e.PluginId).HasColumnName("plugin_id");
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
            entity.HasIndex(e => e.Name).IsUnique();
            entity.Property(e => e.DisplayName).HasColumnName("display_name").HasMaxLength(500).IsRequired();
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.OwnerId).HasColumnName("owner_id").IsRequired();
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(50).IsRequired()
                .HasConversion(new ValueConverter<PluginStatus, string>(
                    v => v.ToString(),
                    v => Enum.Parse<PluginStatus>(v)));
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").IsRequired();
            entity.Property(e => e.DeletedAt).HasColumnName("deleted_at");

            entity.HasQueryFilter(p => p.DeletedAt == null);
        });
    }

    private static void ConfigurePluginVersions(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PluginVersion>(entity =>
        {
            entity.ToTable("plugin_versions");
            entity.HasKey(e => e.VersionId);
            entity.Property(e => e.VersionId).HasColumnName("version_id");
            entity.Property(e => e.PluginId).HasColumnName("plugin_id").IsRequired();
            entity.Property(e => e.Version).HasColumnName("version").HasMaxLength(50).IsRequired();
            entity.Property(e => e.StorageUri).HasColumnName("storage_uri").HasMaxLength(2000).IsRequired();
            entity.Property(e => e.Sha256).HasColumnName("sha256").HasMaxLength(64).IsRequired();
            entity.Property(e => e.EntryPoint).HasColumnName("entry_point").HasMaxLength(500).IsRequired();
            entity.Property(e => e.EntryClass).HasColumnName("entry_class").HasMaxLength(500).IsRequired();
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(50).IsRequired()
                .HasConversion(new ValueConverter<PluginVersionStatus, string>(
                    v => v.ToString(),
                    v => Enum.Parse<PluginVersionStatus>(v)));
            entity.Property(e => e.ApprovedBy).HasColumnName("approved_by");
            entity.Property(e => e.ApprovedAt).HasColumnName("approved_at");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();

            entity.HasIndex(e => new { e.PluginId, e.Version }).IsUnique()
                .HasDatabaseName("uq_plugin_version");
        });
    }

    private static void ConfigureManifests(ModelBuilder modelBuilder)
    {
        var jsonElementComparer = new ValueComparer<JsonElement>(
            (l, r) => l.GetRawText() == r.GetRawText(),
            v => v.GetRawText().GetHashCode(),
            v => v.Clone());

        modelBuilder.Entity<Manifest>(entity =>
        {
            entity.ToTable("manifests");
            entity.HasKey(e => e.ManifestId);
            entity.Property(e => e.ManifestId).HasColumnName("manifest_id");
            entity.Property(e => e.VersionId).HasColumnName("version_id").IsRequired();
            entity.HasIndex(e => e.VersionId).IsUnique();
            entity.Property(e => e.ManifestVersion).HasColumnName("manifest_version").HasMaxLength(20).IsRequired();
            entity.Property(e => e.TargetCoreVersion).HasColumnName("target_core_version").HasMaxLength(100).IsRequired();
            entity.Property(e => e.Permissions).HasColumnName("permissions").HasColumnType("jsonb").IsRequired()
                .HasConversion(
                    v => v.GetRawText(),
                    v => JsonDocument.Parse(v).RootElement.Clone())
                .Metadata.SetValueComparer(jsonElementComparer);
            entity.Property(e => e.Capabilities).HasColumnName("capabilities").HasColumnType("jsonb").IsRequired()
                .HasConversion(
                    v => v.GetRawText(),
                    v => JsonDocument.Parse(v).RootElement.Clone())
                .Metadata.SetValueComparer(jsonElementComparer);
            entity.Property(e => e.ExecutionTimeoutMs).HasColumnName("execution_timeout_ms").IsRequired();
            entity.Property(e => e.MaxMemoryMb).HasColumnName("max_memory_mb").IsRequired();
            entity.Property(e => e.MaxCpuMs).HasColumnName("max_cpu_ms").IsRequired();
            entity.Property(e => e.AllowParallel).HasColumnName("allow_parallel").IsRequired();
            entity.Property(e => e.Signature).HasColumnName("signature").IsRequired();
            entity.Property(e => e.SignatureAlgorithm).HasColumnName("signature_algorithm").HasMaxLength(50).IsRequired()
                .HasConversion(new ValueConverter<string, string>(
                    v => v,
                    v => v));
            entity.Property(e => e.PublicKeyId).HasColumnName("public_key_id").HasMaxLength(200).IsRequired();
            entity.Property(e => e.IssuedAt).HasColumnName("issued_at").IsRequired();
            entity.Property(e => e.ExpiresAt).HasColumnName("expires_at").IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();
        });
    }

    private static void ConfigureCapabilities(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CapabilityEntity>(entity =>
        {
            entity.ToTable("capabilities");
            entity.HasKey(e => e.CapabilityId);
            entity.Property(e => e.CapabilityId).HasColumnName("capability_id");
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
            entity.HasIndex(e => e.Name).IsUnique();
            entity.Property(e => e.Version).HasColumnName("version").HasMaxLength(50).IsRequired();
            entity.Property(e => e.Category).HasColumnName("category").HasMaxLength(100).IsRequired();
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Enabled).HasColumnName("enabled").IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();
        });
    }

    private static void ConfigureExecutions(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Execution>(entity =>
        {
            entity.ToTable("executions");
            entity.HasKey(e => e.ExecutionId);
            entity.Property(e => e.ExecutionId).HasColumnName("execution_id");
            entity.Property(e => e.PluginId).HasColumnName("plugin_id").IsRequired();
            entity.Property(e => e.VersionId).HasColumnName("version_id").IsRequired();
            entity.Property(e => e.TraceId).HasColumnName("trace_id").HasMaxLength(200).IsRequired();
            entity.Property(e => e.CorrelationId).HasColumnName("correlation_id").HasMaxLength(200);
            entity.Property(e => e.TenantId).HasColumnName("tenant_id").HasMaxLength(200);
            entity.Property(e => e.UserId).HasColumnName("user_id").HasMaxLength(200);
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(50).IsRequired()
                .HasConversion(new ValueConverter<ExecutionStatus, string>(
                    v => v.ToString(),
                    v => Enum.Parse<ExecutionStatus>(v)));
            entity.Property(e => e.ErrorCode).HasColumnName("error_code").HasMaxLength(50);
            entity.Property(e => e.ErrorMessage).HasColumnName("error_message");
            entity.Property(e => e.StartTime).HasColumnName("start_time").IsRequired();
            entity.Property(e => e.EndTime).HasColumnName("end_time");
            entity.Property(e => e.DurationMs).HasColumnName("duration_ms");
            entity.Property(e => e.NodeId).HasColumnName("node_id").HasMaxLength(200);
        });
    }

    private static void ConfigureAuditLogs(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuditLogEntity>(entity =>
        {
            entity.ToTable("audit_logs");
            entity.HasKey(e => e.AuditId);
            entity.Property(e => e.AuditId).HasColumnName("audit_id");
            entity.Property(e => e.Timestamp).HasColumnName("timestamp").IsRequired();
            entity.Property(e => e.ActorId).HasColumnName("actor_id").HasMaxLength(200).IsRequired();
            entity.Property(e => e.ActorType).HasColumnName("actor_type").HasMaxLength(50).IsRequired()
                .HasConversion(new ValueConverter<ActorType, string>(
                    v => v.ToString(),
                    v => Enum.Parse<ActorType>(v)));
            entity.Property(e => e.Action).HasColumnName("action").HasMaxLength(200).IsRequired();
            entity.Property(e => e.ResourceType).HasColumnName("resource_type").HasMaxLength(100).IsRequired();
            entity.Property(e => e.ResourceId).HasColumnName("resource_id").HasMaxLength(200).IsRequired();
            entity.Property(e => e.IpAddress).HasColumnName("ip_address").HasMaxLength(50);
            entity.Property(e => e.Result).HasColumnName("result").HasMaxLength(50).IsRequired()
                .HasConversion(new ValueConverter<AuditResult, string>(
                    v => v.ToString(),
                    v => Enum.Parse<AuditResult>(v)));
            entity.Property(e => e.Metadata).HasColumnName("metadata").HasColumnType("jsonb");
        });
    }

    private static void ConfigureRevocations(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RevocationEntity>(entity =>
        {
            entity.ToTable("revocations");
            entity.HasKey(e => e.RevocationId);
            entity.Property(e => e.RevocationId).HasColumnName("revocation_id");
            entity.Property(e => e.VersionId).HasColumnName("version_id").IsRequired();
            entity.Property(e => e.Reason).HasColumnName("reason").IsRequired();
            entity.Property(e => e.RevokedBy).HasColumnName("revoked_by").IsRequired();
            entity.Property(e => e.RevokedAt).HasColumnName("revoked_at").IsRequired();
            entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");
        });
    }

    private static void ConfigureApprovals(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ApprovalEntity>(entity =>
        {
            entity.ToTable("approvals");
            entity.HasKey(e => e.ApprovalId);
            entity.Property(e => e.ApprovalId).HasColumnName("approval_id");
            entity.Property(e => e.VersionId).HasColumnName("version_id").IsRequired();
            entity.Property(e => e.ReviewerId).HasColumnName("reviewer_id").IsRequired();
            entity.Property(e => e.Decision).HasColumnName("decision").HasMaxLength(50).IsRequired()
                .HasConversion(new ValueConverter<ApprovalDecision, string>(
                    v => v.ToString(),
                    v => Enum.Parse<ApprovalDecision>(v)));
            entity.Property(e => e.Comment).HasColumnName("comment");
            entity.Property(e => e.DecidedAt).HasColumnName("decided_at").IsRequired();
        });
    }

    private static void ConfigureRuntimeNodes(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RuntimeNodeEntity>(entity =>
        {
            entity.ToTable("runtime_nodes");
            entity.HasKey(e => e.NodeId);
            entity.Property(e => e.NodeId).HasColumnName("node_id").HasMaxLength(200);
            entity.Property(e => e.Hostname).HasColumnName("hostname").HasMaxLength(500).IsRequired();
            entity.Property(e => e.Version).HasColumnName("version").HasMaxLength(50).IsRequired();
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(50).IsRequired();
            entity.Property(e => e.StartedAt).HasColumnName("started_at").IsRequired();
            entity.Property(e => e.LastHeartbeat).HasColumnName("last_heartbeat").IsRequired();
        });
    }

    private static void ConfigureExtensionRegistry(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ExtensionRegistryEntity>(entity =>
        {
            entity.ToTable("extension_registry");
            entity.HasKey(e => e.ExtensionId);
            entity.Property(e => e.ExtensionId).HasColumnName("extension_id").HasMaxLength(200);
            entity.Property(e => e.PluginId).HasColumnName("plugin_id").IsRequired();
            entity.Property(e => e.DisplayName).HasColumnName("display_name").HasMaxLength(500).IsRequired();
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.AuthorId).HasColumnName("author_id").IsRequired();
            entity.Property(e => e.Visibility).HasColumnName("visibility").HasMaxLength(50).IsRequired()
                .HasConversion(new ValueConverter<Visibility, string>(
                    v => v.ToString(),
                    v => Enum.Parse<Visibility>(v)));
            entity.Property(e => e.Category).HasColumnName("category").HasMaxLength(100);
            entity.Property(e => e.LatestVersion).HasColumnName("latest_version").HasMaxLength(50);
            entity.Property(e => e.TotalVersions).HasColumnName("total_versions").IsRequired();
            entity.Property(e => e.SubscriberCount).HasColumnName("subscriber_count").IsRequired();
            entity.Property(e => e.InvocationPolicy).HasColumnName("invocation_policy").HasColumnType("jsonb");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").IsRequired();
        });
    }

    private static void ConfigureExtensionSubscriptions(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ExtensionSubscriptionEntity>(entity =>
        {
            entity.ToTable("extension_subscriptions");
            entity.HasKey(e => e.SubscriptionId);
            entity.Property(e => e.SubscriptionId).HasColumnName("subscription_id");
            entity.Property(e => e.SourceExtensionId).HasColumnName("source_extension_id").HasMaxLength(200).IsRequired();
            entity.Property(e => e.TargetExtensionId).HasColumnName("target_extension_id").HasMaxLength(200).IsRequired();
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(50).IsRequired()
                .HasConversion(new ValueConverter<SubscriptionStatus, string>(
                    v => v.ToString(),
                    v => Enum.Parse<SubscriptionStatus>(v)));
            entity.Property(e => e.Reason).HasColumnName("reason");
            entity.Property(e => e.ExpectedUsage).HasColumnName("expected_usage").HasColumnType("jsonb");
            entity.Property(e => e.Conditions).HasColumnName("conditions");
            entity.Property(e => e.DecidedBy).HasColumnName("decided_by");
            entity.Property(e => e.DecidedAt).HasColumnName("decided_at");
            entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");
            entity.Property(e => e.RevokedAt).HasColumnName("revoked_at");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();

            entity.HasIndex(e => new { e.SourceExtensionId, e.TargetExtensionId }).IsUnique()
                .HasDatabaseName("uq_subscription");
        });
    }

    private static void ConfigurePermissionReviews(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PermissionReviewEntity>(entity =>
        {
            entity.ToTable("permission_reviews");
            entity.HasKey(e => e.ReviewId);
            entity.Property(e => e.ReviewId).HasColumnName("review_id");
            entity.Property(e => e.VersionId).HasColumnName("version_id").IsRequired();
            entity.Property(e => e.Permissions).HasColumnName("permissions").HasColumnType("jsonb").IsRequired();
            entity.Property(e => e.RiskSummary).HasColumnName("risk_summary").HasColumnType("jsonb").IsRequired();
            entity.Property(e => e.PermissionDiff).HasColumnName("permission_diff").HasColumnType("jsonb");
            entity.Property(e => e.OverallRiskLevel).HasColumnName("overall_risk_level").HasMaxLength(50).IsRequired()
                .HasConversion(new ValueConverter<RiskLevel, string>(
                    v => v.ToString(),
                    v => Enum.Parse<RiskLevel>(v)));
            entity.Property(e => e.ReviewerId).HasColumnName("reviewer_id");
            entity.Property(e => e.Decision).HasColumnName("decision").HasMaxLength(50)
                .HasConversion(new ValueConverter<ApprovalDecision?, string?>(
                    v => v.HasValue ? v.Value.ToString() : null,
                    v => v != null ? Enum.Parse<ApprovalDecision>(v) : null));
            entity.Property(e => e.Comment).HasColumnName("comment");
            entity.Property(e => e.Conditions).HasColumnName("conditions").HasColumnType("jsonb");
            entity.Property(e => e.DecidedAt).HasColumnName("decided_at");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();
        });
    }

    private static void ConfigureDeclarativeConfigs(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DeclarativeConfigEntity>(entity =>
        {
            entity.ToTable("declarative_configs");
            entity.HasKey(e => e.ConfigId);
            entity.Property(e => e.ConfigId).HasColumnName("config_id");
            entity.Property(e => e.ExtensionId).HasColumnName("extension_id").HasMaxLength(200).IsRequired();
            entity.Property(e => e.Version).HasColumnName("version").HasMaxLength(50).IsRequired();
            entity.Property(e => e.Config).HasColumnName("config").HasColumnType("jsonb").IsRequired();
            entity.Property(e => e.InputSchema).HasColumnName("input_schema").HasColumnType("jsonb");
            entity.Property(e => e.OutputSchema).HasColumnName("output_schema").HasColumnType("jsonb");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();

            entity.HasIndex(e => new { e.ExtensionId, e.Version }).IsUnique()
                .HasDatabaseName("uq_declarative_version");
        });
    }
}
