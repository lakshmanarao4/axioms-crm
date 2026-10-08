using System.Collections.Generic;
using AcxiomCRM.Models.Entities;

namespace AcxiomCRM.Models.ViewModels
{
    public class DashboardViewModel
    {
        // KPI Cards
        public int TotalCustomers { get; set; }
        public int TotalLeads { get; set; }
        public int OpenLeads { get; set; }
        public int TotalOpportunities { get; set; }
        public int OpenOpportunities { get; set; }
        public int WonOpportunities { get; set; }
        public int LostOpportunities { get; set; }
        public decimal TotalPipelineValue { get; set; }
        public decimal WonPipelineValue { get; set; }

        // Filter
        public string DateFilter { get; set; } = "All"; // All, Today, Week, Month

        // Chart 1: Lead Status Breakdown
        public List<string> LeadStatusLabels { get; set; } = new();
        public List<int> LeadStatusData { get; set; } = new();

        // Chart 2: Opportunity Pipeline Stage Breakdown
        public List<string> PipelineStageLabels { get; set; } = new();
        public List<decimal> PipelineStageValues { get; set; } = new();
        public List<int> PipelineStageCounts { get; set; } = new();

        // Chart 3: Monthly Sales Trend (Past 6 Months)
        public List<string> MonthlyLabels { get; set; } = new();
        public List<decimal> MonthlySalesData { get; set; } = new();

        // Recent items
        public List<FollowUp> UpcomingFollowUps { get; set; } = new();
        public List<FollowUp> OverdueFollowUps { get; set; } = new();
        public List<Opportunity> RecentOpportunities { get; set; } = new();
        public List<Lead> RecentLeads { get; set; } = new();
    }
}
