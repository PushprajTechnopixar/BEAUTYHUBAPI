
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Xml;
using BeautyHubAPI.Repository;
using System.Xml.Linq;
using System.Linq;
using BeautyHubAPI.Models.Dtos;
using BeautyHubAPI.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using AutoMapper.QueryableExtensions;
using AutoMapper;
using System.Collections.Generic;
using System;
using BeautyHubAPI.Firebase;
using BeautyHubAPI.Data;
using BeautyHubAPI.Helpers;
using static BeautyHubAPI.Common.GlobalVariables;
using BeautyHubAPI.Models;
using Microsoft.AspNetCore.Identity;
using BeautyHubAPI.Models.Helper;
using System.Net;
using BeautyHubAPI.Common;

namespace BeautyHubAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IConfiguration _config;
        private readonly IMapper _mapper;
        protected APIResponse _response;
        private readonly IMobileMessagingClient _mobileMessagingClient;
        public NotificationController(ApplicationDbContext context, RoleManager<IdentityRole> roleManager, UserManager<ApplicationUser> userManager, IConfiguration config, IMapper mapper, IMobileMessagingClient mobileMessagingClient)
        {
            _context = context;
            _config = config;
            _mapper = mapper;
            _mobileMessagingClient = mobileMessagingClient;
            _userManager = userManager;
            _response = new();
            _roleManager = roleManager;
        }

        #region GetBroadcastNotificationList
        /// <summary>
        /// get notification list for admin.
        /// </summary>        
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Authorize(Roles = "SuperAdmin,Vendor")]
        [HttpGet]
        [Route("GetBroadcastNotificationList")]
        public async Task<IActionResult> GetBroadcastNotificationList([FromQuery] int pageNumber, int pageSize, string? searchByRole, string? searchQuery)
        {
            try
            {
                var currentUserId = HttpContext.User.Claims.First().Value;
                var currentUserDetail = _userManager.FindByIdAsync(currentUserId).GetAwaiter().GetResult();
                if (currentUserDetail == null)
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = "Token expired.";
                    return Ok(_response);
                }

                var getNotification = await _context.Notification.Where(u => u.CreatedBy == currentUserId)
                    .ProjectTo<NotificationDTO>(_mapper.ConfigurationProvider)
                    .OrderByDescending(i => i.CreateDate)
                    .ToListAsync();

                if (!string.IsNullOrEmpty(searchByRole))
                {
                    getNotification = getNotification.Where(x => (x.userRole?.IndexOf(searchByRole, StringComparison.OrdinalIgnoreCase) >= 0)
                   ).ToList();
                }
                if (!string.IsNullOrEmpty(searchQuery))
                {
                    getNotification = getNotification.Where(x => (x.title?.IndexOf(searchQuery, StringComparison.OrdinalIgnoreCase) >= 0)
                    || (x.description?.IndexOf(searchQuery, StringComparison.OrdinalIgnoreCase) >= 0)
                   ).ToList();
                }

                FilterationListDTO model = new FilterationListDTO();
                model.pageNumber = (pageNumber <= 0) ? 1 : pageNumber;
                model.pageSize = (pageSize <= 0) ? 10 : pageSize;

                // Get's No of Rows Count
                int count = getNotification.Count();

                // Parameter is passed from Query string if it is null then it default Value will be pageNumber:1
                int CurrentPage = model.pageNumber;

                // Parameter is passed from Query string if it is null then it default Value will be pageSize:20
                int PageSize = model.pageSize;

                // Display TotalCount to Records to User
                int TotalCount = count;

                // Calculating Totalpage by Dividing (No of Records / Pagesize)
                int TotalPages = (int)Math.Ceiling(count / (double)PageSize);

                // Returns List of Customer after applying Paging
                var items = getNotification
                    .Skip((CurrentPage - 1) * PageSize)
                    .Take(PageSize)
                    .ToList();

                // if CurrentPage is greater than 1 means it has previousPage
                var previousPage = CurrentPage > 1 ? "Yes" : "No";

                // if TotalPages is greater than CurrentPage means it has nextPage
                var nextPage = CurrentPage < TotalPages ? "Yes" : "No";

                // Returing List of Customers Collections
                FilterationResponseModel<NotificationDTO> obj1 =
                    new FilterationResponseModel<NotificationDTO>();
                obj1.totalCount = TotalCount;
                obj1.pageSize = PageSize;
                obj1.currentPage = CurrentPage;
                obj1.totalPages = TotalPages;
                obj1.previousPage = previousPage;
                obj1.nextPage = nextPage;
                obj1.searchQuery = string.IsNullOrEmpty(model.searchQuery) ? "no parameter passed" : model.searchQuery;
                obj1.dataList = items.ToList();

                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Data = obj1;
                _response.Messages = "Notification" + ResponseMessages.msgListFoundSuccess;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.IsSuccess = false;
                _response.Messages = ex.Message;
                return Ok(_response);
            }
        }

        #endregion

        #region BroadcastNotification
        /// <summary>
        /// Broadcast notification.
        /// </summary>
        [HttpPost]
        [Route("BroadcastNotification")]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Authorize(Roles = "SuperAdmin,Vendor")]
        public async Task<IActionResult> BroadcastNotification([FromBody] AddNotificationDTO model)
        {
            try
            {
                // var resp1 = await _mobileMessagingClient.SendNotificationAsync(
                //     "cagk8ZlDQBq-wphrbaC4Dx:APA91bGbxFqIckWSxleF88kVBpWJq7nBgGydLGCdexWmDBu22sdPVL5l6qdDqNDy-u-vJrGOUEKoxcbiZjAd7FvPNZHiZKENIo6rUbEWP-fWgB4aSt7CrcI0s1sP3x6Ia4gjpi73B35H",
                //     "Test",
                //     "Test Notification");

                // return Ok();

                var currentUserId = HttpContext.User.Claims.First().Value;
                var currentUserDetail = _userManager.FindByIdAsync(currentUserId).GetAwaiter().GetResult();
                if (currentUserDetail == null)
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = "Token expired.";
                    return Ok(_response);
                }

                string[] allRoles = model.sendToRole.Split(",");

                var roles = await _userManager.GetRolesAsync(currentUserDetail);
                int salonId = 0;

                foreach (var item in allRoles)
                {
                    if (model.sendToRole != Role.Admin.ToString()
                    && model.sendToRole != Role.Vendor.ToString()
                    && model.sendToRole != Role.Customer.ToString())
                    {
                        _response.StatusCode = HttpStatusCode.OK;
                        _response.IsSuccess = false;
                        _response.Messages = "Please enter valid role.";
                        return Ok(_response);
                    }

                    if (roles.FirstOrDefault().ToString() == Role.Vendor.ToString())
                    {
                        if (model.sendToRole != Role.Customer.ToString())
                        {
                            _response.StatusCode = HttpStatusCode.OK;
                            _response.IsSuccess = false;
                            _response.Messages = "Please enter valid role.";
                            return Ok(_response);
                        }
                        model.sendToRole = Role.Customer.ToString();
                        salonId = await _context.SalonDetail.Where(u => u.VendorId == currentUserId).Select(u => u.SalonId).FirstOrDefaultAsync();
                    }
                }

                foreach (var item in allRoles)
                {
                    var addNotification = new Notification();
                    addNotification.Title = model.title;
                    addNotification.Description = model.description;
                    addNotification.UserRole = item;
                    addNotification.CreatedBy = currentUserId;
                    addNotification.NotificationType = NotificationType.Broadcast.ToString();

                    await _context.AddAsync(addNotification);
                    await _context.SaveChangesAsync();

                    var mapData = _mapper.Map<NotificationDTO>(addNotification);

                    // send notification
                    var allUsers = await _userManager.GetUsersInRoleAsync(item);

                    if (allUsers.Count > 0)
                    {
                        foreach (var user in allUsers)
                        {
                            var token = "";
                            var userDetail = await _context.UserDetail.Where(u => u.UserId == user.Id).FirstOrDefaultAsync();
                            if (userDetail != null)
                            {
                                if (roles.FirstOrDefault().ToString() == Role.Vendor.ToString())
                                {
                                    var customerSalon = await _context.CustomerSalon.Where(u => u.SalonId == salonId && u.CustomerUserId == userDetail.UserId).FirstOrDefaultAsync();
                                    if (customerSalon == null)
                                    {
                                        continue;
                                    }
                                }
                                if (!string.IsNullOrEmpty(userDetail.Fcmtoken))
                                {
                                    // if (user.IsNotificationEnabled == true)
                                    // {
                                    token = userDetail.Fcmtoken;
                                    var resp = await _mobileMessagingClient.SendNotificationAsync(token, addNotification.Title, addNotification.Description);
                                    // if (!string.IsNullOrEmpty(resp))
                                    // {
                                    // update notification sent
                                    var notificationSent = new NotificationSent();
                                    notificationSent.Title = addNotification.Title;
                                    notificationSent.Description = addNotification.Description;
                                    notificationSent.UserId = user.Id;
                                    notificationSent.NotificationType = NotificationType.Broadcast.ToString();

                                    await _context.AddAsync(notificationSent);
                                    await _context.SaveChangesAsync();
                                    // }
                                }
                                // }
                            }
                        }
                    }

                }
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Messages = "Notification sent sucessfullly.";
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.IsSuccess = false;
                _response.Messages = ex.Message;
                return Ok(_response);
            }
        }
        #endregion

        #region DeleteBroadcastNotification
        /// <summary>
        /// Delete broadcast notification.
        /// </summary>
        [HttpDelete]
        [Route("DeleteBroadcastNotification")]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Authorize]
        public async Task<IActionResult> DeleteBroadcastNotification([FromQuery] int? notificationId)
        {
            try
            {
                var currentUserId = HttpContext.User.Claims.First().Value;
                var currentUserDetail = _userManager.FindByIdAsync(currentUserId).GetAwaiter().GetResult();
                if (currentUserDetail == null)
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = "Token expired.";
                    return Ok(_response);
                }
                var roles = await _userManager.GetRolesAsync(currentUserDetail);

                if (notificationId > 0)
                {
                    var notification = await _context.Notification
                        .FindAsync(notificationId);

                    if (notification != null)
                    {
                        _context.Remove(notification);
                        await _context.SaveChangesAsync();

                        _response.StatusCode = HttpStatusCode.OK;
                        _response.IsSuccess = true;
                        _response.Messages = "Notification" + ResponseMessages.msgDeletionSuccess;
                        return Ok(_response);
                    }
                    else
                    {
                        _response.StatusCode = HttpStatusCode.OK;
                        _response.IsSuccess = true;
                        _response.Messages = ResponseMessages.msgNotFound;
                        return Ok(_response);
                    }
                }
                else
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = true;
                    _response.Messages = "Notification id must be greater than zero.";
                    return Ok(_response);
                }
            }
            catch (Exception ex)
            {
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.IsSuccess = false;
                _response.Messages = ex.Message;
                return Ok(_response);
            }
        }
        #endregion

        #region GetNotificationList
        /// <summary>
        /// Get notification list.
        /// </summary>
        [HttpGet]
        [Route("GetNotificationList")]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Authorize]
        public async Task<IActionResult> GetNotificationList([FromQuery] int pageNumber, int pageSize, string? searchQuery)
        {
            try
            {
                var currentUserId = HttpContext.User.Claims.First().Value;
                var currentUserDetail = _userManager.FindByIdAsync(currentUserId).GetAwaiter().GetResult();
                if (currentUserDetail == null)
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = "Token expired.";
                    return Ok(_response);
                }
                var roles = await _userManager.GetRolesAsync(currentUserDetail);

                var getNotificationSent = await _context.NotificationSent
                    .Where(u => u.UserId == currentUserId)
                    .OrderByDescending(i => i.CreateDate)
                    .ToListAsync();

                var response = new List<NotificationSentDTO>();

                foreach (var item in getNotificationSent)
                {
                    var notificationDetail = new NotificationSentDTO();
                    notificationDetail.isNotificationRead = item.IsNotificationRead;
                    notificationDetail.description = item.Description;
                    notificationDetail.title = item.Title;
                    notificationDetail.notificationSentId = item.NotificationSentId;
                    notificationDetail.userId = item.UserId;
                    notificationDetail.notificationType = item.NotificationType;
                    notificationDetail.createDate = (Convert.ToDateTime(item.CreateDate).ToString(@"yyyy-MM-dd"));
                    response.Add(notificationDetail);
                }

                if (!string.IsNullOrEmpty(searchQuery))
                {
                    response = response.Where(a => (a.title.ToLower() == searchQuery.ToLower())
                    || (a.description.ToLower() == searchQuery.ToLower())
                    ).ToList();
                }

                var unreadnotificationCount = response.Where(u => u.isNotificationRead != true).ToList().Count;

                FilterationListDTO model = new FilterationListDTO();
                model.pageNumber = (pageNumber <= 0) ? 1 : pageNumber;
                model.pageSize = (pageSize <= 0) ? 10 : pageSize;

                // Get's No of Rows Count
                int count = response.Count();

                // Parameter is passed from Query string if it is null then it default Value will be pageNumber:1
                int CurrentPage = model.pageNumber;

                // Parameter is passed from Query string if it is null then it default Value will be pageSize:20
                int PageSize = model.pageSize;

                // Display TotalCount to Records to User
                int TotalCount = count;

                // Calculating Totalpage by Dividing (No of Records / Pagesize)
                int TotalPages = (int)Math.Ceiling(count / (double)PageSize);

                // Returns List of Customer after applying Paging
                var items = response
                    .Skip((CurrentPage - 1) * PageSize)
                    .Take(PageSize)
                    .ToList();

                // if CurrentPage is greater than 1 means it has previousPage
                var previousPage = CurrentPage > 1 ? "Yes" : "No";

                // if TotalPages is greater than CurrentPage means it has nextPage
                var nextPage = CurrentPage < TotalPages ? "Yes" : "No";

                // Returing List of Customers Collections
                FilterationResponseModel<NotificationSentDTO> obj1 =
                    new FilterationResponseModel<NotificationSentDTO>();
                obj1.totalCount = TotalCount;
                obj1.pageSize = PageSize;
                obj1.currentPage = CurrentPage;
                obj1.totalPages = TotalPages;
                obj1.previousPage = previousPage;
                obj1.nextPage = nextPage;
                obj1.searchQuery = string.IsNullOrEmpty(model.searchQuery) ? "no parameter passed" : model.searchQuery;
                obj1.dataList = items.ToList();

                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Data = new { unreadnotificationCount = unreadnotificationCount, notificationList = obj1 };
                _response.Messages = "Notification" + ResponseMessages.msgListFoundSuccess;
                return Ok(_response);

            }
            catch (Exception ex)
            {
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.IsSuccess = false;
                _response.Messages = ex.Message;
                return Ok(_response);
            }
        }
        #endregion

        #region ReadNotification
        /// <summary>
        /// Read notification.
        /// </summary>
        [HttpGet]
        [Route("ReadNotification")]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Authorize]
        public async Task<IActionResult> ReadNotification(int? notificationSentId)
        {
            try
            {
                var currentUserId = HttpContext.User.Claims.First().Value;
                var currentUserDetail = _userManager.FindByIdAsync(currentUserId).GetAwaiter().GetResult();
                if (currentUserDetail == null)
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = "Token expired.";
                    return Ok(_response);
                }

                var roles = await _userManager.GetRolesAsync(currentUserDetail);
                if (notificationSentId > 0)
                {
                    var getNotificationSent = await _context.NotificationSent
                        .Where(i => i.NotificationSentId == notificationSentId)
                        .FirstOrDefaultAsync();

                    if (getNotificationSent == null)
                    {
                        return Ok(new
                        {
                            status = false,
                            message = ResponseMessages.msgNotFound + "record.",
                            code = StatusCodes.Status200OK,
                        });
                    }

                    getNotificationSent.IsNotificationRead = true;

                    _context.Update(getNotificationSent);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    List<NotificationSent> getNotificationSent;

                    getNotificationSent = await _context.NotificationSent
                       .Where(i => (i.UserId == currentUserId))
                       .ToListAsync();

                    if (getNotificationSent.Count > 0)
                    {
                        foreach (var a in getNotificationSent)
                        {
                            a.IsNotificationRead = true;

                            _context.Update(a);
                            await _context.SaveChangesAsync();
                        }
                    }

                    getNotificationSent = await _context.NotificationSent
                       .Where(i => (i.UserId == currentUserId))
                       .ToListAsync();

                    if (getNotificationSent.Count > 0)
                    {
                        foreach (var a in getNotificationSent)
                        {
                            a.IsNotificationRead = true;

                            _context.Update(a);
                            await _context.SaveChangesAsync();
                        }
                    }

                }

                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                // _response.Data = obj1;
                _response.Messages = "Notification status" + ResponseMessages.msgUpdationSuccess;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.IsSuccess = false;
                _response.Messages = ex.Message;
                return Ok(_response);
            }
        }
        #endregion

        // [HttpPost("setNotificationStatus")]
        // public async Task<IActionResult> setNotificationStatus(UserStatus model)
        // {
        //     try
        //     {
        //         if (model.userId > 0)
        //         {
        //             var user = await _context.Users
        //                 .FindAsync(model.userId);

        //             if (user != null)
        //             {
        //                 user.IsNotificationEnabled = model.notificationStatus;

        //                 _context.Update(user);
        //                 await _context.SaveChangesAsync();
        //                 return StatusCode(200, new
        //                 {
        //                     Status = 200,
        //                     Success = true,
        //                     Message = "Status set successfully."
        //                 });
        //             }
        //             else
        //             {
        //                 return StatusCode(200, new
        //                 {
        //                     Status = 200,
        //                     Success = true,
        //                     Message = "This user doesn't exist."
        //                 });
        //             }


        //         }
        //         else
        //         {
        //             return StatusCode(200, new
        //             {
        //                 Status = 200,
        //                 Success = false,
        //                 Message = "UserId is null or empty string."
        //             });
        //         }
        //     }
        //     catch (Exception ex)
        //     {
        //         return StatusCode(500, new
        //         {
        //             Status = 500,
        //             Success = false,
        //             Message = ex.Message,
        //         });
        //     }
        // }

        #region DeleteNotification
        /// <summary>
        /// Delete notification.
        /// </summary>
        [HttpDelete]
        [Route("DeleteNotification")]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Authorize]
        public async Task<IActionResult> DeleteNotification([FromQuery] int? notificationSentId)
        {
            try
            {
                var currentUserId = HttpContext.User.Claims.First().Value;
                var currentUserDetail = _userManager.FindByIdAsync(currentUserId).GetAwaiter().GetResult();
                if (currentUserDetail == null)
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = "Token expired.";
                    return Ok(_response);
                }
                var roles = await _userManager.GetRolesAsync(currentUserDetail);

                if (notificationSentId > 0)
                {
                    var notification = await _context.NotificationSent
                        .FindAsync(notificationSentId);

                    if (notification != null)
                    {
                        _context.Remove(notification);
                        await _context.SaveChangesAsync();

                        _response.StatusCode = HttpStatusCode.OK;
                        _response.IsSuccess = true;
                        _response.Messages = "Notification" + ResponseMessages.msgDeletionSuccess;
                        return Ok(_response);
                    }
                    else
                    {
                        _response.StatusCode = HttpStatusCode.OK;
                        _response.IsSuccess = true;
                        _response.Messages = ResponseMessages.msgNotFound;
                        return Ok(_response);
                    }
                }
                else
                {
                    List<NotificationSent> notification;

                    notification = await _context.NotificationSent.Where(a => (a.UserId == currentUserId)).ToListAsync(); ;
                    foreach (var item in notification)
                    {
                        _context.Remove(item);
                        await _context.SaveChangesAsync();
                    }

                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = true;
                    _response.Messages = "Notification" + ResponseMessages.msgDeletionSuccess;
                    return Ok(_response);
                }
            }
            catch (Exception ex)
            {
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.IsSuccess = false;
                _response.Messages = ex.Message;
                return Ok(_response);
            }
        }
        #endregion

        #region UpdateFCMToken
        /// <summary>
        /// Update FCM token.
        /// </summary>
        [HttpPost]
        [Authorize]
        [Route("UpdateFCMToken")]
        public async Task<IActionResult> UpdateFCMToken(FCMTokenDTO model)
        {
            try
            {
                var currentUserId = HttpContext.User.Claims.First().Value;
                var currentUserDetail = _userManager.FindByIdAsync(currentUserId).GetAwaiter().GetResult();
                if (currentUserDetail == null)
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = "Token expired.";
                    return Ok(_response);
                }
                var roles = await _userManager.GetRolesAsync(currentUserDetail);

                var user = await _context.UserDetail.Where(a => a.UserId == currentUserId).FirstOrDefaultAsync();

                if (user != null)
                {
                    user.Fcmtoken = model.fcmToken;

                    _context.Update(user);
                    await _context.SaveChangesAsync();

                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = true;
                    _response.Data = model;
                    _response.Messages = "Token" + ResponseMessages.msgUpdationSuccess;
                    return Ok(_response);
                }
                else
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = ResponseMessages.msgNotFound;
                    return Ok(_response);
                }
            }
            catch (Exception ex)
            {
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.IsSuccess = false;
                _response.Messages = ex.Message;
                return Ok(_response);
            }
        }
        #endregion

        #region GetNotificationCount
        /// <summary>
        /// Get notification list.
        /// </summary>
        [HttpGet]
        [Route("GetNotificationCount")]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Authorize]
        public async Task<IActionResult> GetNotificationCount()
        {
            try
            {
                var currentUserId = HttpContext.User.Claims.First().Value;
                var currentUserDetail = _userManager.FindByIdAsync(currentUserId).GetAwaiter().GetResult();
                if (currentUserDetail == null)
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = "Token expired.";
                    return Ok(_response);
                }
                // var roles = await _userManager.GetRolesAsync(currentUserDetail);

                var getNotificationSent = await _context.NotificationSent
                    .Where(u => u.UserId == currentUserId && u.IsNotificationRead != true)
                    .ToListAsync();

                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Data = new { notificationCount = getNotificationSent.Count };
                _response.Messages = "Notification count shown successfully.";
                return Ok(_response);

            }
            catch (Exception ex)
            {
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.IsSuccess = false;
                _response.Messages = ex.Message;
                return Ok(_response);
            }
        }
        #endregion

    }
}
