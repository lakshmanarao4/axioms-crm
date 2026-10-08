using System.Collections.Generic;
using System.Threading.Tasks;
using AcxiomCRM.Models.Entities;

namespace AcxiomCRM.Services
{
    public interface ICustomerService
    {
        Task<List<Customer>> GetAllAsync(string? search = null, string? status = null, string? currentUserId = null, string? userRole = null);
        Task<Customer?> GetByIdAsync(int id);
        Task<bool> IsEmailUniqueAsync(string email, int? excludeId = null);
        Task<bool> IsPhoneUniqueAsync(string phone, int? excludeId = null);
        Task<bool> IsDuplicateCustomerAsync(string name, string company, int? excludeId = null);
        Task<(bool Success, string? ErrorMessage, Customer? Customer)> CreateAsync(Customer customer, string currentUserId, string? ipAddress = null);
        Task<(bool Success, string? ErrorMessage)> UpdateAsync(Customer customer, string currentUserId, string? ipAddress = null);
        Task<(bool Success, string? ErrorMessage)> DeleteAsync(int id, string currentUserId, string? ipAddress = null);
        Task<string> GenerateCustomerCodeAsync();
    }
}
