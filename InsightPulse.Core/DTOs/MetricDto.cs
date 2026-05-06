using System;
using InsightPulse.Core.Models;

namespace InsightPulse.Core.DTOs
{
    // Request from frontend/external service to ingest a metric
    public class IngestMetricRequest
    {
        public string MetricName { get; set; } = string.Empty;
        public decimal Value { get; set; }
        public string? Dimension { get; set; }
    }

    // Response after ingesting metric
    public class MetricDataDto
    {
        public Guid Id { get; set; }
        public string MetricName { get; set; } = string.Empty;
        public decimal Value { get; set; }
        public string? Dimension { get; set; }
        public DateTime Timestamp { get; set; }
    }

    // Response for aggregated metrics
    public class AggregatedMetricDto
    {
        public decimal Sum { get; set; }
        public decimal Average { get; set; }
        public decimal Min { get; set; }
        public decimal Max { get; set; }
        public long Count { get; set; }
    }
}