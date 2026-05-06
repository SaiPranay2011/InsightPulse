using System;
using System.Collections.Generic;
using InsightPulse.Core.Models;

namespace InsightPulse.Core.DTOs
{
    // Request to create dashboard
    public class CreateDashboardRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int RefreshIntervalSeconds { get; set; } = 300;
        public bool IsPublic { get; set; }
    }

    // Request to add widget to dashboard
    public class CreateWidgetRequest
    {
        public string Title { get; set; } = string.Empty;
        public WidgetType Type { get; set; }
        public string Metric { get; set; } = string.Empty;
        public string? Dimension { get; set; }
        public int PositionX { get; set; }
        public int PositionY { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }

    // Request to update widget
    public class UpdateWidgetRequest
    {
        public string Title { get; set; } = string.Empty;
        public WidgetType Type { get; set; }
        public string Metric { get; set; } = string.Empty;
        public string? Dimension { get; set; }
        public int PositionX { get; set; }
        public int PositionY { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }

    // Response for dashboard
    public class DashboardDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int RefreshIntervalSeconds { get; set; }
        public bool IsPublic { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public List<DashboardWidgetDto> Widgets { get; set; } = new();
    }

    // Response for widget
    public class DashboardWidgetDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public WidgetType Type { get; set; }
        public string Metric { get; set; } = string.Empty;
        public string? Dimension { get; set; }
        public int PositionX { get; set; }
        public int PositionY { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }
}