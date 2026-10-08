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
    public class CustomerService : ICustomerService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;

        public CustomerService(ApplicationDbContext context, IAuditService auditService)
        {
            _context = context;
            _auditService = auditService;
        }

        public async Task<List<Customer>> GetAllAsync(string? search = null, string? status = null, string? currentUserId = null, string? userRole = null)
        {
            var query = _context.Customers.AsQueryable();

            // Role-based scoping: SalesExecutive sees assigned/created customers
            if (userRole == "SalesExecutive" && !string.IsNullOrWhiteSpace(currentUserId))
            {
                query = query.Where(c => c.CreatedBy == currentUserId);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(c => c.CustomerName.ToLower().Contains(term) ||
                                         c.Email.ToLower().Contains(term) ||
                                         c.Phone.Contains(term) ||
                                         c.CompanyName.ToLower().Contains(term) ||
                                         c.CustomerCode.ToLower().Contains(term));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(c => c.Status == status);
            }

            return await query.OrderByDescending(c => c.CreatedDate).ToListAsync();
        }

        public async Task<Customer?> GetByIdAsync(int id)
        {
            return await _context.Customers
                .Include(c => c.Opportunities)
                .Include(c => c.FollowUps)
                .Include(c => c.Activities)
                .FirstOrDefaultAsync(c => c.CustomerId == id);
        }

        public async Task<bool> IsEmailUniqueAsync(string email, int? excludeId = null)
        {
            if (string.IsNullOrWhiteSpace(email)) return true;
            var cleanEmail = email.Trim().ToLower();
            return !await _context.Customers.AnyAsync(c => c.Email.ToLower() == cleanEmail && (!excludeId.HasValue || c.CustomerId != excludeId.Value));
        }

        public async Task<bool> IsPhoneUniqueAsync(string phone, int? excludeId = null)
        {
            if (string.IsNullOrWhiteSpace(phone)) return true;
            var cleanPhone = phone.Trim();
            return !await _context.Customers.AnyAsync(c => c.Phone == cleanPhone && (!excludeId.HasValue || c.CustomerId != excludeId.Value));
        }

        public async Task<bool> IsDuplicateCustomerAsync(string name, string company, int? excludeId = null)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            var cleanName = name.Trim().ToLower();
            var cleanCompany = (company ?? string.Empty).Trim().ToLower();

            return await _context.Customers.AnyAsync(c =>
                c.CustomerName.ToLower() == cleanName &&
                (c.CompanyName ?? string.Empty).ToLower() == cleanCompany &&
                (!excludeId.HasValue || c.CustomerId != excludeId.Value));
        }

        public async Task<string> GenerateCustomerCodeAsync()
        {
            var count = await _context.Customers.CountAsync();
            return $"CUST-{(count + 1001):D4}";
        }

        public async Task<(bool Success, string? ErrorMessage, Customer? Customer)> CreateAsync(Customer customer, string currentUserId, string? ipAddress = null)
        {
            // Business validations
            if (string.IsNullOrWhiteSpace(customer.CustomerName))
                return (false, "Customer Name is required.", null);

            if (string.IsNullOrWhiteSpace(customer.Email))
                return (false, "Email is required.", null);

            if (string.IsNullOrWhiteSpace(customer.Phone))
                return (false, "Phone is required.", null);

            if (!await IsEmailUniqueAsync(customer.Email))
                return (false, "A customer with this email address already exists.", null);

            if (!await IsPhoneUniqueAsync(customer.Phone))
                return (false, "A customer with this phone number already exists.", null);

            if (await IsDuplicateCustomerAsync(customer.CustomerName, customer.CompanyName))
                return (false, "A customer with the same name and company already exists.", null);

            if (string.IsNullOrWhiteSpace(customer.CustomerCode))
            {
                customer.CustomerCode = await GenerateCustomerCodeAsync();
            }

            customer.CreatedDate = DateTime.UtcNow;
            customer.CreatedBy = currentUserId;

            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();

            var newJson = JsonSerializer.Serialize(new { customer.CustomerId, customer.CustomerName, customer.Email, customer.Phone });
            await _auditService.LogAsync(currentUserId, "Create", "Customer", customer.CustomerId.ToString(), null, newJson, ipAddress);

            return (true, null, customer);
        }

        public async Task<(bool Success, string? ErrorMessage)> UpdateAsync(Customer customer, string currentUserId, string? ipAddress = null)
        {
            var existing = await _context.Customers.FindAsync(customer.CustomerId);
            if (existing == null)
                return (false, "Customer not found.");

            // Business validations
            if (!await IsEmailUniqueAsync(customer.Email, customer.CustomerId))
                return (false, "A customer with this email address already exists.");

            if (!await IsPhoneUniqueAsync(customer.Phone, customer.CustomerId))
                return (false, "A customer with this phone number already exists.");

            if (await IsDuplicateCustomerAsync(customer.CustomerName, customer.CompanyName, customer.CustomerId))
                return (false, "A customer with the same name and company already exists.");

            var oldJson = JsonSerializer.Serialize(new { existing.CustomerName, existing.Email, existing.Phone, existing.Status });

            existing.CustomerName = customer.CustomerName;
            existing.Email = customer.Email;
            existing.Phone = customer.Phone;
            existing.CompanyName = customer.CompanyName;
            existing.Address = customer.Address;
            existing.City = customer.City;
            existing.State = customer.State;
            existing.Status = customer.Status;

            await _context.SaveChangesAsync();

            var newJson = JsonSerializer.Serialize(new { existing.CustomerName, existing.Email, existing.Phone, existing.Status });
            await _auditService.LogAsync(currentUserId, "Update", "Customer", existing.CustomerId.ToString(), oldJson, newJson, ipAddress);

            return (true, null);
        }

        public async Task<(bool Success, string? ErrorMessage)> DeleteAsync(int id, string currentUserId, string? ipAddress = null)
        {
            var existing = await _context.Customers
                .Include(c => c.Opportunities)
                .Include(c => c.FollowUps)
                .Include(c => c.Activities)
                .FirstOrDefaultAsync(c => c.CustomerId == id);

            if (existing == null)
                return (false, "Customer not found.");

            var oldJson = JsonSerializer.Serialize(new { existing.CustomerId, existing.CustomerName, existing.Email });

            _context.Customers.Remove(existing);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(currentUserId, "Delete", "Customer", id.ToString(), oldJson, null, ipAddress);

            return (true, null);
        }
    }
}
