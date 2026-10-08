using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using AcxiomCRM.Models.Entities;
using AcxiomCRM.Models.ViewModels;
using AcxiomCRM.Services;

namespace AcxiomCRM.Controllers
{
    [Authorize]
    public class ActivityController : Controller
    {
        private readonly IActivityService _activityService;
        private readonly ICustomerService _customerService;
        private readonly ILeadService _leadService;

        public ActivityController(
            IActivityService activityService,
            ICustomerService customerService,
            ILeadService leadService)
        {
            _activityService = activityService;
            _customerService = customerService;
            _leadService = leadService;
        }

        public async Task<IActionResult> Index(string? search = null, string? type = null, string? status = null)
        {
            var userEmail = User.Identity?.Name;
            var role = User.IsInRole("Admin") ? "Admin" 
                     : User.IsInRole("Manager") ? "Manager" 
                     : "SalesExecutive";

            var activities = await _activityService.GetAllAsync(search, type, status, userEmail, role);
            ViewBag.CurrentSearch = search;
            ViewBag.CurrentType = type;
            ViewBag.CurrentStatus = status;
            return View(activities);
        }

        [HttpGet]
        public async Task<IActionResult> Create(int? customerId = null, int? leadId = null)
        {
            var userEmail = User.Identity?.Name;
            await PopulateDropdownsAsync();

            var model = new ActivityViewModel
            {
                CustomerId = customerId,
                LeadId = leadId,
                ActivityDate = DateTime.Now,
                AssignedTo = userEmail
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ActivityViewModel model)
        {
            var userEmail = User.Identity?.Name ?? "system";
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

            if (!ModelState.IsValid)
            {
                await PopulateDropdownsAsync();
                return View(model);
            }

            var activity = new Activity
            {
                ActivityType = model.ActivityType,
                Subject = model.Subject,
                Description = model.Description ?? string.Empty,
                ActivityDate = model.ActivityDate,
                CustomerId = model.CustomerId,
                LeadId = model.LeadId,
                Status = model.Status,
                AssignedTo = string.IsNullOrWhiteSpace(model.AssignedTo) ? userEmail : model.AssignedTo
            };

            var (success, errorMsg, created) = await _activityService.CreateAsync(activity, userEmail, ip);
            if (!success)
            {
                ModelState.AddModelError(string.Empty, errorMsg ?? "Failed to create activity.");
                await PopulateDropdownsAsync();
                return View(model);
            }

            TempData["SuccessMessage"] = "Activity logged successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Delete(int id)
        {
            var userEmail = User.Identity?.Name ?? "system";
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

            var (success, errorMsg) = await _activityService.DeleteAsync(id, userEmail, ip);
            if (!success)
            {
                TempData["ErrorMessage"] = errorMsg ?? "Failed to delete activity.";
            }
            else
            {
                TempData["SuccessMessage"] = "Activity deleted successfully.";
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateDropdownsAsync()
        {
            var customers = await _customerService.GetAllAsync();
            ViewBag.Customers = new SelectList(customers, "CustomerId", "CustomerName");

            var leads = await _leadService.GetAllAsync();
            ViewBag.Leads = new SelectList(leads, "LeadId", "LeadName");
        }
    }
}
