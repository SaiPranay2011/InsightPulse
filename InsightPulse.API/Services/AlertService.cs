using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using InsightPulse.API.Data;
using InsightPulse.Core.DTOs;
using InsightPulse.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace InsightPulse.API.Services
{
    public interface IAlertService
    {
        Task<AlertDto> CreateAlertAsync(Guid tenantId, Guid dashboardId, CreateAlertRequest request);
        Task<List<AlertDto>> GetAlertsAsync(Guid tenantId, Guid dashboardId);
        Task<AlertDto> GetAlertAsync(Guid tenantId, Guid alertId);
        Task DeleteAlertAsync(Guid tenantId, Guid alertId);
        Task CheckAlertsAsync(Guid tenantId, string metricName, decimal value);
        Task<List<AlertHistoryDto>> GetAlertHistoryAsync(Guid tenantId, Guid alertId);
    }

    public class AlertService : IAlertService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<AlertService> _logger;
        private readonly HttpClient _httpClient;

        public AlertService(AppDbContext context, ILogger<AlertService> logger, HttpClient httpClient)
        {
            _context = context;
            _logger = logger;
            _httpClient = httpClient;
        }

        public async Task<AlertDto> CreateAlertAsync(Guid tenantId, Guid dashboardId, CreateAlertRequest request)
        {
            // Verify dashboard belongs to tenant
            var dashboard = await _context.Dashboards
                .FirstOrDefaultAsync(d => d.TenantId == tenantId && d.Id == dashboardId);
            
            if (dashboard == null)
                throw new KeyNotFoundException("Dashboard not found");

            var alert = new DashboardAlert
            {
                Id = Guid.NewGuid(),
                DashboardId = dashboardId,
                TenantId = tenantId,
                Name = request.Name,
                Metric = request.Metric,
                Condition = request.Condition,
                Threshold = request.Threshold,
                SlackWebhookUrl = request.SlackWebhookUrl,
                EmailAddress = request.EmailAddress,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.DashboardAlerts.Add(alert);
            await _context.SaveChangesAsync();

            return MapToDto(alert);
        }

        public async Task<List<AlertDto>> GetAlertsAsync(Guid tenantId, Guid dashboardId)
        {
            var alerts = await _context.DashboardAlerts
                .Where(a => a.TenantId == tenantId && a.DashboardId == dashboardId)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

            return alerts.Select(MapToDto).ToList();
        }

        public async Task<AlertDto> GetAlertAsync(Guid tenantId, Guid alertId)
        {
            var alert = await _context.DashboardAlerts
                .FirstOrDefaultAsync(a => a.TenantId == tenantId && a.Id == alertId);
            
            if (alert == null)
                throw new KeyNotFoundException("Alert not found");

            return MapToDto(alert);
        }

        public async Task DeleteAlertAsync(Guid tenantId, Guid alertId)
        {
            var alert = await _context.DashboardAlerts
                .FirstOrDefaultAsync(a => a.TenantId == tenantId && a.Id == alertId);
            
            if (alert == null)
                throw new KeyNotFoundException("Alert not found");

            _context.DashboardAlerts.Remove(alert);
            await _context.SaveChangesAsync();
        }

        // Check if metric triggers any alerts
        public async Task CheckAlertsAsync(Guid tenantId, string metricName, decimal value)
        {
            // Get all active alerts for this metric
            var alerts = await _context.DashboardAlerts
                .Where(a => a.TenantId == tenantId && a.Metric == metricName && a.IsActive)
                .ToListAsync();

            foreach (var alert in alerts)
            {
                bool triggered = false;
                string message = "";

                // Check condition
                if (alert.Condition == AlertCondition.GreaterThan)
                {
                    if (value > alert.Threshold)
                    {
                        triggered = true;
                        message = $"Alert: {alert.Name} triggered! {metricName} = {value} (threshold: {alert.Threshold})";
                    }
                }
                else if (alert.Condition == AlertCondition.LessThan)
                {
                    if (value < alert.Threshold)
                    {
                        triggered = true;
                        message = $"Alert: {alert.Name} triggered! {metricName} = {value} (threshold: {alert.Threshold})";
                    }
                }

                // Save to history
                var history = new AlertHistory
                {
                    Id = Guid.NewGuid(),
                    AlertId = alert.Id,
                    TenantId = tenantId,
                    MetricValue = value,
                    Message = message,
                    WasTriggered = triggered,
                    Timestamp = DateTime.UtcNow
                };

                _context.AlertHistories.Add(history);

                // Send notification if triggered
                if (triggered)
                {
                    await SendNotificationAsync(alert, message);
                }
            }

            await _context.SaveChangesAsync();
        }

        // Send email or Slack notification
        private async Task SendNotificationAsync(DashboardAlert alert, string message)
        {
            try
            {
                // Send Slack notification
                if (!string.IsNullOrEmpty(alert.SlackWebhookUrl))
                {
                    await SendSlackNotificationAsync(alert.SlackWebhookUrl, message);
                }

                // Send email notification
                if (!string.IsNullOrEmpty(alert.EmailAddress))
                {
                    await SendEmailNotificationAsync(alert.EmailAddress, message);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending notification for alert {AlertId}", alert.Id);
            }
        }

        private async Task SendSlackNotificationAsync(string webhookUrl, string message)
        {
            var payload = new
            {
                text = message,
                username = "InsightPulse Alert",
                icon_emoji = ":bell:"
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(webhookUrl, content);
            response.EnsureSuccessStatusCode();

            _logger.LogInformation("Slack notification sent successfully");
        }

        private async Task SendEmailNotificationAsync(string email, string message)
        {
            // For now, just log it (implement SendGrid or similar later)
            _logger.LogInformation("Email notification would be sent to {Email}: {Message}", email, message);
            
            // TODO: Implement email service with SendGrid or similar
            await Task.CompletedTask;
        }

        public async Task<List<AlertHistoryDto>> GetAlertHistoryAsync(Guid tenantId, Guid alertId)
        {
            // Verify alert belongs to tenant
            var alert = await _context.DashboardAlerts
                .FirstOrDefaultAsync(a => a.TenantId == tenantId && a.Id == alertId);
            
            if (alert == null)
                throw new KeyNotFoundException("Alert not found");

            var history = await _context.AlertHistories
                .Where(h => h.AlertId == alertId)
                .OrderByDescending(h => h.Timestamp)
                .Take(100)  // Last 100 history entries
                .ToListAsync();

            return history.Select(h => new AlertHistoryDto
            {
                Id = h.Id,
                AlertId = h.AlertId,
                MetricValue = h.MetricValue,
                Message = h.Message,
                WasTriggered = h.WasTriggered,
                Timestamp = h.Timestamp
            }).ToList();
        }

        private AlertDto MapToDto(DashboardAlert alert) => new()
        {
            Id = alert.Id,
            DashboardId = alert.DashboardId,
            Name = alert.Name,
            Metric = alert.Metric,
            Condition = alert.Condition,
            Threshold = alert.Threshold,
            SlackWebhookUrl = alert.SlackWebhookUrl,
            EmailAddress = alert.EmailAddress,
            IsActive = alert.IsActive,
            CreatedAt = alert.CreatedAt
        };
    }
}