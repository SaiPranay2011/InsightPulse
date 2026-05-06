using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using InsightPulse.API.Services;
using InsightPulse.Core.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InsightPulse.API.Controllers
{
    [ApiController]
    [Route("api/dashboards/{dashboardId}/alerts")]  // Change this route
    [Authorize]
    public class AlertsController : ControllerBase
    {
        private readonly IAlertService _alertService;

        public AlertsController(IAlertService alertService)
        {
            _alertService = alertService;
        }

        private Guid GetTenantIdFromToken()
        {
            var tenantIdClaim = User.FindFirst("tenant_id");
            if (tenantIdClaim == null)
                throw new UnauthorizedAccessException("Tenant ID not found in token");
            return Guid.Parse(tenantIdClaim.Value);
        }

        // POST /api/dashboards/{dashboardId}/alerts
        [HttpPost]
        [ProducesResponseType(typeof(AlertDto), StatusCodes.Status201Created)]
        public async Task<IActionResult> CreateAlert(Guid dashboardId, [FromBody] CreateAlertRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var tenantId = GetTenantIdFromToken();
                var alert = await _alertService.CreateAlertAsync(tenantId, dashboardId, request);
                return CreatedAtAction(nameof(GetAlert), new { dashboardId = dashboardId, alertId = alert.Id }, alert);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
            }
        }

        // GET /api/dashboards/{dashboardId}/alerts
        [HttpGet]
        [ProducesResponseType(typeof(List<AlertDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAlerts(Guid dashboardId)
        {
            try
            {
                var tenantId = GetTenantIdFromToken();
                var alerts = await _alertService.GetAlertsAsync(tenantId, dashboardId);
                return Ok(alerts);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
            }
        }

        // GET /api/dashboards/{dashboardId}/alerts/{alertId}
        [HttpGet("{alertId}")]
        [ProducesResponseType(typeof(AlertDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAlert(Guid dashboardId, Guid alertId)
        {
            try
            {
                var tenantId = GetTenantIdFromToken();
                var alert = await _alertService.GetAlertAsync(tenantId, alertId);
                return Ok(alert);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
            }
        }

        // DELETE /api/dashboards/{dashboardId}/alerts/{alertId}
        [HttpDelete("{alertId}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> DeleteAlert(Guid dashboardId, Guid alertId)
        {
            try
            {
                var tenantId = GetTenantIdFromToken();
                await _alertService.DeleteAlertAsync(tenantId, alertId);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
            }
        }

        // GET /api/dashboards/{dashboardId}/alerts/{alertId}/history
        [HttpGet("{alertId}/history")]
        [ProducesResponseType(typeof(List<AlertHistoryDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAlertHistory(Guid dashboardId, Guid alertId)
        {
            try
            {
                var tenantId = GetTenantIdFromToken();
                var history = await _alertService.GetAlertHistoryAsync(tenantId, alertId);
                return Ok(history);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
            }
        }
    }
}