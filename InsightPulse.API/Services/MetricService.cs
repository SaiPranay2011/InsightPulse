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
        Task<AggregatedMetricDto> GetAggregatedMetricAsync(Guid tenantId, string metricName, AggregationLevel level, DateTime periodStart, string? dimension = null);
        Task RefreshAggregationsAsync(Guid tenantId);
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
                Id        = Guid.NewGuid(),
                TenantId  = tenantId,
                MetricName = request.MetricName,
                Value     = request.Value,
                Dimension = request.Dimension,
                Timestamp = DateTime.UtcNow
            };

            _context.MetricData.Add(metricData);
            await _context.SaveChangesAsync();

            // Invalidate any cached aggregation for this metric so the next read
            // reflects the newly ingested value immediately.
            var cacheKey = $"agg:{tenantId}:{request.MetricName}:{request.Dimension}";
            await _cache.RemoveAsync(cacheKey);

            // Check alerts (fire-and-forget)
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
                .Where(m => m.TenantId == tenantId
                         && m.MetricName == metricName
                         && m.Timestamp >= startDate
                         && m.Timestamp <= endDate);

            if (!string.IsNullOrEmpty(dimension))
                query = query.Where(m => m.Dimension == dimension);

            var metrics = await query
                .OrderBy(m => m.Timestamp)
                .ToListAsync();

            return metrics.Select(MapToDto).ToList();
        }

        /// <summary>
        /// Returns aggregated stats for a metric over a period.
        ///
        /// Strategy:
        ///   1. Check the pre-aggregated table first (populated by RefreshAggregationsAsync).
        ///   2. If no pre-aggregated row exists, compute the aggregation live from raw
        ///      MetricData rows. This ensures widgets always show real data even before
        ///      the background aggregation job has run — which was the reason all widgets
        ///      showed zeros on a fresh deployment.
        /// </summary>
        public async Task<AggregatedMetricDto> GetAggregatedMetricAsync(
            Guid tenantId,
            string metricName,
            AggregationLevel level,
            DateTime periodStart,
            string? dimension = null)
        {
            // 1. Try cache
            var cacheKey = $"agg:{tenantId}:{metricName}:{dimension}:{level}:{periodStart:yyyyMMdd}";
            var cached = await _cache.GetStringAsync(cacheKey);
            if (cached != null)
            {
                var cachedDto = JsonSerializer.Deserialize<AggregatedMetricDto>(cached);
                if (cachedDto != null) return cachedDto;
            }

            // 2. Try pre-aggregated table
            var aggQuery = _context.AggregatedMetrics
                .Where(a => a.TenantId  == tenantId
                         && a.MetricName == metricName
                         && a.Level      == level
                         && a.PeriodStart == periodStart);

            if (!string.IsNullOrEmpty(dimension))
                aggQuery = aggQuery.Where(a => a.Dimension == dimension);

            var agg = await aggQuery.FirstOrDefaultAsync();
            if (agg != null)
            {
                var preAggDto = new AggregatedMetricDto
                {
                    Sum     = agg.Sum,
                    Average = agg.Average,
                    Min     = agg.Min,
                    Max     = agg.Max,
                    Count   = agg.Count
                };
                await CacheDto(cacheKey, preAggDto, TimeSpan.FromMinutes(5));
                return preAggDto;
            }

            // 3. Compute live from raw data
            //    For Daily level: aggregate the full calendar day of periodStart.
            //    This is the critical fix — without this, brand-new deployments with
            //    no pre-aggregated rows always return zeros.
            var periodEnd = level switch
            {
                AggregationLevel.Daily   => periodStart.AddDays(1),
                AggregationLevel.Weekly  => periodStart.AddDays(7),
                AggregationLevel.Monthly => periodStart.AddMonths(1),
                _                        => periodStart.AddDays(1)
            };

            var rawQuery = _context.MetricData
                .Where(m => m.TenantId   == tenantId
                         && m.MetricName == metricName
                         && m.Timestamp  >= periodStart
                         && m.Timestamp  <  periodEnd);

            if (!string.IsNullOrEmpty(dimension))
                rawQuery = rawQuery.Where(m => m.Dimension == dimension);

            var values = await rawQuery.Select(m => m.Value).ToListAsync();

            if (values.Count == 0)
                return new AggregatedMetricDto(); // all zeros — no data ingested yet

            var liveDto = new AggregatedMetricDto
            {
                Sum     = values.Sum(),
                Average = values.Average(),
                Min     = values.Min(),
                Max     = values.Max(),
                Count   = values.Count
            };

            // Cache live aggregations for a shorter window so fresh ingests show up quickly
            await CacheDto(cacheKey, liveDto, TimeSpan.FromSeconds(30));
            return liveDto;
        }

        /// <summary>
        /// Pre-computes daily aggregations for yesterday and stores them in the
        /// AggregatedMetrics table. Call this from a scheduled job / cron so that
        /// GetAggregatedMetricAsync can serve cached rows instead of raw scans.
        /// </summary>
        public async Task RefreshAggregationsAsync(Guid tenantId)
        {
            var yesterday = DateTime.UtcNow.AddDays(-1).Date;

            var rawData = await _context.MetricData
                .Where(m => m.TenantId == tenantId && m.Timestamp.Date == yesterday)
                .GroupBy(m => new { m.MetricName, m.Dimension })
                .ToListAsync();

            foreach (var group in rawData)
            {
                var values = group.Select(m => m.Value).ToList();

                var existing = await _context.AggregatedMetrics
                    .FirstOrDefaultAsync(a =>
                        a.TenantId   == tenantId
                     && a.MetricName == group.Key.MetricName
                     && a.Dimension  == group.Key.Dimension
                     && a.Level      == AggregationLevel.Daily
                     && a.PeriodStart == yesterday);

                if (existing != null)
                    _context.AggregatedMetrics.Remove(existing);

                _context.AggregatedMetrics.Add(new AggregatedMetric
                {
                    Id          = Guid.NewGuid(),
                    TenantId   = tenantId,
                    MetricName = group.Key.MetricName,
                    Level       = AggregationLevel.Daily,
                    PeriodStart = yesterday,
                    PeriodEnd   = yesterday.AddDays(1),
                    Sum         = values.Sum(),
                    Average     = values.Average(),
                    Min         = values.Min(),
                    Max         = values.Max(),
                    Count       = values.Count,
                    Dimension   = group.Key.Dimension
                });
            }

            await _context.SaveChangesAsync();
        }

        private async Task CacheDto(string key, AggregatedMetricDto dto, TimeSpan ttl)
        {
            try
            {
                var json = JsonSerializer.Serialize(dto);
                await _cache.SetStringAsync(key, json, new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = ttl
                });
            }
            catch
            {
                // Cache failures are non-fatal — the app continues without caching
            }
        }

        private static MetricDataDto MapToDto(MetricData metric) => new()
        {
            Id         = metric.Id,
            MetricName = metric.MetricName,
            Value      = metric.Value,
            Dimension  = metric.Dimension,
            Timestamp  = metric.Timestamp
        };
    }
}