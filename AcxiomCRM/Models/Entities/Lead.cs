using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AcxiomCRM.Models.Entities
{
    public class Lead
    {
        public int LeadId { get; set; }

        [Required]
        [MaxLength(50)]
        public string LeadCode { get; set; } = string.Empty;

        [Required]
        [MaxLength(150)]
        public string LeadName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [MaxLength(150)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string Phone { get; set; } = string.Empty;

        [MaxLength(150)]
        public string CompanyName { get; set; } = string.Empty;

        [MaxLength(100)]
        public string Source { get; set; } = "Website"; // Website, Referral, Cold Call, Campaign, Partner

        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "New"; // New, Contacted, Qualified, Unqualified, Converted, Lost

        [Column(TypeName = "decimal(18,2)")]
        public decimal ExpectedValue { get; set; } = 0.00m;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        [MaxLength(100)]
        public string AssignedTo { get; set; } = string.Empty; // User ID or Email

        // Navigation
        public virtual ICollection<Opportunity> Opportunities { get; set; } = new List<Opportunity>();
        public virtual ICollection<FollowUp> FollowUps { get; set; } = new List<FollowUp>();
        public virtual ICollection<Activity> Activities { get; set; } = new List<Activity>();
    }
}
