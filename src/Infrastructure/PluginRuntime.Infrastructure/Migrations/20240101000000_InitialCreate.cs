using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PluginRuntime.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // -------------------------------------------------------
            // 3.1 plugins
            // -------------------------------------------------------
            migrationBuilder.CreateTable(
                name: "plugins",
                columns: table => new
                {
                    plugin_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    display_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    owner_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Active"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_plugins", x => x.plugin_id);
                    table.UniqueConstraint("AK_plugins_name", x => x.name);
                });

            // -------------------------------------------------------
            // 3.4 capabilities
            // -------------------------------------------------------
            migrationBuilder.CreateTable(
                name: "capabilities",
                columns: table => new
                {
                    capability_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    version = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "1.0"),
                    category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_capabilities", x => x.capability_id);
                    table.UniqueConstraint("AK_capabilities_name", x => x.name);
                });

            // -------------------------------------------------------
            // 3.9 runtime_nodes
            // -------------------------------------------------------
            migrationBuilder.CreateTable(
                name: "runtime_nodes",
                columns: table => new
                {
                    node_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    hostname = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    version = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Active"),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    last_heartbeat = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_runtime_nodes", x => x.node_id);
                });

            // -------------------------------------------------------
            // 3.2 plugin_versions  (FK → plugins)
            // -------------------------------------------------------
            migrationBuilder.CreateTable(
                name: "plugin_versions",
                columns: table => new
                {
                    version_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    plugin_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    storage_uri = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    sha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    entry_point = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    entry_class = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Draft"),
                    approved_by = table.Column<Guid>(type: "uuid", nullable: true),
                    approved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_plugin_versions", x => x.version_id);
                    table.UniqueConstraint("uq_plugin_version", x => new { x.plugin_id, x.version });
                    table.ForeignKey(
                        name: "FK_plugin_versions_plugins_plugin_id",
                        column: x => x.plugin_id,
                        principalTable: "plugins",
                        principalColumn: "plugin_id",
                        onDelete: ReferentialAction.Restrict);
                });

            // -------------------------------------------------------
            // 3.5 executions
            // -------------------------------------------------------
            migrationBuilder.CreateTable(
                name: "executions",
                columns: table => new
                {
                    execution_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    plugin_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    trace_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    correlation_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    tenant_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Running"),
                    error_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    error_message = table.Column<string>(type: "text", nullable: true),
                    start_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    end_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    duration_ms = table.Column<int>(type: "integer", nullable: true),
                    node_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_executions", x => x.execution_id);
                });

            // -------------------------------------------------------
            // 3.6 audit_logs  (APPEND-ONLY)
            // -------------------------------------------------------
            migrationBuilder.CreateTable(
                name: "audit_logs",
                columns: table => new
                {
                    audit_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    actor_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    actor_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "User"),
                    action = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    resource_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    resource_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ip_address = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    result = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    metadata = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_logs", x => x.audit_id);
                });

            // -------------------------------------------------------
            // 3.3 manifests  (FK → plugin_versions)
            // -------------------------------------------------------
            migrationBuilder.CreateTable(
                name: "manifests",
                columns: table => new
                {
                    manifest_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    manifest_version = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "1.0"),
                    target_core_version = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    permissions = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "[]"),
                    capabilities = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "[]"),
                    execution_timeout_ms = table.Column<int>(type: "integer", nullable: false, defaultValue: 5000),
                    max_memory_mb = table.Column<int>(type: "integer", nullable: false, defaultValue: 256),
                    max_cpu_ms = table.Column<int>(type: "integer", nullable: false, defaultValue: 2000),
                    allow_parallel = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    signature = table.Column<string>(type: "text", nullable: false),
                    signature_algorithm = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "RSA-SHA256"),
                    public_key_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    issued_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_manifests", x => x.manifest_id);
                    table.UniqueConstraint("AK_manifests_version_id", x => x.version_id);
                    table.ForeignKey(
                        name: "FK_manifests_plugin_versions_version_id",
                        column: x => x.version_id,
                        principalTable: "plugin_versions",
                        principalColumn: "version_id",
                        onDelete: ReferentialAction.Restrict);
                });

            // -------------------------------------------------------
            // 3.7 revocations  (FK → plugin_versions)
            // -------------------------------------------------------
            migrationBuilder.CreateTable(
                name: "revocations",
                columns: table => new
                {
                    revocation_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reason = table.Column<string>(type: "text", nullable: false),
                    revoked_by = table.Column<Guid>(type: "uuid", nullable: false),
                    revoked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_revocations", x => x.revocation_id);
                    table.ForeignKey(
                        name: "FK_revocations_plugin_versions_version_id",
                        column: x => x.version_id,
                        principalTable: "plugin_versions",
                        principalColumn: "version_id",
                        onDelete: ReferentialAction.Restrict);
                });

            // -------------------------------------------------------
            // 3.8 approvals  (FK → plugin_versions)
            // -------------------------------------------------------
            migrationBuilder.CreateTable(
                name: "approvals",
                columns: table => new
                {
                    approval_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reviewer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    decision = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    comment = table.Column<string>(type: "text", nullable: true),
                    decided_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_approvals", x => x.approval_id);
                    table.ForeignKey(
                        name: "FK_approvals_plugin_versions_version_id",
                        column: x => x.version_id,
                        principalTable: "plugin_versions",
                        principalColumn: "version_id",
                        onDelete: ReferentialAction.Restrict);
                });

            // -------------------------------------------------------
            // 3.12 permission_reviews  (FK → plugin_versions)
            // -------------------------------------------------------
            migrationBuilder.CreateTable(
                name: "permission_reviews",
                columns: table => new
                {
                    review_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    permissions = table.Column<string>(type: "jsonb", nullable: false),
                    risk_summary = table.Column<string>(type: "jsonb", nullable: false),
                    permission_diff = table.Column<string>(type: "jsonb", nullable: true),
                    overall_risk_level = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    reviewer_id = table.Column<Guid>(type: "uuid", nullable: true),
                    decision = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    comment = table.Column<string>(type: "text", nullable: true),
                    conditions = table.Column<string>(type: "jsonb", nullable: true),
                    decided_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_permission_reviews", x => x.review_id);
                    table.ForeignKey(
                        name: "FK_permission_reviews_plugin_versions_version_id",
                        column: x => x.version_id,
                        principalTable: "plugin_versions",
                        principalColumn: "version_id",
                        onDelete: ReferentialAction.Restrict);
                });

            // -------------------------------------------------------
            // 3.10 extension_registry  (FK → plugins)
            // -------------------------------------------------------
            migrationBuilder.CreateTable(
                name: "extension_registry",
                columns: table => new
                {
                    extension_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    plugin_id = table.Column<Guid>(type: "uuid", nullable: false),
                    display_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    author_id = table.Column<Guid>(type: "uuid", nullable: false),
                    visibility = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Private"),
                    category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    latest_version = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    total_versions = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    subscriber_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    invocation_policy = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_extension_registry", x => x.extension_id);
                    table.ForeignKey(
                        name: "FK_extension_registry_plugins_plugin_id",
                        column: x => x.plugin_id,
                        principalTable: "plugins",
                        principalColumn: "plugin_id",
                        onDelete: ReferentialAction.Restrict);
                });

            // -------------------------------------------------------
            // 3.11 extension_subscriptions  (FK → extension_registry)
            // -------------------------------------------------------
            migrationBuilder.CreateTable(
                name: "extension_subscriptions",
                columns: table => new
                {
                    subscription_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    source_extension_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    target_extension_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Requested"),
                    reason = table.Column<string>(type: "text", nullable: true),
                    expected_usage = table.Column<string>(type: "jsonb", nullable: true),
                    conditions = table.Column<string>(type: "text", nullable: true),
                    decided_by = table.Column<Guid>(type: "uuid", nullable: true),
                    decided_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    revoked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_extension_subscriptions", x => x.subscription_id);
                    table.UniqueConstraint("uq_subscription", x => new { x.source_extension_id, x.target_extension_id });
                    table.ForeignKey(
                        name: "FK_extension_subscriptions_extension_registry_target_extension_id",
                        column: x => x.target_extension_id,
                        principalTable: "extension_registry",
                        principalColumn: "extension_id",
                        onDelete: ReferentialAction.Restrict);
                });

            // -------------------------------------------------------
            // 3.13 declarative_configs  (FK → extension_registry)
            // -------------------------------------------------------
            migrationBuilder.CreateTable(
                name: "declarative_configs",
                columns: table => new
                {
                    config_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    extension_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    version = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    config = table.Column<string>(type: "jsonb", nullable: false),
                    input_schema = table.Column<string>(type: "jsonb", nullable: true),
                    output_schema = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_declarative_configs", x => x.config_id);
                    table.UniqueConstraint("uq_declarative_version", x => new { x.extension_id, x.version });
                    table.ForeignKey(
                        name: "FK_declarative_configs_extension_registry_extension_id",
                        column: x => x.extension_id,
                        principalTable: "extension_registry",
                        principalColumn: "extension_id",
                        onDelete: ReferentialAction.Restrict);
                });

            // -------------------------------------------------------
            // Section 4: All 19 indexes from database-schema.md
            // -------------------------------------------------------

            // plugins
            migrationBuilder.CreateIndex(
                name: "idx_plugins_name",
                table: "plugins",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "idx_plugins_status",
                table: "plugins",
                column: "status",
                filter: "deleted_at IS NULL");

            // plugin_versions
            migrationBuilder.CreateIndex(
                name: "idx_versions_plugin_id",
                table: "plugin_versions",
                column: "plugin_id");

            migrationBuilder.CreateIndex(
                name: "idx_versions_status",
                table: "plugin_versions",
                column: "status");

            // manifests
            migrationBuilder.CreateIndex(
                name: "idx_manifests_version_id",
                table: "manifests",
                column: "version_id");

            migrationBuilder.CreateIndex(
                name: "idx_manifests_expires_at",
                table: "manifests",
                column: "expires_at");

            // executions
            migrationBuilder.CreateIndex(
                name: "idx_executions_trace_id",
                table: "executions",
                column: "trace_id");

            migrationBuilder.CreateIndex(
                name: "idx_executions_plugin_version",
                table: "executions",
                columns: new[] { "plugin_id", "version_id" });

            migrationBuilder.CreateIndex(
                name: "idx_executions_start_time",
                table: "executions",
                column: "start_time",
                descending: new[] { true });

            migrationBuilder.CreateIndex(
                name: "idx_executions_status",
                table: "executions",
                column: "status",
                filter: "status = 'Running'");

            // audit_logs
            migrationBuilder.CreateIndex(
                name: "idx_audit_timestamp",
                table: "audit_logs",
                column: "timestamp",
                descending: new[] { true });

            migrationBuilder.CreateIndex(
                name: "idx_audit_action",
                table: "audit_logs",
                column: "action");

            migrationBuilder.CreateIndex(
                name: "idx_audit_resource",
                table: "audit_logs",
                columns: new[] { "resource_type", "resource_id" });

            // revocations
            migrationBuilder.CreateIndex(
                name: "idx_revocations_version",
                table: "revocations",
                column: "version_id");

            // extension_registry
            migrationBuilder.CreateIndex(
                name: "idx_registry_visibility",
                table: "extension_registry",
                column: "visibility");

            migrationBuilder.CreateIndex(
                name: "idx_registry_author",
                table: "extension_registry",
                column: "author_id");

            migrationBuilder.CreateIndex(
                name: "idx_registry_category",
                table: "extension_registry",
                column: "category");

            // extension_subscriptions
            migrationBuilder.CreateIndex(
                name: "idx_subscriptions_source",
                table: "extension_subscriptions",
                column: "source_extension_id");

            migrationBuilder.CreateIndex(
                name: "idx_subscriptions_target",
                table: "extension_subscriptions",
                column: "target_extension_id");

            migrationBuilder.CreateIndex(
                name: "idx_subscriptions_status",
                table: "extension_subscriptions",
                column: "status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop indexes first (for explicit cleanup)
            migrationBuilder.DropIndex("idx_subscriptions_status", "extension_subscriptions");
            migrationBuilder.DropIndex("idx_subscriptions_target", "extension_subscriptions");
            migrationBuilder.DropIndex("idx_subscriptions_source", "extension_subscriptions");
            migrationBuilder.DropIndex("idx_registry_category", "extension_registry");
            migrationBuilder.DropIndex("idx_registry_author", "extension_registry");
            migrationBuilder.DropIndex("idx_registry_visibility", "extension_registry");
            migrationBuilder.DropIndex("idx_revocations_version", "revocations");
            migrationBuilder.DropIndex("idx_audit_resource", "audit_logs");
            migrationBuilder.DropIndex("idx_audit_action", "audit_logs");
            migrationBuilder.DropIndex("idx_audit_timestamp", "audit_logs");
            migrationBuilder.DropIndex("idx_executions_status", "executions");
            migrationBuilder.DropIndex("idx_executions_start_time", "executions");
            migrationBuilder.DropIndex("idx_executions_plugin_version", "executions");
            migrationBuilder.DropIndex("idx_executions_trace_id", "executions");
            migrationBuilder.DropIndex("idx_manifests_expires_at", "manifests");
            migrationBuilder.DropIndex("idx_manifests_version_id", "manifests");
            migrationBuilder.DropIndex("idx_versions_status", "plugin_versions");
            migrationBuilder.DropIndex("idx_versions_plugin_id", "plugin_versions");
            migrationBuilder.DropIndex("idx_plugins_status", "plugins");
            migrationBuilder.DropIndex("idx_plugins_name", "plugins");

            // Drop tables in reverse FK order
            migrationBuilder.DropTable(name: "declarative_configs");
            migrationBuilder.DropTable(name: "extension_subscriptions");
            migrationBuilder.DropTable(name: "extension_registry");
            migrationBuilder.DropTable(name: "permission_reviews");
            migrationBuilder.DropTable(name: "approvals");
            migrationBuilder.DropTable(name: "revocations");
            migrationBuilder.DropTable(name: "manifests");
            migrationBuilder.DropTable(name: "audit_logs");
            migrationBuilder.DropTable(name: "executions");
            migrationBuilder.DropTable(name: "plugin_versions");
            migrationBuilder.DropTable(name: "capabilities");
            migrationBuilder.DropTable(name: "runtime_nodes");
            migrationBuilder.DropTable(name: "plugins");
        }
    }
}
