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
    [Route("api/customers")]
    public class CustomersApiController : ControllerBase
    {
        private readonly ICustomerService _customerService;

        public CustomersApiController(ICustomerService customerService)
        {
            _customerService = customerService;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<CustomerResponseDto>>>> GetCustomers([FromQuery] string? search = null, [FromQuery] string? status = null)
        {
            var customers = await _customerService.GetAllAsync(search, status);
            var dtos = customers.Select(c => new CustomerResponseDto
            {
                CustomerId = c.CustomerId,
                CustomerCode = c.CustomerCode,
                CustomerName = c.CustomerName,
                Email = c.Email,
                Phone = c.Phone,
                CompanyName = c.CompanyName,
                Address = c.Address,
                City = c.City,
                State = c.State,
                Status = c.Status,
                CreatedDate = c.CreatedDate,
                CreatedBy = c.CreatedBy
            }).ToList();

            return Ok(ApiResponse<List<CustomerResponseDto>>.Ok(dtos));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<CustomerResponseDto>>> GetCustomer(int id)
        {
            var c = await _customerService.GetByIdAsync(id);
            if (c == null)
            {
                return NotFound(ApiResponse<CustomerResponseDto>.Fail($"Customer with ID {id} was not found."));
            }

            var dto = new CustomerResponseDto
            {
                CustomerId = c.CustomerId,
                CustomerCode = c.CustomerCode,
                CustomerName = c.CustomerName,
                Email = c.Email,
                Phone = c.Phone,
                CompanyName = c.CompanyName,
                Address = c.Address,
                City = c.City,
                State = c.State,
                Status = c.Status,
                CreatedDate = c.CreatedDate,
                CreatedBy = c.CreatedBy
            };

            return Ok(ApiResponse<CustomerResponseDto>.Ok(dto));
        }

        [HttpPost]
        public async Task<ActionResult<ApiResponse<CustomerResponseDto>>> CreateCustomer([FromBody] CreateCustomerDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<CustomerResponseDto>.Fail("Invalid customer payload.", errors));
            }

            var customer = new Customer
            {
                CustomerName = dto.CustomerName,
                Email = dto.Email,
                Phone = dto.Phone,
                CompanyName = dto.CompanyName ?? string.Empty,
                Address = dto.Address ?? string.Empty,
                City = dto.City ?? string.Empty,
                State = dto.State ?? string.Empty,
                Status = dto.Status
            };

            var (success, errorMsg, created) = await _customerService.CreateAsync(customer, "api_user", HttpContext.Connection.RemoteIpAddress?.ToString());
            if (!success)
            {
                return Conflict(ApiResponse<CustomerResponseDto>.Fail(errorMsg ?? "Could not create customer."));
            }

            var responseDto = new CustomerResponseDto
            {
                CustomerId = created!.CustomerId,
                CustomerCode = created.CustomerCode,
                CustomerName = created.CustomerName,
                Email = created.Email,
                Phone = created.Phone,
                CompanyName = created.CompanyName,
                Address = created.Address,
                City = created.City,
                State = created.State,
                Status = created.Status,
                CreatedDate = created.CreatedDate,
                CreatedBy = created.CreatedBy
            };

            return CreatedAtAction(nameof(GetCustomer), new { id = responseDto.CustomerId }, ApiResponse<CustomerResponseDto>.Ok(responseDto, "Customer created successfully."));
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<ApiResponse<string>>> UpdateCustomer(int id, [FromBody] UpdateCustomerDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<string>.Fail("Invalid customer payload.", errors));
            }

            var customer = new Customer
            {
                CustomerId = id,
                CustomerName = dto.CustomerName,
                Email = dto.Email,
                Phone = dto.Phone,
                CompanyName = dto.CompanyName ?? string.Empty,
                Address = dto.Address ?? string.Empty,
                City = dto.City ?? string.Empty,
                State = dto.State ?? string.Empty,
                Status = dto.Status
            };

            var (success, errorMsg) = await _customerService.UpdateAsync(customer, "api_user", HttpContext.Connection.RemoteIpAddress?.ToString());
            if (!success)
            {
                return BadRequest(ApiResponse<string>.Fail(errorMsg ?? "Could not update customer."));
            }

            return Ok(ApiResponse<string>.Ok("Customer updated successfully."));
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse<string>>> DeleteCustomer(int id)
        {
            var (success, errorMsg) = await _customerService.DeleteAsync(id, "api_user", HttpContext.Connection.RemoteIpAddress?.ToString());
            if (!success)
            {
                return NotFound(ApiResponse<string>.Fail(errorMsg ?? $"Customer with ID {id} not found."));
            }

            return Ok(ApiResponse<string>.Ok("Customer deleted successfully."));
        }
    }
}
