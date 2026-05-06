using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InsightPulse.API.Data;
using InsightPulse.Core.DTOs;
using InsightPulse.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace InsightPulse.API.Services
{
    public interface IDashboardService
    {
        Task<DashboardDto> CreateDashboardAsync(Guid tenantId, Guid userId, CreateDashboardRequest request);
        Task<List<DashboardDto>> GetDashboardsAsync(Guid tenantId);
        Task<DashboardDto> GetDashboardAsync(Guid tenantId, Guid dashboardId);
        Task<DashboardWidgetDto> AddWidgetAsync(Guid tenantId, Guid dashboardId, CreateWidgetRequest request);
        Task<DashboardWidgetDto> UpdateWidgetAsync(Guid tenantId, Guid dashboardId, Guid widgetId, UpdateWidgetRequest request);
        Task DeleteWidgetAsync(Guid tenantId, Guid dashboardId, Guid widgetId);
        Task DeleteDashboardAsync(Guid tenantId, Guid dashboardId);
    }

    public class DashboardService : IDashboardService
    {
        private readonly AppDbContext _context;

        public DashboardService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<DashboardDto> CreateDashboardAsync(Guid tenantId, Guid userId, CreateDashboardRequest request)
        {
            // Verify user belongs to tenant
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId && u.TenantId == tenantId);
            
            if (user == null)
                throw new UnauthorizedAccessException("User not found in tenant");

            var dashboard = new Dashboard
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                CreatedByUserId = userId,
                Name = request.Name,
                Description = request.Description,
                RefreshIntervalSeconds = request.RefreshIntervalSeconds,
                IsPublic = request.IsPublic,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Dashboards.Add(dashboard);
            await _context.SaveChangesAsync();

            return MapToDto(dashboard);
        }

        public async Task<List<DashboardDto>> GetDashboardsAsync(Guid tenantId)
        {
            var dashboards = await _context.Dashboards
                .Where(d => d.TenantId == tenantId)
                .Include(d => d.Widgets)
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();

            return dashboards.Select(MapToDto).ToList();
        }

        public async Task<DashboardDto> GetDashboardAsync(Guid tenantId, Guid dashboardId)
        {
            var dashboard = await _context.Dashboards
                .Where(d => d.TenantId == tenantId && d.Id == dashboardId)
                .Include(d => d.Widgets)
                .FirstOrDefaultAsync();

            if (dashboard == null)
                throw new KeyNotFoundException("Dashboard not found");

            return MapToDto(dashboard);
        }

        public async Task<DashboardWidgetDto> AddWidgetAsync(Guid tenantId, Guid dashboardId, CreateWidgetRequest request)
        {
            // Verify dashboard belongs to tenant
            var dashboard = await _context.Dashboards
                .FirstOrDefaultAsync(d => d.TenantId == tenantId && d.Id == dashboardId);
            
            if (dashboard == null)
                throw new KeyNotFoundException("Dashboard not found");

            var widget = new DashboardWidget
            {
                Id = Guid.NewGuid(),
                DashboardId = dashboardId,
                Title = request.Title,
                Type = request.Type,
                Metric = request.Metric,
                Dimension = request.Dimension,
                PositionX = request.PositionX,
                PositionY = request.PositionY,
                Width = request.Width,
                Height = request.Height
            };

            _context.DashboardWidgets.Add(widget);
            dashboard.UpdatedAt = DateTime.UtcNow;
            _context.Dashboards.Update(dashboard);
            await _context.SaveChangesAsync();

            return MapWidgetToDto(widget);
        }

        public async Task<DashboardWidgetDto> UpdateWidgetAsync(Guid tenantId, Guid dashboardId, Guid widgetId, UpdateWidgetRequest request)
        {
            // Verify dashboard belongs to tenant
            var dashboard = await _context.Dashboards
                .FirstOrDefaultAsync(d => d.TenantId == tenantId && d.Id == dashboardId);
            
            if (dashboard == null)
                throw new KeyNotFoundException("Dashboard not found");

            // Get widget
            var widget = await _context.DashboardWidgets
                .FirstOrDefaultAsync(w => w.Id == widgetId && w.DashboardId == dashboardId);
            
            if (widget == null)
                throw new KeyNotFoundException("Widget not found");

            // Update widget
            widget.Title = request.Title;
            widget.Type = request.Type;
            widget.Metric = request.Metric;
            widget.Dimension = request.Dimension;
            widget.PositionX = request.PositionX;
            widget.PositionY = request.PositionY;
            widget.Width = request.Width;
            widget.Height = request.Height;

            _context.DashboardWidgets.Update(widget);
            dashboard.UpdatedAt = DateTime.UtcNow;
            _context.Dashboards.Update(dashboard);
            await _context.SaveChangesAsync();

            return MapWidgetToDto(widget);
        }

        public async Task DeleteWidgetAsync(Guid tenantId, Guid dashboardId, Guid widgetId)
        {
            // Verify dashboard belongs to tenant
            var dashboard = await _context.Dashboards
                .FirstOrDefaultAsync(d => d.TenantId == tenantId && d.Id == dashboardId);
            
            if (dashboard == null)
                throw new KeyNotFoundException("Dashboard not found");

            // Get and delete widget
            var widget = await _context.DashboardWidgets
                .FirstOrDefaultAsync(w => w.Id == widgetId && w.DashboardId == dashboardId);
            
            if (widget == null)
                throw new KeyNotFoundException("Widget not found");

            _context.DashboardWidgets.Remove(widget);
            dashboard.UpdatedAt = DateTime.UtcNow;
            _context.Dashboards.Update(dashboard);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteDashboardAsync(Guid tenantId, Guid dashboardId)
        {
            var dashboard = await _context.Dashboards
                .FirstOrDefaultAsync(d => d.TenantId == tenantId && d.Id == dashboardId);
            
            if (dashboard == null)
                throw new KeyNotFoundException("Dashboard not found");

            _context.Dashboards.Remove(dashboard);
            await _context.SaveChangesAsync();
        }

        private DashboardDto MapToDto(Dashboard dashboard) => new()
        {
            Id = dashboard.Id,
            Name = dashboard.Name,
            Description = dashboard.Description,
            RefreshIntervalSeconds = dashboard.RefreshIntervalSeconds,
            IsPublic = dashboard.IsPublic,
            CreatedAt = dashboard.CreatedAt,
            UpdatedAt = dashboard.UpdatedAt,
            Widgets = dashboard.Widgets.Select(MapWidgetToDto).ToList()
        };

        private DashboardWidgetDto MapWidgetToDto(DashboardWidget widget) => new()
        {
            Id = widget.Id,
            Title = widget.Title,
            Type = widget.Type,
            Metric = widget.Metric,
            Dimension = widget.Dimension,
            PositionX = widget.PositionX,
            PositionY = widget.PositionY,
            Width = widget.Width,
            Height = widget.Height
        };
    }
}