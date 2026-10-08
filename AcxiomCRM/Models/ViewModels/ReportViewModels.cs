using System;
using System.Collections.Generic;
using AcxiomCRM.Models.Entities;

namespace AcxiomCRM.Models.ViewModels
{
    public class PipelineReportItem
    {
        public string Stage { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal WeightedAmount { get; set; }
    }

    public class ConversionReportViewModel
    {
        public int TotalLeads { get; set; }
        public int ConvertedLeads { get; set; }
        public int LostLeads { get; set; }
        public int ActiveLeads { get; set; }
        public double ConversionRate => TotalLeads == 0 ? 0 : Math.Round((double)ConvertedLeads / TotalLeads * 100, 1);
    }

    public class UserPerformanceReportItem
    {
        public string UserEmail { get; set; } = string.Empty;
        public int AssignedLeads { get; set; }
        public int AssignedOpportunities { get; set; }
        public int WonOpportunities { get; set; }
        public decimal WonValue { get; set; }
        public int CompletedFollowUps { get; set; }
    }

    public class ReportsIndexViewModel
    {
        public List<PipelineReportItem> PipelineSummary { get; set; } = new();
        public ConversionReportViewModel ConversionStats { get; set; } = new();
        public List<UserPerformanceReportItem> UserPerformance { get; set; } = new();
        public List<Customer> Customers { get; set; } = new();
        public List<Lead> Leads { get; set; } = new();
        public List<Opportunity> Opportunities { get; set; } = new();
        public List<FollowUp> FollowUps { get; set; } = new();
        public List<AuditLog> AuditLogs { get; set; } = new();
    }
}
