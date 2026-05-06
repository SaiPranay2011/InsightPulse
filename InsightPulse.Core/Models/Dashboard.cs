using System;
using System.Collections.Generic;

namespace InsightPulse.Core.Models
{
    public class Dashboard
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public Guid CreatedByUserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int RefreshIntervalSeconds { get; set; } = 300;
        public bool IsPublic { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public virtual Tenant? Tenant { get; set; }
        public virtual User? CreatedByUser { get; set; }
        public virtual ICollection<DashboardWidget> Widgets { get; set; } = [];
    }

    public class DashboardWidget
    {
        public Guid Id { get; set; }
        public Guid DashboardId { get; set; }
        public string Title { get; set; } = string.Empty;
        public WidgetType Type { get; set; }
        public string Metric { get; set; } = string.Empty;
        public string? Dimension { get; set; }
        public int PositionX { get; set; }
        public int PositionY { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        public virtual Dashboard? Dashboard { get; set; }
    }

    public enum WidgetType
    {
        Metric = 0,
        LineChart = 1,
        BarChart = 2,
        PieChart = 3,
        Table = 4,
        Gauge = 5
    }
}

