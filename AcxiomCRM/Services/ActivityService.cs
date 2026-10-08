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
    public class ActivityService : IActivityService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;

        public ActivityService(ApplicationDbContext context, IAuditService auditService)
        {
            _context = context;
            _auditService = auditService;
        }

        public async Task<List<Activity>> GetAllAsync(string? search = null, string? type = null, string? status = null, string? currentUserId = null, string? userRole = null)
        {
            var query = _context.Activities
                .Include(a => a.Customer)
                .Include(a => a.Lead)
                .AsQueryable();

            if (userRole == "SalesExecutive" && !string.IsNullOrWhiteSpace(currentUserId))
            {
                query = query.Where(a => a.AssignedTo == currentUserId);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(a => a.Subject.ToLower().Contains(term) ||
                                         (a.Description != null && a.Description.ToLower().Contains(term)));
            }

            if (!string.IsNullOrWhiteSpace(type))
            {
                query = query.Where(a => a.ActivityType == type);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(a => a.Status == status);
            }

            return await query.OrderByDescending(a => a.ActivityDate).ToListAsync();
        }

        public async Task<Activity?> GetByIdAsync(int id)
        {
            return await _context.Activities
                .Include(a => a.Customer)
                .Include(a => a.Lead)
                .FirstOrDefaultAsync(a => a.ActivityId == id);
        }

        public async Task<(bool Success, string? ErrorMessage, Activity? Activity)> CreateAsync(Activity activity, string currentUserId, string? ipAddress = null)
        {
            if (string.IsNullOrWhiteSpace(activity.Subject))
                return (false, "Subject is required.", null);

            if (activity.ActivityDate == default)
                activity.ActivityDate = DateTime.UtcNow;

            if (string.IsNullOrWhiteSpace(activity.AssignedTo))
            {
                activity.AssignedTo = currentUserId;
            }

            _context.Activities.Add(activity);
            await _context.SaveChangesAsync();

            var newJson = JsonSerializer.Serialize(new { activity.ActivityId, activity.ActivityType, activity.Subject });
            await _auditService.LogAsync(currentUserId, "Create", "Activity", activity.ActivityId.ToString(), null, newJson, ipAddress);

            return (true, null, activity);
        }

        public async Task<(bool Success, string? ErrorMessage)> DeleteAsync(int id, string currentUserId, string? ipAddress = null)
        {
            var existing = await _context.Activities.FindAsync(id);
            if (existing == null)
                return (false, "Activity not found.");

            _context.Activities.Remove(existing);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(currentUserId, "Delete", "Activity", id.ToString(), null, null, ipAddress);

            return (true, null);
        }
    }
}
