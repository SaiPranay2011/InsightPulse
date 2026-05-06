using System;

namespace InsightPulse.Core.Models
{
    public class MetricData
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public string MetricName { get; set; } = string.Empty;
        public decimal Value { get; set; }
        public string? Dimension { get; set; }
        public DateTime Timestamp { get; set; }

        public virtual Tenant? Tenant { get; set; }
    }

    public class AggregatedMetric
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public string MetricName { get; set; } = string.Empty;
        public AggregationLevel Level { get; set; }
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public decimal Sum { get; set; }
        public decimal Average { get; set; }
        public decimal Min { get; set; }
        public decimal Max { get; set; }
        public long Count { get; set; }
        public string? Dimension { get; set; }
    }

    public enum AggregationLevel
    {
        Hourly = 0,
        Daily = 1,
        Weekly = 2,
        Monthly = 3
    }
}