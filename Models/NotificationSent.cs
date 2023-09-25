using System;
using System.Collections.Generic;

namespace BeautyHubAPI.Models
{
    public partial class NotificationSent
    {
        public int NotificationSentId { get; set; }
        public string UserId { get; set; } = null!;
        public string Title { get; set; } = null!;
        public string Description { get; set; } = null!;
        public bool IsNotificationRead { get; set; }
        public string? NotificationType { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime? ModifyDate { get; set; }

        public virtual UserDetail User { get; set; } = null!;
    }
}
