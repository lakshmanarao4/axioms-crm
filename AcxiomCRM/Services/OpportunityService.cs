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
    public class OpportunityService : IOpportunityService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;

        public OpportunityService(ApplicationDbContext context, IAuditService auditService)
        {
            _context = context;
            _auditService = auditService;
        }

        public async Task<List<Opportunity>> GetAllAsync(string? search = null, string? stage = null, string? status = null, string? currentUserId = null, string? userRole = null)
        {
            var query = _context.Opportunities
                .Include(o => o.Customer)
                .Include(o => o.Lead)
                .AsQueryable();

            if (userRole == "SalesExecutive" && !string.IsNullOrWhiteSpace(currentUserId))
            {
                query = query.Where(o => o.AssignedTo == currentUserId);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(o => o.OpportunityName.ToLower().Contains(term) ||
                                         (o.Customer != null && o.Customer.CustomerName.ToLower().Contains(term)));
            }

            if (!string.IsNullOrWhiteSpace(stage))
            {
                query = query.Where(o => o.Stage == stage);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(o => o.Status == status);
            }

            return await query.OrderByDescending(o => o.CreatedDate).ToListAsync();
        }

        public async Task<Opportunity?> GetByIdAsync(int id)
        {
            return await _context.Opportunities
                .Include(o => o.Customer)
                .Include(o => o.Lead)
                .FirstOrDefaultAsync(o => o.OpportunityId == id);
        }

        public async Task<(bool Success, string? ErrorMessage, Opportunity? Opportunity)> CreateAsync(Opportunity opportunity, string currentUserId, string? ipAddress = null)
        {
            // Business validations
            if (string.IsNullOrWhiteSpace(opportunity.OpportunityName))
                return (false, "Opportunity Name is required.", null);

            if (opportunity.CustomerId <= 0)
                return (false, "A valid Customer must be selected.", null);

            if (opportunity.Amount <= 0)
                return (false, "Opportunity Amount must be greater than 0.", null);

            if (opportunity.Probability < 0 || opportunity.Probability > 100)
                return (false, "Probability must be between 0 and 100.", null);

            var isClosed = opportunity.Stage == "Won" || opportunity.Stage == "Lost";
            if (!isClosed && opportunity.ExpectedCloseDate.Date < DateTime.UtcNow.Date)
                return (false, "Expected Close Date cannot be in the past.", null);

            // Update status matching stage
            if (opportunity.Stage == "Won") opportunity.Status = "Won";
            else if (opportunity.Stage == "Lost") opportunity.Status = "Lost";
            else opportunity.Status = "Open";

            opportunity.CreatedDate = DateTime.UtcNow;
            if (string.IsNullOrWhiteSpace(opportunity.AssignedTo))
            {
                opportunity.AssignedTo = currentUserId;
            }

            _context.Opportunities.Add(opportunity);
            await _context.SaveChangesAsync();

            var newJson = JsonSerializer.Serialize(new { opportunity.OpportunityId, opportunity.OpportunityName, opportunity.Amount, opportunity.Stage, opportunity.Probability });
            await _auditService.LogAsync(currentUserId, "Create", "Opportunity", opportunity.OpportunityId.ToString(), null, newJson, ipAddress);

            return (true, null, opportunity);
        }

        public async Task<(bool Success, string? ErrorMessage)> UpdateAsync(Opportunity opportunity, string currentUserId, string? ipAddress = null)
        {
            var existing = await _context.Opportunities.FindAsync(opportunity.OpportunityId);
            if (existing == null)
                return (false, "Opportunity not found.");

            if (opportunity.Amount <= 0)
                return (false, "Opportunity Amount must be greater than 0.");

            if (opportunity.Probability < 0 || opportunity.Probability > 100)
                return (false, "Probability must be between 0 and 100.");

            var isClosed = opportunity.Stage == "Won" || opportunity.Stage == "Lost";
            if (!isClosed && opportunity.ExpectedCloseDate.Date < DateTime.UtcNow.Date)
                return (false, "Expected Close Date cannot be in the past.");

            var oldJson = JsonSerializer.Serialize(new { existing.OpportunityName, existing.Amount, existing.Stage, existing.Probability, existing.Status });

            existing.OpportunityName = opportunity.OpportunityName;
            existing.CustomerId = opportunity.CustomerId;
            existing.LeadId = opportunity.LeadId;
            existing.Amount = opportunity.Amount;
            existing.Stage = opportunity.Stage;
            existing.Probability = opportunity.Probability;
            existing.ExpectedCloseDate = opportunity.ExpectedCloseDate;
            existing.AssignedTo = opportunity.AssignedTo;

            if (existing.Stage == "Won") existing.Status = "Won";
            else if (existing.Stage == "Lost") existing.Status = "Lost";
            else existing.Status = "Open";

            await _context.SaveChangesAsync();

            var newJson = JsonSerializer.Serialize(new { existing.OpportunityName, existing.Amount, existing.Stage, existing.Probability, existing.Status });
            await _auditService.LogAsync(currentUserId, "Update", "Opportunity", existing.OpportunityId.ToString(), oldJson, newJson, ipAddress);

            return (true, null);
        }

        public async Task<(bool Success, string? ErrorMessage)> DeleteAsync(int id, string currentUserId, string? ipAddress = null)
        {
            var existing = await _context.Opportunities.FindAsync(id);
            if (existing == null)
                return (false, "Opportunity not found.");

            var oldJson = JsonSerializer.Serialize(new { existing.OpportunityId, existing.OpportunityName, existing.Amount });

            _context.Opportunities.Remove(existing);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(currentUserId, "Delete", "Opportunity", id.ToString(), oldJson, null, ipAddress);

            return (true, null);
        }
    }
}
