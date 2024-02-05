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
        protected APIResponse _response;
        public CustomerController(IMapper mapper,
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context,
            IMobileMessagingClient mobileMessagingClient
            )
        {
            _response = new();
            _mapper = mapper;
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
                    _response.Messages = "Salon" + ResponseMessages.msgAdditionSuccess;
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

                var customerDeatil = new CustomerSalon();
                customerDeatil.CustomerUserId = userProfileDetail.UserId;
                customerDeatil.SalonId = salonDetail.SalonId;
                customerDeatil.Status = true;

                var checksalonDetail2 = await _context.CustomerSalon.Where(u => (u.CustomerUserId == currentUserId) && (u.SalonId == model.salonId)).FirstOrDefaultAsync();
                if (checksalonDetail2 == null)
                {
                    await _context.AddAsync(customerDeatil);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = "Salon is already added.";
                    return Ok(_response);
                }

                var response = _mapper.Map<SalonResponseDTO>(salonDetail);

                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Data = response;
                _response.Messages = "Salon" + ResponseMessages.msgAdditionSuccess;
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

                var salonList = await _context.CustomerSalon.Where(u => (u.CustomerUserId == currentUserId) && (u.Status == true)).ToListAsync();

                if (salonList.Count < 1)
                {
                    salonList = new List<CustomerSalon>();
                }

                double startLong = 0;
                double startLat = 0;


                if (!string.IsNullOrEmpty(userProfileDetail.AddressLatitude) && !string.IsNullOrEmpty(userProfileDetail.AddressLongitude))
                {
                    startLat = Convert.ToDouble(userProfileDetail.AddressLatitude != null ? userProfileDetail.AddressLatitude : "0");
                    startLong = Convert.ToDouble(userProfileDetail.AddressLongitude != null ? userProfileDetail.AddressLongitude : "0");
                }
                else
                {
                    if (liveLocation == 1)
                    {
                        _response.StatusCode = HttpStatusCode.OK;
                        _response.IsSuccess = false;
                        _response.Messages = ResponseMessages.msgNotFound + "Location";
                        return Ok(_response);
                    }
                }

                if (!string.IsNullOrEmpty(salonQuery))
                {
                    salonList = salonList.OrderByDescending(u => u.CreateDate).ToList();
                    var customerAdress = await _context.CustomerAddress.Where(u => u.CustomerUserId == currentUserId && u.Status == true).FirstOrDefaultAsync();
                    if (customerAdress != null && liveLocation != 1)
                    {
                        startLat = Convert.ToDouble(customerAdress.AddressLatitude != null ? customerAdress.AddressLatitude : "0");
                        startLong = Convert.ToDouble(customerAdress.AddressLongitude != null ? customerAdress.AddressLongitude : "0");
                    }
                    var salonResponse = new List<CustomerSalonListDTO>();
                    foreach (var item in salonList)
                    {
                        // var salonDetail = await _context.SalonDetail.Where(u => (u.SalonId == item.SalonId) && (u.IsDeleted != true)).FirstOrDefaultAsync();
                        SalonDetail? salonDetail = new SalonDetail();
                        if (string.IsNullOrEmpty(salonType))
                        {
                            salonDetail = await _context.SalonDetail.Where(u => (u.SalonId == item.SalonId) && (u.IsDeleted != true)).FirstOrDefaultAsync();
                        }
                        else if (salonType == "Male" || salonType == "Female" || salonType == "Unisex")
                        {
                            salonDetail = await _context.SalonDetail.Where(u => (u.SalonId == item.SalonId)
                            && (u.IsDeleted != true)
                            && (u.SalonType == salonType)
                            ).FirstOrDefaultAsync();
                        }
                        else
                        {
                            salonDetail = await _context.SalonDetail.Where(u => (u.SalonId == item.SalonId) && (u.IsDeleted != true)).FirstOrDefaultAsync();
                        }
                        if (salonDetail != null)
                        {
                            var vendorDetail = _userManager.FindByIdAsync(salonDetail.VendorId).GetAwaiter().GetResult();
                            var mappedData = _mapper.Map<CustomerSalonListDTO>(salonDetail);
                            mappedData.vendorName = vendorDetail.FirstName + " " + vendorDetail.LastName;
                            mappedData.isSalonAdded = true;

                            if (startLat == 0 && startLong == 0)
                            {
                                startLat = 30.741482;
                                startLong = 76.768066;
                            }

                            if (startLat != 0 && startLong != 0)
                            {
                                double endLat = Convert.ToDouble(salonDetail.AddressLatitude != null ? salonDetail.AddressLatitude : "0");
                                double endLong = Convert.ToDouble(salonDetail.AddressLongitude != null ? salonDetail.AddressLongitude : "0");

                                var APIResponse = CommonMethod.GoogleDistanceMatrixAPILatLonAsync(startLat, startLong, endLat, endLong).GetAwaiter().GetResult();
                                mappedData.distance = APIResponse.distance;
                                mappedData.duration = APIResponse.duration;
                            }
                            mappedData.favoritesStatus = (_context.FavouriteSalon.Where(u => u.SalonId == mappedData.salonId && u.CustomerUserId == currentUserId)).FirstOrDefault() != null ? true : false;
                            salonResponse.Add(mappedData);
                        }
                    }
                    var salonIds = salonList.Select(a => a.SalonId);
                    var nearBysalon = await _context.SalonDetail.Where(u => !salonIds.Contains(u.SalonId) && u.IsDeleted != true).ToListAsync();
                    var nearBysalonResponse = new List<CustomerSalonListDTO>();
                    foreach (var item in nearBysalon)
                    {
                        SalonDetail? salonDetail = new SalonDetail();
                        if (string.IsNullOrEmpty(salonType))
                        {
                            salonDetail = await _context.SalonDetail.Where(u => (u.SalonId == item.SalonId) && (u.IsDeleted != true)).FirstOrDefaultAsync();
                        }
                        else if (salonType == "Male" || salonType == "Female" || salonType == "Unisex")
                        {
                            salonDetail = await _context.SalonDetail.Where(u => (u.SalonId == item.SalonId)
                            && (u.IsDeleted != true)
                            && (u.SalonType == salonType)
                            ).FirstOrDefaultAsync();
                        }
                        else
                        {
                            salonDetail = await _context.SalonDetail.Where(u => (u.SalonId == item.SalonId) && (u.IsDeleted != true)).FirstOrDefaultAsync();
                        }

                        if (salonDetail != null)
                        {
                            var vendorDetail = _userManager.FindByIdAsync(salonDetail.VendorId).GetAwaiter().GetResult();
                            var mappedData = _mapper.Map<CustomerSalonListDTO>(salonDetail);
                            mappedData.vendorName = vendorDetail.FirstName + " " + vendorDetail.LastName;

                            double endLat = Convert.ToDouble(salonDetail.AddressLatitude != null ? salonDetail.AddressLatitude : "0");
                            double endLong = Convert.ToDouble(salonDetail.AddressLongitude != null ? salonDetail.AddressLongitude : "0");

                            var APIResponse = CommonMethod.GoogleDistanceMatrixAPILatLonAsync(startLat, startLong, endLat, endLong).GetAwaiter().GetResult();
                            mappedData.distance = APIResponse.distance;
                            mappedData.duration = APIResponse.duration;
                            mappedData.isSalonAdded = false;
                            mappedData.favoritesStatus = (_context.FavouriteSalon.Where(u => u.SalonId == mappedData.salonId && u.CustomerUserId == currentUserId)).FirstOrDefault() != null ? true : false;

                            nearBysalonResponse.Add(mappedData);
                        }
                    }

                    if (!string.IsNullOrEmpty(searchBy))
                    {
                        nearBysalonResponse = nearBysalonResponse.Where(x => (x.salonName?.IndexOf(searchBy, StringComparison.OrdinalIgnoreCase) >= 0)).ToList();
                        salonResponse = salonResponse.Where(x => (x.salonName?.IndexOf(searchBy, StringComparison.OrdinalIgnoreCase) >= 0)).ToList();
                    }


                    var res = new AllCustomerSalonList();
                    res.customerSalonList = salonResponse.OrderBy(u => Convert.ToDecimal(u.distance != null ? (u.distance.IndexOf("km") != -1 ? u.distance.Replace(" km", "") : u.distance.Replace(" m", "")) : 0)).ToList();
                    res.nearByCustomerSalonList = nearBysalonResponse.OrderBy(u => Convert.ToDecimal(u.distance != null ? (u.distance.IndexOf("km") != -1 ? u.distance.Replace(" km", "") : u.distance.Replace(" m", "")) : 0)).ToList();

                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = true;
                    _response.Data = res;
                    _response.Messages = "Salon" + ResponseMessages.msgListFoundSuccess;
                    return Ok(_response);
                }
                else
                {
                    salonList = salonList.OrderByDescending(u => u.CreateDate).ToList();
                    var customerAdress = await _context.CustomerAddress.Where(u => u.CustomerUserId == currentUserId && u.Status == true).FirstOrDefaultAsync();

                    if (customerAdress != null && liveLocation != 1)
                    {
                        startLat = Convert.ToDouble(customerAdress.AddressLatitude != null ? customerAdress.AddressLatitude : "0");
                        startLong = Convert.ToDouble(customerAdress.AddressLongitude != null ? customerAdress.AddressLongitude : "0");
                    }
                    var salonResponse = new List<CustomerSalonListDTO>();
                    foreach (var item in salonList)
                    {
                        // var salonDetail = await _context.SalonDetail.Where(u => (u.SalonId == item.SalonId) && (u.IsDeleted != true)).FirstOrDefaultAsync();
                        SalonDetail? salonDetail = new SalonDetail();
                        if (string.IsNullOrEmpty(salonType))
                        {
                            salonDetail = await _context.SalonDetail.Where(u => (u.SalonId == item.SalonId) && (u.IsDeleted != true)).FirstOrDefaultAsync();
                        }
                        else if (salonType == "Male" || salonType == "Female" || salonType == "Unisex")
                        {
                            salonDetail = await _context.SalonDetail.Where(u => (u.SalonId == item.SalonId)
                            && (u.IsDeleted != true)
                            && (u.SalonType == salonType)
                            ).FirstOrDefaultAsync();
                        }
                        else
                        {
                            salonDetail = await _context.SalonDetail.Where(u => (u.SalonId == item.SalonId) && (u.IsDeleted != true)).FirstOrDefaultAsync();
                        }
                        if (salonDetail != null)
                        {
                            var vendorDetail = _userManager.FindByIdAsync(salonDetail.VendorId).GetAwaiter().GetResult();
                            var mappedData = _mapper.Map<CustomerSalonListDTO>(salonDetail);
                            mappedData.vendorName = vendorDetail.FirstName + " " + vendorDetail.LastName;

                            if (startLat != 0 && startLong != 0)
                            {
                                double endLat = Convert.ToDouble(salonDetail.AddressLatitude != null ? salonDetail.AddressLatitude : "0");
                                double endLong = Convert.ToDouble(salonDetail.AddressLongitude != null ? salonDetail.AddressLongitude : "0");

                                var APIResponse = CommonMethod.GoogleDistanceMatrixAPILatLonAsync(startLat, startLong, endLat, endLong).GetAwaiter().GetResult();
                                mappedData.distance = APIResponse.distance;
                                mappedData.duration = APIResponse.duration;
                            }
                            mappedData.isSalonAdded = true;
                            mappedData.favoritesStatus = (_context.FavouriteSalon.Where(u => u.SalonId == mappedData.salonId && u.CustomerUserId == currentUserId)).FirstOrDefault() != null ? true : false;

                            salonResponse.Add(mappedData);
                        }
                    }

                    if (!string.IsNullOrEmpty(searchBy))
                    {
                        salonResponse = salonResponse.Where(x => (x.salonName?.IndexOf(searchBy, StringComparison.OrdinalIgnoreCase) >= 0)).ToList();
                    }

                    salonResponse = salonResponse.OrderBy(u => Convert.ToDecimal(u.distance != null ? (u.distance.IndexOf("km") != -1 ? u.distance.Replace(" km", "") : u.distance.Replace(" m", "")) : 0)).ToList();

                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = true;
                    _response.Data = salonResponse;
                    _response.Messages = "Salon" + ResponseMessages.msgListFoundSuccess;
                    return Ok(_response);
                }

                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = false;
                _response.Messages = ResponseMessages.msgNotFound + "record.";
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

                var salonList = await _context.FavouriteSalon.Where(u => u.CustomerUserId == currentUserId).ToListAsync();

                if (salonList.Count < 1)
                {
                    salonList = new List<FavouriteSalon>();
                }

                double startLong = 0;
                double startLat = 0;


                if (!string.IsNullOrEmpty(userProfileDetail.AddressLatitude) && !string.IsNullOrEmpty(userProfileDetail.AddressLongitude))
                {
                    startLat = Convert.ToDouble(userProfileDetail.AddressLatitude != null ? userProfileDetail.AddressLatitude : "0");
                    startLong = Convert.ToDouble(userProfileDetail.AddressLongitude != null ? userProfileDetail.AddressLongitude : "0");
                }
                else
                {
                    if (liveLocation == 1)
                    {
                        _response.StatusCode = HttpStatusCode.OK;
                        _response.IsSuccess = false;
                        _response.Messages = "Location not found.";
                        return Ok(_response);
                    }
                }

                salonList = salonList.OrderByDescending(u => u.CreateDate).ToList();
                var customerAdress = await _context.CustomerAddress.Where(u => u.CustomerUserId == currentUserId && u.Status == true).FirstOrDefaultAsync();

                if (customerAdress != null && liveLocation != 1)
                {
                    startLat = Convert.ToDouble(customerAdress.AddressLatitude != null ? customerAdress.AddressLatitude : "0");
                    startLong = Convert.ToDouble(customerAdress.AddressLongitude != null ? customerAdress.AddressLongitude : "0");
                }
                var salonResponse = new List<CustomerSalonListDTO>();
                foreach (var item in salonList)
                {
                    // var salonDetail = await _context.SalonDetail.Where(u => (u.SalonId == item.SalonId) && (u.IsDeleted != true)).FirstOrDefaultAsync();
                    SalonDetail? salonDetail = new SalonDetail();
                    if (string.IsNullOrEmpty(salonType))
                    {
                        salonDetail = await _context.SalonDetail.Where(u => (u.SalonId == item.SalonId) && (u.IsDeleted != true)).FirstOrDefaultAsync();
                    }
                    else if (salonType == "Male" || salonType == "Female" || salonType == "Unisex")
                    {
                        salonDetail = await _context.SalonDetail.Where(u => (u.SalonId == item.SalonId)
                        && (u.IsDeleted != true)
                        && (u.SalonType == salonType)
                        ).FirstOrDefaultAsync();
                    }
                    else
                    {
                        salonDetail = await _context.SalonDetail.Where(u => (u.SalonId == item.SalonId) && (u.IsDeleted != true)).FirstOrDefaultAsync();
                    }
                    if (salonDetail != null)
                    {
                        var vendorDetail = _userManager.FindByIdAsync(salonDetail.VendorId).GetAwaiter().GetResult();
                        var mappedData = _mapper.Map<CustomerSalonListDTO>(salonDetail);
                        mappedData.vendorName = vendorDetail.FirstName + " " + vendorDetail.LastName;

                        if (startLat != 0 && startLong != 0)
                        {
                            double endLat = Convert.ToDouble(salonDetail.AddressLatitude != null ? salonDetail.AddressLatitude : "0");
                            double endLong = Convert.ToDouble(salonDetail.AddressLongitude != null ? salonDetail.AddressLongitude : "0");

                            var APIResponse = CommonMethod.GoogleDistanceMatrixAPILatLonAsync(startLat, startLong, endLat, endLong).GetAwaiter().GetResult();
                            mappedData.distance = APIResponse.distance;
                            mappedData.duration = APIResponse.duration;
                        }
                        mappedData.isSalonAdded = true;
                        mappedData.favoritesStatus = (_context.FavouriteSalon.Where(u => u.SalonId == mappedData.salonId && u.CustomerUserId == currentUserId)).FirstOrDefault() != null ? true : false;

                        salonResponse.Add(mappedData);
                    }
                }

                if (!string.IsNullOrEmpty(searchBy))
                {
                    salonResponse = salonResponse.Where(x => (x.salonName?.IndexOf(searchBy, StringComparison.OrdinalIgnoreCase) >= 0)).ToList();
                }

                salonResponse = salonResponse.OrderBy(u => Convert.ToDecimal(u.distance != null ? (u.distance.IndexOf("km") != -1 ? u.distance.Replace(" km", "") : u.distance.Replace(" m", "")) : 0)).ToList();

                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Data = salonResponse;
                _response.Messages = "Favourite Salon" + ResponseMessages.msgListFoundSuccess;
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

                var customerDeatil = _mapper.Map<CustomerAddress>(model);
                customerDeatil.CustomerUserId = currentUserId;
                var allAdddresses = _context.CustomerAddress.Select(u => (u.CustomerUserId == currentUserId));
                if (allAdddresses == null)
                {
                    customerDeatil.Status = true;
                }
                _context.Add(customerDeatil);
                _context.SaveChanges();

                var response = _mapper.Map<CustomerAddressDTO>(customerDeatil);

                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Data = response;
                _response.Messages = "Address added successfully.";
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

                var checkCustomerDetail = await _context.CustomerAddress.FirstOrDefaultAsync(u => (u.CustomerAddressId == model.customerAddressId) && (u.AddressType == model.addressType));
                if (checkCustomerDetail != null)
                {
                    var checkExistingCustomerDetail = await _context.CustomerAddress.FirstOrDefaultAsync(u => (u.CustomerUserId == currentUserId)
                    && (u.AddressType == model.addressType) && (u.CustomerAddressId != model.customerAddressId));

                    if (checkExistingCustomerDetail != null)
                    {
                        _response.StatusCode = HttpStatusCode.OK;
                        _response.IsSuccess = false;
                        _response.Messages = "Address type has been already added.";
                        return Ok(_response);
                    }
                }
                if (checkCustomerDetail == null)
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = ResponseMessages.msgNotFound + "record.";

                    return Ok(_response);
                }

                var customerDeatil = _mapper.Map(model, checkCustomerDetail);

                _context.Update(customerDeatil);
                _context.SaveChanges();



                var response = _mapper.Map<CustomerAddressDTO>(customerDeatil);

                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Data = response;
                _response.Messages = "Address updated successfully.";
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

                _context.Remove(customerDetail);
                _context.SaveChanges();


                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Messages = "Address deleted successfully.";
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

                var response = _mapper.Map<CustomerAddressDTO>(customerDetail);

                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Data = response;
                _response.Messages = "Address detail shown successfully.";
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

                customerDetail.Status = model.status;
                if (model.status == true)
                {
                    var updateStatus = await _context.CustomerAddress.Where(u => (u.CustomerUserId == currentUserId) && (u.CustomerAddressId != model.customerAddressId)).ToListAsync();
                    if (updateStatus.Count > 0)
                    {
                        foreach (var item in updateStatus)
                        {
                            item.Status = false;
                            _context.Update(item);
                            _context.SaveChanges();

                        }
                    }
                }

                _context.Update(customerDetail);
                _context.SaveChanges();

                if (model.status == false)
                {
                    var allAdddresses = await _context.CustomerAddress.Where(u => (u.CustomerUserId == currentUserId)).ToListAsync();

                    var addressStatus = allAdddresses.Select(u => u.Status == true);
                    if (addressStatus.FirstOrDefault() == false)
                    {
                        allAdddresses[0].Status = true;
                        _context.Update(allAdddresses[0]);
                        _context.SaveChanges();
                    }
                }

                var response = _mapper.Map<CustomerAddressDTO>(customerDetail);

                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Data = response;
                _response.Messages = "Address status updated successfully.";
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

                CustomerAddressDTO customerAddressDTO = new CustomerAddressDTO();
                var response = _mapper.Map<List<CustomerAddressDTO>>(customerDetail);

                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Data = response;
                _response.Messages = "Address list shown successfully.";
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

                // var orderDetail = await _context.OrderDetail.Where(u => u.CustomerUserId == currentUserId).FirstOrDefaultAsync();
                // if (orderDetail != null)
                // {
                //     //update Service quantity
                //     var BookedService = await _orderServicesRepository.GetAllAsync(u => u.AppointmentId == orderDetail.AppointmentId);
                //     foreach (var item in BookedService)
                //     {
                //         var ServiceCount = item.ServiceCountInCart != null ? item.ServiceCountInCart : 0;
                //         var ServiceDetail = await _ServiceRepository.GetAsync(u => u.ServiceId == item.ServiceId);
                //         ServiceDetail.InStock = (int)(ServiceDetail.InStock + ServiceCount);
                //         await _ServiceRepository.UpdateService(ServiceDetail);
                //     }
                //     orderDetail.CancelledBy = "Customer";
                //     orderDetail.AppointmentStatus = AppointmentStatus.Cancelled.ToString();

                //     _context.Update(orderDetail);
                //     await _context.SaveChangesAsync();
                // }

                //Service.Status = Convert.ToInt32(ServiceStatus.InActive);
                // await _userManager.UpdateSecurityStampAsync(currentUserDetail);

                var cartServices = await _context.Cart.Where(u => u.CustomerUserId == currentUserId).ToListAsync();
                foreach (var item in cartServices)
                {
                    _context.Remove(item);
                    await _context.SaveChangesAsync();
                }

                var favouriteService = await _context.FavouriteService.Where(u => u.CustomerUserId == currentUserId).ToListAsync();
                foreach (var item in favouriteService)
                {
                    _context.Remove(item);
                    await _context.SaveChangesAsync();
                }

                user.IsDeleted = true;

                _context.Update(user);
                await _context.SaveChangesAsync();

                currentUserDetail.Email = DateTime.Now.Ticks + "deleted" + currentUserDetail.Email;
                currentUserDetail.UserName = (DateTime.Now.Ticks).ToString();
                currentUserDetail.NormalizedUserName = DateTime.Now.Ticks + "deleted" + currentUserDetail.Email;
                currentUserDetail.PhoneNumber = DateTime.Now.Ticks + "001" + currentUserDetail.PhoneNumber;
                currentUserDetail.SecurityStamp = CommonMethod.RandomString(20);

                var res = await _userManager.UpdateAsync(currentUserDetail);

                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Messages = "Account deleted successfully.";
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

                Cart? cartDeatil;
                CartServicesDTO? response;
                string responseMessage;

                var getCartDetail = await _context.Cart.Where(u => (u.ServiceId == model.serviceId) && (u.SlotId == model.slotId) && (u.CustomerUserId == currentUserId)).FirstOrDefaultAsync();
                if (getCartDetail != null)
                {
                    getCartDetail.ServiceCountInCart = getCartDetail.ServiceCountInCart + 1;
                    if (checkTimeSlot.SlotCount < getCartDetail.ServiceCountInCart)
                    {
                        _response.StatusCode = HttpStatusCode.OK;
                        _response.IsSuccess = false;
                        _response.Messages = "Slot is unavailable.";
                        return Ok(_response);
                    }
                    else
                    {
                        _context.Update(getCartDetail);
                        await _context.SaveChangesAsync();
                    }

                    response = _mapper.Map<CartServicesDTO>(getCartDetail);
                }
                else
                {
                    cartDeatil = _mapper.Map<Cart>(model);
                    cartDeatil.CustomerUserId = currentUserId;
                    cartDeatil.ServiceCountInCart = 1;
                    cartDeatil.SalonId = checkServiceDetail.SalonId;
                    if (checkTimeSlot.SlotCount < cartDeatil.ServiceCountInCart)
                    {
                        _response.StatusCode = HttpStatusCode.OK;
                        _response.IsSuccess = false;
                        _response.Messages = "Slot is unavailable.";
                        return Ok(_response);
                    }
                    else
                    {
                        await _context.AddAsync(cartDeatil);
                        await _context.SaveChangesAsync();
                    }
                    response = _mapper.Map<CartServicesDTO>(cartDeatil);
                }

                if (checkServiceDetail != null)
                {
                    _mapper.Map(checkServiceDetail, response);
                    _mapper.Map(checkTimeSlot, response);
                    response.serviceImage = checkServiceDetail.ServiceIconImage;
                    response.slotDate = Convert.ToDateTime(response.slotDate).ToString(@"dd-MM-yyy");
                }

                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Data = response;
                _response.Messages = "Service added to cart successfully.";
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

                var getCartSalonIdList = await _context.Cart.Where(u => (u.CustomerUserId == currentUserId)).Select(u => u.SalonId).Distinct().ToListAsync();
                foreach (var item in getCartSalonIdList)
                {
                    var getCartDetail = await _context.Cart.Where(u => (u.CustomerUserId == currentUserId) && (u.SalonId == item.Value)).ToListAsync();
                    List<string> servideIds = new List<string>();
                    foreach (var item1 in getCartDetail)
                    {
                        var ServiceDetail = await _context.SalonService.Where(u => u.ServiceId == item1.ServiceId && u.Status == 1).FirstOrDefaultAsync();
                        if (ServiceDetail == null)
                        {
                            servideIds.Add((item1.ServiceId).ToString());
                        }
                    }
                    foreach (var id in servideIds)
                    {
                        var serviceId = Convert.ToInt32(id);
                        var getCartService = await _context.Cart.Where(u => (u.CustomerUserId == currentUserId) && (u.SalonId == item.Value) && (u.ServiceId == serviceId)).FirstOrDefaultAsync();
                        _context.Remove(getCartService);
                        await _context.SaveChangesAsync();
                    }
                }

                if (!string.IsNullOrEmpty(availableService))
                {
                    getCartSalonIdList = await _context.Cart.Where(u => (u.CustomerUserId == currentUserId)).Select(u => u.SalonId).Distinct().ToListAsync();
                    //remove service
                    foreach (var item in getCartSalonIdList)
                    {
                        var getCartDetail = await _context.Cart.Where(u => (u.CustomerUserId == currentUserId) && (u.SalonId == item.Value)).ToListAsync();
                        foreach (var Service in getCartDetail)
                        {
                            var ServiceDetail = await _context.SalonService.Where(u => u.ServiceId == Service.ServiceId && u.Status == 1).FirstOrDefaultAsync();

                            if (ServiceDetail != null)
                            {
                                var timeSlot = await _context.TimeSlot.Where(u => u.SlotId == Service.SlotId).FirstOrDefaultAsync();
                                if (timeSlot.Status == false || timeSlot.SlotCount < Service.ServiceCountInCart)
                                {
                                    _context.Cart.Remove(Service);
                                    await _context.SaveChangesAsync();
                                }
                            }
                            else
                            {
                                _context.Cart.Remove(Service);
                                await _context.SaveChangesAsync();
                            }
                        }
                    }

                    getCartSalonIdList = await _context.Cart.Where(u => (u.CustomerUserId == currentUserId)).Select(u => u.SalonId).Distinct().ToListAsync();
                    CartDetailDTO cartDetail = new CartDetailDTO();
                    var CartDetailPerSalonList = new List<CartDetailPerSalonDTO>();
                    foreach (var item in getCartSalonIdList)
                    {
                        var getCartDetail = await _context.Cart.Where(u => (u.CustomerUserId == currentUserId) && (u.SalonId == item.Value)).ToListAsync();
                        List<CartServicesDTO>? cartServiceList = new List<CartServicesDTO>();

                        cartServiceList = _mapper.Map<List<CartServicesDTO>>(getCartDetail);
                        foreach (var cartService in cartServiceList)
                        {
                            var ServiceDetail = await _context.SalonService.Where(u => u.ServiceId == cartService.serviceId && u.Status == 1).FirstOrDefaultAsync();
                            var timeSlot = await _context.TimeSlot.Where(u => u.SlotId == cartService.slotId).FirstOrDefaultAsync();
                            cartService.slotDate = timeSlot.SlotDate.ToString(@"dd-MM-yyyy");
                            cartService.fromTime = timeSlot.FromTime;
                            cartService.toTime = timeSlot.ToTime;
                            cartService.slotStatus = timeSlot.Status;
                            cartService.serviceImage = ServiceDetail.ServiceIconImage;
                            cartService.isSlotAvailable = timeSlot.SlotCount;
                        }

                        foreach (var item2 in cartServiceList)
                        {
                            var getServiceDetail = await _context.SalonService.Where(u => (u.ServiceId == item2.serviceId)).FirstOrDefaultAsync();

                            if (getServiceDetail != null)
                            {
                                _mapper.Map(getServiceDetail, item2);
                                item2.statusDisplay = ((ServiceStatus)getServiceDetail.Status).ToString();
                                item2.serviceImage = getServiceDetail.ServiceIconImage;
                                double basePrice = (double)item2.basePrice;
                                item2.basePrice = double.Parse((basePrice * item2.ServiceCountInCart).ToString("0.00"));
                                item2.listingPrice = double.Parse((item2.listingPrice * item2.ServiceCountInCart).ToString("0.00"));
                                item2.discount = double.Parse((cartDetail.totalMrp - cartDetail.totalSellingPrice).ToString("0.00"));
                                item2.serviceId = getServiceDetail.ServiceId;
                            }
                            var favoritesStatus = await _context.FavouriteService.Where(u => u.ServiceId == item2.serviceId && u.CustomerUserId == currentUserId).FirstOrDefaultAsync();
                            item2.favoritesStatus = favoritesStatus != null ? true : false;
                        }

                        // add to per Salon record
                        var CartDetailPerSalon = new CartDetailPerSalonDTO();
                        foreach (var item1 in cartServiceList)
                        {
                            CartDetailPerSalon.salonTotalItem = CartDetailPerSalon.salonTotalItem + 1;
                            CartDetailPerSalon.salonTotalMrp = (double)(CartDetailPerSalon.salonTotalMrp + item1.basePrice);
                            CartDetailPerSalon.salonTotalSellingPrice = double.Parse((CartDetailPerSalon.salonTotalSellingPrice + item1.listingPrice).ToString("0.00")); ;
                            CartDetailPerSalon.salonTotalDiscountAmount = double.Parse((CartDetailPerSalon.salonTotalMrp - CartDetailPerSalon.salonTotalSellingPrice).ToString("0.00"));//CartDetailPerSalon.SalonTotalMrp - CartDetailPerSalon.SalonTotalSellingPrice;
                            CartDetailPerSalon.salonTotalDiscount = double.Parse(((CartDetailPerSalon.salonTotalDiscountAmount * 100) / CartDetailPerSalon.salonTotalMrp).ToString("0.00"));//(CartDetailPerSalon.SalonTotalDiscountAmount * 100) / CartDetailPerSalon.SalonTotalMrp;
                        }
                        CartDetailPerSalon.cartServices = cartServiceList;
                        var SalonDetail = await _context.SalonDetail.Where(u => u.SalonId == item.Value).FirstOrDefaultAsync();
                        CartDetailPerSalon.salonId = SalonDetail.SalonId;
                        CartDetailPerSalon.salonName = SalonDetail.SalonName;
                        var customerAdress = await _context.CustomerAddress.Where(u => u.CustomerUserId == currentUserId && u.Status == true).FirstOrDefaultAsync();
                        double startLong = 0;
                        double startLat = 0;
                        if (customerAdress != null)
                        {
                            startLat = Convert.ToDouble(customerAdress.AddressLatitude != null ? customerAdress.AddressLatitude : "0");
                            startLong = Convert.ToDouble(customerAdress.AddressLongitude != null ? customerAdress.AddressLongitude : "0");
                        }
                        if (liveLocation == 1)
                        {
                            startLat = Convert.ToDouble(userProfileDetail.AddressLatitude != null ? userProfileDetail.AddressLatitude : "0");
                            startLong = Convert.ToDouble(userProfileDetail.AddressLongitude != null ? userProfileDetail.AddressLongitude : "0");
                        }
                        double endLat = Convert.ToDouble(SalonDetail.AddressLatitude != null ? SalonDetail.AddressLatitude : "0");
                        double endLong = Convert.ToDouble(SalonDetail.AddressLongitude != null ? SalonDetail.AddressLongitude : "0");

                        if (startLat != 0 && startLong != 0 && endLat != 0 && endLong != 0)
                        {
                            var APIResponse = CommonMethod.GoogleDistanceMatrixAPILatLonAsync(startLat, startLong, endLat, endLong).GetAwaiter().GetResult();
                            CartDetailPerSalon.distance = APIResponse.distance;
                            CartDetailPerSalon.duration = APIResponse.duration;
                        }
                        CartDetailPerSalonList.Add(CartDetailPerSalon);
                    }

                    foreach (var item3 in CartDetailPerSalonList)
                    {
                        cartDetail.totalItem = cartDetail.totalItem + item3.salonTotalItem;
                        cartDetail.totalMrp = double.Parse((cartDetail.totalMrp + item3.salonTotalMrp).ToString("0.00"));
                        cartDetail.totalSellingPrice = double.Parse((cartDetail.totalSellingPrice + item3.salonTotalSellingPrice).ToString("0.00")); ;
                        cartDetail.totalDiscountAmount = double.Parse((cartDetail.totalMrp - cartDetail.totalSellingPrice).ToString("0.00"));
                        cartDetail.totalDiscount = double.Parse(((cartDetail.totalDiscountAmount * 100) / cartDetail.totalMrp).ToString("0.00"));//(cartDetail.totalDiscountAmount * 100) / cartDetail.totalMrp
                    }
                    cartDetail.allCartServices = CartDetailPerSalonList;
                    cartDetail.salonCount = getCartSalonIdList.Count;

                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = true;
                    _response.Data = cartDetail;
                    _response.Messages = "Cart Services shown successfully.";
                    return Ok(_response);
                }
                else
                {
                    CartDetailDTO cartDetail = new CartDetailDTO();
                    var CartDetailPerSalonList = new List<CartDetailPerSalonDTO>();
                    getCartSalonIdList = await _context.Cart.Where(u => (u.CustomerUserId == currentUserId)).Select(u => u.SalonId).Distinct().ToListAsync();

                    foreach (var item in getCartSalonIdList)
                    {
                        var getCartDetail = await _context.Cart.Where(u => (u.CustomerUserId == currentUserId) && (u.SalonId == item.Value)).ToListAsync();
                        List<string> servideIds = new List<string>();
                        foreach (var item1 in getCartDetail)
                        {
                            var ServiceDetail = await _context.SalonService.Where(u => u.ServiceId == item1.ServiceId && u.Status == 1).FirstOrDefaultAsync();
                            if (ServiceDetail == null)
                            {
                                servideIds.Add((item1.ServiceId).ToString());
                            }
                        }
                        foreach (var id in servideIds)
                        {
                            var serviceId = Convert.ToInt32(id);
                            var getCartService = await _context.Cart.Where(u => (u.CustomerUserId == currentUserId) && (u.SalonId == item.Value) && (u.ServiceId == serviceId)).FirstOrDefaultAsync();
                            _context.Remove(getCartService);
                            await _context.SaveChangesAsync();
                        }
                        getCartDetail = await _context.Cart.Where(u => (u.CustomerUserId == currentUserId) && (u.SalonId == item.Value)).ToListAsync();
                        List<CartServicesDTO>? cartServiceList = new List<CartServicesDTO>();

                        cartServiceList = _mapper.Map<List<CartServicesDTO>>(getCartDetail);
                        foreach (var cartService in cartServiceList)
                        {
                            var ServiceDetail = await _context.SalonService.Where(u => u.ServiceId == cartService.serviceId && u.Status == 1).FirstOrDefaultAsync();
                            var timeSlot = await _context.TimeSlot.Where(u => u.SlotId == cartService.slotId).FirstOrDefaultAsync();
                            cartService.slotDate = timeSlot.SlotDate.ToString(@"dd-MM-yyyy");
                            cartService.fromTime = timeSlot.FromTime;
                            cartService.toTime = timeSlot.ToTime;//todocart

                            cartService.slotStatus = timeSlot.Status;
                            cartService.serviceImage = ServiceDetail.ServiceIconImage;//todocart
                            cartService.isSlotAvailable = timeSlot.SlotCount;
                        }

                        foreach (var item2 in cartServiceList)
                        {
                            var getServiceDetail = await _context.SalonService.Where(u => (u.ServiceId == item2.serviceId)).FirstOrDefaultAsync();

                            if (getServiceDetail != null)
                            {
                                _mapper.Map(getServiceDetail, item2);
                                item2.statusDisplay = ((ServiceStatus)getServiceDetail.Status).ToString();
                                item2.serviceImage = getServiceDetail.ServiceIconImage;
                                double basePrice = (double)item2.basePrice;
                                item2.basePrice = double.Parse((basePrice * item2.ServiceCountInCart).ToString("0.00"));
                                item2.listingPrice = double.Parse((item2.listingPrice * item2.ServiceCountInCart).ToString("0.00"));
                                item2.discount = double.Parse((cartDetail.totalMrp - cartDetail.totalSellingPrice).ToString("0.00"));
                                item2.serviceId = getServiceDetail.ServiceId;
                            }
                            var favoritesStatus = await _context.FavouriteService.Where(u => u.ServiceId == item2.serviceId && u.CustomerUserId == currentUserId).FirstOrDefaultAsync();
                            item2.favoritesStatus = favoritesStatus != null ? true : false;
                        }
                        // add to per Salon record
                        var CartDetailPerSalon = new CartDetailPerSalonDTO();
                        foreach (var item1 in cartServiceList)
                        {
                            CartDetailPerSalon.salonTotalItem = CartDetailPerSalon.salonTotalItem + 1;
                            CartDetailPerSalon.salonTotalMrp = (double)(CartDetailPerSalon.salonTotalMrp + item1.basePrice);
                            CartDetailPerSalon.salonTotalSellingPrice = double.Parse((CartDetailPerSalon.salonTotalSellingPrice + item1.listingPrice).ToString("0.00")); ;
                            CartDetailPerSalon.salonTotalDiscountAmount = double.Parse((CartDetailPerSalon.salonTotalMrp - CartDetailPerSalon.salonTotalSellingPrice).ToString("0.00"));//CartDetailPerSalon.SalonTotalMrp - CartDetailPerSalon.SalonTotalSellingPrice;
                            CartDetailPerSalon.salonTotalDiscount = double.Parse(((CartDetailPerSalon.salonTotalDiscountAmount * 100) / CartDetailPerSalon.salonTotalMrp).ToString("0.00"));//(CartDetailPerSalon.SalonTotalDiscountAmount * 100) / CartDetailPerSalon.SalonTotalMrp;
                        }
                        CartDetailPerSalon.cartServices = cartServiceList;
                        var SalonDetail = await _context.SalonDetail.Where(u => u.SalonId == item.Value).FirstOrDefaultAsync();
                        CartDetailPerSalon.salonId = SalonDetail.SalonId;
                        CartDetailPerSalon.salonName = SalonDetail.SalonName;
                        var customerAdress = await _context.CustomerAddress.Where(u => u.CustomerUserId == currentUserId && u.Status == true).FirstOrDefaultAsync();
                        double startLong = 0;
                        double startLat = 0;
                        if (customerAdress != null)
                        {
                            startLat = Convert.ToDouble(customerAdress.AddressLatitude != null ? customerAdress.AddressLatitude : "0");
                            startLong = Convert.ToDouble(customerAdress.AddressLongitude != null ? customerAdress.AddressLongitude : "0");
                        }
                        if (liveLocation == 1)
                        {
                            startLat = Convert.ToDouble(userProfileDetail.AddressLatitude != null ? userProfileDetail.AddressLatitude : "0");
                            startLong = Convert.ToDouble(userProfileDetail.AddressLongitude != null ? userProfileDetail.AddressLongitude : "0");
                        }
                        double endLat = Convert.ToDouble(SalonDetail.AddressLatitude != null ? SalonDetail.AddressLatitude : "0");
                        double endLong = Convert.ToDouble(SalonDetail.AddressLongitude != null ? SalonDetail.AddressLongitude : "0");

                        if (startLat != 0 && startLong != 0 && endLat != 0 && endLong != 0)
                        {
                            var APIResponse = CommonMethod.GoogleDistanceMatrixAPILatLonAsync(startLat, startLong, endLat, endLong).GetAwaiter().GetResult();
                            CartDetailPerSalon.distance = APIResponse.distance;
                            CartDetailPerSalon.duration = APIResponse.duration;
                        }
                        CartDetailPerSalonList.Add(CartDetailPerSalon);
                    }

                    foreach (var item3 in CartDetailPerSalonList)
                    {
                        cartDetail.totalItem = cartDetail.totalItem + item3.salonTotalItem;
                        cartDetail.totalMrp = double.Parse((cartDetail.totalMrp + item3.salonTotalMrp).ToString("0.00"));
                        cartDetail.totalSellingPrice = double.Parse((cartDetail.totalSellingPrice + item3.salonTotalSellingPrice).ToString("0.00")); ;
                        cartDetail.totalDiscountAmount = double.Parse((cartDetail.totalMrp - cartDetail.totalSellingPrice).ToString("0.00"));
                        cartDetail.totalDiscount = double.Parse(((cartDetail.totalDiscountAmount * 100) / cartDetail.totalMrp).ToString("0.00"));//(cartDetail.totalDiscountAmount * 100) / cartDetail.totalMrp
                    }
                    cartDetail.allCartServices = CartDetailPerSalonList;
                    cartDetail.salonCount = getCartSalonIdList.Count;

                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = true;
                    _response.Data = cartDetail;
                    _response.Messages = "Cart Services shown successfully.";
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

                var cartDetail = await _context.Cart.Where(u => u.CustomerUserId == currentUserId && u.ServiceId == serviceId).ToListAsync();
                if (cartDetail.Count > 0)
                {
                    if (slotId > 0 || cartDetail.Count == 1)
                    {
                        Cart? getCartDetail;
                        if (true)
                        {
                            if (slotId > 0)
                            {
                                getCartDetail = cartDetail.Where(u => (u.CustomerUserId == currentUserId) && u.SlotId == slotId && u.ServiceId == serviceId).FirstOrDefault();
                            }
                            else
                            {
                                getCartDetail = cartDetail.FirstOrDefault();
                            }

                            if (getCartDetail != null && getCartDetail.ServiceCountInCart == 1)
                            {
                                _context.Cart.Remove(getCartDetail);
                                await _context.SaveChangesAsync();
                            }
                            else
                            {
                                getCartDetail.ServiceCountInCart = getCartDetail.ServiceCountInCart - 1;
                                _context.Update(getCartDetail);
                                await _context.SaveChangesAsync();
                            }

                            _response.StatusCode = HttpStatusCode.OK;
                            _response.IsSuccess = true;
                            _response.Messages = "Service removed from cart.";
                            return Ok(_response);
                        }
                    }
                    else
                    {
                        List<CartServicesDTO>? cartServiceList = new List<CartServicesDTO>();

                        foreach (var cart in cartDetail)
                        {
                            var serviceDetail = await _context.SalonService.Where(u => u.ServiceId == cart.ServiceId).FirstOrDefaultAsync();
                            var mappedData = _mapper.Map<CartServicesDTO>(cart);
                            _mapper.Map(serviceDetail, mappedData);
                            mappedData.serviceImage = serviceDetail.ServiceIconImage;
                            var timeSlot = await _context.TimeSlot.Where(u => u.SlotId == cart.SlotId).FirstOrDefaultAsync();
                            mappedData.slotDate = timeSlot.SlotDate.ToString(@"dd-MM-yyyy");
                            mappedData.fromTime = timeSlot.FromTime;
                            mappedData.toTime = timeSlot.ToTime;
                            mappedData.slotStatus = timeSlot.Status;
                            var favoritesStatus = await _context.FavouriteService.Where(u => u.ServiceId == cart.ServiceId && u.CustomerUserId == currentUserId).FirstOrDefaultAsync();
                            mappedData.favoritesStatus = favoritesStatus != null ? true : false;
                            cartServiceList.Add(mappedData);
                        }

                        _response.StatusCode = HttpStatusCode.OK;
                        _response.IsSuccess = true;
                        _response.Messages = "Service list shown successfully.";
                        _response.Data = cartServiceList;
                        return Ok(_response);
                    }
                }
                else
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = "Service not found.";
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

                List<string> slotIds = new List<string>();
                if (!string.IsNullOrEmpty(model.slotIds))
                {
                    string[] splitSlotIds = model.slotIds.Split(",");
                    foreach (var item in splitSlotIds)
                    {
                        slotIds.Add(item);
                    }
                }
                var bookedServices = await _context.BookedService.Where(u => u.AppointmentId == model.appointmentId).ToListAsync();//&& u.AppointmentStatus != AppointmentStatus.Cancelled.ToString()
                if (bookedServices.Count > 0)
                {
                    if (slotIds.Count > 0 && model.cancelAllAppointments != true)
                    {
                        double? finalPrice = 0;
                        double? discount = 0;
                        double? cancelledPrice = 0;

                        foreach (var item in slotIds)
                        {
                            var slotIdInt = Convert.ToInt32(item);
                            BookedService? bookedService;
                            if (slotIdInt > 0)
                            {
                                bookedService = bookedServices.FirstOrDefault(x => x.AppointmentId == model.appointmentId && x.SlotId == slotIdInt);
                            }
                            else
                            {
                                bookedService = bookedServices.FirstOrDefault();
                            }

                            if (bookedService != null)
                            {
                                if (bookedService.AppointmentStatus == AppointmentStatus.Cancelled.ToString())
                                {
                                    _response.StatusCode = HttpStatusCode.OK;
                                    _response.IsSuccess = false;
                                    _response.Messages = "Appointment already cancelled.";
                                    return Ok(_response);
                                }

                                var timeSlot = await _context.TimeSlot.Where(u => u.SlotId == bookedService.SlotId).FirstOrDefaultAsync();
                                if (timeSlot.SlotCount == 0 && timeSlot.Status == false)
                                {
                                    timeSlot.Status = true;
                                }
                                timeSlot.SlotCount = (int)(timeSlot.SlotCount + bookedService.ServiceCountInCart);
                                _context.Update(timeSlot);
                                await _context.SaveChangesAsync();
                            }
                            finalPrice = finalPrice + bookedService.ListingPrice;
                            discount = discount + bookedService.Discount;

                            bookedService.FinalPrice = bookedService.FinalPrice - bookedService.ListingPrice;
                            bookedService.CancelledPrice = bookedService.ListingPrice + bookedService.Discount;
                            bookedService.Discount = bookedService.Discount - bookedService.Discount;

                            cancelledPrice = bookedService.CancelledPrice;
                            bookedService.AppointmentStatus = AppointmentStatus.Cancelled.ToString();

                            _context.Update(bookedService);
                            await _context.SaveChangesAsync();
                        }

                        var bookingServiceStatus = await _context.BookedService
                            .Where(u => u.AppointmentId == model.appointmentId &&
                                        (u.AppointmentStatus == AppointmentStatus.Scheduled.ToString() || u.AppointmentStatus == AppointmentStatus.Completed.ToString()))
                            .ToListAsync();

                        if (bookingServiceStatus.Count < 1)
                        {
                            appointmentDetail.AppointmentStatus = AppointmentStatus.Cancelled.ToString();
                            appointmentDetail.FinalPrice = appointmentDetail.FinalPrice - finalPrice;
                            appointmentDetail.Discount = appointmentDetail.Discount - discount;
                            appointmentDetail.CancelledPrice = appointmentDetail.CancelledPrice + cancelledPrice;
                            _context.Update(appointmentDetail);
                            await _context.SaveChangesAsync();
                        }
                        else
                        {
                            appointmentDetail.FinalPrice = appointmentDetail.FinalPrice - finalPrice;
                            appointmentDetail.Discount = appointmentDetail.Discount - discount;
                            appointmentDetail.CancelledPrice = appointmentDetail.CancelledPrice + cancelledPrice;

                            _context.Update(appointmentDetail);
                            await _context.SaveChangesAsync();
                        }

                        _response.StatusCode = HttpStatusCode.OK;
                        _response.IsSuccess = true;
                        _response.Messages = "Appointment cancelled successfully.";
                        return Ok(_response);
                    }
                    else
                    {
                        double? finalPrice = 0;
                        double? discount = 0;
                        double? cancelledPrice = 0;
                        bookedServices = bookedServices.Where(u => u.AppointmentStatus == "Scheduled").ToList();
                        foreach (var booked in bookedServices)
                        {
                            var timeSlot = await _context.TimeSlot.Where(u => u.SlotId == booked.SlotId).FirstOrDefaultAsync();
                            if (timeSlot.SlotCount == 0 && timeSlot.Status == false)
                            {
                                timeSlot.Status = true;
                            }
                            timeSlot.SlotCount = (int)(timeSlot.SlotCount + booked.ServiceCountInCart);
                            _context.Update(timeSlot);
                            await _context.SaveChangesAsync();

                            finalPrice = finalPrice + booked.ListingPrice;
                            discount = discount + booked.Discount;

                            booked.FinalPrice = booked.FinalPrice - booked.ListingPrice;
                            booked.CancelledPrice = booked.ListingPrice + booked.Discount;
                            booked.Discount = booked.Discount - booked.Discount;

                            cancelledPrice = cancelledPrice + booked.CancelledPrice;

                            booked.AppointmentStatus = AppointmentStatus.Cancelled.ToString();
                            _context.Update(booked);
                            await _context.SaveChangesAsync();
                        }

                        appointmentDetail.FinalPrice = appointmentDetail.FinalPrice - finalPrice;
                        appointmentDetail.Discount = appointmentDetail.Discount - discount;
                        appointmentDetail.CancelledPrice = appointmentDetail.CancelledPrice + cancelledPrice;
                        appointmentDetail.AppointmentStatus = AppointmentStatus.Cancelled.ToString();

                        _context.Update(appointmentDetail);
                        await _context.SaveChangesAsync();

                        _response.StatusCode = HttpStatusCode.OK;
                        _response.IsSuccess = true;
                        _response.Messages = "Appointment cancelled successfully.";
                        return Ok(_response);
                    }
                }
                else
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = "Appointment not found.";
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

                int serviceCount;
                double finalPrice = 0;

                var getCartItems = await _context.Cart.Where(u => (u.CustomerUserId == currentUserId)).ToListAsync();
                serviceCount = getCartItems.Count;
                foreach (var item in getCartItems)
                {
                    if (item.ServiceCountInCart > 1)
                    {
                        serviceCount = (int)(serviceCount + item.ServiceCountInCart - 1);
                    }
                    var serviceDetail = await _context.SalonService.Where(u => (u.ServiceId == item.ServiceId)).FirstOrDefaultAsync();

                    finalPrice = finalPrice + (double)(serviceDetail.ListingPrice * item.ServiceCountInCart);
                }

                return Ok(new
                {
                    StatusCode = HttpStatusCode.OK,
                    IsSuccess = true,
                    Data = new { totalCount = serviceCount, finalPrice = finalPrice },
                    Messages = "Total count and price shown successfully."
                });
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

                var CartDetailPerSalonList = new List<UnavailableServicesPerSalonDTO>();

                var getCartSalonIdList = await _context.Cart.Where(u => (u.CustomerUserId == currentUserId)).Select(u => u.SalonId).Distinct().ToListAsync();

                foreach (var item in getCartSalonIdList)
                {
                    var getCartDetail = await _context.Cart.Where(u => (u.CustomerUserId == currentUserId) && (u.SalonId == item.Value)).ToListAsync();
                    var cartServiceList = new List<CartServicesDTO>();
                    foreach (var item2 in getCartDetail)
                    {
                        var getServiceDetail = await _context.SalonService.Where(u => (u.ServiceId == item2.ServiceId) && (u.IsDeleted != true)).FirstOrDefaultAsync();

                        if (getServiceDetail != null)
                        {
                            var cartService = new CartServicesDTO();
                            _mapper.Map(getServiceDetail, cartService);
                            cartService.statusDisplay = ((ServiceStatus)getServiceDetail.Status).ToString();
                            cartService.serviceImage = getServiceDetail.ServiceIconImage;
                            var favoritesStatus = await _context.FavouriteService.Where(u => u.ServiceId == item2.ServiceId && u.CustomerUserId == currentUserId).FirstOrDefaultAsync();
                            cartService.favoritesStatus = favoritesStatus != null ? true : false;
                            var timeSlot = await _context.TimeSlot.Where(u => u.SlotId == item2.SlotId).FirstOrDefaultAsync();
                            cartService.slotDate = timeSlot.SlotDate.ToString(@"dd-MM-yyyy");
                            cartService.fromTime = timeSlot.FromTime;
                            cartService.toTime = timeSlot.ToTime;
                            cartService.slotStatus = timeSlot.Status;
                            cartService.slotId = timeSlot.SlotId;
                            if (timeSlot.Status != true || getServiceDetail.Status != 1)
                            {
                                if (timeSlot.SlotCount >= cartService.ServiceCountInCart)
                                {
                                    cartServiceList.Add(cartService);
                                }
                            }
                        }
                    }
                    // add to per Salon record
                    if (cartServiceList.Count > 0)
                    {
                        var CartDetailPerSalon = new UnavailableServicesPerSalonDTO();
                        CartDetailPerSalon.cartServices = cartServiceList;
                        var SalonDetail = await _context.SalonDetail.Where(u => u.SalonId == item.Value).FirstOrDefaultAsync();
                        CartDetailPerSalon.salonId = SalonDetail.SalonId;
                        CartDetailPerSalon.salonName = SalonDetail.SalonName;

                        var customerAdress = await _context.CustomerAddress.Where(u => u.CustomerUserId == currentUserId && u.Status == true).FirstOrDefaultAsync();
                        double startLong = 0;
                        double startLat = 0;
                        if (customerAdress != null)
                        {
                            startLat = Convert.ToDouble(customerAdress.AddressLatitude != null ? customerAdress.AddressLatitude : "0");
                            startLong = Convert.ToDouble(customerAdress.AddressLongitude != null ? customerAdress.AddressLongitude : "0");
                        }
                        if (liveLocation == 1)
                        {
                            startLat = Convert.ToDouble(userProfileDetail.AddressLatitude != null ? userProfileDetail.AddressLatitude : "0");
                            startLong = Convert.ToDouble(userProfileDetail.AddressLongitude != null ? userProfileDetail.AddressLongitude : "0");
                        }
                        double endLat = Convert.ToDouble(SalonDetail.AddressLatitude != null ? SalonDetail.AddressLatitude : "0");
                        double endLong = Convert.ToDouble(SalonDetail.AddressLongitude != null ? SalonDetail.AddressLongitude : "0");

                        if (startLat != 0 && startLong != 0 && endLat != 0 && endLong != 0)
                        {
                            var APIResponse = CommonMethod.GoogleDistanceMatrixAPILatLonAsync(startLat, startLong, endLat, endLong).GetAwaiter().GetResult();
                            CartDetailPerSalon.distance = APIResponse.distance;
                            CartDetailPerSalon.duration = APIResponse.duration;
                        }
                        CartDetailPerSalonList.Add(CartDetailPerSalon);
                    }
                }

                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Data = CartDetailPerSalonList;
                _response.Messages = "Service list shown successfully.";
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

                var favouriteSalon = await _context.FavouriteSalon.FirstOrDefaultAsync(u => u.SalonId == model.salonId && u.CustomerUserId == currentUserId);
                string msg = string.Empty;

                if (favouriteSalon == null)
                {
                    if (model.status == false)
                    {
                        _response.StatusCode = HttpStatusCode.OK;
                        _response.IsSuccess = false;
                        _response.Messages = ResponseMessages.msgNotFound + "Service";
                        return Ok(_response);
                    }

                    var addFavouriteSalon = new FavouriteSalon();
                    addFavouriteSalon.CustomerUserId = currentUserId;
                    addFavouriteSalon.SalonId = model.salonId;

                    _context.Add(addFavouriteSalon);
                    _context.SaveChanges();

                    msg = "Salon added to favorite.";
                }
                else
                {
                    if (model.status == true)
                    {
                        _response.StatusCode = HttpStatusCode.OK;
                        _response.IsSuccess = false;
                        _response.Messages = "Already added to favourite.";
                        return Ok(_response);
                    }

                    var entity = _context.Remove(favouriteSalon).Entity;
                    _context.SaveChanges();

                    msg = "Salon removed from favorite successfully.";
                }

                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Messages = msg;
                return Ok(_response);
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
                var customerAddress = await _context.CustomerAddress.Where(u => u.CustomerUserId == currentUserId && u.Status == true).FirstOrDefaultAsync();
                // get all cart Services

                //Remove service from cart if service is unavailable
                var getCartSalonIdList = await _context.Cart.Where(u => (u.CustomerUserId == currentUserId)).Select(u => u.SalonId).Distinct().ToListAsync();
                foreach (var item in getCartSalonIdList)
                {
                    var getCartDetail = await _context.Cart.Where(u => (u.CustomerUserId == currentUserId) && (u.SalonId == item.Value)).ToListAsync();
                    List<string> servideIds = new List<string>();
                    foreach (var item1 in getCartDetail)
                    {
                        var ServiceDetail = await _context.SalonService.Where(u => u.ServiceId == item1.ServiceId && u.Status == 1).FirstOrDefaultAsync();
                        if (ServiceDetail == null)
                        {
                            servideIds.Add((item1.ServiceId).ToString());
                        }
                    }
                    foreach (var id in servideIds)
                    {
                        var serviceId = Convert.ToInt32(id);
                        var getCartService = await _context.Cart.Where(u => (u.CustomerUserId == currentUserId) && (u.SalonId == item.Value) && (u.ServiceId == serviceId)).FirstOrDefaultAsync();
                        _context.Remove(getCartService);
                        await _context.SaveChangesAsync();
                    }
                }

                // get cart detail for booking
                var cartDetail = new List<Cart>();
                cartDetail = await _context.Cart.Where(u => u.CustomerUserId == currentUserId).ToListAsync();
                if (cartDetail.Count < 1)
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = "Not found any service in cart.";
                    return Ok(_response);
                }

                // delete unavailable service
                foreach (var item in cartDetail)
                {
                    var timeSlot = await _context.TimeSlot.Where(u => (u.ServiceId == item.ServiceId) && (u.SlotId == item.SlotId) && (u.Status == true)).FirstOrDefaultAsync();
                    var ServiceDetail = await _context.SalonService.Where(u => (u.ServiceId == item.ServiceId) && (u.Status == Convert.ToInt32(ServiceStatus.Active))).FirstOrDefaultAsync();
                    if (ServiceDetail != null)
                    {
                        if (timeSlot != null)
                        {
                            if (timeSlot.SlotCount < item.ServiceCountInCart)
                            {
                                _context.Remove(item);
                                await _context.SaveChangesAsync();
                                // _response.StatusCode = HttpStatusCode.OK;
                                // _response.IsSuccess = false;
                                // _response.Messages = "Slot unavailable.";
                                // return Ok(_response);
                            }
                        }
                        if (timeSlot == null)
                        {
                            _context.Remove(item);
                            await _context.SaveChangesAsync();
                            // _response.StatusCode = HttpStatusCode.OK;
                            // _response.IsSuccess = false;
                            // _response.Messages = "Slot unavailable.";
                            // return Ok(_response);
                        }
                    }
                    else
                    {
                        _response.StatusCode = HttpStatusCode.OK;
                        _response.IsSuccess = false;
                        _response.Messages = ResponseMessages.msgNotFound + "Service";
                        return Ok(_response);
                    }
                }

                //get available service
                var inStockCartServices = await _context.Cart.Where(u => u.CustomerUserId == currentUserId).ToListAsync();

                if (inStockCartServices.Count < 1)
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = "Service unavailable.";
                    return Ok(_response);
                }
                int totalServices = 0;
                double totalDiscount = 0;
                double finalPrice = 0;
                double totalPrice = 0;
                double basePrice = 0;
                double discount = 0;

                var appointmentDetail = new Appointment();
                appointmentDetail.CustomerUserId = currentUserId;
                appointmentDetail.TransactionId = "TX" + CommonMethod.GenerateOTP();
                appointmentDetail.CustomerFirstName = userDetail.FirstName;
                appointmentDetail.CustomerLastName = userDetail.LastName;
                appointmentDetail.AppointmentStatus = AppointmentStatus.Scheduled.ToString();
                appointmentDetail.PaymentStatus = model.paymentMethod == PaymentMethod.InCash.ToString() ? PaymentStatus.Unpaid.ToString() : PaymentStatus.Paid.ToString();
                appointmentDetail.CustomerAddress = customerAddress != null ? customerAddress.StreetAddresss : null;
                appointmentDetail.PhoneNumber = customerAddress != null ? customerAddress.PhoneNumber : userDetail.PhoneNumber;
                appointmentDetail.CancelledPrice = 0;
                appointmentDetail.BasePrice = 1;
                appointmentDetail.FinalPrice = 1;
                appointmentDetail.TotalDiscount = 1;
                appointmentDetail.Discount = 1;
                appointmentDetail.IsUpdated = true;
                appointmentDetail.TotalPrice = 1;

                if (!string.IsNullOrEmpty(model.paymentMethod))
                {
                    if (model.paymentMethod == PaymentMethod.PayByUPI.ToString())
                    {
                        if (model.paymentReceiptId > 0)
                        {
                            var paymentReceipt = await _context.PaymentReceipt.Where(u => u.PaymentReceiptId == model.paymentReceiptId).FirstOrDefaultAsync();
                            if (paymentReceipt == null)
                            {
                                _response.StatusCode = HttpStatusCode.OK;
                                _response.IsSuccess = false;
                                _response.Messages = ResponseMessages.msgNotFound + "record.";
                                return Ok(_response);
                            }
                            appointmentDetail.PaymentReceipt = paymentReceipt.PaymentReceiptImage;
                            appointmentDetail.PaymentMethod = model.paymentMethod;
                        }
                        else
                        {
                            _response.StatusCode = HttpStatusCode.OK;
                            _response.IsSuccess = false;
                            _response.Messages = "Please upload payment receipt.";
                            return Ok(_response);
                        }
                    }
                    else
                    {
                        appointmentDetail.PaymentMethod = PaymentMethod.InCash.ToString();
                    }
                }
                else
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = "Please enter payment method.";
                    return Ok(_response);
                }

                await _context.AddAsync(appointmentDetail);
                await _context.SaveChangesAsync();

                foreach (var item in inStockCartServices)
                {
                    var bookedService = new BookedService();
                    var ServiceDetail = await _context.SalonService.Where(u => u.ServiceId == item.ServiceId).FirstOrDefaultAsync();
                    bookedService.AppointmentId = appointmentDetail.AppointmentId;
                    bookedService.ServiceId = item.ServiceId;
                    bookedService.ServiceImage = ServiceDetail.ServiceIconImage;
                    bookedService.ServiceName = ServiceDetail.ServiceName;
                    bookedService.ListingPrice = ServiceDetail.ListingPrice * item.ServiceCountInCart;
                    bookedService.BasePrice = (double)ServiceDetail.BasePrice * item.ServiceCountInCart;
                    bookedService.Discount = ServiceDetail.Discount * item.ServiceCountInCart;
                    bookedService.TotalDiscount = ServiceDetail.Discount * item.ServiceCountInCart;
                    bookedService.SalonId = ServiceDetail.SalonId;
                    bookedService.CancelledPrice = 0;
                    bookedService.DurationInMinutes = ServiceDetail.DurationInMinutes;
                    var salonDetail = await _context.SalonDetail.FirstOrDefaultAsync(u => u.SalonId == ServiceDetail.SalonId);
                    bookedService.VendorId = salonDetail.VendorId;
                    var user = _userManager.FindByIdAsync(salonDetail.VendorId).GetAwaiter().GetResult();
                    bookedService.SalonName = bookedService.SalonName;
                    bookedService.VendorName = user.FirstName + " " + user.LastName;
                    basePrice = (double)(basePrice + bookedService.BasePrice);
                    finalPrice = (double)(finalPrice + bookedService.ListingPrice);
                    totalPrice = finalPrice;
                    totalServices = (int)(totalServices + item.ServiceCountInCart);
                    bookedService.FinalPrice = bookedService.ListingPrice;
                    bookedService.TotalPrice = bookedService.ListingPrice;
                    var slotDetail = await _context.TimeSlot.Where(u => u.SlotId == item.SlotId).FirstOrDefaultAsync();
                    bookedService.AppointmentDate = slotDetail.SlotDate;
                    bookedService.FromTime = slotDetail.FromTime;
                    bookedService.ToTime = slotDetail.ToTime;
                    bookedService.AppointmentStatus = AppointmentStatus.Scheduled.ToString();
                    vendorId = salonDetail.VendorId;
                    bookedService.SlotId = item.SlotId;
                    bookedService.ServiceCountInCart = item.ServiceCountInCart;

                    await _context.BookedService.AddAsync(bookedService);
                    await _context.SaveChangesAsync();

                    // slotDetail.SlotCount = (int)(slotDetail.SlotCount - bookedService.ServiceCountInCart);
                    // slotDetail.Status = slotDetail.SlotCount == 0 ? false : slotDetail.Status;
                    // _context.Update(slotDetail);
                    // await _context.SaveChangesAsync();

                }

                appointmentDetail.BasePrice = basePrice;
                appointmentDetail.FinalPrice = finalPrice;
                appointmentDetail.TotalPrice = totalPrice;
                appointmentDetail.TotalDiscount = basePrice - finalPrice;
                appointmentDetail.Discount = basePrice - finalPrice;
                appointmentDetail.TotalServices = totalServices;

                appointmentDetail.IsUpdated = true;
                _context.Appointment.Update(appointmentDetail);
                await _context.SaveChangesAsync();

                var response = _mapper.Map<AppointmentDetailDTO>(appointmentDetail);

                foreach (var item in inStockCartServices)
                {
                    var updateTimeSlot = await _context.TimeSlot.Where(u => (u.ServiceId == item.ServiceId) && (u.SlotId == item.SlotId)).FirstOrDefaultAsync();
                    updateTimeSlot.SlotCount = (int)(updateTimeSlot.SlotCount - item.ServiceCountInCart);
                    if (updateTimeSlot.SlotCount == 0)
                    {
                        updateTimeSlot.Status = false;
                    }
                    _context.Update(updateTimeSlot);
                    await _context.SaveChangesAsync();

                    _context.Cart.Remove(item);
                    await _context.SaveChangesAsync();
                }

                var stockDetail = inStockCartServices.FirstOrDefault();
                var selectedSlotDetail = await _context.TimeSlot.Where(u => (u.ServiceId == stockDetail.ServiceId) && (u.SlotId == stockDetail.SlotId)).FirstOrDefaultAsync();
                // send Notification

                // string notificationMessage = "Dear {0}, \nYou have received a new appointment from {1}.";

                var vendorDetail = await _context.UserDetail.Where(a => (a.UserId == vendorId) && (a.IsDeleted != true)).FirstOrDefaultAsync();
                var customerDetail = await _context.UserDetail.Where(a => (a.UserId == currentUserId) && (a.IsDeleted != true)).FirstOrDefaultAsync();
                var customerprofileDetail = _userManager.FindByIdAsync(customerDetail.UserId).GetAwaiter().GetResult();
                var vendorprofileDetail = _userManager.FindByIdAsync(vendorDetail.UserId).GetAwaiter().GetResult();
                var token = vendorDetail.Fcmtoken;
                var title = "New appointment received";
                var description = String.Format("Hi {0},\nYou have a new appointment request from {1} for {2}, at {3}.", vendorprofileDetail.FirstName, customerprofileDetail.FirstName, selectedSlotDetail.SlotDate.ToString(@"dd-MM-yyyy"), selectedSlotDetail.FromTime);
                if (!string.IsNullOrEmpty(token))
                {
                    // if (user.IsNotificationEnabled == true)
                    // {
                    var resp = await _mobileMessagingClient.SendNotificationAsync(token, title, description);
                }
                // if (!string.IsNullOrEmpty(resp))
                // {
                // update notification sent
                var notificationSent = new NotificationSent();
                notificationSent.Title = title;
                notificationSent.Description = description;
                notificationSent.NotificationType = NotificationType.Appointment.ToString();
                notificationSent.UserId = vendorDetail.UserId;

                await _context.AddAsync(notificationSent);
                await _context.SaveChangesAsync();
                // }
                // }


                // StringBuilder messageBuilder = new StringBuilder();

                // messageBuilder.AppendLine();
                // messageBuilder.AppendLine("Order Detail:");
                // messageBuilder.AppendLine($"Order Number: {response.AppointmentId}");
                // messageBuilder.AppendLine($"Order Date: {response.OrderDate}");
                // messageBuilder.AppendLine($"Total Amount: \u20B9{response.TotalSellingPrice}");
                // messageBuilder.AppendLine($"Delivery Type: {response.DeliveryType}");
                // messageBuilder.AppendLine($"Total Services: {response.TotalServices}");
                // messageBuilder.AppendLine();
                // messageBuilder.AppendLine("Service List:");

                // var BookedService = await _orderServicesRepository.GetAllAsync(u => u.AppointmentId == response.AppointmentId);
                // foreach (var item in BookedService)
                // {
                //     messageBuilder.AppendLine($"- Service Name: {item.ServiceName}");
                //     // messageBuilder.AppendLine($"- SKUID: {item.Skuid}");
                //     messageBuilder.AppendLine($"- Amount: \u20B9{item.TotalSellingPrice}");
                //     messageBuilder.AppendLine($"- Service Count: {item.ServiceCountInCart}");
                //     messageBuilder.AppendLine();
                // }


                // if (!string.IsNullOrEmpty(vendorprofileDetail.PhoneNumber))
                // {
                //     var url = "https://api.ultramsg.com/instance54002/messages/chat";
                //     var client = new RestClient(url);

                //     var request = new RestRequest(url, Method.Post);
                //     request.AddHeader("content-type", "application/x-www-form-urlencoded");
                //     request.AddParameter("token", "r0ztj9cky7ry6vjf");
                //     request.AddParameter("to", "+91" + vendorprofileDetail.PhoneNumber);
                //     request.AddParameter("body", description + messageBuilder.ToString());

                //     RestResponse response1 = await client.ExecuteAsync(request);
                //     var output = response1.Content;
                // }
                // Console.WriteLine(output);
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Data = response;
                _response.Messages = "Appointment booked successfully.";
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

                DateTime fromDate = DateTime.Now;
                DateTime toDate = DateTime.Now;

                List<Appointment>? appointmentList;
                string appointmentTitle = "";
                string appointmentDescription = "";
                // int totalServices = 0;
                appointmentList = (await _context.Appointment.Where(u => u.CustomerUserId == currentUserId).ToListAsync()).OrderByDescending(u => u.CreateDate).ToList();

                if (model.fromDate != null && model.toDate != null)
                {
                    if (!CommonMethod.IsValidDateFormat_ddmmyyyy(model.fromDate) || !CommonMethod.IsValidDateFormat_ddmmyyyy(model.toDate))
                    {
                        _response.StatusCode = HttpStatusCode.OK;
                        _response.IsSuccess = false;
                        _response.Messages = "Please enter date in dd-MM-yyyy format.";
                        return Ok(_response);
                    }
                    fromDate = DateTime.ParseExact(model.fromDate, "dd-MM-yyyy", null);
                    toDate = DateTime.ParseExact(model.toDate, "dd-MM-yyyy", null);
                    appointmentList = appointmentList.Where(x => (x.CreateDate.Date >= fromDate) && (x.CreateDate.Date <= toDate)).ToList();
                }

                var response = _mapper.Map<List<CustomerAppointmentedListDTO>>(appointmentList);
                foreach (var item in response)
                {
                    List<BookedService>? bookedServices;
                    string appointmentStatus;
                    bookedServices = await _context.BookedService.Where(u => u.AppointmentId == item.appointmentId).OrderByDescending(u => u.AppointmentDate).ToListAsync();

                    int serviceId = 0;
                    int timeValue = 0;
                    foreach (var item2 in bookedServices)
                    {
                        var slotDetail = await _context.TimeSlot.Where(u => u.SlotId == item2.SlotId).FirstOrDefaultAsync();
                        TimeSpan appointmentFromTime = Convert.ToDateTime(slotDetail.FromTime).TimeOfDay;
                        string appointmentDate = item2.AppointmentDate.ToString("dd-MM-yyyy");
                        DateTime appointmentDateTime = DateTime.ParseExact(appointmentDate, "dd-MM-yyyy", CultureInfo.InvariantCulture);
                        appointmentDateTime = appointmentDateTime.Add(appointmentFromTime);
                        TimeSpan timeSpan = appointmentDateTime - convrtedZoneDate;
                        int difference = Convert.ToInt32(timeSpan.TotalMinutes);
                        if (timeValue != 0)
                        {
                            if (difference > 0 && difference < timeValue)
                            {
                                serviceId = (int)item2.ServiceId;
                                timeValue = difference;
                            }
                        }
                        else
                        {
                            serviceId = (int)item2.ServiceId;
                            timeValue = difference;
                        }
                    }
                    // Move the item with targetServiceId to the top of the list
                    var topService = bookedServices.FirstOrDefault(u => u.ServiceId == serviceId);

                    if (topService != null)
                    {
                        bookedServices.Remove(topService);
                        bookedServices.Insert(0, topService);
                    }
                    if (bookedServices.Count > 0)
                    {
                        var scheduledList = bookedServices.Where(a => a.AppointmentStatus == AppointmentStatus.Scheduled.ToString()).ToList();
                        var completedList = bookedServices.Where(a => a.AppointmentStatus == AppointmentStatus.Completed.ToString()).ToList();
                        var cancelledList = bookedServices.Where(a => a.AppointmentStatus == AppointmentStatus.Cancelled.ToString()).ToList();
                        item.scheduleCount = scheduledList.Count;
                        item.completedCount = completedList.Count;
                        item.cancelledCount = cancelledList.Count;
                        if (scheduledList.Count > 0)
                        {
                            bookedServices = scheduledList;
                            appointmentStatus = AppointmentStatus.Scheduled.ToString();
                        }
                        else
                        {
                            if (completedList.Count < 1)
                            {
                                bookedServices = cancelledList;
                                appointmentStatus = AppointmentStatus.Cancelled.ToString();
                            }
                            else
                            {
                                bookedServices = completedList;
                                appointmentStatus = AppointmentStatus.Completed.ToString();
                            }
                        }
                        if (bookedServices.Count > 1)
                        {
                            appointmentDescription = (item.totalServices).ToString() + " services.";
                        }
                        else
                        {
                            appointmentDescription = bookedServices.FirstOrDefault().ServiceName;
                        }
                        var salonDetail = await _context.SalonDetail.Where(u => u.SalonId == bookedServices.FirstOrDefault().SalonId).FirstOrDefaultAsync();
                        var vendorDetail = await _context.Users.Where(u => u.Id == salonDetail.VendorId).FirstOrDefaultAsync();

                        // totalServices = (int)_context.BookedService.Where(a => a.AppointmentId == item.appointmentId).Sum(a => a.ServiceCountInCart);
                        item.salonName = salonDetail.SalonName;
                        item.salonLatitude = salonDetail.AddressLatitude;
                        item.salonLongitude = salonDetail.AddressLongitude;

                        var customerAdress = await _context.CustomerAddress.Where(u => u.CustomerUserId == currentUserId && u.Status == true).FirstOrDefaultAsync();
                        double startLong = 0;
                        double startLat = 0;
                        if (customerAdress != null)
                        {
                            startLat = Convert.ToDouble(customerAdress.AddressLatitude != null ? customerAdress.AddressLatitude : "0");
                            startLong = Convert.ToDouble(customerAdress.AddressLongitude != null ? customerAdress.AddressLongitude : "0");
                        }
                        if (model.liveLocation == 1)
                        {
                            startLat = Convert.ToDouble(userProfileDetail.AddressLatitude != null ? userProfileDetail.AddressLatitude : "0");
                            startLong = Convert.ToDouble(userProfileDetail.AddressLongitude != null ? userProfileDetail.AddressLongitude : "0");
                        }
                        double endLat = Convert.ToDouble(salonDetail.AddressLatitude != null ? salonDetail.AddressLatitude : "0");
                        double endLong = Convert.ToDouble(salonDetail.AddressLongitude != null ? salonDetail.AddressLongitude : "0");

                        if (startLat != 0 && startLong != 0 && endLat != 0 && endLong != 0)
                        {
                            var APIResponse = CommonMethod.GoogleDistanceMatrixAPILatLonAsync(startLat, startLong, endLat, endLong).GetAwaiter().GetResult();
                            item.distance = APIResponse.distance;
                            item.duration = APIResponse.duration;
                        }
                        item.salonAddress = salonDetail.SalonAddress;
                        item.appointmentTitle = salonDetail.SalonName;
                        item.salonPhoneNumber = vendorDetail.PhoneNumber;
                        item.appointmentDescription = appointmentDescription;
                        // item.totalServices = totalServices;
                        item.serviceImage = bookedServices.FirstOrDefault().ServiceImage;
                        item.appointmentFromTime = bookedServices.FirstOrDefault().FromTime;
                        item.appointmentToTime = bookedServices.FirstOrDefault().ToTime;
                        item.appointmentDate = bookedServices.FirstOrDefault().AppointmentDate.ToString(@"dd-MM-yyyy");
                        TimeSpan appointmentFromTime = Convert.ToDateTime(item.appointmentFromTime).TimeOfDay;
                        string appointmentDate = item.appointmentDate;
                        DateTime appointmentDateTime = DateTime.ParseExact(appointmentDate, "dd-MM-yyyy", CultureInfo.InvariantCulture);
                        appointmentDateTime = appointmentDateTime.Add(appointmentFromTime);
                        item.appointmentDateTime = appointmentDateTime;
                        var favoritesStatus = await _context.FavouriteService.Where(u => u.ServiceId == bookedServices.FirstOrDefault().ServiceId && u.CustomerUserId == currentUserId).FirstOrDefaultAsync();
                        item.favoritesStatus = favoritesStatus != null ? true : false;
                    }

                }

                if (!string.IsNullOrEmpty(model.paymentStatus))
                {
                    response = response.Where(x => (x.paymentStatus == model.paymentStatus)
                    ).ToList();
                }
                if (!string.IsNullOrEmpty(model.appointmentStatus))
                {
                    response = response.Where(x => (x.appointmentStatus == model.appointmentStatus)
                    ).ToList();
                }

                if (!string.IsNullOrEmpty(model.searchQuery))
                {
                    response = response.Where(x => (x.appointmentTitle?.IndexOf(model.searchQuery, StringComparison.OrdinalIgnoreCase) >= 0)
                    ).ToList();
                }

                if (model.sortDateBy == 2)
                {
                    if (model.fromDate != null && model.toDate != null)
                    {
                        response = response.Where(x => (Convert.ToDateTime(x.appointmentDate).Date >= fromDate) && (Convert.ToDateTime(x.appointmentDate) <= toDate)).OrderByDescending(x => Convert.ToDateTime(x.appointmentDate)).ToList();
                    }

                }
                response = response
                    .OrderByDescending(u => u.appointmentStatus == "Scheduled")
                    .ThenByDescending(u => u.appointmentDateTime)
                    .ToList();

                foreach (var item in response.Where(x => x.appointmentStatus == "Cancelled" && x.cancelledPrice == x.basePrice))
                {
                    item.finalPrice = (item.totalPrice);
                }

                int appointmentId = 0;
                int timeValue1 = 0;
                foreach (var item3 in response)
                {
                    TimeSpan timeSpan = item3.appointmentDateTime - convrtedZoneDate;
                    int difference = Convert.ToInt32(timeSpan.TotalMinutes);
                    if (timeValue1 != 0)
                    {
                        if (difference > 0 && difference < timeValue1)
                        {
                            appointmentId = (int)item3.appointmentId;
                            timeValue1 = difference;
                        }
                    }
                    else
                    {
                        appointmentId = (int)item3.appointmentId;
                        timeValue1 = difference;
                    }
                }
                // Move the item with targetServiceId to the top of the list
                var topResponse = response.FirstOrDefault(u => u.appointmentId == appointmentId);

                if (topResponse != null)
                {
                    response.Remove(topResponse);
                    response.Insert(0, topResponse);
                }

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
                var items = response.Skip((CurrentPage - 1) * PageSize).Take(PageSize).ToList();

                // if CurrentPage is greater than 1 means it has previousPage  
                var previousPage = CurrentPage > 1 ? "Yes" : "No";

                // if TotalPages is greater than CurrentPage means it has nextPage  
                var nextPage = CurrentPage < TotalPages ? "Yes" : "No";

                // Returing List of Customers Collections  
                FilterationResponseModel<CustomerAppointmentedListDTO> obj = new FilterationResponseModel<CustomerAppointmentedListDTO>();
                obj.totalCount = TotalCount;
                obj.pageSize = PageSize;
                obj.currentPage = CurrentPage;
                obj.totalPages = TotalPages;
                obj.previousPage = previousPage;
                obj.nextPage = nextPage;
                obj.searchQuery = string.IsNullOrEmpty(model.searchQuery) ? "no parameter passed" : model.searchQuery;
                obj.dataList = items.ToList();

                if (obj == null)
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = "Something went wrong.";
                    return Ok(_response);
                }

                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Data = obj;
                _response.Messages = "Appointment list shown successfully.";
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
                liveLocation = liveLocation != null ? liveLocation : 0;
                var userProfileDetail = await _context.UserDetail.Where(u => u.UserId == currentUserId).FirstOrDefaultAsync();

                string serviceDescription = "";
                var orderDetail = await _context.Appointment.Where(u => u.AppointmentId == appointmentId).FirstOrDefaultAsync();

                var response = _mapper.Map<AppointmentDetailDTO>(orderDetail);
                // convert datetime into india time zone
                var ctz = TZConvert.GetTimeZoneInfo("India Standard Time");
                var convrtedZoneDate = TimeZoneInfo.ConvertTimeFromUtc(Convert.ToDateTime(orderDetail.CreateDate), ctz);
                response.createDate = Convert.ToDateTime(convrtedZoneDate).ToString(@"hh:mm tt");
                response.createDate = Convert.ToDateTime(convrtedZoneDate).ToString(@"dd-MM-yyyy");

                var salonList = await _context.BookedService.Where(u => u.AppointmentId == response.appointmentId).ToListAsync();
                salonList = salonList.OrderByDescending(u => u.CreateDate).DistinctBy(u => u.SalonId).ToList();
                var bookedServicesPerShopList = new List<BookedServicesPerSalonDTO>();
                foreach (var item in salonList)
                {
                    var bookedServicePerShop = new BookedServicesPerSalonDTO();
                    var salonDetails = await _context.SalonDetail.FirstOrDefaultAsync(u => u.SalonId == item.SalonId);
                    var vendorDetail = await _context.Users.Where(u => u.Id == salonDetails.VendorId).FirstOrDefaultAsync();
                    bookedServicePerShop.salonName = salonDetails.SalonName;
                    bookedServicePerShop.salonId = salonDetails.SalonId;
                    bookedServicePerShop.salonImage = salonDetails.SalonImage;
                    bookedServicePerShop.salonPhoneNumber = vendorDetail.PhoneNumber;
                    bookedServicePerShop.salonLatitude = salonDetails.AddressLatitude;
                    bookedServicePerShop.salonLongitude = salonDetails.AddressLongitude;
                    bookedServicePerShop.salonAddress = salonDetails.SalonAddress;
                    bookedServicePerShop.totalDiscount = 0;
                    bookedServicePerShop.basePrice = 0;
                    bookedServicePerShop.finalPrice = 0;
                    bookedServicePerShop.totalPrice = 0;
                    bookedServicePerShop.discount = 0;
                    bookedServicePerShop.cancelledPrice = 0;

                    var serviceDetail = await _context.BookedService.Where(u => u.SalonId == item.SalonId && u.AppointmentId == item.AppointmentId).OrderByDescending(u => u.AppointmentDate).ToListAsync();

                    var serviceList = new List<BookedServicesDTO>();
                    foreach (var item1 in serviceDetail)
                    {
                        var service = _mapper.Map<BookedServicesDTO>(item1);
                        service.salonName = salonDetails.SalonName;
                        service.appointmentDate = item1.AppointmentDate.ToString(@"dd-MM-yyyy");
                        var slotDetail = await _context.TimeSlot.Where(u => u.SlotId == item1.SlotId).FirstOrDefaultAsync();
                        TimeSpan appointmentFromTime = Convert.ToDateTime(slotDetail.FromTime).TimeOfDay;
                        string appointmentDate = service.appointmentDate;
                        DateTime appointmentDateTime = DateTime.ParseExact(appointmentDate, "dd-MM-yyyy", CultureInfo.InvariantCulture);
                        appointmentDateTime = appointmentDateTime.Add(appointmentFromTime);
                        service.appointmentDateTime = appointmentDateTime;
                        service.createDate = item1.CreateDate.ToString(@"dd-MM-yyyy");
                        bookedServicePerShop.salonName = salonDetails.SalonName;
                        bookedServicePerShop.basePrice = bookedServicePerShop.basePrice + service.basePrice;
                        if (orderDetail.AppointmentStatus == "Cancelled" && orderDetail.CancelledPrice == orderDetail.BasePrice)
                        {
                            bookedServicePerShop.finalPrice = bookedServicePerShop.finalPrice + service.listingPrice;
                            bookedServicePerShop.cancelledPrice = 0;
                            bookedServicePerShop.totalPrice = bookedServicePerShop.totalPrice + service.totalPrice;
                            bookedServicePerShop.discount = bookedServicePerShop.discount + service.totalDiscount;
                            bookedServicePerShop.totalDiscount = bookedServicePerShop.totalDiscount + service.totalDiscount;
                        }
                        else
                        {
                            bookedServicePerShop.finalPrice = bookedServicePerShop.finalPrice + service.finalPrice;
                            bookedServicePerShop.cancelledPrice = bookedServicePerShop.cancelledPrice + service.cancelledPrice;
                            bookedServicePerShop.totalPrice = bookedServicePerShop.totalPrice + service.totalPrice;
                            bookedServicePerShop.discount = bookedServicePerShop.discount + service.discount;
                            bookedServicePerShop.totalDiscount = bookedServicePerShop.totalDiscount + service.totalDiscount;
                        }
                        bookedServicePerShop.serviceCountInCart = bookedServicePerShop.serviceCountInCart + service.serviceCountInCart;
                        var customerAdress = await _context.CustomerAddress.Where(u => u.CustomerUserId == currentUserId && u.Status == true).FirstOrDefaultAsync();
                        double startLong = 0;
                        double startLat = 0;
                        if (customerAdress != null)
                        {
                            startLat = Convert.ToDouble(customerAdress.AddressLatitude != null ? customerAdress.AddressLatitude : "0");
                            startLong = Convert.ToDouble(customerAdress.AddressLongitude != null ? customerAdress.AddressLongitude : "0");
                        }
                        if (liveLocation == 1)
                        {
                            startLat = Convert.ToDouble(userProfileDetail.AddressLatitude != null ? userProfileDetail.AddressLatitude : "0");
                            startLong = Convert.ToDouble(userProfileDetail.AddressLongitude != null ? userProfileDetail.AddressLongitude : "0");
                        }
                        double endLat = Convert.ToDouble(salonDetails.AddressLatitude != null ? salonDetails.AddressLatitude : "0");
                        double endLong = Convert.ToDouble(salonDetails.AddressLongitude != null ? salonDetails.AddressLongitude : "0");

                        if (startLat != 0 && startLong != 0 && endLat != 0 && endLong != 0)
                        {
                            var APIResponse = CommonMethod.GoogleDistanceMatrixAPILatLonAsync(startLat, startLong, endLat, endLong).GetAwaiter().GetResult();
                            bookedServicePerShop.distance = APIResponse.distance;
                            bookedServicePerShop.duration = APIResponse.duration;
                        }
                        var favoritesStatus = await _context.FavouriteSalon.FirstOrDefaultAsync(u => u.SalonId == service.salonId && u.CustomerUserId == currentUserId);
                        service.favoritesStatus = favoritesStatus != null ? true : false;

                        serviceList.Add(service);
                    }
                    bookedServicePerShop.AppointmentedServices = serviceList.OrderByDescending(u => u.appointmentDateTime).ToList();
                    bookedServicesPerShopList.Add(bookedServicePerShop);
                }

                response.appointmentFromSalon = bookedServicesPerShopList;
                if (response.appointmentStatus == "Cancelled")
                {
                    response.finalPrice = response.totalPrice;
                    response.cancelledPrice = 0;
                    response.totalPrice = response.totalPrice;
                    response.discount = response.totalDiscount;
                    response.totalDiscount = response.totalDiscount;
                }

                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Data = response;
                _response.Messages = "Appointment detail shown successfully.";
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

                var favouriteService = await _context.FavouriteService.FirstOrDefaultAsync(u => u.ServiceId == model.serviceId && u.CustomerUserId == currentUserId);
                string msg = string.Empty;

                if (favouriteService == null)
                {
                    if (model.status == false)
                    {
                        _response.StatusCode = HttpStatusCode.OK;
                        _response.IsSuccess = false;
                        _response.Messages = ResponseMessages.msgNotFound + "Service";
                        return Ok(_response);
                    }

                    var addFavouriteService = new FavouriteService();
                    addFavouriteService.CustomerUserId = currentUserId;
                    addFavouriteService.ServiceId = model.serviceId;

                    _context.Add(addFavouriteService);
                    _context.SaveChanges();

                    msg = "Service added to favorite.";
                }
                else
                {
                    if (model.status == true)
                    {
                        _response.StatusCode = HttpStatusCode.OK;
                        _response.IsSuccess = false;
                        _response.Messages = "Already added to favourite.";
                        return Ok(_response);
                    }

                    var entity = _context.Remove(favouriteService).Entity;
                    _context.SaveChanges();

                    msg = "Service removed from favorite successfully.";
                }

                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Messages = msg;
                return Ok(_response);
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

                var serviceIdList = await _context.FavouriteService.Where(u => u.CustomerUserId == currentUserId).Select(u => u.ServiceId).ToListAsync();

                var query = from t1 in _context.SalonService
                            join t2 in _context.MainCategory on t1.MainCategoryId equals t2.MainCategoryId
                            where t1.IsDeleted != true
                            where t1.Status == 1
                            where serviceIdList.Contains(t1.ServiceId)
                            where t1.SalonId == salonId
                            select new SalonServiceListDTO
                            {
                                serviceName = t1.ServiceName,
                                serviceId = t1.ServiceId,
                                vendorId = _context.SalonDetail.Where(u => u.SalonId == (t1.SalonId != null ? t1.SalonId : 0)).Select(u => u.VendorId).FirstOrDefault(),
                                salonId = t1.SalonId,
                                salonName = _context.SalonDetail.Where(u => u.SalonId == (t1.SalonId != null ? t1.SalonId : 0)).Select(u => u.SalonName).FirstOrDefault(),
                                mainCategoryId = t1.MainCategoryId,
                                mainCategoryName = t2.CategoryName,
                                subCategoryId = t1.SubcategoryId,
                                subCategoryName = _context.SubCategory.Where(u => u.SubCategoryId == (t1.SubcategoryId != null ? t1.SubcategoryId : 0)).Select(u => u.CategoryName).FirstOrDefault(),
                                serviceDescription = t1.ServiceDescription,
                                serviceImage = t1.ServiceIconImage,
                                listingPrice = t1.ListingPrice,
                                basePrice = (double)t1.BasePrice,
                                favoritesStatus = (_context.FavouriteService.Where(u => u.ServiceId == t1.ServiceId && u.CustomerUserId == currentUserId)).FirstOrDefault() != null ? true : false,
                                discount = t1.Discount,
                                genderPreferences = t1.GenderPreferences,
                                ageRestrictions = t1.AgeRestrictions,
                                ServiceType = t1.ServiceType,
                                totalCountPerDuration = t1.TotalCountPerDuration,
                                durationInMinutes = t1.DurationInMinutes,
                                status = t1.Status,
                                isSlotAvailable = _context.TimeSlot.Where(a => a.ServiceId == t1.ServiceId && a.Status && a.SlotCount > 0 && !a.IsDeleted)
                                                            .Select(u => u.SlotDate).Distinct().Count(),
                                serviceCountInCart = _context.Cart.Where(a => a.ServiceId == t1.ServiceId && a.CustomerUserId == currentUserId).Sum(a => a.ServiceCountInCart),
                                // Additional properties from other tables
                            };

                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Data = query.ToList();
                _response.Messages = "Favourite service" + ResponseMessages.msgListFoundSuccess;
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

            var salonName = "";

            if (model.salonId > 0)
            {
                var salon = await _context.SalonDetail.Where(u => u.SalonId == model.salonId).FirstOrDefaultAsync();
                if (salon == null)
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = ResponseMessages.msgNotFound + "record.";
                    return Ok(_response);
                }
                if (salon.Status == 0)
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = ResponseMessages.msgNotFound + "record.";
                    return Ok(_response);
                }
                salonName = salon.SalonName;
            }
            var roles = await _userManager.GetRolesAsync(currentUserDetail);

            IQueryable<SalonServiceListDTO>? query1;

            query1 = from t1 in _context.SalonService
                     join t2 in _context.MainCategory on t1.MainCategoryId equals t2.MainCategoryId
                     where t1.IsDeleted != true && t1.SalonId == model.salonId
                     where t1.Status == 1
                     // where t1.ServiceType == (model.mainCategoryId != 53 ? "Single" : "Package")
                     // where t6.CustomerUserId == currentUserId
                     orderby t1.CreateDate descending
                     // Add more joins as needed
                     select new SalonServiceListDTO
                     {
                         serviceName = t1.ServiceName,
                         serviceId = t1.ServiceId,
                         vendorId = _context.SalonDetail.Where(u => u.SalonId == (t1.SalonId != null ? t1.SalonId : 0)).Select(u => u.VendorId).FirstOrDefault(),
                         salonId = t1.SalonId,
                         salonName = _context.SalonDetail.Where(u => u.SalonId == (t1.SalonId != null ? t1.SalonId : 0)).Select(u => u.SalonName).FirstOrDefault(),
                         mainCategoryId = t1.MainCategoryId,
                         mainCategoryName = t2.CategoryName,
                         subCategoryId = t1.SubcategoryId,
                         subCategoryName = _context.SubCategory.Where(u => u.SubCategoryId == (t1.SubcategoryId != null ? t1.SubcategoryId : 0)).Select(u => u.CategoryName).FirstOrDefault(),
                         serviceDescription = t1.ServiceDescription,
                         serviceImage = t1.ServiceIconImage,
                         listingPrice = t1.ListingPrice,
                         basePrice = (double)t1.BasePrice,
                         favoritesStatus = (_context.FavouriteService.Where(u => u.ServiceId == t1.ServiceId && u.CustomerUserId == currentUserId)).FirstOrDefault() != null ? true : false,
                         discount = t1.Discount,
                         genderPreferences = t1.GenderPreferences,
                         ageRestrictions = t1.AgeRestrictions,
                         ServiceType = t1.ServiceType,
                         totalCountPerDuration = t1.TotalCountPerDuration,
                         durationInMinutes = t1.DurationInMinutes,
                         status = t1.Status,
                         // Ensure the discount is non-negative
                         isSlotAvailable = _context.TimeSlot.Where(a => a.ServiceId == t1.ServiceId && a.Status && a.SlotCount > 0 && !a.IsDeleted)
                                         .Select(u => u.SlotDate).Distinct().Count(),
                         serviceCountInCart = _context.Cart.Where(a => a.ServiceId == t1.ServiceId && a.CustomerUserId == currentUserId).Sum(a => a.ServiceCountInCart),
                         // Additional properties from other tables
                     };

            var categoryIdsQuery = _context.VendorCategory
                        .Where(u => u.SalonId == model.salonId && (u.MainCategoryId > 0 || u.SubCategoryId > 0))
                        .Select(u => new { u.MainCategoryId, u.SubCategoryId });

            var distinctCategoryIds = await categoryIdsQuery.ToListAsync();

            var mainCategoryIDs = distinctCategoryIds.Where(u => u.MainCategoryId > 0).Select(u => u.MainCategoryId).ToList();
            var subCategoryIDs = distinctCategoryIds.Where(u => u.SubCategoryId > 0).Select(u => u.SubCategoryId).ToList();

            var query = query1
                .Where(u => !mainCategoryIDs.Contains(u.mainCategoryId) && !subCategoryIDs.Contains(u.subCategoryId))
                // .Where(mainCategoryIDs.Contains(subCategoryIDs.))
                .ToList()
                .Select(item => new SalonServiceListDTO
                {
                    serviceName = item.serviceName,
                    serviceId = item.serviceId,
                    vendorId = item.vendorId,
                    salonId = item.salonId,
                    salonName = item.salonName,
                    mainCategoryId = item.mainCategoryId,
                    mainCategoryName = item.mainCategoryName,
                    subCategoryId = item.subCategoryId,
                    subCategoryName = item.subCategoryName,
                    serviceDescription = item.serviceDescription,
                    serviceImage = item.serviceImage,
                    listingPrice = item.listingPrice,
                    basePrice = item.basePrice,
                    favoritesStatus = item.favoritesStatus,
                    discount = item.discount,
                    genderPreferences = item.genderPreferences,
                    ageRestrictions = item.ageRestrictions,
                    ServiceType = item.ServiceType,
                    totalCountPerDuration = item.totalCountPerDuration,
                    durationInMinutes = item.durationInMinutes,
                    status = item.status,
                    isSlotAvailable = item.isSlotAvailable,
                    serviceCountInCart = item.serviceCountInCart,
                    discountInPercentage = Math.Max(0, Math.Round(((item.basePrice - item.listingPrice) / item.basePrice) * 100, 2))
                })
                .ToList();

            if (!string.IsNullOrEmpty(model.genderPreferences))
            {
                query = query.Where(x => (x.genderPreferences == model.genderPreferences)).ToList();
            }
            if (!string.IsNullOrEmpty(model.ageRestrictions))
            {
                query = query.Where(x => (x.ageRestrictions == model.ageRestrictions)).ToList();
            }

            var mainCategories = query.Where(u => u.mainCategoryId > 0)
                                    .Select(t1 => new DashboardSalonServiceCategoryDTO
                                    {
                                        MainCategoryId = (int)t1.mainCategoryId,
                                        CategoryName = t1.mainCategoryName,
                                        CategoryImage = _context.MainCategory
                                                            .Where(u => u.MainCategoryId == (t1.mainCategoryId ?? 0))
                                                            .Select(u => (model.genderPreferences == "Male" ? u.CategoryImageMale : u.CategoryImageFemale))
                                                            .FirstOrDefault(),
                                    })
                                    // .DistinctBy(u => u.MainCategoryId).Take(9)
                                    .ToList().DistinctBy(u => u.MainCategoryId);

            var subCategories = query.Where(u => u.subCategoryId > 0)
                                    .Select(t1 => new DashboardSalonServiceCategoryDTO
                                    {
                                        SubCategoryId = (int)t1.subCategoryId,
                                        CategoryName = t1.subCategoryName,
                                        CategoryImage = _context.SubCategory
                                                            .Where(u => u.SubCategoryId == (t1.subCategoryId ?? 0))
                                                            .Select(u => (model.genderPreferences == "Male" ? u.CategoryImageMale : u.CategoryImageFemale))
                                                            .FirstOrDefault(),
                                    })
                                    .ToList().DistinctBy(u => u.SubCategoryId).Take(9);

            var mainCategoriesOfferMinPercentage = query.Where(u => u.mainCategoryId > 0 && u.discountInPercentage < 50 && u.discountInPercentage > 1)
                                            .Select(t1 => new DashboardSalonServiceCategoryDTO
                                            {
                                                MainCategoryId = (int)t1.mainCategoryId,
                                                MaxOrMinDiscount = "Max",
                                                Discount = 50,
                                                CategoryName = t1.mainCategoryName,
                                                CategoryImage = _context.MainCategory
                                                                    .Where(u => u.MainCategoryId == (t1.mainCategoryId ?? 0))
                                                                    .Select(u => (model.genderPreferences == "Male" ? u.CategoryImageMale : u.CategoryImageFemale))
                                                                    .FirstOrDefault(),
                                            })
                                            .ToList().DistinctBy(u => u.MainCategoryId).Take(9);

            var subCategoriesOfferMinPercentage = query.Where(u => u.mainCategoryId > 0 && u.discountInPercentage < 50 && u.discountInPercentage > 1)
                                    .Select(t1 => new DashboardSalonServiceCategoryDTO
                                    {
                                        SubCategoryId = (int)t1.subCategoryId,
                                        MaxOrMinDiscount = "Max",
                                        Discount = 50,
                                        CategoryName = t1.subCategoryName,
                                        CategoryImage = _context.SubCategory
                                                            .Where(u => u.SubCategoryId == (t1.subCategoryId ?? 0))
                                                            .Select(u => (model.genderPreferences == "Male" ? u.CategoryImageMale : u.CategoryImageFemale))
                                                            .FirstOrDefault(),
                                    })
                                    .ToList().DistinctBy(u => u.SubCategoryId).Take(9);
            string bannerCategoryType = "Male_and_Female";
            if (model.genderPreferences == "Male")
            {
                bannerCategoryType = model.genderPreferences;
            }
            if (model.genderPreferences == "Female")
            {
                bannerCategoryType = model.genderPreferences;
            }
            var shopBanners = _context.SalonBanner.Where(u => (u.SalonId == model.salonId))
            .Select(t1 => new GetDashboardSalonBannerDTO
            {
                mainCategoryId = t1.MainCategoryId,
                subCategoryId = t1.SubCategoryId,
                bannerType = t1.BannerType,
                male = t1.Male,
                female = t1.Female,
                bannerImage = t1.BannerImage,
                mainCategoryName = _context.MainCategory.Where(u => u.MainCategoryId == (t1.MainCategoryId ?? 0))
                                                                .Select(u => u.CategoryName).FirstOrDefault(),
                subCategoryName = _context.SubCategory.Where(u => u.SubCategoryId == (t1.SubCategoryId ?? 0))
                                                                .Select(u => u.CategoryName).FirstOrDefault(),
            })
            .ToList();

            if (model.genderPreferences == "Male")
            {
                shopBanners = shopBanners.Where(u => u.male == true).ToList();
            }
            if (model.genderPreferences == "Female")
            {
                shopBanners = shopBanners.Where(u => u.female == true).ToList();
            }

            var productList = query.ToList();

            var dashboardResponse = new CustomerDashboardViewModel();

            var categorywiseBannerList = shopBanners.Where(u => u.bannerType == BannerType.SalonCategoryBanner.ToString());
            var salonBannerList = shopBanners.Where(u => u.bannerType == BannerType.SalonBanner.ToString());

            // product list
            var dashboardCategoryWiseBannerResponse = new CustomerDashboardBannerDTO();
            dashboardCategoryWiseBannerResponse.name = "Category Banner";
            dashboardCategoryWiseBannerResponse.type = "Banner";
            var counter1 = 1;
            foreach (var item in categorywiseBannerList)
            {
                counter1 = counter1 + 1;
                if (counter1 < 4)
                {
                    dashboardCategoryWiseBannerResponse.dashboardSalonBanner = categorywiseBannerList.Take(4).ToList();
                    dashboardResponse.categoryBanner1 = dashboardCategoryWiseBannerResponse;
                    dashboardResponse.categoryBanner2 = dashboardCategoryWiseBannerResponse;
                    dashboardResponse.categoryBanner3 = dashboardCategoryWiseBannerResponse;
                    dashboardResponse.categoryBanner4 = dashboardCategoryWiseBannerResponse;
                    dashboardResponse.categoryBanner5 = dashboardCategoryWiseBannerResponse;
                }
                if (counter1 > 4 && counter1 < 8)
                {
                    dashboardCategoryWiseBannerResponse.dashboardSalonBanner = categorywiseBannerList.Take(8).OrderByDescending(u => u.mainCategoryId).Take(4).ToList();
                    dashboardResponse.categoryBanner2 = dashboardCategoryWiseBannerResponse;
                }
                if (counter1 > 8 && counter1 < 12)
                {
                    dashboardCategoryWiseBannerResponse.dashboardSalonBanner = categorywiseBannerList.Take(12).OrderByDescending(u => u.mainCategoryId).Take(4).ToList();
                    dashboardResponse.categoryBanner3 = dashboardCategoryWiseBannerResponse;
                }
                if (counter1 > 12 && counter1 < 16)
                {
                    dashboardCategoryWiseBannerResponse.dashboardSalonBanner = categorywiseBannerList.Take(16).OrderByDescending(u => u.mainCategoryId).Take(4).ToList();
                    dashboardResponse.categoryBanner4 = dashboardCategoryWiseBannerResponse;
                }
                if (counter1 > 16 && counter1 < 20)
                {
                    dashboardCategoryWiseBannerResponse.dashboardSalonBanner = categorywiseBannerList.OrderByDescending(u => u.mainCategoryId).Take(4).ToList();
                    dashboardResponse.categoryBanner5 = dashboardCategoryWiseBannerResponse;
                }
            }

            // main category list
            var customerDashboardProductCategoryrDTO = new CustomerDashboardServiceCategoryrDTO();
            var dashboardProductCategoryList = new List<DashboardSalonServiceCategoryDTO>();
            foreach (var item in mainCategories)
            {
                var dashboardProductCategoryDTO = new DashboardSalonServiceCategoryDTO();
                dashboardProductCategoryDTO.CategoryName = item.CategoryName;
                dashboardProductCategoryDTO.CategoryImage = item.CategoryImage;
                dashboardProductCategoryDTO.MainCategoryId = item.MainCategoryId;
                dashboardProductCategoryList.Add(item);
            }
            customerDashboardProductCategoryrDTO.name = "Service by category";
            customerDashboardProductCategoryrDTO.type = "Category";
            customerDashboardProductCategoryrDTO.dashboarCategory = dashboardProductCategoryList;
            dashboardResponse.mainCategoryList = customerDashboardProductCategoryrDTO;

            // home banner list
            var dashboardBannerResponse = new CustomerDashboardBannerDTO();
            var dashboardBannerList = new List<GetDashboardSalonBannerDTO>();
            foreach (var item in salonBannerList)
            {
                var dashboardBannerDTO = new GetDashboardSalonBannerDTO();
                dashboardBannerDTO.bannerImage = item.bannerImage;
                dashboardBannerDTO.bannerType = item.bannerType;
                dashboardBannerList.Add(dashboardBannerDTO);
            }
            dashboardBannerResponse.name = "Salon Banner";
            dashboardBannerResponse.type = "Banner";
            dashboardBannerResponse.dashboardSalonBanner = dashboardBannerList;
            dashboardResponse.salonBanner = dashboardBannerResponse;

            // product list
            var dashboardTopPicksProductResponse = new CustomerDashboardSalonServiceDTO();
            dashboardTopPicksProductResponse.name = "New Launch";
            dashboardTopPicksProductResponse.type = "Service";
            dashboardTopPicksProductResponse.description = "";
            dashboardTopPicksProductResponse.serviceListDTO = query.Take(15).ToList();
            dashboardResponse.newlyLaunched = dashboardTopPicksProductResponse;

            // product list
            var dashboardBestPackageResponse = new CustomerDashboardSalonServiceDTO();
            dashboardBestPackageResponse.name = "Best Packages";
            dashboardBestPackageResponse.type = "Service";
            dashboardBestPackageResponse.description = "";
            dashboardBestPackageResponse.serviceListDTO = query.Where(u => u.ServiceType == "Package").OrderByDescending(u => u.discount).Take(15).ToList();
            dashboardResponse.bestPackages = dashboardBestPackageResponse;

            // product list
            var distinctSubCategories = query.Select(u => u.subCategoryId)
                                             .Distinct().ToList();
            var resultList = new List<SalonServiceListDTO>();

            for (int i = 0; i < 9; i++)
            {
                if (distinctSubCategories.Count > (i + 1))
                {
                    var topProductsForSubCategory = query
                        .Where(u => u.subCategoryId == distinctSubCategories[i].Value).FirstOrDefault();

                    resultList.Add(topProductsForSubCategory);
                }
            }

            var dashboardsugestedProductResponse = new CustomerDashboardSalonServiceDTO();
            dashboardsugestedProductResponse.name = "Suggested for You";
            dashboardsugestedProductResponse.type = "Service";
            dashboardsugestedProductResponse.serviceListDTO = resultList.Take(9).ToList();
            dashboardResponse.suggestedForYou = dashboardsugestedProductResponse;

            // product list
            var resultList1 = new List<SalonServiceListDTO>();
            for (int i = 0; i < 9; i++)
            {
                if (distinctSubCategories.Count > (i + 9))
                {
                    var topProductsForSubCategory = query
                        .Where(u => u.subCategoryId == distinctSubCategories[i + 9].Value).FirstOrDefault();

                    resultList.Add(topProductsForSubCategory);
                }
            }
            var dashboardRecommendedProductResponse = new CustomerDashboardSalonServiceDTO();
            dashboardRecommendedProductResponse.name = "Recommended for You";
            dashboardRecommendedProductResponse.type = "Service";
            // Assuming 'query' is an IQueryable collection
            dashboardRecommendedProductResponse.serviceListDTO = resultList1;
            dashboardResponse.recommendedForYou = dashboardRecommendedProductResponse;

            // product list
            var dashboardFavouriteProductResponse = new CustomerDashboardSalonServiceDTO();
            dashboardFavouriteProductResponse.name = "Your Favourite Services";
            dashboardFavouriteProductResponse.type = "Service";
            dashboardFavouriteProductResponse.serviceListDTO = query.Where(u => u.favoritesStatus == true).Take(9).ToList();
            dashboardResponse.favourites = dashboardFavouriteProductResponse;

            // product list
            var dashboardCartProductResponse = new CustomerDashboardSalonServiceDTO();
            dashboardCartProductResponse.name = "Items in Your Cart";
            dashboardCartProductResponse.type = "Service";
            dashboardCartProductResponse.serviceListDTO = query.Where(u => u.serviceCountInCart > 0).Take(9).ToList();
            dashboardResponse.servicesInYourCart = dashboardCartProductResponse;

            // product list
            var dashboardServiceMaxOfferResponse = new CustomerDashboardSalonServiceDTO();
            dashboardServiceMaxOfferResponse.name = "Up to 50% off";
            dashboardServiceMaxOfferResponse.type = "Service";
            dashboardServiceMaxOfferResponse.Discount = 50;
            dashboardServiceMaxOfferResponse.type = "Max";
            dashboardServiceMaxOfferResponse.serviceListDTO = query.Where(u => u.discountInPercentage < 50 && u.discountInPercentage > 0).OrderByDescending(u => u.discountInPercentage).Take(12).ToList();
            dashboardResponse.maxServiceOffer = dashboardServiceMaxOfferResponse;

            // product list
            var dashboardServiceMinOfferResponse = new CustomerDashboardSalonServiceDTO();
            dashboardServiceMinOfferResponse.name = "Minimun 50% off";
            dashboardServiceMinOfferResponse.type = "Service";
            dashboardServiceMinOfferResponse.Discount = 50;
            dashboardServiceMinOfferResponse.type = "Min";
            dashboardServiceMinOfferResponse.serviceListDTO = query.Where(u => u.discountInPercentage > 50).OrderByDescending(u => u.discountInPercentage).Take(12).ToList();
            dashboardResponse.minServiceOffer = dashboardServiceMinOfferResponse;


            var customerSearchRecord = _context.CustomerSearchRecord.Where(u => u.CustomerUserId == currentUserId).OrderByDescending(u => u.RecordId);
            // product list
            var dashboardRecentProductResponse = new CustomerDashboardSalonServiceDTO();
            dashboardRecentProductResponse.name = "Recently Viewed";
            dashboardRecentProductResponse.type = "Product";

            var searchQuery = customerSearchRecord.Where(u => !string.IsNullOrEmpty(u.CustomerSearchItem)).FirstOrDefault();

            if (searchQuery != null)
            {
                dashboardRecentProductResponse.serviceListDTO = query.Where(x => (x.serviceName?.IndexOf(searchQuery.CustomerSearchItem, StringComparison.OrdinalIgnoreCase) >= 0)
                || (x.mainCategoryName?.IndexOf(searchQuery.CustomerSearchItem, StringComparison.OrdinalIgnoreCase) >= 0)
                || (x.subCategoryName?.IndexOf(searchQuery.CustomerSearchItem, StringComparison.OrdinalIgnoreCase) >= 0)
                ).Take(9).ToList();
                // dashboardRecentProductResponse.productListDTO = query.Where(u => u.productCountInCart > 0).ToList();
                dashboardResponse.RecentlyViewed = dashboardRecentProductResponse;
            }

            // product list
            var searchSubCategory = customerSearchRecord.Where(u => u.MaincategoryId > 0).FirstOrDefault();

            if (searchSubCategory != null)
            {
                var dashboardContinueProductResponse = new CustomerDashboardSalonServiceDTO();
                dashboardContinueProductResponse.name = "You may like";
                dashboardContinueProductResponse.type = "Service";

                dashboardContinueProductResponse.serviceListDTO = query.Where(u => u.subCategoryId == searchSubCategory.SubcategoryId).Take(9).ToList();

                // dashboardCartProductResponse.productListDTO = query.Where(u => u.productCountInCart > 0).ToList();
                dashboardResponse.youMayLike = dashboardContinueProductResponse;
            }

            var categoryList = query
                .Select(u => new
                {
                    MainCategoryName = u.mainCategoryName,
                    MainCategoryId = u.mainCategoryId
                })
                .ToList().DistinctBy(u => u.MainCategoryId);

            var subCategoryList = await _context.SubCategory.Where(u => u.CategoryStatus == 1).ToListAsync();
            int counter = 1;
            foreach (var item2 in categoryList)
            {
                // product list
                var dashboardCategoryWiseResponse = new CustomerDashboardSalonServiceDTO();
                var description = "";
                var subcategoryByMain = subCategoryList.Where(u => u.MainCategoryId == item2.MainCategoryId).Take(3).ToList();
                int coutForSub = 0;
                foreach (var item in subcategoryByMain)
                {
                    description = description + (!string.IsNullOrEmpty(description) ? (", " + subcategoryByMain[coutForSub].CategoryName) : subcategoryByMain[coutForSub].CategoryName);
                    coutForSub = coutForSub + 1;
                }
                dashboardCategoryWiseResponse.name = item2.MainCategoryName;
                dashboardCategoryWiseResponse.MainCategoryId = (int)item2.MainCategoryId;
                dashboardCategoryWiseResponse.type = "Service";
                dashboardCategoryWiseResponse.serviceListDTO = query.Where(u => u.mainCategoryId == item2.MainCategoryId).Take(9).ToList();
                if (counter == 1)
                {
                    dashboardResponse.categoryWiseServices1 = dashboardCategoryWiseResponse;
                }
                else if (counter == 2)
                {
                    dashboardResponse.categoryWiseServices2 = dashboardCategoryWiseResponse;
                }
                else if (counter == 3)
                {
                    dashboardResponse.categoryWiseServices3 = dashboardCategoryWiseResponse;
                }
                else if (counter == 4)
                {
                    dashboardResponse.categoryWiseServices4 = dashboardCategoryWiseResponse;
                }
                else if (counter == 5)
                {
                    dashboardResponse.categoryWiseServices5 = dashboardCategoryWiseResponse;
                }
                else if (counter == 6)
                {
                    dashboardResponse.categoryWiseServices6 = dashboardCategoryWiseResponse;
                }
                else
                {
                    dashboardResponse.categoryWiseServices7 = dashboardCategoryWiseResponse;
                }
                counter++;
            }

            // subSubCategories Offer Max Flat
            var mainCategoriyOfferList = new CustomerDashboardServiceCategoryrDTO();

            mainCategoriyOfferList.name = "Up to 50% Off";
            mainCategoriyOfferList.type = "Category";
            mainCategoriyOfferList.dashboarCategory = mainCategoriesOfferMinPercentage.Take(9).ToList();
            dashboardResponse.mainCategoriesOfferMin = mainCategoriyOfferList;

            var subCategoriyOfferList = new CustomerDashboardServiceCategoryrDTO();

            subCategoriyOfferList.name = "Up to 50% Off";
            subCategoriyOfferList.type = "Category";
            subCategoriyOfferList.dashboarCategory = subCategoriesOfferMinPercentage.Take(9).ToList();
            dashboardResponse.mainCategoriesOfferMin = subCategoriyOfferList;

            _response.StatusCode = HttpStatusCode.OK;
            _response.IsSuccess = true;
            _response.Data = dashboardResponse;
            _response.Messages = "Dashboard data" + ResponseMessages.msgShownSuccess;
            return Ok(_response);
        }
        #endregion

    }
}
