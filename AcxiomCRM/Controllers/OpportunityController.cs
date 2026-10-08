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
    public class OpportunityController : Controller
    {
        private readonly IOpportunityService _opportunityService;
        private readonly ICustomerService _customerService;
        private readonly ILeadService _leadService;

        public OpportunityController(
            IOpportunityService opportunityService,
            ICustomerService customerService,
            ILeadService leadService)
        {
            _opportunityService = opportunityService;
            _customerService = customerService;
            _leadService = leadService;
        }

        public async Task<IActionResult> Index(string? search = null, string? stage = null, string? status = null)
        {
            var userEmail = User.Identity?.Name;
            var role = User.IsInRole("Admin") ? "Admin" 
                     : User.IsInRole("Manager") ? "Manager" 
                     : "SalesExecutive";

            var opps = await _opportunityService.GetAllAsync(search, stage, status, userEmail, role);
            ViewBag.CurrentSearch = search;
            ViewBag.CurrentStage = stage;
            ViewBag.CurrentStatus = status;
            return View(opps);
        }

        public async Task<IActionResult> Details(int id)
        {
            var opp = await _opportunityService.GetByIdAsync(id);
            if (opp == null)
            {
                return NotFound();
            }
            return View(opp);
        }

        [HttpGet]
        public async Task<IActionResult> Create(int? customerId = null, int? leadId = null)
        {
            var userEmail = User.Identity?.Name;
            await PopulateDropdownsAsync();

            var model = new OpportunityViewModel
            {
                CustomerId = customerId ?? 0,
                LeadId = leadId,
                AssignedTo = userEmail,
                ExpectedCloseDate = DateTime.Today.AddDays(30)
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(OpportunityViewModel model)
        {
            var userEmail = User.Identity?.Name ?? "system";
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

            // Server-side & Business Validations
            if (model.Amount <= 0)
            {
                ModelState.AddModelError(nameof(model.Amount), "Opportunity Amount must be greater than 0.");
            }

            if (model.Probability < 0 || model.Probability > 100)
            {
                ModelState.AddModelError(nameof(model.Probability), "Probability must be between 0 and 100.");
            }

            var isClosed = model.Stage == "Won" || model.Stage == "Lost";
            if (!isClosed && model.ExpectedCloseDate.Date < DateTime.UtcNow.Date)
            {
                ModelState.AddModelError(nameof(model.ExpectedCloseDate), "Expected Close Date cannot be in the past.");
            }

            if (!ModelState.IsValid)
            {
                await PopulateDropdownsAsync();
                return View(model);
            }

            var opp = new Opportunity
            {
                OpportunityName = model.OpportunityName,
                CustomerId = model.CustomerId,
                LeadId = model.LeadId,
                Amount = model.Amount,
                Stage = model.Stage,
                Probability = model.Probability,
                ExpectedCloseDate = model.ExpectedCloseDate,
                AssignedTo = string.IsNullOrWhiteSpace(model.AssignedTo) ? userEmail : model.AssignedTo
            };

            var (success, errorMsg, created) = await _opportunityService.CreateAsync(opp, userEmail, ip);
            if (!success)
            {
                ModelState.AddModelError(string.Empty, errorMsg ?? "Failed to create opportunity.");
                await PopulateDropdownsAsync();
                return View(model);
            }

            TempData["SuccessMessage"] = $"Opportunity '{opp.OpportunityName}' created successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var opp = await _opportunityService.GetByIdAsync(id);
            if (opp == null)
            {
                return NotFound();
            }

            await PopulateDropdownsAsync();

            var model = new OpportunityViewModel
            {
                OpportunityId = opp.OpportunityId,
                OpportunityName = opp.OpportunityName,
                CustomerId = opp.CustomerId,
                LeadId = opp.LeadId,
                Amount = opp.Amount,
                Stage = opp.Stage,
                Probability = opp.Probability,
                ExpectedCloseDate = opp.ExpectedCloseDate,
                Status = opp.Status,
                AssignedTo = opp.AssignedTo,
                CreatedDate = opp.CreatedDate
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(OpportunityViewModel model)
        {
            var userEmail = User.Identity?.Name ?? "system";
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

            if (model.Amount <= 0)
            {
                ModelState.AddModelError(nameof(model.Amount), "Opportunity Amount must be greater than 0.");
            }

            if (model.Probability < 0 || model.Probability > 100)
            {
                ModelState.AddModelError(nameof(model.Probability), "Probability must be between 0 and 100.");
            }

            var isClosed = model.Stage == "Won" || model.Stage == "Lost";
            if (!isClosed && model.ExpectedCloseDate.Date < DateTime.UtcNow.Date)
            {
                ModelState.AddModelError(nameof(model.ExpectedCloseDate), "Expected Close Date cannot be in the past.");
            }

            if (!ModelState.IsValid)
            {
                await PopulateDropdownsAsync();
                return View(model);
            }

            var opp = new Opportunity
            {
                OpportunityId = model.OpportunityId,
                OpportunityName = model.OpportunityName,
                CustomerId = model.CustomerId,
                LeadId = model.LeadId,
                Amount = model.Amount,
                Stage = model.Stage,
                Probability = model.Probability,
                ExpectedCloseDate = model.ExpectedCloseDate,
                AssignedTo = model.AssignedTo ?? userEmail
            };

            var (success, errorMsg) = await _opportunityService.UpdateAsync(opp, userEmail, ip);
            if (!success)
            {
                ModelState.AddModelError(string.Empty, errorMsg ?? "Failed to update opportunity.");
                await PopulateDropdownsAsync();
                return View(model);
            }

            TempData["SuccessMessage"] = $"Opportunity '{opp.OpportunityName}' updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Delete(int id)
        {
            var userEmail = User.Identity?.Name ?? "system";
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

            var (success, errorMsg) = await _opportunityService.DeleteAsync(id, userEmail, ip);
            if (!success)
            {
                TempData["ErrorMessage"] = errorMsg ?? "Failed to delete opportunity.";
            }
            else
            {
                TempData["SuccessMessage"] = "Opportunity deleted successfully.";
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
