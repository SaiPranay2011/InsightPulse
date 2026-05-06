using System;
using InsightPulse.Core.Models;

namespace InsightPulse.Core.DTOs
{
    public class RegisterRequest
    {
        public string Email {get; set;} = string.Empty;
        public string Password {get; set;} = string.Empty;
        public string FirstName {get; set;} = string.Empty;
        public string LastName{get; set;} = string.Empty;
        
    }

    public class LoginRequest
    {
        public string Email {get; set;} = string.Empty;
        public string Password {get; set;} = string.Empty;
    }

    public class AuthResponse
    {
        public string Token { get; set;} = string.Empty;
        public UserDto User {get; set;}
    }

    public class UserDto
    {
        public Guid Id {get; set;}
        public Guid TenantId { get; set;}
        public string Email { get; set;} = string.Empty;
        public string FirstName { get; set;} = string.Empty;
        public string LastName { get; set;} = string.Empty;
        public UserRole Role { get; set;}
    }
}