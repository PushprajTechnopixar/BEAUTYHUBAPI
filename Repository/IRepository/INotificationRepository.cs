using BeautyHubAPI.Common;
using BeautyHubAPI.Models.Dtos;
using BeautyHubAPI.Models.Helper;
using BeautyHubAPI.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static BeautyHubAPI.Common.GlobalVariables;
using System.Net;

namespace BeautyHubAPI.Repository.IRepository
{
    public interface INotificationRepository
    {
        Task<Object> GetBroadcastNotificationList([FromQuery] int pageNumber, int pageSize, string? searchByRole, string? searchQuery, string currentUserId);
        Task<Object> BroadcastNotification([FromBody] AddNotificationDTO model, string currentUserId);
        Task<Object> DeleteBroadcastNotification([FromQuery] int? notificationId, string currentUserId);
        Task<Object> GetNotificationList([FromQuery] int pageNumber, int pageSize, string? searchQuery, string currentUserId);
        Task<Object> ReadNotification(int? notificationSentId, string currentUserId);
        Task<Object> DeleteNotification([FromQuery] int? notificationSentId, string currentUserId);
        Task<Object> UpdateFCMToken(FCMTokenDTO model, string currentUserId);
        Task<Object> GetNotificationCount(string currentUserId);
    }
}
