using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Data;
using AcxiomCRM.Models.ViewModels;

namespace AcxiomCRM.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly ApplicationDbContext _context;

        public DashboardService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<DashboardViewModel> GetDashboardDataAsync(string? currentUserId, string? userRole, string? dateFilter = "All")
        {
            var vm = new DashboardViewModel { DateFilter = dateFilter ?? "All" };

            // Determine date filter boundaries
            DateTime? startDate = null;
            var now = DateTime.UtcNow;
            if (dateFilter == "Today")
                startDate = now.Date;
            else if (dateFilter == "Week")
                startDate = now.Date.AddDays(-(int)now.DayOfWeek);
            else if (dateFilter == "Month")
                startDate = new DateTime(now.Year, now.Month, 1);

            // Customers query
            var custQuery = _context.Customers.AsQueryable();
            if (userRole == "SalesExecutive" && !string.IsNullOrWhiteSpace(currentUserId))
                custQuery = custQuery.Where(c => c.CreatedBy == currentUserId);
            if (startDate.HasValue)
                custQuery = custQuery.Where(c => c.CreatedDate >= startDate.Value);

            vm.TotalCustomers = await custQuery.CountAsync();

            // Leads query
            var leadQuery = _context.Leads.AsQueryable();
            if (userRole == "SalesExecutive" && !string.IsNullOrWhiteSpace(currentUserId))
                leadQuery = leadQuery.Where(l => l.AssignedTo == currentUserId);
            if (startDate.HasValue)
                leadQuery = leadQuery.Where(l => l.CreatedDate >= startDate.Value);

            var leadsList = await leadQuery.ToListAsync();
            vm.TotalLeads = leadsList.Count;
            vm.OpenLeads = leadsList.Count(l => l.Status != "Converted" && l.Status != "Lost");

            // Opportunities query
            var oppQuery = _context.Opportunities.Include(o => o.Customer).AsQueryable();
            if (userRole == "SalesExecutive" && !string.IsNullOrWhiteSpace(currentUserId))
                oppQuery = oppQuery.Where(o => o.AssignedTo == currentUserId);
            if (startDate.HasValue)
                oppQuery = oppQuery.Where(o => o.CreatedDate >= startDate.Value);

            var oppsList = await oppQuery.ToListAsync();
            vm.TotalOpportunities = oppsList.Count;
            vm.OpenOpportunities = oppsList.Count(o => o.Stage != "Won" && o.Stage != "Lost");
            vm.WonOpportunities = oppsList.Count(o => o.Stage == "Won");
            vm.LostOpportunities = oppsList.Count(o => o.Stage == "Lost");
            vm.TotalPipelineValue = oppsList.Where(o => o.Stage != "Won" && o.Stage != "Lost").Sum(o => o.Amount);
            vm.WonPipelineValue = oppsList.Where(o => o.Stage == "Won").Sum(o => o.Amount);

            // Chart 1: Lead Status Chart
            var leadStatuses = new[] { "New", "Contacted", "Qualified", "Lost", "Converted" };
            vm.LeadStatusLabels = leadStatuses.ToList();
            vm.LeadStatusData = leadStatuses.Select(s => leadsList.Count(l => string.Equals(l.Status, s, StringComparison.OrdinalIgnoreCase))).ToList();

            // Chart 2: Pipeline Stage Chart
            var stages = new[] { "Qualification", "Proposal", "Negotiation", "Won", "Lost" };
            vm.PipelineStageLabels = stages.ToList();
            vm.PipelineStageValues = stages.Select(s => oppsList.Where(o => string.Equals(o.Stage, s, StringComparison.OrdinalIgnoreCase)).Sum(o => o.Amount)).ToList();
            vm.PipelineStageCounts = stages.Select(s => oppsList.Count(o => string.Equals(o.Stage, s, StringComparison.OrdinalIgnoreCase))).ToList();

            // Chart 3: Monthly Sales Trend (Last 6 Months)
            var monthlyLabels = new List<string>();
            var monthlyData = new List<decimal>();
            for (int i = 5; i >= 0; i--)
            {
                var targetMonth = now.AddMonths(-i);
                var monthStart = new DateTime(targetMonth.Year, targetMonth.Month, 1);
                var monthEnd = monthStart.AddMonths(1).AddTicks(-1);

                monthlyLabels.Add(targetMonth.ToString("MMM yyyy", CultureInfo.InvariantCulture));
                var wonInMonth = oppsList
                    .Where(o => o.Stage == "Won" && o.CreatedDate >= monthStart && o.CreatedDate <= monthEnd)
                    .Sum(o => o.Amount);
                monthlyData.Add(wonInMonth);
            }
            vm.MonthlyLabels = monthlyLabels;
            vm.MonthlySalesData = monthlyData;

            // Follow-ups lists
            var today = DateTime.UtcNow.Date;
            var followUpQuery = _context.FollowUps
                .Include(f => f.Customer)
                .Include(f => f.Lead)
                .AsQueryable();

            if (userRole == "SalesExecutive" && !string.IsNullOrWhiteSpace(currentUserId))
                followUpQuery = followUpQuery.Where(f => f.AssignedTo == currentUserId);

            vm.UpcomingFollowUps = await followUpQuery
                .Where(f => f.Status == "Planned" && f.FollowUpDate.Date >= today)
                .OrderBy(f => f.FollowUpDate)
                .Take(5)
                .ToListAsync();

            vm.OverdueFollowUps = await followUpQuery
                .Where(f => f.Status == "Planned" && f.FollowUpDate.Date < today)
                .OrderBy(f => f.FollowUpDate)
                .Take(5)
                .ToListAsync();

            vm.RecentOpportunities = oppsList.OrderByDescending(o => o.CreatedDate).Take(5).ToList();
            vm.RecentLeads = leadsList.OrderByDescending(l => l.CreatedDate).Take(5).ToList();

            return vm;
        }
    }
}
