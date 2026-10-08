using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AcxiomCRM.Models.Entities;
using AcxiomCRM.Models.ViewModels;
using AcxiomCRM.Services;

namespace AcxiomCRM.Controllers
{
    [Authorize]
    public class CustomerController : Controller
    {
        private readonly ICustomerService _customerService;

        public CustomerController(ICustomerService customerService)
        {
            _customerService = customerService;
        }

        public async Task<IActionResult> Index(string? search = null, string? status = null)
        {
            var userEmail = User.Identity?.Name;
            var role = User.IsInRole("Admin") ? "Admin" 
                     : User.IsInRole("Manager") ? "Manager" 
                     : "SalesExecutive";

            var customers = await _customerService.GetAllAsync(search, status, userEmail, role);
            ViewBag.CurrentSearch = search;
            ViewBag.CurrentStatus = status;
            return View(customers);
        }

        public async Task<IActionResult> Details(int id)
        {
            var customer = await _customerService.GetByIdAsync(id);
            if (customer == null)
            {
                return NotFound();
            }
            return View(customer);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new CustomerViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CustomerViewModel model)
        {
            var userEmail = User.Identity?.Name ?? "system";
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

            // Server-side validations
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (!await _customerService.IsEmailUniqueAsync(model.Email))
            {
                ModelState.AddModelError(nameof(model.Email), "A customer with this email address already exists.");
                return View(model);
            }

            if (!await _customerService.IsPhoneUniqueAsync(model.Phone))
            {
                ModelState.AddModelError(nameof(model.Phone), "A customer with this phone number already exists.");
                return View(model);
            }

            if (await _customerService.IsDuplicateCustomerAsync(model.CustomerName, model.CompanyName ?? string.Empty))
            {
                ModelState.AddModelError(string.Empty, "A duplicate customer with this name and company already exists.");
                return View(model);
            }

            var customer = new Customer
            {
                CustomerName = model.CustomerName,
                Email = model.Email,
                Phone = model.Phone,
                CompanyName = model.CompanyName ?? string.Empty,
                Address = model.Address ?? string.Empty,
                City = model.City ?? string.Empty,
                State = model.State ?? string.Empty,
                Status = model.Status
            };

            var (success, errorMsg, created) = await _customerService.CreateAsync(customer, userEmail, ip);
            if (!success)
            {
                ModelState.AddModelError(string.Empty, errorMsg ?? "Failed to create customer.");
                return View(model);
            }

            TempData["SuccessMessage"] = $"Customer '{customer.CustomerName}' created successfully ({customer.CustomerCode}).";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var customer = await _customerService.GetByIdAsync(id);
            if (customer == null)
            {
                return NotFound();
            }

            var model = new CustomerViewModel
            {
                CustomerId = customer.CustomerId,
                CustomerCode = customer.CustomerCode,
                CustomerName = customer.CustomerName,
                Email = customer.Email,
                Phone = customer.Phone,
                CompanyName = customer.CompanyName,
                Address = customer.Address,
                City = customer.City,
                State = customer.State,
                Status = customer.Status
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(CustomerViewModel model)
        {
            var userEmail = User.Identity?.Name ?? "system";
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (!await _customerService.IsEmailUniqueAsync(model.Email, model.CustomerId))
            {
                ModelState.AddModelError(nameof(model.Email), "A customer with this email address already exists.");
                return View(model);
            }

            if (!await _customerService.IsPhoneUniqueAsync(model.Phone, model.CustomerId))
            {
                ModelState.AddModelError(nameof(model.Phone), "A customer with this phone number already exists.");
                return View(model);
            }

            var customer = new Customer
            {
                CustomerId = model.CustomerId,
                CustomerName = model.CustomerName,
                Email = model.Email,
                Phone = model.Phone,
                CompanyName = model.CompanyName ?? string.Empty,
                Address = model.Address ?? string.Empty,
                City = model.City ?? string.Empty,
                State = model.State ?? string.Empty,
                Status = model.Status
            };

            var (success, errorMsg) = await _customerService.UpdateAsync(customer, userEmail, ip);
            if (!success)
            {
                ModelState.AddModelError(string.Empty, errorMsg ?? "Failed to update customer.");
                return View(model);
            }

            TempData["SuccessMessage"] = $"Customer '{customer.CustomerName}' updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Delete(int id)
        {
            var userEmail = User.Identity?.Name ?? "system";
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

            var (success, errorMsg) = await _customerService.DeleteAsync(id, userEmail, ip);
            if (!success)
            {
                TempData["ErrorMessage"] = errorMsg ?? "Failed to delete customer.";
            }
            else
            {
                TempData["SuccessMessage"] = "Customer deleted successfully.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> CheckEmail(string email, int? customerId)
        {
            var isUnique = await _customerService.IsEmailUniqueAsync(email, customerId);
            return Json(isUnique);
        }

        [HttpGet]
        public async Task<IActionResult> CheckPhone(string phone, int? customerId)
        {
            var isUnique = await _customerService.IsPhoneUniqueAsync(phone, customerId);
            return Json(isUnique);
        }
    }
}
