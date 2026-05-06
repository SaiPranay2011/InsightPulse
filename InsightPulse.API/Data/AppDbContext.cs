using InsightPulse.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace InsightPulse.API.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {

        }

        public DbSet<Tenant> Tenants { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Dashboard> Dashboards { get; set; }
        public DbSet<DashboardWidget> DashboardWidgets { get; set; }
        public DbSet<MetricData> MetricData { get; set; }
        public DbSet<AggregatedMetric> AggregatedMetrics { get; set; }
        public DbSet<DashboardAlert> DashboardAlerts { get; set; }  // Add this
        public DbSet<AlertHistory> AlertHistories { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>()
                .HasOne(u => u.Tenant)
                .WithMany(t => t.Users)
                .HasForeignKey(u => u.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<User>()
            .HasIndex(u => new { u.TenantId, u.Email })
            .IsUnique();

            modelBuilder.Entity<Dashboard>()
            .HasOne(d => d.Tenant)
            .WithMany(t => t.Dashboards)
            .HasForeignKey(d => d.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Dashboard>()
            .HasOne(d => d.CreatedByUser)
            .WithMany()
            .HasForeignKey(d => d.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DashboardWidget>()
            .HasOne(w => w.Dashboard)
            .WithMany(d => d.Widgets)
            .HasForeignKey(w => w.DashboardId)
            .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<MetricData>()
                .HasOne(m => m.Tenant)
                .WithMany(t => t.MetricData)
                .HasForeignKey(m => m.TenantId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<MetricData>()
                .HasIndex(m => new {m.TenantId, m.MetricName, m.Timestamp})
                .HasDatabaseName("idx_metrics_tenant_name_timestamp");

            modelBuilder.Entity<Dashboard>()
                .HasIndex(d => new { d.TenantId, d.CreatedAt })
                .HasDatabaseName("idx_dashboard_tenant_created");

            modelBuilder.Entity<AggregatedMetric>()
                .HasIndex(a => new { a.TenantId, a.MetricName, a.Level, a.PeriodStart })
                .HasDatabaseName("idx_agg_metrics");

            modelBuilder.Entity<DashboardAlert>()
                .HasOne(a => a.Dashboard)
                .WithMany()
                .HasForeignKey(a => a.DashboardId)
                .OnDelete(DeleteBehavior.Cascade);

            // AlertHistory -> DashboardAlert relationship
            modelBuilder.Entity<AlertHistory>()
                .HasOne(a => a.Alert)
                .WithMany()
                .HasForeignKey(a => a.AlertId)
                .OnDelete(DeleteBehavior.Cascade);

            // Index for alert queries
            modelBuilder.Entity<DashboardAlert>()
                .HasIndex(a => new { a.TenantId, a.DashboardId })
                .HasDatabaseName("idx_alerts_tenant_dashboard");

            // Index for alert history
            modelBuilder.Entity<AlertHistory>()
                .HasIndex(a => new { a.TenantId, a.AlertId, a.Timestamp })
                .HasDatabaseName("idx_alert_history");
            
        }
    }
}