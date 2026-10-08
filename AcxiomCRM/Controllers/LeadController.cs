using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AcxiomCRM.Models.Entities;
using AcxiomCRM.Models.ViewModels;
using AcxiomCRM.Services;

namespace AcxiomCRM.Controllers
{
    [Authorize]
    public class LeadController : Controller
    {
        private readonly ILeadService _leadService;

        public LeadController(ILeadService leadService)
        {
            _leadService = leadService;
        }

        public async Task<IActionResult> Index(string? search = null, string? status = null, string? assignedTo = null)
        {
            var userEmail = User.Identity?.Name;
            var role = User.IsInRole("Admin") ? "Admin" 
                     : User.IsInRole("Manager") ? "Manager" 
                     : "SalesExecutive";

            var leads = await _leadService.GetAllAsync(search, status, assignedTo, userEmail, role);
            ViewBag.CurrentSearch = search;
            ViewBag.CurrentStatus = status;
            ViewBag.CurrentAssignedTo = assignedTo;
            return View(leads);
        }

        public async Task<IActionResult> Details(int id)
        {
            var lead = await _leadService.GetByIdAsync(id);
            if (lead == null)
            {
                return NotFound();
            }
            return View(lead);
        }

        [HttpGet]
        public IActionResult Create()
        {
            var userEmail = User.Identity?.Name;
            return View(new LeadViewModel { AssignedTo = userEmail });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(LeadViewModel model)
        {
            var userEmail = User.Identity?.Name ?? "system";
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var lead = new Lead
            {
                LeadName = model.LeadName,
                Email = model.Email,
                Phone = model.Phone,
                CompanyName = model.CompanyName ?? string.Empty,
                Source = model.Source,
                Status = model.Status,
                ExpectedValue = model.ExpectedValue,
                AssignedTo = string.IsNullOrWhiteSpace(model.AssignedTo) ? userEmail : model.AssignedTo
            };

            var (success, errorMsg, created) = await _leadService.CreateAsync(lead, userEmail, ip);
            if (!success)
            {
                ModelState.AddModelError(string.Empty, errorMsg ?? "Failed to create lead.");
                return View(model);
            }

            TempData["SuccessMessage"] = $"Lead '{lead.LeadName}' created successfully ({lead.LeadCode}).";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var lead = await _leadService.GetByIdAsync(id);
            if (lead == null)
            {
                return NotFound();
            }

            var model = new LeadViewModel
            {
                LeadId = lead.LeadId,
                LeadCode = lead.LeadCode,
                LeadName = lead.LeadName,
                Email = lead.Email,
                Phone = lead.Phone,
                CompanyName = lead.CompanyName,
                Source = lead.Source,
                Status = lead.Status,
                ExpectedValue = lead.ExpectedValue,
                AssignedTo = lead.AssignedTo,
                CreatedDate = lead.CreatedDate
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(LeadViewModel model)
        {
            var userEmail = User.Identity?.Name ?? "system";
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var lead = new Lead
            {
                LeadId = model.LeadId,
                LeadName = model.LeadName,
                Email = model.Email,
                Phone = model.Phone,
                CompanyName = model.CompanyName ?? string.Empty,
                Source = model.Source,
                Status = model.Status,
                ExpectedValue = model.ExpectedValue,
                AssignedTo = model.AssignedTo ?? userEmail
            };

            var (success, errorMsg) = await _leadService.UpdateAsync(lead, userEmail, ip);
            if (!success)
            {
                ModelState.AddModelError(string.Empty, errorMsg ?? "Failed to update lead.");
                return View(model);
            }

            TempData["SuccessMessage"] = $"Lead '{lead.LeadName}' updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Delete(int id)
        {
            var userEmail = User.Identity?.Name ?? "system";
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

            var (success, errorMsg) = await _leadService.DeleteAsync(id, userEmail, ip);
            if (!success)
            {
                TempData["ErrorMessage"] = errorMsg ?? "Failed to delete lead.";
            }
            else
            {
                TempData["SuccessMessage"] = "Lead deleted successfully.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Convert(int id)
        {
            var userEmail = User.Identity?.Name ?? "system";
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

            var (success, errorMsg, custId, oppId) = await _leadService.ConvertToCustomerAsync(id, userEmail, ip);
            if (!success)
            {
                TempData["ErrorMessage"] = errorMsg ?? "Failed to convert lead.";
                return RedirectToAction(nameof(Details), new { id });
            }

            TempData["SuccessMessage"] = $"Lead successfully converted! Created Customer ID #{custId}." + 
                (oppId.HasValue ? $" Created Opportunity ID #{oppId}." : "");

            return RedirectToAction("Details", "Customer", new { id = custId });
        }
    }
}
