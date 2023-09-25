using System;
using System.ComponentModel.DataAnnotations;

namespace BeautyHubAPI.Models.Dtos
{
    public class NotificationDTO
    {
        public int notificationId { get; set; }
        public string title { get; set; }
        public string description { get; set; }
        public string? notificationType { get; set; }
        public string? userRole { get; set; }
        public DateTime CreateDate { get; set; }
    }

    public class AddNotificationDTO
    {
        public string title { get; set; }
        public string description { get; set; }
        public string sendToRole { get; set; }
    }

    public class UserStatus
    {
        public int userId { get; set; }
        public bool? notificationStatus { get; set; }
    }
    public class FCMTokenDTO
    {
        public string fcmToken { get; set; }
    }
    public class NotificationSentDTO
    {
        public int notificationSentId { get; set; }
        public string userId { get; set; }
        public string title { get; set; }
        public string description { get; set; }
        public bool isNotificationRead { get; set; }
        public string? notificationType { get; set; }
        public string createDate { get; set; }
    }


}