using System;
using System.Collections.Generic;

namespace BeautyHubAPI.Models
{
    public partial class Notification
    {
        public int NotificationId { get; set; }
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public string? NotificationType { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime? ModifyDate { get; set; }
        public string? UserRole { get; set; }
        public string? CreatedBy { get; set; }
    }
}
