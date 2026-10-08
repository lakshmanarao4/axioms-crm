using System.Threading.Tasks;
using AcxiomCRM.Models.ViewModels;

namespace AcxiomCRM.Services
{
    public interface IDashboardService
    {
        Task<DashboardViewModel> GetDashboardDataAsync(string? currentUserId, string? userRole, string? dateFilter = "All");
    }
}
