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
    [Route("api/[controller]")]
    [Authorize]
    public class DashboardsController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;

        public DashboardsController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        // Extract user ID and tenant ID from JWT token
        private Guid GetUserIdFromToken()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                throw new UnauthorizedAccessException("User ID not found in token");
            return Guid.Parse(userIdClaim.Value);
        }

        private Guid GetTenantIdFromToken()
        {
            var tenantIdClaim = User.FindFirst("tenant_id");
            if (tenantIdClaim == null)
                throw new UnauthorizedAccessException("Tenant ID not found in token");
            return Guid.Parse(tenantIdClaim.Value);
        }

        // POST /api/dashboards
        // Create a new dashboard
        [HttpPost]
        [ProducesResponseType(typeof(DashboardDto), StatusCodes.Status201Created)]
        public async Task<IActionResult> CreateDashboard([FromBody] CreateDashboardRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var userId = GetUserIdFromToken();
                var tenantId = GetTenantIdFromToken();
                var dashboard = await _dashboardService.CreateDashboardAsync(tenantId, userId, request);
                return CreatedAtAction(nameof(GetDashboard), new { id = dashboard.Id }, dashboard);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
            }
        }

        // GET /api/dashboards
        // Get all dashboards for user's company
        [HttpGet]
        [ProducesResponseType(typeof(List<DashboardDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetDashboards()
        {
            try
            {
                var tenantId = GetTenantIdFromToken();
                var dashboards = await _dashboardService.GetDashboardsAsync(tenantId);
                return Ok(dashboards);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
            }
        }

        // GET /api/dashboards/{id}
        // Get specific dashboard with all widgets
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(DashboardDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetDashboard(Guid id)
        {
            try
            {
                var tenantId = GetTenantIdFromToken();
                var dashboard = await _dashboardService.GetDashboardAsync(tenantId, id);
                return Ok(dashboard);
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

        // POST /api/dashboards/{dashboardId}/widgets
        // Add widget to dashboard
        [HttpPost("{dashboardId}/widgets")]
        [ProducesResponseType(typeof(DashboardWidgetDto), StatusCodes.Status201Created)]
        public async Task<IActionResult> AddWidget(Guid dashboardId, [FromBody] CreateWidgetRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var tenantId = GetTenantIdFromToken();
                var widget = await _dashboardService.AddWidgetAsync(tenantId, dashboardId, request);
                return CreatedAtAction(nameof(GetDashboard), new { id = dashboardId }, widget);
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

        // PUT /api/dashboards/{dashboardId}/widgets/{widgetId}
        // Update widget
        [HttpPut("{dashboardId}/widgets/{widgetId}")]
        [ProducesResponseType(typeof(DashboardWidgetDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateWidget(Guid dashboardId, Guid widgetId, [FromBody] UpdateWidgetRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var tenantId = GetTenantIdFromToken();
                var widget = await _dashboardService.UpdateWidgetAsync(tenantId, dashboardId, widgetId, request);
                return Ok(widget);
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

        // DELETE /api/dashboards/{dashboardId}/widgets/{widgetId}
        // Remove widget from dashboard
        [HttpDelete("{dashboardId}/widgets/{widgetId}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> DeleteWidget(Guid dashboardId, Guid widgetId)
        {
            try
            {
                var tenantId = GetTenantIdFromToken();
                await _dashboardService.DeleteWidgetAsync(tenantId, dashboardId, widgetId);
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

        // DELETE /api/dashboards/{id}
        // Delete entire dashboard
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> DeleteDashboard(Guid id)
        {
            try
            {
                var tenantId = GetTenantIdFromToken();
                await _dashboardService.DeleteDashboardAsync(tenantId, id);
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
    }
}