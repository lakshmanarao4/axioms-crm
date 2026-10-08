using System;
using System.ComponentModel.DataAnnotations;

namespace AcxiomCRM.Models.Entities
{
    public class AuditLog
    {
        public int AuditLogId { get; set; }

        [Required]
        [MaxLength(100)]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Action { get; set; } = string.Empty; // Login, FailedLogin, Create, Update, Delete, RoleChange, Security, Convert

        [Required]
        [MaxLength(100)]
        public string EntityName { get; set; } = string.Empty; // Customer, Lead, Opportunity, FollowUp, User, Auth

        [MaxLength(50)]
        public string RecordId { get; set; } = string.Empty;

        public string? OldValue { get; set; }

        public string? NewValue { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        [MaxLength(50)]
        public string IpAddress { get; set; } = "127.0.0.1";
    }
}
