using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Data;
using AcxiomCRM.Models.DTOs;
using AcxiomCRM.Models.Entities;
using AcxiomCRM.Services;

namespace AcxiomCRM.Controllers.Api
{
    [ApiController]
    [Route("api/followups")]
    public class FollowUpsApiController : ControllerBase
    {
        private readonly IFollowUpService _followUpService;

        public FollowUpsApiController(IFollowUpService followUpService)
        {
            _followUpService = followUpService;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<FollowUpResponseDto>>>> GetFollowUps([FromQuery] string? status = null)
        {
            var followUps = await _followUpService.GetAllAsync(status: status);
            var dtos = followUps.Select(f => new FollowUpResponseDto
            {
                FollowUpId = f.FollowUpId,
                CustomerId = f.CustomerId,
                CustomerName = f.Customer?.CustomerName,
                LeadId = f.LeadId,
                LeadName = f.Lead?.LeadName,
                FollowUpDate = f.FollowUpDate,
                FollowUpType = f.FollowUpType,
                Remarks = f.Remarks,
                Status = f.Status,
                AssignedTo = f.AssignedTo
            }).ToList();

            return Ok(ApiResponse<List<FollowUpResponseDto>>.Ok(dtos));
        }

        [HttpPost]
        public async Task<ActionResult<ApiResponse<FollowUpResponseDto>>> CreateFollowUp([FromBody] CreateFollowUpDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<FollowUpResponseDto>.Fail("Invalid follow-up payload.", errors));
            }

            var followUp = new FollowUp
            {
                CustomerId = dto.CustomerId,
                LeadId = dto.LeadId,
                FollowUpDate = dto.FollowUpDate,
                FollowUpType = dto.FollowUpType,
                Remarks = dto.Remarks ?? string.Empty,
                Status = dto.Status,
                AssignedTo = dto.AssignedTo ?? string.Empty
            };

            var (success, errorMsg, created) = await _followUpService.CreateAsync(followUp, "api_user", HttpContext.Connection.RemoteIpAddress?.ToString());
            if (!success)
            {
                return BadRequest(ApiResponse<FollowUpResponseDto>.Fail(errorMsg ?? "Could not create follow-up."));
            }

            var responseDto = new FollowUpResponseDto
            {
                FollowUpId = created!.FollowUpId,
                CustomerId = created.CustomerId,
                LeadId = created.LeadId,
                FollowUpDate = created.FollowUpDate,
                FollowUpType = created.FollowUpType,
                Remarks = created.Remarks,
                Status = created.Status,
                AssignedTo = created.AssignedTo
            };

            return StatusCode(201, ApiResponse<FollowUpResponseDto>.Ok(responseDto, "Follow-up created successfully."));
        }
    }

    [ApiController]
    [Route("api/reports")]
    public class ReportsApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ReportsApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("pipeline")]
        public async Task<ActionResult<ApiResponse<object>>> GetPipelineReport()
        {
            var opps = await _context.Opportunities.ToListAsync();
            var stages = new[] { "Qualification", "Proposal", "Negotiation", "Won", "Lost" };

            var summary = stages.Select(s => new
            {
                Stage = s,
                Count = opps.Count(o => string.Equals(o.Stage, s, System.StringComparison.OrdinalIgnoreCase)),
                TotalAmount = opps.Where(o => string.Equals(o.Stage, s, System.StringComparison.OrdinalIgnoreCase)).Sum(o => o.Amount),
                WeightedAmount = opps.Where(o => string.Equals(o.Stage, s, System.StringComparison.OrdinalIgnoreCase)).Sum(o => o.WeightedValue)
            }).ToList();

            var totalPipeline = opps.Where(o => o.Stage != "Won" && o.Stage != "Lost").Sum(o => o.Amount);

            return Ok(ApiResponse<object>.Ok(new { Stages = summary, TotalOpenPipeline = totalPipeline }));
        }
    }
}
