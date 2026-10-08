using System;
using System.ComponentModel.DataAnnotations;

namespace AcxiomCRM.Models.ViewModels
{
    public class FollowUpViewModel
    {
        public int FollowUpId { get; set; }

        public int? CustomerId { get; set; }
        public string? CustomerName { get; set; }

        public int? LeadId { get; set; }
        public string? LeadName { get; set; }

        [Required(ErrorMessage = "Follow-up Date is required.")]
        [DataType(DataType.Date)]
        public DateTime FollowUpDate { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "Follow-up Type is required.")]
        public string FollowUpType { get; set; } = "Call"; // Call, Email, Meeting, Demo

        [StringLength(500, ErrorMessage = "Remarks cannot exceed 500 characters.")]
        public string? Remarks { get; set; }

        [Required(ErrorMessage = "Status is required.")]
        public string Status { get; set; } = "Planned"; // Planned, Completed, Missed, Cancelled

        public string? AssignedTo { get; set; }

        public DateTime? CreatedDate { get; set; }
    }
}
