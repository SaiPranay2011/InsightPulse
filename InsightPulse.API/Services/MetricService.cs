using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InsightPulse.API.Data;
using InsightPulse.Core.DTOs;
using InsightPulse.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace InsightPulse.API.Services
{
    public interface IMetricService
    {
        Task<MetricDataDto> IngestMetricAsync(Guid tenantId, IngestMetricRequest request);
        Task<List<MetricDataDto>> GetMetricsAsync(Guid tenantId, string metricName, DateTime startDate, DateTime endDate, string? dimension = null);
        Task<AggregatedMetricDto> GetAggregatedMetricAsync(Guid tenantId, string metricName, AggregationLevel level, DateTime periodStart);
        Task RefreshAggregationsAsync(Guid tenantId);  // Add this
    }

    public class MetricService : IMetricService
    {
        private readonly AppDbContext _context;
        private readonly IDistributedCache _cache;
        private readonly IAlertService _alertService;

        public MetricService(AppDbContext context, IDistributedCache cache, IAlertService alertService)
        {
            _context = context;
            _cache = cache;
            _alertService = alertService;
        }

        public async Task<MetricDataDto> IngestMetricAsync(Guid tenantId, IngestMetricRequest request)
        {
            var metricData = new MetricData
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                MetricName = request.MetricName,
                Value = request.Value,
                Dimension = request.Dimension,
                Timestamp = DateTime.UtcNow
            };

            _context.MetricData.Add(metricData);
            await _context.SaveChangesAsync();

            var cacheKey = $"metrics:{tenantId}:{request.MetricName}";
            await _cache.RemoveAsync(cacheKey);

            // Check alerts asynchronously (don't wait for response)
            _ = _alertService.CheckAlertsAsync(tenantId, request.MetricName, request.Value);

            return MapToDto(metricData);
        }

        public async Task<List<MetricDataDto>> GetMetricsAsync(
            Guid tenantId, 
            string metricName, 
            DateTime startDate, 
            DateTime endDate, 
            string? dimension = null)
        {
            var query = _context.MetricData
                .Where(m => m.TenantId == tenantId && m.MetricName == metricName)
                .Where(m => m.Timestamp >= startDate && m.Timestamp <= endDate);

            if (!string.IsNullOrEmpty(dimension))
                query = query.Where(m => m.Dimension == dimension);

            var metrics = await query
                .OrderBy(m => m.Timestamp)
                .ToListAsync();

            return metrics.Select(MapToDto).ToList();
        }

        public async Task<AggregatedMetricDto> GetAggregatedMetricAsync(
            Guid tenantId, 
            string metricName, 
            AggregationLevel level, 
            DateTime periodStart)
        {
            var agg = await _context.AggregatedMetrics
                .FirstOrDefaultAsync(a => 
                    a.TenantId == tenantId && 
                    a.MetricName == metricName && 
                    a.Level == level && 
                    a.PeriodStart == periodStart);

            if (agg == null)
                return new AggregatedMetricDto();

            return new AggregatedMetricDto
            {
                Sum = agg.Sum,
                Average = agg.Average,
                Min = agg.Min,
                Max = agg.Max,
                Count = agg.Count
            };
        }

        // NEW: Create aggregated metrics from raw data
        public async Task RefreshAggregationsAsync(Guid tenantId)
        {
            // Get yesterday's date
            var yesterday = DateTime.UtcNow.AddDays(-1).Date;

            // Group raw metrics by metric name
            var rawData = await _context.MetricData
                .Where(m => m.TenantId == tenantId && m.Timestamp.Date == yesterday)
                .GroupBy(m => new { m.MetricName, m.Dimension })
                .ToListAsync();

            // For each group, calculate sum, avg, min, max
            foreach (var group in rawData)
            {
                var values = group.Select(m => m.Value).ToList();

                // Delete old aggregation if exists
                var existing = await _context.AggregatedMetrics
                    .FirstOrDefaultAsync(a => 
                        a.TenantId == tenantId &&
                        a.MetricName == group.Key.MetricName &&
                        a.Dimension == group.Key.Dimension &&
                        a.Level == AggregationLevel.Daily &&
                        a.PeriodStart == yesterday);

                if (existing != null)
                    _context.AggregatedMetrics.Remove(existing);

                // Create new aggregation
                var agg = new AggregatedMetric
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    MetricName = group.Key.MetricName,
                    Level = AggregationLevel.Daily,
                    PeriodStart = yesterday,
                    PeriodEnd = yesterday.AddDays(1),
                    Sum = values.Sum(),
                    Average = values.Average(),
                    Min = values.Min(),
                    Max = values.Max(),
                    Count = values.Count,
                    Dimension = group.Key.Dimension
                };

                _context.AggregatedMetrics.Add(agg);
            }

            await _context.SaveChangesAsync();
        }

        private MetricDataDto MapToDto(MetricData metric) => new()
        {
            Id = metric.Id,
            MetricName = metric.MetricName,
            Value = metric.Value,
            Dimension = metric.Dimension,
            Timestamp = metric.Timestamp
        };
    }
}