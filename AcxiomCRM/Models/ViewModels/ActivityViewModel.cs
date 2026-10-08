using System;
using System.ComponentModel.DataAnnotations;

namespace AcxiomCRM.Models.ViewModels
{
    public class ActivityViewModel
    {
        public int ActivityId { get; set; }

        [Required(ErrorMessage = "Activity Type is required.")]
        public string ActivityType { get; set; } = "Call"; // Call, Meeting, Email, Task

        [Required(ErrorMessage = "Subject is required.")]
        [StringLength(200, ErrorMessage = "Subject cannot exceed 200 characters.")]
        public string Subject { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters.")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Activity Date is required.")]
        public DateTime ActivityDate { get; set; } = DateTime.UtcNow;

        public int? CustomerId { get; set; }
        public string? CustomerName { get; set; }

        public int? LeadId { get; set; }
        public string? LeadName { get; set; }

        public string? AssignedTo { get; set; }

        [Required(ErrorMessage = "Status is required.")]
        public string Status { get; set; } = "Completed";
    }
}
