using System.Collections.Generic;
using System.Threading.Tasks;
using AcxiomCRM.Models.Entities;

namespace AcxiomCRM.Services
{
    public interface IOpportunityService
    {
        Task<List<Opportunity>> GetAllAsync(string? search = null, string? stage = null, string? status = null, string? currentUserId = null, string? userRole = null);
        Task<Opportunity?> GetByIdAsync(int id);
        Task<(bool Success, string? ErrorMessage, Opportunity? Opportunity)> CreateAsync(Opportunity opportunity, string currentUserId, string? ipAddress = null);
        Task<(bool Success, string? ErrorMessage)> UpdateAsync(Opportunity opportunity, string currentUserId, string? ipAddress = null);
        Task<(bool Success, string? ErrorMessage)> DeleteAsync(int id, string currentUserId, string? ipAddress = null);
    }
}
