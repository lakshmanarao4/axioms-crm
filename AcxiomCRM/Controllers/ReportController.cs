using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Data;
using AcxiomCRM.Models.Entities;
using AcxiomCRM.Models.ViewModels;

namespace AcxiomCRM.Controllers
{
    [Authorize]
    public class ReportController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ReportController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var userEmail = User.Identity?.Name;
            var isAdmin = User.IsInRole("Admin");
            var isManager = User.IsInRole("Manager");

            var model = new ReportsIndexViewModel();

            // Pipeline stage aggregation
            var opps = await _context.Opportunities.Include(o => o.Customer).ToListAsync();
            var stages = new[] { "Qualification", "Proposal", "Negotiation", "Won", "Lost" };
            model.PipelineSummary = stages.Select(s => new PipelineReportItem
            {
                Stage = s,
                Count = opps.Count(o => string.Equals(o.Stage, s, StringComparison.OrdinalIgnoreCase)),
                TotalAmount = opps.Where(o => string.Equals(o.Stage, s, StringComparison.OrdinalIgnoreCase)).Sum(o => o.Amount),
                WeightedAmount = opps.Where(o => string.Equals(o.Stage, s, StringComparison.OrdinalIgnoreCase)).Sum(o => o.WeightedValue)
            }).ToList();

            // Conversion stats
            var leads = await _context.Leads.ToListAsync();
            model.ConversionStats = new ConversionReportViewModel
            {
                TotalLeads = leads.Count,
                ConvertedLeads = leads.Count(l => l.Status == "Converted"),
                LostLeads = leads.Count(l => l.Status == "Lost"),
                ActiveLeads = leads.Count(l => l.Status != "Converted" && l.Status != "Lost")
            };

            // User performance summary (Admin & Manager)
            if (isAdmin || isManager)
            {
                var users = await _userManager.Users.ToListAsync();
                var followUps = await _context.FollowUps.ToListAsync();

                model.UserPerformance = users.Select(u => new UserPerformanceReportItem
                {
                    UserEmail = u.Email ?? u.UserName ?? "Unknown",
                    AssignedLeads = leads.Count(l => l.AssignedTo == u.Email),
                    AssignedOpportunities = opps.Count(o => o.AssignedTo == u.Email),
                    WonOpportunities = opps.Count(o => o.AssignedTo == u.Email && o.Stage == "Won"),
                    WonValue = opps.Where(o => o.AssignedTo == u.Email && o.Stage == "Won").Sum(o => o.Amount),
                    CompletedFollowUps = followUps.Count(f => f.AssignedTo == u.Email && f.Status == "Completed")
                }).ToList();
            }

            model.Customers = await _context.Customers.OrderByDescending(c => c.CreatedDate).Take(20).ToListAsync();
            model.Leads = leads.Take(20).ToList();
            model.Opportunities = opps.Take(20).ToList();
            model.FollowUps = await _context.FollowUps.Include(f => f.Customer).Include(f => f.Lead).Take(20).ToListAsync();

            if (isAdmin)
            {
                model.AuditLogs = await _context.AuditLogs.OrderByDescending(a => a.CreatedDate).Take(20).ToListAsync();
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> ExportPipelineCsv()
        {
            var opps = await _context.Opportunities.Include(o => o.Customer).ToListAsync();
            var sb = new StringBuilder();
            sb.AppendLine("OpportunityId,OpportunityName,CustomerName,Amount,Stage,Probability,WeightedValue,ExpectedCloseDate,AssignedTo");

            foreach (var o in opps)
            {
                sb.AppendLine($"{o.OpportunityId},\"{o.OpportunityName}\",\"{o.Customer?.CustomerName}\",{o.Amount},{o.Stage},{o.Probability},{o.WeightedValue},{o.ExpectedCloseDate:yyyy-MM-dd},\"{o.AssignedTo}\"");
            }

            return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", $"PipelineReport_{DateTime.UtcNow:yyyyMMdd}.csv");
        }

        [HttpGet]
        public async Task<IActionResult> ExportCustomersCsv()
        {
            var customers = await _context.Customers.ToListAsync();
            var sb = new StringBuilder();
            sb.AppendLine("CustomerId,CustomerCode,CustomerName,Email,Phone,CompanyName,City,State,Status,CreatedDate,CreatedBy");

            foreach (var c in customers)
            {
                sb.AppendLine($"{c.CustomerId},{c.CustomerCode},\"{c.CustomerName}\",{c.Email},{c.Phone},\"{c.CompanyName}\",\"{c.City}\",\"{c.State}\",{c.Status},{c.CreatedDate:yyyy-MM-dd},\"{c.CreatedBy}\"");
            }

            return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", $"CustomerReport_{DateTime.UtcNow:yyyyMMdd}.csv");
        }
    }
}
