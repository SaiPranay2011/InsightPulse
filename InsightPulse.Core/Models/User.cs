using System;

namespace InsightPulse.Core.Models
{
    public class User
    {
        public Guid Id { get; set;}
        public Guid TenantId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public UserRole Role { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }

        public virtual Tenant? Tenant { get; set; }
    }

    public enum UserRole
    {
        Viewer = 0,
        Analyst = 1,
        Manager = 2,
        Admin = 3
    }
}