using System;
using InsightPulse.Core.Models;

namespace InsightPulse.Core.DTOs
{
    // Request to create alert
    public class CreateAlertRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Metric { get; set; } = string.Empty;
        public AlertCondition Condition { get; set; }
        public decimal Threshold { get; set; }
        public string? SlackWebhookUrl { get; set; }
        public string? EmailAddress { get; set; }
    }

    // Response for alert
    public class AlertDto
    {
        public Guid Id { get; set; }
        public Guid DashboardId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Metric { get; set; } = string.Empty;
        public AlertCondition Condition { get; set; }
        public decimal Threshold { get; set; }
        public string? SlackWebhookUrl { get; set; }
        public string? EmailAddress { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // Response for alert history
    public class AlertHistoryDto
    {
        public Guid Id { get; set; }
        public Guid AlertId { get; set; }
        public decimal MetricValue { get; set; }
        public string Message { get; set; } = string.Empty;
        public bool WasTriggered { get; set; }
        public DateTime Timestamp { get; set; }
    }
}