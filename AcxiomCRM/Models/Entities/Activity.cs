using System;
using System.ComponentModel.DataAnnotations;

namespace AcxiomCRM.Models.Entities
{
    public class Activity
    {
        public int ActivityId { get; set; }

        [Required]
        [MaxLength(50)]
        public string ActivityType { get; set; } = "Call"; // Call, Meeting, Email, Task

        [Required]
        [MaxLength(200)]
        public string Subject { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string Description { get; set; } = string.Empty;

        [Required]
        public DateTime ActivityDate { get; set; } = DateTime.UtcNow;

        public int? CustomerId { get; set; }
        public virtual Customer? Customer { get; set; }

        public int? LeadId { get; set; }
        public virtual Lead? Lead { get; set; }

        [MaxLength(100)]
        public string AssignedTo { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "Completed"; // Scheduled, InProgress, Completed, Cancelled
    }
}
