using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AcxiomCRM.Models.Entities
{
    public class Opportunity
    {
        public int OpportunityId { get; set; }

        [Required]
        [MaxLength(150)]
        public string OpportunityName { get; set; } = string.Empty;

        [Required]
        public int CustomerId { get; set; }
        public virtual Customer? Customer { get; set; }

        public int? LeadId { get; set; }
        public virtual Lead? Lead { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Opportunity Amount must be greater than 0.")]
        public decimal Amount { get; set; }

        [Required]
        [MaxLength(50)]
        public string Stage { get; set; } = "Qualification"; // Qualification, Proposal, Negotiation, Won, Lost

        [Required]
        [Range(0, 100, ErrorMessage = "Probability must be between 0 and 100.")]
        public int Probability { get; set; } = 20;

        [Required]
        public DateTime ExpectedCloseDate { get; set; }

        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "Open"; // Open, Won, Lost

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        [MaxLength(100)]
        public string AssignedTo { get; set; } = string.Empty;

        [NotMapped]
        public decimal WeightedValue => Amount * ((decimal)Probability / 100m);
    }
}
