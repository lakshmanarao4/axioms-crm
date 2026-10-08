using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Data;
using AcxiomCRM.Models.Entities;

namespace AcxiomCRM.Services
{
    public class AuditService : IAuditService
    {
        private readonly ApplicationDbContext _context;

        public AuditService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task LogAsync(string userId, string action, string entityName, string recordId, string? oldValue = null, string? newValue = null, string? ipAddress = "127.0.0.1")
        {
            try
            {
                var audit = new AuditLog
                {
                    UserId = string.IsNullOrWhiteSpace(userId) ? "System" : userId,
                    Action = action,
                    EntityName = entityName,
                    RecordId = recordId,
                    OldValue = oldValue,
                    NewValue = newValue,
                    CreatedDate = DateTime.UtcNow,
                    IpAddress = string.IsNullOrWhiteSpace(ipAddress) ? "127.0.0.1" : ipAddress
                };

                _context.AuditLogs.Add(audit);
                await _context.SaveChangesAsync();
            }
            catch
            {
                // Never break core business operations on audit logging failures
            }
        }

        public async Task<List<AuditLog>> GetRecentLogsAsync(int count = 100)
        {
            return await _context.AuditLogs
                .OrderByDescending(a => a.CreatedDate)
                .Take(count)
                .ToListAsync();
        }

        public async Task<List<AuditLog>> FilterLogsAsync(string? userId, string? entityName, string? action, DateTime? fromDate, DateTime? toDate)
        {
            var query = _context.AuditLogs.AsQueryable();

            if (!string.IsNullOrWhiteSpace(userId))
            {
                query = query.Where(a => a.UserId.Contains(userId));
            }

            if (!string.IsNullOrWhiteSpace(entityName))
            {
                query = query.Where(a => a.EntityName == entityName);
            }

            if (!string.IsNullOrWhiteSpace(action))
            {
                query = query.Where(a => a.Action == action);
            }

            if (fromDate.HasValue)
            {
                query = query.Where(a => a.CreatedDate >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                var endOfDay = toDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(a => a.CreatedDate <= endOfDay);
            }

            return await query.OrderByDescending(a => a.CreatedDate).ToListAsync();
        }
    }
}
