using System.Collections.Generic;
using System.Threading.Tasks;
using AcxiomCRM.Models.Entities;

namespace AcxiomCRM.Services
{
    public interface ILeadService
    {
        Task<List<Lead>> GetAllAsync(string? search = null, string? status = null, string? assignedTo = null, string? currentUserId = null, string? userRole = null);
        Task<Lead?> GetByIdAsync(int id);
        Task<(bool Success, string? ErrorMessage, Lead? Lead)> CreateAsync(Lead lead, string currentUserId, string? ipAddress = null);
        Task<(bool Success, string? ErrorMessage)> UpdateAsync(Lead lead, string currentUserId, string? ipAddress = null);
        Task<(bool Success, string? ErrorMessage)> DeleteAsync(int id, string currentUserId, string? ipAddress = null);
        Task<(bool Success, string? ErrorMessage, int? CreatedCustomerId, int? CreatedOpportunityId)> ConvertToCustomerAsync(int leadId, string currentUserId, string? ipAddress = null);
        Task<string> GenerateLeadCodeAsync();
    }
}
