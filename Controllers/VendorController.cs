using AutoMapper;
using BeautyHubAPI.Data;
using BeautyHubAPI.Models;
using BeautyHubAPI.Models.Dtos;
using BeautyHubAPI.Models.Helper;
using BeautyHubAPI.Repository.IRepository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;
using BeautyHubAPI.Helpers;
using Microsoft.AspNetCore.Authorization;
using BeautyHubAPI.Repository;
using System.Net.Http.Headers;
using static BeautyHubAPI.Common.GlobalVariables;
using TimeZoneConverter;
using OfficeOpenXml;
using System.IO;
using Microsoft.AspNetCore.Identity;
using BeautyHubAPI.Firebase;
using BeautyHubAPI.Dtos;
using System.Globalization;
using System.Text;
using MimeKit.Encodings;
using Newtonsoft.Json;
using RestSharp;
using BeautyHubAPI.Common;

namespace BeautyHubAPI.Controllers
{
    [Route("api/Vendor")]
    [ApiController]
    public class VendorController : ControllerBase
    {
        private readonly IMapper _mapper;
        protected APIResponse _response;
        private readonly HttpClient httpClient;
        private readonly IUploadRepository _uploadRepository;
        private readonly IVendorRepository _vendorRepository;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IMembershipRecordRepository _membershipRecordRepository;
        private readonly IWebHostEnvironment _hostingEnvironment;
        private readonly IMobileMessagingClient _mobileMessagingClient;

        public VendorController(IMapper mapper,
        IUploadRepository uploadRepository,
        IVendorRepository vendorRepository,
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IMembershipRecordRepository membershipRecordRepository,
        IMobileMessagingClient mobileMessagingClient,

        IWebHostEnvironment hostingEnvironment
        )
        {
            _mapper = mapper;
            _vendorRepository = vendorRepository;
            _uploadRepository = uploadRepository;
            _response = new();
            _context = context;
            _userManager = userManager;
            _membershipRecordRepository = membershipRecordRepository;
            _mobileMessagingClient = mobileMessagingClient;
            httpClient = new HttpClient();

        }

        #region buyMembershipPlan
        /// <summary>
        /// Buy membership plan.
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Authorize]
        [Route("buyMembershipPlan")]
        public async Task<IActionResult> buyServicePlan(buyMembershipPlanDTO model)
        {
            try
            {
                string currentUserId = (HttpContext.User.Claims.First().Value);
                if (string.IsNullOrEmpty(currentUserId))
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = "Token expired.";
                    return Ok(_response);
                }

                var currentUserDetail = _userManager.FindByIdAsync(currentUserId).GetAwaiter().GetResult();
                if (currentUserDetail == null)
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = ResponseMessages.msgUserNotFound;
                    return Ok(_response);
                }

