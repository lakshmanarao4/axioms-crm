using System;
using System.ComponentModel.DataAnnotations;

namespace AcxiomCRM.Models.Entities
{
    public class FollowUp
    {
        public int FollowUpId { get; set; }

        public int? CustomerId { get; set; }
        public virtual Customer? Customer { get; set; }

        public int? LeadId { get; set; }
        public virtual Lead? Lead { get; set; }

        [Required]
        public DateTime FollowUpDate { get; set; }

        [Required]
        [MaxLength(50)]
        public string FollowUpType { get; set; } = "Call"; // Call, Email, Meeting, Demo

        [MaxLength(500)]
        public string Remarks { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "Planned"; // Planned, Completed, Missed, Cancelled

        [MaxLength(100)]
        public string AssignedTo { get; set; } = string.Empty;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }
}
