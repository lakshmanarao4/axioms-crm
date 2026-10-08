using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using AcxiomCRM.Models.DTOs;
using AcxiomCRM.Models.Entities;
using AcxiomCRM.Services;

namespace AcxiomCRM.Controllers.Api
{
    [ApiController]
    [Route("api/opportunities")]
    public class OpportunitiesApiController : ControllerBase
    {
        private readonly IOpportunityService _opportunityService;

        public OpportunitiesApiController(IOpportunityService opportunityService)
        {
            _opportunityService = opportunityService;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<OpportunityResponseDto>>>> GetOpportunities([FromQuery] string? search = null, [FromQuery] string? stage = null, [FromQuery] string? status = null)
        {
            var opps = await _opportunityService.GetAllAsync(search, stage, status);
            var dtos = opps.Select(o => new OpportunityResponseDto
            {
                OpportunityId = o.OpportunityId,
                OpportunityName = o.OpportunityName,
                CustomerId = o.CustomerId,
                CustomerName = o.Customer?.CustomerName,
                LeadId = o.LeadId,
                Amount = o.Amount,
                Stage = o.Stage,
                Probability = o.Probability,
                ExpectedCloseDate = o.ExpectedCloseDate,
                Status = o.Status,
                WeightedValue = o.WeightedValue,
                AssignedTo = o.AssignedTo,
                CreatedDate = o.CreatedDate
            }).ToList();

            return Ok(ApiResponse<List<OpportunityResponseDto>>.Ok(dtos));
        }

        [HttpPost]
        public async Task<ActionResult<ApiResponse<OpportunityResponseDto>>> CreateOpportunity([FromBody] CreateOpportunityDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<OpportunityResponseDto>.Fail("Invalid opportunity payload.", errors));
            }

            var opp = new Opportunity
            {
                OpportunityName = dto.OpportunityName,
                CustomerId = dto.CustomerId,
                LeadId = dto.LeadId,
                Amount = dto.Amount,
                Stage = dto.Stage,
                Probability = dto.Probability,
                ExpectedCloseDate = dto.ExpectedCloseDate,
                AssignedTo = dto.AssignedTo ?? string.Empty
            };

            var (success, errorMsg, created) = await _opportunityService.CreateAsync(opp, "api_user", HttpContext.Connection.RemoteIpAddress?.ToString());
            if (!success)
            {
                return BadRequest(ApiResponse<OpportunityResponseDto>.Fail(errorMsg ?? "Could not create opportunity."));
            }

            var responseDto = new OpportunityResponseDto
            {
                OpportunityId = created!.OpportunityId,
                OpportunityName = created.OpportunityName,
                CustomerId = created.CustomerId,
                LeadId = created.LeadId,
                Amount = created.Amount,
                Stage = created.Stage,
                Probability = created.Probability,
                ExpectedCloseDate = created.ExpectedCloseDate,
                Status = created.Status,
                WeightedValue = created.WeightedValue,
                AssignedTo = created.AssignedTo,
                CreatedDate = created.CreatedDate
            };

            return StatusCode(201, ApiResponse<OpportunityResponseDto>.Ok(responseDto, "Opportunity created successfully."));
        }
    }
}