                var response = await _vendorRepository.buyServicePlan(model, currentUserId);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    status = false,
                    data = new { },
                    message = ResponseMessages.msgSomethingWentWrong + ex.Message,
                    code = StatusCodes.Status500InternalServerError
                });
            }
        }
        #endregion

        #region GetVendorCategoryList
        /// <summary>
        ///  Get vendor  category list.
        /// </summary>
        [HttpGet("GetVendorCategoryList")]
        [Authorize(Roles = "Vendor,SuperAdmin,Admin")]
        public async Task<IActionResult> GetVendorCategoryList([FromQuery] GetCategoryRequestDTO model)
        {
            try
            {
                string currentUserId = (HttpContext.User.Claims.First().Value);
                if (string.IsNullOrEmpty(currentUserId))
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = "Token expired.";
                    return Ok(_response);
                }
               
                var response = await _vendorRepository.GetVendorCategoryList(model, currentUserId);
                return Ok(response);
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

        #region SetVendorCategoryStatus
        /// <summary>
        /// Set vendor category status.
        /// </summary>
        [HttpPost]
        [Route("SetVendorCategoryStatus")]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Authorize(Roles = "Vendor")]
        public async Task<IActionResult> SetVendorCategoryStatus([FromBody] VendorCategoryRequestDTO model)
        {
            try
            {
                string currentUserId = (HttpContext.User.Claims.First().Value);
                if (string.IsNullOrEmpty(currentUserId))
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = "Token expired.";
                    return Ok(_response);
                }

                
                var response = await _vendorRepository.SetVendorCategoryStatus(model, currentUserId);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.IsSuccess = false;
                _response.Data = new { };
                _response.Messages = ResponseMessages.msgSomethingWentWrong + ex.Message;
                return Ok(_response);
            }
        }
        #endregion

        #region AddSalonBanner
        /// <summary>
        /// Add SalonBanner {SalonBanner, SalonCategoryBanner}.
        /// </summary>
        [HttpPost]
        [Route("AddSalonBanner")]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Authorize(Roles = "SuperAdmin,Admin,Vendor")]
        public async Task<IActionResult> AddSalonBanner([FromForm] AddSalonBannerDTO model)
        {
            try
            {
                string currentUserId = (HttpContext.User.Claims.First().Value);
                if (string.IsNullOrEmpty(currentUserId))
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = "Token expired.";
                    return Ok(_response);
                }

                if (model.bannerImage == null)
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = "Please choose image.";
                    return Ok(_response);
                }

                model.mainCategoryId = model.mainCategoryId == null ? model.mainCategoryId = 0 : model.mainCategoryId;
                model.subCategoryId = model.subCategoryId == null ? model.subCategoryId = 0 : model.subCategoryId;

                if (model.bannerType != BannerType.SalonBanner.ToString() && model.bannerType != BannerType.SalonCategoryBanner.ToString())
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = "Please enter correct banner type.";
                    return Ok(_response);
                }

                if (model.bannerType == BannerType.SalonBanner.ToString() && (model.mainCategoryId > 0))
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = "Please enter correct banner type.";
                    return Ok(_response);
                }

              
                var response = await _vendorRepository.AddSalonBanner(model);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.IsSuccess = false;
                _response.Data = new { };
                _response.Messages = ResponseMessages.msgSomethingWentWrong + ex.Message;
                return Ok(_response);
            }
        }
        #endregion

        #region UpdateSalonBanner
        /// <summary>
        /// Update Salon Banner.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Route("UpdateSalonBanner")]
        [Authorize(Roles = "SuperAdmin,Admin,Vendor")]
        public async Task<IActionResult> UpdateSalonBanner([FromForm] UpdateSalonBannerDTO model)
        {
            try
            {
                string currentUserId = (HttpContext.User.Claims.First().Value);
                if (string.IsNullOrEmpty(currentUserId))
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = "Token expired.";
                    return Ok(_response);
                }

                model.mainCategoryId = model.mainCategoryId == null ? model.mainCategoryId = 0 : model.mainCategoryId;
                model.subCategoryId = model.subCategoryId == null ? model.subCategoryId = 0 : model.subCategoryId;

                if (model.bannerType != BannerType.SalonBanner.ToString() && model.bannerType != BannerType.SalonCategoryBanner.ToString())
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = "Please enter correct banner type.";
                    return Ok(_response);
                }

                if (model.bannerType == BannerType.SalonBanner.ToString() && (model.mainCategoryId != 0))
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = "Please enter correct banner type.";
                    return Ok(_response);
                }

                if (model.bannerType == BannerType.SalonCategoryBanner.ToString())
                {
                    if ((model.mainCategoryId == 0 && model.subCategoryId == 0))
                    {
                        _response.StatusCode = HttpStatusCode.OK;
                        _response.IsSuccess = false;
                        _response.Messages = "Please enter correct banner type.";
                        return Ok(_response);
                    }
                }
              
                var response = await _vendorRepository.UpdateSalonBanner(model);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.IsSuccess = false;
                _response.Data = new { };
                _response.Messages = ResponseMessages.msgSomethingWentWrong + ex.Message;
                return Ok(_response);
            }
        }
        #endregion

        #region DeleteSalonBanner
        /// <summary>
        /// Delete salon banner.
        /// </summary>
        [HttpDelete]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Route("DeleteSalonBanner")]
        [Authorize(Roles = "SuperAdmin,Admin,Vendor")]
        public async Task<IActionResult> DeleteSalonBanner(int salonBannerId)
        {
            try
            {
                string currentUserId = (HttpContext.User.Claims.First().Value);
                if (string.IsNullOrEmpty(currentUserId))
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = "Token expired.";
                    return Ok(_response);
                }
              
                var response = await _vendorRepository.DeleteSalonBanner(salonBannerId);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.IsSuccess = false;
                _response.Data = new { };
                _response.Messages = ResponseMessages.msgSomethingWentWrong + ex.Message;
                return Ok(_response);
            }
        }
        #endregion

        #region GetSalonBannerDetail
        /// <summary>
        /// Get SalonBanner.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Route("GetSalonBannerDetail")]
        [Authorize(Roles = "SuperAdmin,Admin,Vendor,Customer")]
        public async Task<IActionResult> GetSalonBannerDetail(int salonBannerId)
        {
            try
            {
                string currentUserId = (HttpContext.User.Claims.First().Value);
                if (string.IsNullOrEmpty(currentUserId))
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = "Token expired.";
                    return Ok(_response);
                }
            
                var response = await _vendorRepository.GetSalonBannerDetail(salonBannerId);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.IsSuccess = false;
                _response.Data = new { };
                _response.Messages = ResponseMessages.msgSomethingWentWrong + ex.Message;
                return Ok(_response);
            }
        }
        #endregion

        #region GetSalonBannerList
        /// <summary>
        ///  Get SalonBanner list.
        /// </summary>
        [HttpGet]
        [Route("GetSalonBannerList")]
        [Authorize]
        public async Task<IActionResult> GetSalonBannerList([FromQuery] GetSalonBannerrequestDTO model)
        {
            try
            {
                string currentUserId = (HttpContext.User.Claims.First().Value);
                if (string.IsNullOrEmpty(currentUserId))
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = "Token expired.";
                    return Ok(_response);
                }
               
                var response = await _vendorRepository.GetSalonBannerList(model);
                return Ok(response);
            }
            catch (System.Exception ex)
            {

                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.IsSuccess = false;
                _response.Data = new { };
                _response.Messages = ResponseMessages.msgSomethingWentWrong + ex.Message;
                return Ok(_response);
            }
        }
        #endregion

        #region GetVendorAppointmentList
        /// <summary>
        ///  Get appointment list for vendor {date format : dd-MM-yyyy}.
        /// </summary>
        [HttpGet("GetVendorAppointmentList")]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Authorize]
        public async Task<IActionResult> GetVendorAppointmentList([FromQuery] OrderFilterationListDTO model)
        {
            try
            {
                string currentUserId = (HttpContext.User.Claims.First().Value);
                if (string.IsNullOrEmpty(currentUserId))
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = "Token expired.";
                    return Ok(_response);
                }

               
                var response = await _vendorRepository.GetVendorAppointmentList(model);
                return Ok(response);
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

        #region GetVendorAppointmentDetail
        /// <summary>
        ///  Get Vendor Appointment Detail
        /// </summary>
        [HttpGet("GetVendorAppointmentDetail")]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Authorize(Roles = "Vendor")]
        public async Task<IActionResult> GetVendorAppointmentDetail(int appointmentId)
        {
            try
            {
                string currentUserId = (HttpContext.User.Claims.First().Value);
                if (string.IsNullOrEmpty(currentUserId))
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = "Token expired.";
                    return Ok(_response);
                }

               
                var response = await _vendorRepository.GetVendorAppointmentDetail(appointmentId, currentUserId);
                return Ok(response);
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

        #region SetAppointmentStatus
        /// <summary>
        /// Set appointment status {Pending, Completed, Scheduled, Cancelled}.
        /// </summary>
        [HttpPost]
        [Route("SetAppointmentStatus")]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Authorize(Roles = "Vendor")]
        public async Task<IActionResult> SetAppointmentStatus(SetAppointmentStatusDTO model)
        {
            try
            {
                string currentUserId = (HttpContext.User.Claims.First().Value);
                if (string.IsNullOrEmpty(currentUserId))
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = "Token expired.";
                    return Ok(_response);
                }

                var appointmentDetail = await _context.Appointment.Where(u => u.AppointmentId == model.appointmentId).FirstOrDefaultAsync();
                if (appointmentDetail == null)
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Data = new Object { };
                    _response.Messages = ResponseMessages.msgNotFound + "appointment.";
                    return Ok(_response);
                }

                var response = await _vendorRepository.SetAppointmentStatus(model);
                return Ok(response);
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

        #region SetPaymentStatus
        /// <summary>
        /// Set payment status {Paid, Unpaid, Refunded}.
        /// </summary>
        [HttpPost]
        [Route("SetPaymentStatus")]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Authorize(Roles = "Vendor")]
        public async Task<IActionResult> SetPaymentStatus(SetPaymentStatusDTO model)
        {
            try
            {
                string currentUserId = (HttpContext.User.Claims.First().Value);
                if (string.IsNullOrEmpty(currentUserId))
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = "Token expired.";
                    return Ok(_response);
                }

                if (model.paymentStatus != PaymentStatus.Paid.ToString()
                && model.paymentStatus != PaymentStatus.Unpaid.ToString()
                && model.paymentStatus != PaymentStatus.Refunded.ToString())
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = "Please select a valid status.";
                    return Ok(_response);
                }

                var appointmentDetails = await _context.Appointment.Where(u => u.AppointmentId == model.appointmentId).FirstOrDefaultAsync();
                if (appointmentDetails == null)
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Data = new Object { };
                    _response.Messages = ResponseMessages.msgNotFound + "appointment.";
                    return Ok(_response);
                }

              
                var response = await _vendorRepository.SetPaymentStatus(model);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.IsSuccess = false;
                _response.Data = new { };
                _response.Messages = ResponseMessages.msgSomethingWentWrong + ex.Message;
                return Ok(_response);
            }
        }
        #endregion

        #region MarkAppointmentAsRead
        /// <summary>
        /// </summary>
        [HttpPost]
        [Route("MarkAppointmentAsRead")]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Authorize(Roles = "Vendor")]
        public async Task<IActionResult> MarkAppointmentAsRead(ReadStatusDTO model)
        {
            try
            {
                string currentUserId = (HttpContext.User.Claims.First().Value);
                if (string.IsNullOrEmpty(currentUserId))
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = "Token expired.";
                    return Ok(_response);
                }

                var appointmentDetail = await _context.Appointment.Where(u => u.AppointmentId == model.appointmentId).FirstOrDefaultAsync();
                if (appointmentDetail == null)
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Data = new Object { };
                    _response.Messages = "Not found any appointment.";
                    return Ok(_response);
                }

                var response = await _vendorRepository.MarkAppointmentAsRead(model);
                return Ok(response);

            }
            catch (Exception ex)
            {
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.IsSuccess = false;
                _response.Data = new { };
                _response.Messages = ResponseMessages.msgSomethingWentWrong + ex.Message;
                return Ok(_response);
            }
        }
        #endregion

        #region UpComingSchedule
        /// <summary>
        /// Get UpComing Schedule .
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Authorize(Roles = "Vendor")]
        [Route("UpComingSchedule")]
        public async Task<IActionResult> UpComingSchedule(int salonId)
        {
            try
            {
                string currentUserId = (HttpContext.User.Claims.First().Value);
                if (string.IsNullOrEmpty(currentUserId))
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = "Token expired.";
                    return Ok(_response);
                }
                var salon = await _context.SalonDetail.FirstOrDefaultAsync(a => a.SalonId == salonId);
                if (salon == null)
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = ResponseMessages.msgNotFound + "Salon.";
                    return Ok(_response);
                }

              
                var response = await _vendorRepository.UpComingSchedule(salonId);
                return Ok(response);


            }
            catch (Exception ex)
            {
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.IsSuccess = false;
                _response.Data = new { };
                _response.Messages = ResponseMessages.msgSomethingWentWrong + ex.Message;
                return Ok(_response);
            }
        }
        #endregion

        #region UpComingScheduleDetail
        /// <summary>
        /// Get UpComing Schedule .
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Authorize(Roles = "Vendor")]
        [Route("UpComingScheduleDetail")]
        public async Task<IActionResult> UpComingScheduleDetail(string queryDate)
        {
            try
            {
                string currentUserId = (HttpContext.User.Claims.First().Value);
                if (string.IsNullOrEmpty(currentUserId))
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = "Token expired.";
                    return Ok(_response);
                }

              
                var response = await _vendorRepository.UpComingScheduleDetail(queryDate);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.IsSuccess = false;
                _response.Data = new { };
                _response.Messages = ResponseMessages.msgSomethingWentWrong + ex.Message;
                return Ok(_response);
            }
        }
        #endregion

    }
}

