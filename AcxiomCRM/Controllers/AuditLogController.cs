using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AcxiomCRM.Services;

namespace AcxiomCRM.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AuditLogController : Controller
    {
        private readonly IAuditService _auditService;

        public AuditLogController(IAuditService auditService)
        {
            _auditService = auditService;
        }

        public async Task<IActionResult> Index(string? userId = null, string? entityName = null, string? action = null, DateTime? fromDate = null, DateTime? toDate = null)
        {
            var logs = await _auditService.FilterLogsAsync(userId, entityName, action, fromDate, toDate);

            ViewBag.CurrentUserId = userId;
            ViewBag.CurrentEntityName = entityName;
            ViewBag.CurrentAction = action;
            ViewBag.CurrentFromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.CurrentToDate = toDate?.ToString("yyyy-MM-dd");

            return View(logs);
        }
    }
}
