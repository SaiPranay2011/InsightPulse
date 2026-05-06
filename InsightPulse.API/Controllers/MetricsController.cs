using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using InsightPulse.API.Services;
using InsightPulse.Core.DTOs;
using InsightPulse.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InsightPulse.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class MetricsController : ControllerBase
    {
        private readonly IMetricService _metricService;

        public MetricsController(IMetricService metricService)
        {
            _metricService = metricService;
        }

        private Guid GetTenantIdFromToken()
        {
            var tenantIdClaim = User.FindFirst("tenant_id");
            if (tenantIdClaim == null)
                throw new UnauthorizedAccessException("Tenant ID not found in token");

            return Guid.Parse(tenantIdClaim.Value);
        }

        [HttpPost("ingest")]
        [ProducesResponseType(typeof(MetricDataDto), StatusCodes.Status201Created)]
        public async Task<IActionResult> IngestMetric([FromBody] IngestMetricRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var tenantId = GetTenantIdFromToken();
                var result = await _metricService.IngestMetricAsync(tenantId, request);
                return CreatedAtAction(nameof(GetMetrics), new { metricName = request.MetricName }, result);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
            }
        }

        [HttpGet("{metricName}")]
        [ProducesResponseType(typeof(List<MetricDataDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMetrics(
            string metricName,
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate,
            [FromQuery] string? dimension = null)
        {
            startDate = startDate.Kind == DateTimeKind.Unspecified 
                ? DateTime.SpecifyKind(startDate, DateTimeKind.Utc) 
                : startDate.ToUniversalTime();
            
            endDate = endDate.Kind == DateTimeKind.Unspecified 
                ? DateTime.SpecifyKind(endDate, DateTimeKind.Utc) 
                : endDate.ToUniversalTime();

            if (startDate > endDate)
                return BadRequest(new { error = "startDate must be before endDate" });

            try
            {
                var tenantId = GetTenantIdFromToken();
                var metrics = await _metricService.GetMetricsAsync(tenantId, metricName, startDate, endDate, dimension);
                return Ok(metrics);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
            }
        }

        [HttpGet("aggregated/{metricName}")]
        [ProducesResponseType(typeof(AggregatedMetricDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAggregatedMetric(
            string metricName,
            [FromQuery] int level = 1,
            [FromQuery] DateTime? periodStart = null)
        {
            if (periodStart == null)
                periodStart = DateTime.UtcNow.Date;
            else if (periodStart.Value.Kind == DateTimeKind.Unspecified)
                periodStart = DateTime.SpecifyKind(periodStart.Value, DateTimeKind.Utc);
            else
                periodStart = periodStart.Value.ToUniversalTime();

            try
            {
                var tenantId = GetTenantIdFromToken();
                var metric = await _metricService.GetAggregatedMetricAsync(
                    tenantId,
                    metricName,
                    (AggregationLevel)level,
                    (DateTime)periodStart);

                return Ok(metric);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
            }
        }

        // NEW: Refresh aggregations endpoint
        [HttpPost("refresh-aggregations")]
        public async Task<IActionResult> RefreshAggregations()
        {
            try
            {
                var tenantId = GetTenantIdFromToken();
                await _metricService.RefreshAggregationsAsync(tenantId);
                return Ok(new { message = "Aggregations refreshed successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
            }
        }
    }
}