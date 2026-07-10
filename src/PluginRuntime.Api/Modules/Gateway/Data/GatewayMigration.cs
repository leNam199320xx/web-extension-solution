using Microsoft.EntityFrameworkCore;
using PluginRuntime.Api.Shared.Infrastructure;

namespace PluginRuntime.Api.Modules.Gateway.Data;

/// <summary>
/// Database initialization for the Gateway module.
/// Creates plugin_access and failed_notifications tables if they don't exist.
/// Also ensures plan seed data is present.
/// </summary>
public static class GatewayMigration
{
    /// <summary>
    /// Ensures Gateway module tables and seed data exist.
    /// Call from startup or migration pipeline.
    /// </summary>
    public static async Task EnsureGatewaySchemaAsync(AppDbContext db, CancellationToken ct = default)
    {
        // The plugin_access and failed_notifications tables are created via EF Core migrations
        // (through the entity configurations). This method provides SQL-based creation
        // for initial setup or if migrations are not being used.

        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS plugin_access (
                tenant_id   UUID        NOT NULL,
                plugin_id   UUID        NOT NULL,
                source      VARCHAR(50) NOT NULL DEFAULT 'Free',
                package_id  UUID,
                granted_at  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                PRIMARY KEY (tenant_id, plugin_id)
            );
            
            CREATE INDEX IF NOT EXISTS idx_plugin_access_tenant ON plugin_access(tenant_id);
            CREATE INDEX IF NOT EXISTS idx_plugin_access_plugin ON plugin_access(plugin_id);
            CREATE INDEX IF NOT EXISTS idx_plugin_access_package ON plugin_access(package_id);
            """, ct);

        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS failed_notifications (
                notification_id UUID        PRIMARY KEY,
                channel         VARCHAR(200) NOT NULL,
                payload         JSONB       NOT NULL,
                retry_count     INT         NOT NULL DEFAULT 0,
                created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                processed_at    TIMESTAMPTZ
            );
            
            CREATE INDEX IF NOT EXISTS idx_failed_notifications_unprocessed 
                ON failed_notifications(created_at) WHERE processed_at IS NULL;
            """, ct);

        // Ensure plugins table has is_publicly_accessible column
        await db.Database.ExecuteSqlRawAsync("""
            ALTER TABLE plugins ADD COLUMN IF NOT EXISTS is_publicly_accessible BOOLEAN NOT NULL DEFAULT FALSE;
            """, ct);

        // Ensure plan seed data
        await EnsurePlanSeedDataAsync(db, ct);
    }

    private static async Task EnsurePlanSeedDataAsync(AppDbContext db, CancellationToken ct)
    {
        await db.Database.ExecuteSqlRawAsync("""
            INSERT INTO plans (name, type, rate_limit, daily_quota, max_api_keys, max_plugins_upload, max_package_subscriptions, monthly_price, is_billable)
            VALUES
              ('Free',       'Free',       100,  100,  2,    3,    0,    0.00,   true),
              ('Pro',        'Pro',        10000, 10000, 10,   20,   5,    49.00,  true),
              ('Enterprise', 'Enterprise', NULL, NULL,  50,   NULL, NULL, 299.00, true),
              ('Internal',   'Internal',   NULL, NULL,  NULL, NULL, NULL, 0.00,   false)
            ON CONFLICT (name) DO NOTHING;
            """, ct);
    }
}
