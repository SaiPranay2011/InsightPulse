using System;
using System.Collections.Generic;

namespace InsightPulse.Core.Models
{
    public class Tenant
    {
        public Guid Id { get; set; }
        public string Name {get; set; } = string.Empty;
        public string Slug {get; set; } = string.Empty;
        public TenantPlan Plan { get; set;}
        public bool IsActive { get; set;}
        public DateTime CreatedAt { get; set;}

        public virtual ICollection<User> Users { get; set;} = [];
        public virtual ICollection<Dashboard> Dashboards { get; set;} = [];
        public virtual ICollection<MetricData> MetricData { get; set;} = [];
    }

    public enum TenantPlan
    {
        Free = 0,
        Pro = 1,
        Enterprise = 2
    }
}