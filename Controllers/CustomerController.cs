using AutoMapper;
using BeautyHubAPI.Models;
using BeautyHubAPI.Models.Dtos;
using BeautyHubAPI.Models.Helper;
using BeautyHubAPI.Repository.IRepository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Net;
using static BeautyHubAPI.Common.GlobalVariables;
using BeautyHubAPI.Models;
using BeautyHubAPI.Data;
using Microsoft.EntityFrameworkCore;
using TimeZoneConverter;
using BeautyHubAPI.Helpers;
using Newtonsoft.Json.Linq;
using BeautyHubAPI.Firebase;
using System.Diagnostics;
using System.Text;
using RestSharp;
using BeautyHubAPI.Dtos;
using System.Linq;
using System.Drawing;
using System.Globalization;
using System.Data.Common;
using System.Timers;
using Twilio.Http;
using Amazon.S3.Model;
using ExpressionEvaluator.Parser.Expressions;
using GSF.Collections;
using BeautyHubAPI.Common;
using BeautyHubAPI.Repository;

namespace BeautyHubAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CustomerController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly IMobileMessagingClient _mobileMessagingClient;
        private readonly IMapper _mapper;
        private readonly ICustomerRepository _customerRepository;
        protected APIResponse _response;
        public CustomerController(IMapper mapper,
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context,
            ICustomerRepository customerRepository,
            IMobileMessagingClient mobileMessagingClient
            )
        {
            _response = new();
            _mapper = mapper;
            _customerRepository = customerRepository;
            _context = context;
            _userManager = userManager;
            _mobileMessagingClient = mobileMessagingClient;
        }

        #region AddSalon
        /// <summary>
        ///  Add salon.
        /// </summary>
        [HttpPost("AddSalon")]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> AddSalon([FromBody] AddCustomerSalonDTO model)
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

                var userProfileDetail = await _context.UserDetail.Where(u => u.UserId == currentUserId).FirstOrDefaultAsync();
                if (userProfileDetail == null)
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = ResponseMessages.msgNotFound + "user";
                    return Ok(_response);
                }

                var checksalonDetail = await _context.CustomerSalon.Where(u => (u.CustomerUserId == currentUserId) && (u.SalonId == model.salonId)).FirstOrDefaultAsync();
                if (checksalonDetail != null)
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = "Salon" + ResponseMessages.msgAlreadyExists;
                    return Ok(_response);
                }

                var salonDetail = await _context.SalonDetail.Where(u => (u.SalonId == model.salonId) && (u.IsDeleted != true)).FirstOrDefaultAsync();
                if (salonDetail == null)
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = ResponseMessages.msgNotFound + "salon";
                    return Ok(_response);
                }

                var response = await _customerRepository.AddSalon(model, currentUserId);
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

        #region GetSalonList
        /// <summary>
        ///  get salon list.
        /// </summary>
        [HttpGet("GetSalonList")]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> GetSalonList(string? salonQuery, string? salonType, string? searchBy, int? liveLocation)
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
                searchBy = string.IsNullOrEmpty(searchBy) ? null : (searchBy).TrimEnd();

                liveLocation = liveLocation != null ? liveLocation : 0;

                var userProfileDetail = await _context.UserDetail.Where(u => u.UserId == currentUserId).FirstOrDefaultAsync();
                if (userProfileDetail == null)
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = ResponseMessages.msgNotFound + "user";
                    return Ok(_response);
                }

                var response = await _customerRepository.GetSalonList(salonQuery, salonType, searchBy, liveLocation, currentUserId);
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

        #region GetFavouriteSalonList
        /// <summary>
        ///  get salon list.
        /// </summary>
        [HttpGet("GetFavouriteSalonList")]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> GetFavouriteSalonList(string? salonType, string? searchBy, int? liveLocation)
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

                liveLocation = liveLocation != null ? liveLocation : 0;

                var userProfileDetail = await _context.UserDetail.Where(u => u.UserId == currentUserId).FirstOrDefaultAsync();
                if (userProfileDetail == null)
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = ResponseMessages.msgNotFound + "user";
                    return Ok(_response);
                }


                var response = await _customerRepository.GetFavouriteSalonList(salonType, searchBy, liveLocation, currentUserId);
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

        #region AddCustomerAddress
        /// <summary>
        ///  Add customer address {AddressType :  Home, Work, Other}.
        /// </summary>
        [HttpPost("AddCustomerAddress")]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> AddCustomerAddress([FromBody] AddCustomerAddressRequestDTO model)
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

                if (AddressType.Home.ToString() != model.addressType
                 && AddressType.Other.ToString() != model.addressType
                 && AddressType.Work.ToString() != model.addressType
                )
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = "Please enter correct address type.";
                    return Ok(_response);
                }

                var userProfileDetail = _context.UserDetail.Select(u => u.UserId == currentUserId);
                if (userProfileDetail == null)
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = ResponseMessages.msgNotFound + "user";
                    return Ok(_response);
                }

                var checkCustomerDetail = await _context.CustomerAddress.Where(u => (u.CustomerUserId == currentUserId) && (u.AddressType == model.addressType)).FirstOrDefaultAsync();
                if (checkCustomerDetail != null)
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = "Address type has been already added.";
                    return Ok(_response);
                }


                var response = await _customerRepository.AddCustomerAddress(model, currentUserId);
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

        #region UpdateCustomerAddress
        /// <summary>
        ///  Update customer address {AddressType :  Home, Work, Other}.
        /// </summary>
        [HttpPost("UpdateCustomerAddress")]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> UpdateCustomerAddress([FromBody] UpdateCustomerAddressRequestDTO model)
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

                if (AddressType.Home.ToString() != model.addressType
                 && AddressType.Other.ToString() != model.addressType
                 && AddressType.Work.ToString() != model.addressType
                )
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = "Please enter correct address type.";
                    return Ok(_response);
                }

                var userProfileDetail = await _context.UserDetail.FirstOrDefaultAsync(u => u.UserId == currentUserId);
                if (userProfileDetail == null)
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = ResponseMessages.msgNotFound + "user";
                    return Ok(_response);
                }


                var response = await _customerRepository.UpdateCustomerAddress(model, currentUserId);
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

        #region DeleteCustomerAddress
        /// <summary>
        ///  Delete customer address.
        /// </summary>
        [HttpDelete("DeleteCustomerAddress")]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> DeleteCustomerAddress(int customerAddressId)
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

                var userProfileDetail = await _context.UserDetail.FirstOrDefaultAsync(u => u.UserId == currentUserId);
                if (userProfileDetail == null)
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = ResponseMessages.msgNotFound + "user";
                    return Ok(_response);
                }

                var customerDetail = await _context.CustomerAddress.FirstOrDefaultAsync(u => (u.CustomerAddressId == customerAddressId));
                if (customerDetail == null)
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = ResponseMessages.msgNotFound + "record.";
                    return Ok(_response);
                }

                var response = await _customerRepository.DeleteCustomerAddress(customerAddressId, currentUserId);
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

        #region GetCustomerAddressDetail
        /// <summary>
        ///  Get customer address detail.
        /// </summary>
        [HttpGet("GetCustomerAddressDetail")]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> GetCustomerAddressDetail(int customerAddressId)
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

                var userProfileDetail = await _context.UserDetail.FirstOrDefaultAsync(u => u.UserId == currentUserId);
                if (userProfileDetail == null)
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = ResponseMessages.msgNotFound + "user";
                    return Ok(_response);
                }

                var customerDetail = await _context.CustomerAddress.FirstOrDefaultAsync(u => (u.CustomerAddressId == customerAddressId));
                if (customerDetail == null)
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = ResponseMessages.msgNotFound + "record.";
                    return Ok(_response);
                }

                var response = await _customerRepository.GetCustomerAddressDetail(customerAddressId, currentUserId);
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

        #region SetCustomerAddressStatus
        /// <summary>
        ///  Add customer address status.
        /// </summary>
        [HttpPost("SetCustomerAddressStatus")]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> SetCustomerAddressStatus([FromBody] SerCustomerAddressRequestStatusDTO model)
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

                var userProfileDetail = await _context.UserDetail.FirstOrDefaultAsync(u => u.UserId == currentUserId);
                if (userProfileDetail == null)
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = ResponseMessages.msgNotFound + "user";
                    return Ok(_response);
                }

                var customerDetail = await _context.CustomerAddress.FirstOrDefaultAsync(u => (u.CustomerAddressId == model.customerAddressId));
                if (customerDetail == null)
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = ResponseMessages.msgNotFound + "record.";
                    return Ok(_response);
                }

                var response = await _customerRepository.SetCustomerAddressStatus(model, currentUserId);
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

        #region GetCustomerAddressList
        /// <summary>
        ///  Get customer address list.
        /// </summary>
        [HttpGet("GetCustomerAddressList")]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> GetCustomerAddressList()
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

                var userProfileDetail = await _context.UserDetail.FirstOrDefaultAsync(u => u.UserId == currentUserId);
                if (userProfileDetail == null)
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = ResponseMessages.msgNotFound + "user";
                    return Ok(_response);
                }

                var customerDetail = await _context.CustomerAddress.Where(u => (u.CustomerUserId == currentUserId)).ToListAsync();
                if (customerDetail == null)
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = ResponseMessages.msgNotFound + "record.";
                    return Ok(_response);
                }

             
                var response = await _customerRepository.GetCustomerAddressList(currentUserId);
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

        #region DeleteCustomerAccount
        [HttpDelete("DeleteCustomerAccount")]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Authorize]
        public async Task<IActionResult> DeleteCustomerAccount(string? customerUserId)
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

                if (!string.IsNullOrEmpty(customerUserId))
                {
                    currentUserId = customerUserId;
                }

                var currentUserDetail = _userManager.FindByIdAsync(currentUserId).GetAwaiter().GetResult();
                if (currentUserDetail == null)
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = ResponseMessages.msgUserNotFound;
                    return Ok(_response);
                }
                // await _userManager.UpdateSecurityStampAsync(currentUserDetail);
                var roles = await _userManager.GetRolesAsync(currentUserDetail);

                if (roles[0] != "Customer")
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = ResponseMessages.msgUserNotFound;
                    return Ok(_response);
                }

                var user = await _context.UserDetail.Where(u => u.UserId == currentUserId).FirstOrDefaultAsync();
                if (user == null)
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = ResponseMessages.msgNotFound + "record.";
                    return Ok(_response);
                }

                var response = await _customerRepository.DeleteCustomerAccount(customerUserId, currentUserId);
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

        #region AddServiceToCart
        /// <summary>
        ///  Add Service to cart.
        /// </summary>
        [HttpPost("AddServiceToCart")]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> AddServiceToCart([FromBody] AddServiceToCartDTO model)
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

                var userProfileDetail = await _context.UserDetail.Where(u => u.UserId == currentUserId).FirstOrDefaultAsync();
                if (userProfileDetail == null)
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = ResponseMessages.msgNotFound + "user";
                    return Ok(_response);
                }

                var SalonIdList = new List<int?>();
                SalonIdList = await _context.Cart.Where(u => (u.CustomerUserId == currentUserId)).Select(u => u.SalonId).Distinct().ToListAsync();

                var checkServiceDetail = await _context.SalonService.Where(u => (u.ServiceId == model.serviceId) && (u.Status == Convert.ToInt32(ServiceStatus.Active))).FirstOrDefaultAsync();
                if (checkServiceDetail == null)
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = ResponseMessages.msgNotFound + "record.";
                    return Ok(_response);
                }

                var checkTimeSlot = await _context.TimeSlot.Where(u => (u.ServiceId == model.serviceId) && (u.SlotId == model.slotId) && (u.Status != false) && (u.SlotCount > 0)).FirstOrDefaultAsync();
                if (checkTimeSlot == null)
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = "Slot is not available.";
                    return Ok(_response);
                }

                // Validation for Salon count
                if (!(SalonIdList.Any(u => u.Value == checkServiceDetail.SalonId)))
                {
                    if (SalonIdList.Count() == 1)
                    {
                        _response.StatusCode = HttpStatusCode.OK;
                        _response.IsSuccess = false;
                        _response.Messages = "Cannot add service from multiple salons at once.";
                        return Ok(_response);
                    }
                }

                var response = await _customerRepository.AddServiceToCart(model, currentUserId);
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

        #region GetServiceListFromCart
        /// <summary>
        ///  Get Service list from cart.
        /// </summary>
        [HttpGet("GetServiceListFromCart")]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> GetServiceListFromCart(string? availableService, int? liveLocation)
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
                liveLocation = liveLocation != null ? liveLocation : 0;
                var userProfileDetail = await _context.UserDetail.Where(u => u.UserId == currentUserId).FirstOrDefaultAsync();
                if (userProfileDetail == null)
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = ResponseMessages.msgNotFound + "user";
                    return Ok(_response);
                }

               
                var response = await _customerRepository.GetServiceListFromCart(availableService, liveLocation, currentUserId);
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

        #region RemoveServiceFromCart
        /// <summary>
        ///  Remove Service from cart.
        /// </summary>
        [HttpDelete("RemoveServiceFromCart")]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> RemoveServiceFromCart(int serviceId, int? slotId)
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

                slotId = slotId != null ? slotId : 0;

                var userProfileDetail = await _context.UserDetail.Where(u => u.UserId == currentUserId).FirstOrDefaultAsync();
                if (userProfileDetail == null)
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = ResponseMessages.msgNotFound + "user";
                    return Ok(_response);
                }


                var response = await _customerRepository.RemoveServiceFromCart(serviceId, slotId, currentUserId);
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

        #region CancelAppointment
        /// <summary>
        ///  Cancel Appointment.
        /// </summary>
        [HttpPost("CancelAppointment")]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Authorize(Roles = "Customer,Vendor")]
        public async Task<IActionResult> CancelAppointment(CancelAppointmentDTO model)
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

                var userProfileDetail = await _context.UserDetail.Where(u => u.UserId == currentUserId).FirstOrDefaultAsync();
                if (userProfileDetail == null)
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = ResponseMessages.msgNotFound + "user";
                    return Ok(_response);
                }

                var appointmentDetail = await _context.Appointment.Where(u => u.AppointmentId == model.appointmentId).FirstOrDefaultAsync();
                if (appointmentDetail == null)
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = "Appointment not found.";
                    return Ok(_response);
                }

                if (appointmentDetail.AppointmentStatus == AppointmentStatus.Cancelled.ToString())
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = "Appointment already cancelled.";
                    return Ok(_response);
                }

              
                var response = await _customerRepository.CancelAppointment(model, currentUserId);
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

        #region GetServiceCountInCart
        /// <summary>
        ///  Service count in cart.
        /// </summary>
        [HttpGet("GetServiceCountInCart")]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> GetServiceCountInCart()
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

                var userProfileDetail = await _context.UserDetail.Where(u => u.UserId == currentUserId).FirstOrDefaultAsync();
                if (userProfileDetail == null)
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = ResponseMessages.msgNotFound + "user";
                    return Ok(_response);
                }

            
                var response = await _customerRepository.GetServiceCountInCart(currentUserId);
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

        #region GetUnavailableServices
        /// <summary>
        ///  Get Unavailable Services.
        /// </summary>
        [HttpGet("GetUnavailableServices")]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> GetUnavailableServices(int? liveLocation)
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
                liveLocation = liveLocation != null ? liveLocation : 0;
                var userProfileDetail = await _context.UserDetail.Where(u => u.UserId == currentUserId).FirstOrDefaultAsync();
                if (userProfileDetail == null)
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = ResponseMessages.msgNotFound + "user";
                    return Ok(_response);
                }

               
                var response = await _customerRepository.GetUnavailableServices(liveLocation, currentUserId);
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

        #region setFavouriteSalonStatus
        /// <summary>
        ///set favourite salon status
        /// </summary>
        [HttpPost]
        [Route("setFavouriteSalonStatus")]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> setFavouriteSalonStatus(SetFavouriteSalon model)
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

                var serviceDetail = await _context.SalonDetail.FirstOrDefaultAsync(u => u.SalonId == model.salonId);
                if (serviceDetail == null)
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = ResponseMessages.msgNotFound + "Service";
                    return Ok(_response);
                }

               
                var response = await _customerRepository.setFavouriteSalonStatus(model, currentUserId);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.IsSuccess = false;
                _response.Messages = ResponseMessages.msgSomethingWentWrong + ex.Message;
                return Ok(_response);
            }
        }
        #endregion

        #region BookAppointment
        /// <summary>
        ///  Book appointment {Payment method : InCash, PayByUPI}.
        /// </summary>
        [HttpPost("BookAppointment")]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> BookAppointment(PlaceAppointmentRequestDTO model)
        {
            try
            {
                string currentUserId = (HttpContext.User.Claims.First().Value);
                string vendorId = "";
                int SalonId = 0;

                if (string.IsNullOrEmpty(currentUserId))
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = "Token expired.";
                    return Ok(_response);
                }

                var userProfileDetail = await _context.UserDetail.Where(u => u.UserId == currentUserId).FirstOrDefaultAsync();
                if (userProfileDetail == null)
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = ResponseMessages.msgNotFound + "user";
                    return Ok(_response);
                }
                var userDetail = _userManager.FindByIdAsync(currentUserId).GetAwaiter().GetResult();
                if (userDetail == null)
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = ResponseMessages.msgNotFound + "user";
                    return Ok(_response);
                }
                if (model.paymentMethod != PaymentMethod.InCash.ToString())
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = "Only in cash payment is valid";
                    return Ok(_response);
                }

                if (string.IsNullOrEmpty(userDetail.FirstName))
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = "Name required to book appointment.";
                    return Ok(_response);
                }
              
                var response = await _customerRepository.BookAppointment(model, currentUserId);
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

        #region GetCustomerAppointmentList
        /// <summary>
        ///  Get appointment list for customer {date format : dd-MM-yyyy}.
        /// </summary>
        [HttpGet("GetCustomerAppointmentList")]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Authorize]
        public async Task<IActionResult> GetCustomerAppointmentList([FromQuery] CustomerAppointmentFilterationListDTO model)
        {
            try
            {
                var ctz = TZConvert.GetTimeZoneInfo("India Standard Time");
                var convrtedZoneDate = TimeZoneInfo.ConvertTimeFromUtc(Convert.ToDateTime(DateTime.UtcNow), ctz);
                string currentUserId = (HttpContext.User.Claims.First().Value);
                if (string.IsNullOrEmpty(currentUserId))
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = "Token expired.";
                    return Ok(_response);
                }
                model.liveLocation = model.liveLocation != null ? model.liveLocation : 0;
                var userProfileDetail = await _context.UserDetail.Where(u => u.UserId == currentUserId).FirstOrDefaultAsync();

               
                var response = await _customerRepository.GetCustomerAppointmentList(model, currentUserId);
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

        #region GetCustomerAppointmentDetail
        /// <summary>
        ///  Get appointment detail.
        /// </summary>
        [HttpGet("GetCustomerAppointmentDetail")]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Authorize]
        public async Task<IActionResult> GetCustomerAppointmentDetail(int appointmentId, int? liveLocation)
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
               
                var response = await _customerRepository.GetCustomerAppointmentDetail(appointmentId, liveLocation, currentUserId);
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

        #region SetFavouriteServiceStatus
        /// <summary>
        ///set favourite service status
        /// </summary>
        [HttpPost]
        [Route("SetFavouriteServiceStatus")]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> SetFavouriteServiceStatus(SetFavouriteService model)
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

                var serviceDetail = await _context.SalonService.FirstOrDefaultAsync(u => u.ServiceId == model.serviceId && u.IsDeleted == false);
                if (serviceDetail == null)
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = ResponseMessages.msgNotFound + "Service";
                    return Ok(_response);
                }

               
                var response = await _customerRepository.SetFavouriteServiceStatus(model, currentUserId);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.IsSuccess = false;
                _response.Messages = ResponseMessages.msgSomethingWentWrong + ex.Message;
                return Ok(_response);
            }
        }
        #endregion

        #region GetFavouriteServiceList
        /// <summary>
        ///  get service list.
        /// </summary>
        [HttpGet("GetFavouriteServiceList")]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> GetFavouriteServiceList(int salonId)
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

                var userProfileDetail = await _context.UserDetail.Where(u => u.UserId == currentUserId).FirstOrDefaultAsync();
                if (userProfileDetail == null)
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = ResponseMessages.msgNotFound + "user";
                    return Ok(_response);
                }

             
                var response = await _customerRepository.GetFavouriteServiceList(salonId, currentUserId);
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

        #region GetCustomerDashboardData
        /// <summary>
        ///  Get customer dashboard data.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Authorize(Roles = "Customer")]
        [Route("GetCustomerDashboardData")]
        public async Task<IActionResult> GetCustomerDashboardData([FromQuery] DashboardServiceFilterationListDTO model)
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

            var response = await _customerRepository.GetCustomerDashboardData(model, currentUserId);
            return Ok(response);
        }
        #endregion

    }
}
