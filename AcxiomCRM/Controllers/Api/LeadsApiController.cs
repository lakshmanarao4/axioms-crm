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
    [Route("api/leads")]
    public class LeadsApiController : ControllerBase
    {
        private readonly ILeadService _leadService;

        public LeadsApiController(ILeadService leadService)
        {
            _leadService = leadService;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<LeadResponseDto>>>> GetLeads([FromQuery] string? search = null, [FromQuery] string? status = null)
        {
            var leads = await _leadService.GetAllAsync(search, status);
            var dtos = leads.Select(l => new LeadResponseDto
            {
                LeadId = l.LeadId,
                LeadCode = l.LeadCode,
                LeadName = l.LeadName,
                Email = l.Email,
                Phone = l.Phone,
                CompanyName = l.CompanyName,
                Source = l.Source,
                Status = l.Status,
                ExpectedValue = l.ExpectedValue,
                CreatedDate = l.CreatedDate,
                AssignedTo = l.AssignedTo
            }).ToList();

            return Ok(ApiResponse<List<LeadResponseDto>>.Ok(dtos));
        }

        [HttpPost]
        public async Task<ActionResult<ApiResponse<LeadResponseDto>>> CreateLead([FromBody] CreateLeadDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<LeadResponseDto>.Fail("Invalid lead payload.", errors));
            }

            var lead = new Lead
            {
                LeadName = dto.LeadName,
                Email = dto.Email,
                Phone = dto.Phone,
                CompanyName = dto.CompanyName ?? string.Empty,
                Source = dto.Source,
                Status = dto.Status,
                ExpectedValue = dto.ExpectedValue,
                AssignedTo = dto.AssignedTo ?? string.Empty
            };

            var (success, errorMsg, created) = await _leadService.CreateAsync(lead, "api_user", HttpContext.Connection.RemoteIpAddress?.ToString());
            if (!success)
            {
                return BadRequest(ApiResponse<LeadResponseDto>.Fail(errorMsg ?? "Could not create lead."));
            }

            var responseDto = new LeadResponseDto
            {
                LeadId = created!.LeadId,
                LeadCode = created.LeadCode,
                LeadName = created.LeadName,
                Email = created.Email,
                Phone = created.Phone,
                CompanyName = created.CompanyName,
                Source = created.Source,
                Status = created.Status,
                ExpectedValue = created.ExpectedValue,
                CreatedDate = created.CreatedDate,
                AssignedTo = created.AssignedTo
            };

            return StatusCode(201, ApiResponse<LeadResponseDto>.Ok(responseDto, "Lead created successfully."));
        }
    }
}
