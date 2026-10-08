using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AcxiomCRM.Models.Entities;

namespace AcxiomCRM.Services
{
    public interface IFollowUpService
    {
        Task<List<FollowUp>> GetAllAsync(string? search = null, string? status = null, string? type = null, string? currentUserId = null, string? userRole = null);
        Task<FollowUp?> GetByIdAsync(int id);
        Task<(bool Success, string? ErrorMessage, FollowUp? FollowUp)> CreateAsync(FollowUp followUp, string currentUserId, string? ipAddress = null);
        Task<(bool Success, string? ErrorMessage)> CompleteAsync(int id, string remarks, string currentUserId, string? ipAddress = null);
        Task<(bool Success, string? ErrorMessage)> RescheduleAsync(int id, DateTime newDate, string remarks, string currentUserId, string? ipAddress = null);
        Task<(bool Success, string? ErrorMessage)> DeleteAsync(int id, string currentUserId, string? ipAddress = null);
        Task<List<FollowUp>> GetUpcomingAsync(string? currentUserId = null, string? userRole = null);
        Task<List<FollowUp>> GetOverdueAsync(string? currentUserId = null, string? userRole = null);
    }
}
