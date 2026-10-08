using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AcxiomCRM.Models.Entities;

namespace AcxiomCRM.Services
{
    public interface IAuditService
    {
        Task LogAsync(string userId, string action, string entityName, string recordId, string? oldValue = null, string? newValue = null, string? ipAddress = "127.0.0.1");
        Task<List<AuditLog>> GetRecentLogsAsync(int count = 100);
        Task<List<AuditLog>> FilterLogsAsync(string? userId, string? entityName, string? action, DateTime? fromDate, DateTime? toDate);
    }
}
