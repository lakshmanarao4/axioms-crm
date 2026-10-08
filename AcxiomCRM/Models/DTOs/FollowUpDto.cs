using System;
using System.ComponentModel.DataAnnotations;

namespace AcxiomCRM.Models.DTOs
{
    public class FollowUpResponseDto
    {
        public int FollowUpId { get; set; }
        public int? CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public int? LeadId { get; set; }
        public string? LeadName { get; set; }
        public DateTime FollowUpDate { get; set; }
        public string FollowUpType { get; set; } = string.Empty;
        public string? Remarks { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? AssignedTo { get; set; }
    }

    public class CreateFollowUpDto
    {
        public int? CustomerId { get; set; }
        public int? LeadId { get; set; }

        [Required(ErrorMessage = "Follow-up Date is required.")]
        public DateTime FollowUpDate { get; set; }

        [Required(ErrorMessage = "Follow-up Type is required.")]
        public string FollowUpType { get; set; } = "Call";

        public string? Remarks { get; set; }
        public string Status { get; set; } = "Planned";
        public string? AssignedTo { get; set; }
    }
}
