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
    public class FollowUpController : Controller
    {
        private readonly IFollowUpService _followUpService;
        private readonly ICustomerService _customerService;
        private readonly ILeadService _leadService;

        public FollowUpController(
            IFollowUpService followUpService,
            ICustomerService customerService,
            ILeadService leadService)
        {
            _followUpService = followUpService;
            _customerService = customerService;
            _leadService = leadService;
        }

        public async Task<IActionResult> Index(string? search = null, string? status = null, string? type = null)
        {
            var userEmail = User.Identity?.Name;
            var role = User.IsInRole("Admin") ? "Admin" 
                     : User.IsInRole("Manager") ? "Manager" 
                     : "SalesExecutive";

            var followUps = await _followUpService.GetAllAsync(search, status, type, userEmail, role);
            ViewBag.CurrentSearch = search;
            ViewBag.CurrentStatus = status;
            ViewBag.CurrentType = type;
            return View(followUps);
        }

        [HttpGet]
        public async Task<IActionResult> Create(int? customerId = null, int? leadId = null)
        {
            var userEmail = User.Identity?.Name;
            await PopulateDropdownsAsync();

            var model = new FollowUpViewModel
            {
                CustomerId = customerId,
                LeadId = leadId,
                FollowUpDate = DateTime.Today,
                AssignedTo = userEmail
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(FollowUpViewModel model)
        {
            var userEmail = User.Identity?.Name ?? "system";
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

            // Business Validation: FollowUpDate cannot be earlier than today for planned/new
            if (model.Status == "Planned" && model.FollowUpDate.Date < DateTime.UtcNow.Date)
            {
                ModelState.AddModelError(nameof(model.FollowUpDate), "Follow-up date cannot be earlier than today.");
            }

            if (model.CustomerId == null && model.LeadId == null)
            {
                ModelState.AddModelError(string.Empty, "Follow-up must be linked to either a Customer or a Lead.");
            }

            if (!ModelState.IsValid)
            {
                await PopulateDropdownsAsync();
                return View(model);
            }

            var followUp = new FollowUp
            {
                CustomerId = model.CustomerId,
                LeadId = model.LeadId,
                FollowUpDate = model.FollowUpDate,
                FollowUpType = model.FollowUpType,
                Remarks = model.Remarks ?? string.Empty,
                Status = model.Status,
                AssignedTo = string.IsNullOrWhiteSpace(model.AssignedTo) ? userEmail : model.AssignedTo
            };

            var (success, errorMsg, created) = await _followUpService.CreateAsync(followUp, userEmail, ip);
            if (!success)
            {
                ModelState.AddModelError(string.Empty, errorMsg ?? "Failed to create follow-up.");
                await PopulateDropdownsAsync();
                return View(model);
            }

            TempData["SuccessMessage"] = "Follow-up scheduled successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Complete(int id, string remarks)
        {
            var userEmail = User.Identity?.Name ?? "system";
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

            var (success, errorMsg) = await _followUpService.CompleteAsync(id, remarks, userEmail, ip);
            if (!success)
            {
                TempData["ErrorMessage"] = errorMsg ?? "Failed to mark follow-up as completed.";
            }
            else
            {
                TempData["SuccessMessage"] = "Follow-up marked as completed.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reschedule(int id, DateTime newDate, string remarks)
        {
            var userEmail = User.Identity?.Name ?? "system";
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

            var (success, errorMsg) = await _followUpService.RescheduleAsync(id, newDate, remarks, userEmail, ip);
            if (!success)
            {
                TempData["ErrorMessage"] = errorMsg ?? "Failed to reschedule follow-up.";
            }
            else
            {
                TempData["SuccessMessage"] = "Follow-up rescheduled successfully.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var userEmail = User.Identity?.Name ?? "system";
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

            var (success, errorMsg) = await _followUpService.DeleteAsync(id, userEmail, ip);
            if (!success)
            {
                TempData["ErrorMessage"] = errorMsg ?? "Failed to delete follow-up.";
            }
            else
            {
                TempData["SuccessMessage"] = "Follow-up deleted successfully.";
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
