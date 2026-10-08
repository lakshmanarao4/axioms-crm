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
    public class LeadService : ILeadService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;
        private readonly ICustomerService _customerService;

        private static readonly HashSet<string> ValidStatuses = new(StringComparer.OrdinalIgnoreCase)
        {
            "New", "Contacted", "Qualified", "Unqualified", "Converted", "Lost"
        };

        public LeadService(ApplicationDbContext context, IAuditService auditService, ICustomerService customerService)
        {
            _context = context;
            _auditService = auditService;
            _customerService = customerService;
        }

        public async Task<List<Lead>> GetAllAsync(string? search = null, string? status = null, string? assignedTo = null, string? currentUserId = null, string? userRole = null)
        {
            var query = _context.Leads.AsQueryable();

            if (userRole == "SalesExecutive" && !string.IsNullOrWhiteSpace(currentUserId))
            {
                query = query.Where(l => l.AssignedTo == currentUserId);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(l => l.LeadName.ToLower().Contains(term) ||
                                         l.Email.ToLower().Contains(term) ||
                                         l.Phone.Contains(term) ||
                                         l.CompanyName.ToLower().Contains(term) ||
                                         l.LeadCode.ToLower().Contains(term));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(l => l.Status == status);
            }

            if (!string.IsNullOrWhiteSpace(assignedTo))
            {
                query = query.Where(l => l.AssignedTo == assignedTo);
            }

            return await query.OrderByDescending(l => l.CreatedDate).ToListAsync();
        }

        public async Task<Lead?> GetByIdAsync(int id)
        {
            return await _context.Leads
                .Include(l => l.Opportunities)
                .Include(l => l.FollowUps)
                .Include(l => l.Activities)
                .FirstOrDefaultAsync(l => l.LeadId == id);
        }

        public async Task<string> GenerateLeadCodeAsync()
        {
            var count = await _context.Leads.CountAsync();
            return $"LEAD-{(count + 1001):D4}";
        }

        public async Task<(bool Success, string? ErrorMessage, Lead? Lead)> CreateAsync(Lead lead, string currentUserId, string? ipAddress = null)
        {
            if (string.IsNullOrWhiteSpace(lead.LeadName))
                return (false, "Lead Name is required.", null);

            if (string.IsNullOrWhiteSpace(lead.Email))
                return (false, "Email is required.", null);

            if (string.IsNullOrWhiteSpace(lead.Phone))
                return (false, "Phone is required.", null);

            if (!ValidStatuses.Contains(lead.Status))
                return (false, "Invalid lead status provided.", null);

            if (lead.ExpectedValue < 0)
                return (false, "Expected value cannot be negative.", null);

            if (string.IsNullOrWhiteSpace(lead.LeadCode))
            {
                lead.LeadCode = await GenerateLeadCodeAsync();
            }

            lead.CreatedDate = DateTime.UtcNow;
            if (string.IsNullOrWhiteSpace(lead.AssignedTo))
            {
                lead.AssignedTo = currentUserId;
            }

            _context.Leads.Add(lead);
            await _context.SaveChangesAsync();

            var newJson = JsonSerializer.Serialize(new { lead.LeadId, lead.LeadName, lead.Email, lead.Status, lead.ExpectedValue });
            await _auditService.LogAsync(currentUserId, "Create", "Lead", lead.LeadId.ToString(), null, newJson, ipAddress);

            return (true, null, lead);
        }

        public async Task<(bool Success, string? ErrorMessage)> UpdateAsync(Lead lead, string currentUserId, string? ipAddress = null)
        {
            var existing = await _context.Leads.FindAsync(lead.LeadId);
            if (existing == null)
                return (false, "Lead not found.");

            if (!ValidStatuses.Contains(lead.Status))
                return (false, "Invalid lead status provided.");

            // Validate status transitions
            if (existing.Status == "Converted" && lead.Status != "Converted")
                return (false, "Converted lead status cannot be altered.");

            if (lead.ExpectedValue < 0)
                return (false, "Expected value cannot be negative.");

            var oldJson = JsonSerializer.Serialize(new { existing.LeadName, existing.Status, existing.AssignedTo, existing.ExpectedValue });

            existing.LeadName = lead.LeadName;
            existing.Email = lead.Email;
            existing.Phone = lead.Phone;
            existing.CompanyName = lead.CompanyName;
            existing.Source = lead.Source;
            existing.Status = lead.Status;
            existing.ExpectedValue = lead.ExpectedValue;
            existing.AssignedTo = lead.AssignedTo;

            await _context.SaveChangesAsync();

            var newJson = JsonSerializer.Serialize(new { existing.LeadName, existing.Status, existing.AssignedTo, existing.ExpectedValue });
            await _auditService.LogAsync(currentUserId, "Update", "Lead", existing.LeadId.ToString(), oldJson, newJson, ipAddress);

            return (true, null);
        }

        public async Task<(bool Success, string? ErrorMessage)> DeleteAsync(int id, string currentUserId, string? ipAddress = null)
        {
            var existing = await _context.Leads.FindAsync(id);
            if (existing == null)
                return (false, "Lead not found.");

            var oldJson = JsonSerializer.Serialize(new { existing.LeadId, existing.LeadName, existing.Email });

            _context.Leads.Remove(existing);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(currentUserId, "Delete", "Lead", id.ToString(), oldJson, null, ipAddress);

            return (true, null);
        }

        public async Task<(bool Success, string? ErrorMessage, int? CreatedCustomerId, int? CreatedOpportunityId)> ConvertToCustomerAsync(int leadId, string currentUserId, string? ipAddress = null)
        {
            var lead = await _context.Leads.FindAsync(leadId);
            if (lead == null)
                return (false, "Lead not found.", null, null);

            if (lead.Status == "Converted")
                return (false, "Lead is already converted.", null, null);

            // Step 1: Create or find matching customer
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Email.ToLower() == lead.Email.ToLower() || c.Phone == lead.Phone);
            if (customer == null)
            {
                customer = new Customer
                {
                    CustomerCode = await _customerService.GenerateCustomerCodeAsync(),
                    CustomerName = lead.LeadName,
                    Email = lead.Email,
                    Phone = lead.Phone,
                    CompanyName = lead.CompanyName,
                    Status = "Active",
                    CreatedDate = DateTime.UtcNow,
                    CreatedBy = currentUserId
                };
                _context.Customers.Add(customer);
                await _context.SaveChangesAsync();

                await _auditService.LogAsync(currentUserId, "Create", "Customer", customer.CustomerId.ToString(), null,
                    JsonSerializer.Serialize(new { customer.CustomerId, customer.CustomerName, Source = "LeadConversion" }), ipAddress);
            }

            // Step 2: Create initial Opportunity if expected value > 0
            Opportunity? opp = null;
            if (lead.ExpectedValue > 0)
            {
                opp = new Opportunity
                {
                    OpportunityName = $"{lead.LeadName} - Deal",
                    CustomerId = customer.CustomerId,
                    LeadId = lead.LeadId,
                    Amount = lead.ExpectedValue,
                    Stage = "Qualification",
                    Probability = 20,
                    ExpectedCloseDate = DateTime.UtcNow.AddDays(30),
                    Status = "Open",
                    CreatedDate = DateTime.UtcNow,
                    AssignedTo = string.IsNullOrWhiteSpace(lead.AssignedTo) ? currentUserId : lead.AssignedTo
                };
                _context.Opportunities.Add(opp);
                await _context.SaveChangesAsync();

                await _auditService.LogAsync(currentUserId, "Create", "Opportunity", opp.OpportunityId.ToString(), null,
                    JsonSerializer.Serialize(new { opp.OpportunityId, opp.OpportunityName, opp.Amount }), ipAddress);
            }

            // Step 3: Update Lead status to Converted
            var oldStatus = lead.Status;
            lead.Status = "Converted";
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(currentUserId, "Convert", "Lead", lead.LeadId.ToString(),
                JsonSerializer.Serialize(new { Status = oldStatus }),
                JsonSerializer.Serialize(new { Status = "Converted", CustomerId = customer.CustomerId, OpportunityId = opp?.OpportunityId }),
                ipAddress);

            return (true, null, customer.CustomerId, opp?.OpportunityId);
        }
    }
}
