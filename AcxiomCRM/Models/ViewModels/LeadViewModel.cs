using System;
using System.ComponentModel.DataAnnotations;

namespace AcxiomCRM.Models.ViewModels
{
    public class LeadViewModel
    {
        public int LeadId { get; set; }

        public string? LeadCode { get; set; }

        [Required(ErrorMessage = "Lead Name is required.")]
        [StringLength(150, ErrorMessage = "Lead Name cannot exceed 150 characters.")]
        public string LeadName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        [StringLength(150, ErrorMessage = "Email cannot exceed 150 characters.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone is required.")]
        [RegularExpression(@"^[0-9]{10}$", ErrorMessage = "Enter a valid 10-digit phone number.")]
        public string Phone { get; set; } = string.Empty;

        [StringLength(150, ErrorMessage = "Company Name cannot exceed 150 characters.")]
        public string? CompanyName { get; set; }

        [Required(ErrorMessage = "Source is required.")]
        public string Source { get; set; } = "Website";

        [Required(ErrorMessage = "Status is required.")]
        public string Status { get; set; } = "New";

        [Required(ErrorMessage = "Expected Value is required.")]
        [Range(0, 999999999.99, ErrorMessage = "Expected value must be a non-negative number.")]
        public decimal ExpectedValue { get; set; } = 0.00m;

        public string? AssignedTo { get; set; }

        public DateTime? CreatedDate { get; set; }
    }
}
