using System.Collections.Generic;
using System.Threading.Tasks;
using AcxiomCRM.Models.Entities;

namespace AcxiomCRM.Services
{
    public interface IActivityService
    {
        Task<List<Activity>> GetAllAsync(string? search = null, string? type = null, string? status = null, string? currentUserId = null, string? userRole = null);
        Task<Activity?> GetByIdAsync(int id);
        Task<(bool Success, string? ErrorMessage, Activity? Activity)> CreateAsync(Activity activity, string currentUserId, string? ipAddress = null);
        Task<(bool Success, string? ErrorMessage)> DeleteAsync(int id, string currentUserId, string? ipAddress = null);
    }
}
