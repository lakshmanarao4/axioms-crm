using System;
using System.ComponentModel.DataAnnotations;

namespace AcxiomCRM.Models.DTOs
{
    public class LeadResponseDto
    {
        public int LeadId { get; set; }
        public string LeadCode { get; set; } = string.Empty;
        public string LeadName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? CompanyName { get; set; }
        public string Source { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public decimal ExpectedValue { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? AssignedTo { get; set; }
    }

    public class CreateLeadDto
    {
        [Required(ErrorMessage = "Lead Name is required.")]
        [StringLength(150)]
        public string LeadName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone is required.")]
        [RegularExpression(@"^[0-9]{10}$", ErrorMessage = "Enter a valid 10-digit phone number.")]
        public string Phone { get; set; } = string.Empty;

        [StringLength(150)]
        public string? CompanyName { get; set; }

        public string Source { get; set; } = "Website";
        public string Status { get; set; } = "New";

        [Range(0, 999999999.99, ErrorMessage = "Expected value must be a non-negative number.")]
        public decimal ExpectedValue { get; set; } = 0.00m;

        public string? AssignedTo { get; set; }
    }
}
