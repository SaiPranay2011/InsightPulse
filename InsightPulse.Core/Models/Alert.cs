using System;

namespace InsightPulse.Core.Models
{
    public class DashboardAlert
    {
        public Guid Id { get; set; }
        public Guid DashboardId { get; set; }
        public Guid TenantId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Metric { get; set; } = string.Empty;
        public AlertCondition Condition { get; set; }
        public decimal Threshold { get; set; }
        public string? SlackWebhookUrl { get; set; }
        public string? EmailAddress { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }

        public virtual Dashboard? Dashboard { get; set; }
    }

    public enum AlertCondition
    {
        GreaterThan = 0,        // Trigger if value > threshold
        LessThan = 1,           // Trigger if value < threshold
        PercentageChange = 2    // Trigger if value changes by % threshold
    }

    // Store alert history for debugging
    public class AlertHistory
    {
        public Guid Id { get; set; }
        public Guid AlertId { get; set; }
        public Guid TenantId { get; set; }
        public decimal MetricValue { get; set; }
        public string Message { get; set; } = string.Empty;
        public bool WasTriggered { get; set; }
        public DateTime Timestamp { get; set; }

        public virtual DashboardAlert? Alert { get; set; }
    }
}