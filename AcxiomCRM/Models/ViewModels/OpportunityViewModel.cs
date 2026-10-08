using System;
using System.ComponentModel.DataAnnotations;

namespace AcxiomCRM.Models.ViewModels
{
    public class OpportunityViewModel
    {
        public int OpportunityId { get; set; }

        [Required(ErrorMessage = "Opportunity Name is required.")]
        [StringLength(150, ErrorMessage = "Opportunity Name cannot exceed 150 characters.")]
        public string OpportunityName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Customer is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "Please select a valid Customer.")]
        public int CustomerId { get; set; }

        public string? CustomerName { get; set; }

        public int? LeadId { get; set; }
        public string? LeadName { get; set; }

        [Required(ErrorMessage = "Opportunity Amount is required.")]
        [Range(0.01, 999999999.99, ErrorMessage = "Opportunity Amount must be greater than 0.")]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "Stage is required.")]
        public string Stage { get; set; } = "Qualification"; // Qualification, Proposal, Negotiation, Won, Lost

        [Required(ErrorMessage = "Probability is required.")]
        [Range(0, 100, ErrorMessage = "Probability must be between 0 and 100.")]
        public int Probability { get; set; } = 20;

        [Required(ErrorMessage = "Expected Close Date is required.")]
        [DataType(DataType.Date)]
        public DateTime ExpectedCloseDate { get; set; } = DateTime.Today.AddDays(30);

        public string Status { get; set; } = "Open";

        public string? AssignedTo { get; set; }

        public DateTime? CreatedDate { get; set; }

        public decimal WeightedValue => Amount * ((decimal)Probability / 100m);
    }
}
