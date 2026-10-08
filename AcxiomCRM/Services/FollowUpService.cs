using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Data;
using AcxiomCRM.Models.Entities;

namespace AcxiomCRM.Services
{
    public class FollowUpService : IFollowUpService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;

        public FollowUpService(ApplicationDbContext context, IAuditService auditService)
        {
            _context = context;
            _auditService = auditService;
        }

        public async Task<List<FollowUp>> GetAllAsync(string? search = null, string? status = null, string? type = null, string? currentUserId = null, string? userRole = null)
        {
            var query = _context.FollowUps
                .Include(f => f.Customer)
                .Include(f => f.Lead)
                .AsQueryable();

            if (userRole == "SalesExecutive" && !string.IsNullOrWhiteSpace(currentUserId))
            {
                query = query.Where(f => f.AssignedTo == currentUserId);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(f => (f.Remarks != null && f.Remarks.ToLower().Contains(term)) ||
                                         (f.Customer != null && f.Customer.CustomerName.ToLower().Contains(term)) ||
                                         (f.Lead != null && f.Lead.LeadName.ToLower().Contains(term)));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(f => f.Status == status);
            }

            if (!string.IsNullOrWhiteSpace(type))
            {
                query = query.Where(f => f.FollowUpType == type);
            }

            return await query.OrderBy(f => f.FollowUpDate).ToListAsync();
        }

        public async Task<FollowUp?> GetByIdAsync(int id)
        {
            return await _context.FollowUps
                .Include(f => f.Customer)
                .Include(f => f.Lead)
                .FirstOrDefaultAsync(f => f.FollowUpId == id);
        }

        public async Task<(bool Success, string? ErrorMessage, FollowUp? FollowUp)> CreateAsync(FollowUp followUp, string currentUserId, string? ipAddress = null)
        {
            if (followUp.CustomerId == null && followUp.LeadId == null)
                return (false, "Follow-up must be associated with a Customer or Lead.", null);

            // Business validation: FollowUpDate cannot be earlier than today for planned/new
            if (followUp.Status == "Planned" && followUp.FollowUpDate.Date < DateTime.UtcNow.Date)
                return (false, "Follow-up date cannot be earlier than today.", null);

            followUp.CreatedDate = DateTime.UtcNow;
            if (string.IsNullOrWhiteSpace(followUp.AssignedTo))
            {
                followUp.AssignedTo = currentUserId;
            }

            _context.FollowUps.Add(followUp);
            await _context.SaveChangesAsync();

            var newJson = JsonSerializer.Serialize(new { followUp.FollowUpId, followUp.FollowUpDate, followUp.FollowUpType, followUp.Status });
            await _auditService.LogAsync(currentUserId, "Create", "FollowUp", followUp.FollowUpId.ToString(), null, newJson, ipAddress);

            return (true, null, followUp);
        }

        public async Task<(bool Success, string? ErrorMessage)> CompleteAsync(int id, string remarks, string currentUserId, string? ipAddress = null)
        {
            var existing = await _context.FollowUps.FindAsync(id);
            if (existing == null)
                return (false, "Follow-up not found.");

            var oldJson = JsonSerializer.Serialize(new { existing.Status, existing.Remarks });

            existing.Status = "Completed";
            if (!string.IsNullOrWhiteSpace(remarks))
            {
                existing.Remarks = string.IsNullOrWhiteSpace(existing.Remarks)
                    ? remarks
                    : $"{existing.Remarks} | Completed: {remarks}";
            }

            await _context.SaveChangesAsync();

            var newJson = JsonSerializer.Serialize(new { existing.Status, existing.Remarks });
            await _auditService.LogAsync(currentUserId, "Update", "FollowUp", id.ToString(), oldJson, newJson, ipAddress);

            return (true, null);
        }

        public async Task<(bool Success, string? ErrorMessage)> RescheduleAsync(int id, DateTime newDate, string remarks, string currentUserId, string? ipAddress = null)
        {
            var existing = await _context.FollowUps.FindAsync(id);
            if (existing == null)
                return (false, "Follow-up not found.");

            if (newDate.Date < DateTime.UtcNow.Date)
                return (false, "Follow-up date cannot be earlier than today.");

            var oldJson = JsonSerializer.Serialize(new { existing.FollowUpDate, existing.Remarks, existing.Status });

            existing.FollowUpDate = newDate;
            existing.Status = "Planned";
            if (!string.IsNullOrWhiteSpace(remarks))
            {
                existing.Remarks = string.IsNullOrWhiteSpace(existing.Remarks)
                    ? $"Rescheduled: {remarks}"
                    : $"{existing.Remarks} | Rescheduled: {remarks}";
            }

            await _context.SaveChangesAsync();

            var newJson = JsonSerializer.Serialize(new { existing.FollowUpDate, existing.Remarks, existing.Status });
            await _auditService.LogAsync(currentUserId, "Update", "FollowUp", id.ToString(), oldJson, newJson, ipAddress);

            return (true, null);
        }

        public async Task<(bool Success, string? ErrorMessage)> DeleteAsync(int id, string currentUserId, string? ipAddress = null)
        {
            var existing = await _context.FollowUps.FindAsync(id);
            if (existing == null)
                return (false, "Follow-up not found.");

            var oldJson = JsonSerializer.Serialize(new { existing.FollowUpId, existing.FollowUpDate, existing.FollowUpType });

            _context.FollowUps.Remove(existing);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(currentUserId, "Delete", "FollowUp", id.ToString(), oldJson, null, ipAddress);

            return (true, null);
        }

        public async Task<List<FollowUp>> GetUpcomingAsync(string? currentUserId = null, string? userRole = null)
        {
            var today = DateTime.UtcNow.Date;
            var query = _context.FollowUps
                .Include(f => f.Customer)
                .Include(f => f.Lead)
                .Where(f => f.Status == "Planned" && f.FollowUpDate.Date >= today);

            if (userRole == "SalesExecutive" && !string.IsNullOrWhiteSpace(currentUserId))
            {
                query = query.Where(f => f.AssignedTo == currentUserId);
            }

            return await query.OrderBy(f => f.FollowUpDate).Take(10).ToListAsync();
        }

        public async Task<List<FollowUp>> GetOverdueAsync(string? currentUserId = null, string? userRole = null)
        {
            var today = DateTime.UtcNow.Date;
            var query = _context.FollowUps
                .Include(f => f.Customer)
                .Include(f => f.Lead)
                .Where(f => f.Status == "Planned" && f.FollowUpDate.Date < today);

            if (userRole == "SalesExecutive" && !string.IsNullOrWhiteSpace(currentUserId))
            {
                query = query.Where(f => f.AssignedTo == currentUserId);
            }

            return await query.OrderBy(f => f.FollowUpDate).ToListAsync();
        }
    }
}
