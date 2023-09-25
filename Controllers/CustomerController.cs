﻿using AutoMapper;
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
                    _response.Messages = "User not found.";
                    return Ok(_response);
                }

                var checksalonDetail = await _context.CustomerSalon.Where(u => (u.CustomerUserId == currentUserId) && (u.SalonId == model.salonId)).FirstOrDefaultAsync();
                if (checksalonDetail != null)
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = "Salon is already added.";
                    return Ok(_response);
                }

                var salonDetail = await _context.SalonDetail.Where(u => (u.SalonId == model.salonId) && (u.IsDeleted != true)).FirstOrDefaultAsync();
                if (salonDetail == null)
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = "Salon not found.";
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
                _response.Messages = "Salon added successfully.";
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

                liveLocation = liveLocation != null ? liveLocation : 0;

                var userProfileDetail = await _context.UserDetail.Where(u => u.UserId == currentUserId).FirstOrDefaultAsync();
                if (userProfileDetail == null)
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = "User not found.";
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
                        _response.Messages = "Location not found.";
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
                            && (u.SalonType != salonType)
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
                    _response.Messages = "Salon list shown successfully.";
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
                    _response.Messages = "Salon list shown successfully.";
                    return Ok(_response);
                }

                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = false;
                _response.Messages = "Not found any record.";
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
                    _response.Messages = "User not found.";
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
                    _response.Messages = "User not found.";
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
                        _response.Messages = "Adress type has been already added.";
                        return Ok(_response);
                    }
                }
                if (checkCustomerDetail == null)
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = "Not found any record.";
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
                    _response.Messages = "User not found.";
                    return Ok(_response);
                }

                var customerDetail = await _context.CustomerAddress.FirstOrDefaultAsync(u => (u.CustomerAddressId == customerAddressId));
                if (customerDetail == null)
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = "Not found any record.";
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
                    _response.Messages = "User not found.";
                    return Ok(_response);
                }

                var customerDetail = await _context.CustomerAddress.FirstOrDefaultAsync(u => (u.CustomerAddressId == customerAddressId));
                if (customerDetail == null)
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = "Not found any record.";
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
                    _response.Messages = "User not found.";
                    return Ok(_response);
                }

                var customerDetail = await _context.CustomerAddress.FirstOrDefaultAsync(u => (u.CustomerAddressId == model.customerAddressId));
                if (customerDetail == null)
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = "Not found any record.";
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
                _response.Messages = "Address staus updated successfully.";
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
                    _response.Messages = "User not found.";
                    return Ok(_response);
                }

                var customerDetail = await _context.CustomerAddress.Where(u => (u.CustomerUserId == currentUserId)).ToListAsync();
                if (customerDetail == null)
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = "Not found any record.";
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
                    _response.Messages = "User does not exists.";
                    return Ok(_response);
                }
                // await _userManager.UpdateSecurityStampAsync(currentUserDetail);
                var roles = await _userManager.GetRolesAsync(currentUserDetail);

                if (roles[0] != "Customer")
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = "User does not exist";
                    return Ok(_response);
                }

                var user = await _context.UserDetail.Where(u => u.UserId == currentUserId).FirstOrDefaultAsync();
                if (user == null)
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = "Not found any record.";
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

                currentUserDetail.Email = "deleted" + currentUserDetail.Email;
                currentUserDetail.UserName = "deleted" + currentUserDetail.Email;
                currentUserDetail.NormalizedUserName = "deleted" + currentUserDetail.Email;
                currentUserDetail.PhoneNumber = "001" + currentUserDetail.PhoneNumber;
                currentUserDetail.SecurityStamp = CommonMethod.RandomString(20);

                await _userManager.UpdateAsync(currentUserDetail);

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
                    _response.Messages = "User not found.";
                    return Ok(_response);
                }

                var SalonIdList = new List<int?>();
                SalonIdList = await _context.Cart.Where(u => (u.CustomerUserId == currentUserId)).Select(u => u.SalonId).Distinct().ToListAsync();

                var checkServiceDetail = await _context.SalonService.Where(u => (u.ServiceId == model.serviceId) && (u.Status == Convert.ToInt32(ServiceStatus.Active))).FirstOrDefaultAsync();
                if (checkServiceDetail == null)
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = "Not found any record.";
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

                    _context.Update(getCartDetail);
                    await _context.SaveChangesAsync();

                    response = _mapper.Map<CartServicesDTO>(getCartDetail);
                }
                else
                {
                    cartDeatil = _mapper.Map<Cart>(model);
                    cartDeatil.CustomerUserId = currentUserId;
                    cartDeatil.ServiceCountInCart = 1;
                    cartDeatil.SalonId = checkServiceDetail.SalonId;

                    await _context.AddAsync(cartDeatil);
                    await _context.SaveChangesAsync();

                    response = _mapper.Map<CartServicesDTO>(cartDeatil);
                }

                if (checkServiceDetail != null)
                {
                    _mapper.Map(checkServiceDetail, response);
                    _mapper.Map(checkTimeSlot, response);
                    response.serviceImage = checkServiceDetail.ServiceImage1;
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
        // [Authorize(Roles = "Customer")]
        public async Task<IActionResult> GetServiceListFromCart(string? availableService)
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
                    _response.Messages = "User not found.";
                    return Ok(_response);
                }

                CartDetailDTO cartDetail = new CartDetailDTO();
                var CartDetailPerSalonList = new List<CartDetailPerSalonDTO>();
                var getCartSalonIdList = await _context.Cart.Where(u => (u.CustomerUserId == currentUserId)).Select(u => u.SalonId).Distinct().ToListAsync();

                foreach (var item in getCartSalonIdList)
                {
                    var getCartDetail = await _context.Cart.Where(u => (u.CustomerUserId == currentUserId) && (u.SalonId == item.Value)).ToListAsync();
                    List<CartServicesDTO>? cartServiceList = new List<CartServicesDTO>();
                    if (!string.IsNullOrEmpty(availableService))
                    {
                        foreach (var Service in getCartDetail)
                        {
                            var ServiceDetail = await _context.SalonService.Where(u => u.ServiceId == Service.ServiceId && u.Status == 1).FirstOrDefaultAsync();

                            if (ServiceDetail != null)
                            {
                                var mappedData = _mapper.Map<CartServicesDTO>(Service);
                                var timeSlot = await _context.TimeSlot.Where(u => u.SlotId == Service.SlotId).FirstOrDefaultAsync();
                                mappedData.slotDate = timeSlot.SlotDate.ToString(@"dd-MM-yyyy");
                                mappedData.fromTime = timeSlot.FromTime;
                                mappedData.toTime = timeSlot.ToTime;
                                mappedData.slotStatus = timeSlot.Status;
                                if (mappedData.slotStatus == true && timeSlot.SlotCount >= mappedData.ServiceCountInCart)
                                {
                                    cartServiceList.Add(mappedData);
                                }
                                else
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
                    else
                    {
                        cartServiceList = _mapper.Map<List<CartServicesDTO>>(getCartDetail);
                        foreach (var cartService in cartServiceList)
                        {
                            var timeSlot = await _context.TimeSlot.Where(u => u.SlotId == cartService.slotId).FirstOrDefaultAsync();
                            cartService.slotDate = timeSlot.SlotDate.ToString(@"dd-MM-yyyy");
                            cartService.fromTime = timeSlot.FromTime;
                            cartService.toTime = timeSlot.ToTime;
                            cartService.slotStatus = timeSlot.Status;
                        }
                    }
                    foreach (var item2 in cartServiceList)
                    {
                        var getServiceDetail = await _context.SalonService.Where(u => (u.ServiceId == item2.serviceId)).FirstOrDefaultAsync();

                        if (getServiceDetail != null)
                        {
                            _mapper.Map(getServiceDetail, item2);
                            item2.statusDisplay = ((ServiceStatus)getServiceDetail.Status).ToString();
                            item2.serviceImage = item2.serviceImage;
                            item2.basePrice = item2.basePrice * item2.ServiceCountInCart;
                            item2.listingPrice = item2.listingPrice * item2.ServiceCountInCart;
                            item2.discount = item2.discount * item2.ServiceCountInCart;
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
                        CartDetailPerSalon.salonTotalSellingPrice = CartDetailPerSalon.salonTotalSellingPrice + item1.listingPrice;
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
                    cartDetail.totalMrp = cartDetail.totalMrp + item3.salonTotalMrp;
                    cartDetail.totalSellingPrice = cartDetail.totalSellingPrice + item3.salonTotalSellingPrice;
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
                    _response.Messages = "User not found.";
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
                    _response.Messages = "User not found.";
                    return Ok(_response);
                }

                int serviceCount;

                var getCartItems = await _context.Cart.Where(u => (u.CustomerUserId == currentUserId)).ToListAsync();
                serviceCount = getCartItems.Count;
                foreach (var item in getCartItems)
                {
                    if (item.ServiceCountInCart > 1)
                    {
                        serviceCount = (int)(serviceCount + item.ServiceCountInCart - 1);
                    }
                }

                return Ok(new
                {
                    StatusCode = HttpStatusCode.OK,
                    IsSuccess = true,
                    TotalCount = serviceCount,
                    Messages = "Services count in cart shown successfully."
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
        public async Task<IActionResult> GetUnavailableServices()
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
                    _response.Messages = "User not found.";
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
                            cartService.serviceImage = getServiceDetail.ServiceImage1;
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

        #region setFavouriteSalon
        /// <summary>
        ///set Favourite Salon
        /// </summary>
        [HttpPost]
        [Route("setFavouriteSalon")]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> SetFavouriteSalon(SetFavouriteSalon model)
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
                    _response.Messages = "Not found any Service.";
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
                        _response.Messages = "Not found any Service.";
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
                    _response.Messages = "User not found.";
                    return Ok(_response);
                }
                var userDetail = _userManager.FindByIdAsync(currentUserId).GetAwaiter().GetResult();
                if (userDetail == null)
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = "User not found.";
                    return Ok(_response);
                }
                var customerAddress = await _context.CustomerAddress.Where(u => u.CustomerUserId == currentUserId && u.Status == true).FirstOrDefaultAsync();
                // get all cart Services

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
                    var timeSlot = await _context.TimeSlot.Where(u => (u.ServiceId == item.ServiceId) && (u.SlotId == item.SlotId) && (u.Status != true)).FirstOrDefaultAsync();
                    var ServiceDetail = await _context.SalonService.Where(u => (u.ServiceId == item.ServiceId) && (u.Status != Convert.ToInt32(ServiceStatus.Active))).FirstOrDefaultAsync();
                    if (ServiceDetail != null || timeSlot != null)
                    {
                        _context.Remove(item);
                        await _context.SaveChangesAsync();
                    }
                }

                //get available service
                var inStockCartServices = await _context.Cart.Where(u => u.CustomerUserId == currentUserId).ToListAsync();

                int totalServices = 0;
                double totalDiscount = 0;
                double finalPrice = 0;
                double basePrice = 0;
                double discount = 0;

                var appointmentDetail = new Appointment();
                appointmentDetail.CustomerUserId = currentUserId;
                appointmentDetail.TransactionId = "TX" + CommonMethod.GenerateOTP();
                appointmentDetail.CustomerFirstName = userDetail.FirstName;
                appointmentDetail.CustomerLastName = userDetail.LastName;
                appointmentDetail.AppointmentStatus = AppointmentStatus.Confirmed.ToString();
                appointmentDetail.PaymentStatus = PaymentStatus.Paid.ToString();
                appointmentDetail.CustomerAddress = customerAddress != null ? customerAddress.StreetAddresss : null;
                appointmentDetail.PhoneNumber = customerAddress != null ? customerAddress.PhoneNumber : userDetail.PhoneNumber;

                appointmentDetail.BasePrice = 1;
                appointmentDetail.FinalPrice = 1;
                appointmentDetail.TotalDiscount = 1;
                appointmentDetail.Discount = 1;

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
                                _response.Messages = "Not found any record.";
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
                }
                else
                {
                    appointmentDetail.PaymentMethod = PaymentMethod.InCash.ToString();
                }

                await _context.AddAsync(appointmentDetail);
                await _context.SaveChangesAsync();

                foreach (var item in inStockCartServices)
                {
                    var bookedService = new BookedService();
                    var ServiceDetail = await _context.SalonService.Where(u => u.ServiceId == item.ServiceId).FirstOrDefaultAsync();
                    bookedService.AppointmentId = appointmentDetail.AppointmentId;
                    bookedService.ServiceId = item.ServiceId;
                    bookedService.ServiceImage = ServiceDetail.ServiceImage1;
                    bookedService.ServiceName = ServiceDetail.ServiceName;
                    bookedService.ListingPrice = ServiceDetail.ListingPrice * item.ServiceCountInCart;
                    bookedService.BasePrice = (double)ServiceDetail.BasePrice * item.ServiceCountInCart;
                    bookedService.Discount = ServiceDetail.Discount * item.ServiceCountInCart;
                    bookedService.SalonId = ServiceDetail.SalonId;
                    bookedService.DurationInMinutes = ServiceDetail.DurationInMinutes;
                    var salonDetail = await _context.SalonDetail.FirstOrDefaultAsync(u => u.SalonId == ServiceDetail.SalonId);
                    bookedService.VendorId = salonDetail.VendorId;
                    var user = _userManager.FindByIdAsync(salonDetail.VendorId).GetAwaiter().GetResult();
                    bookedService.SalonName = bookedService.SalonName;
                    bookedService.VendorName = user.FirstName + " " + user.LastName;
                    basePrice = (double)(basePrice + ServiceDetail.BasePrice);
                    finalPrice = (double)(finalPrice + ServiceDetail.ListingPrice);
                    totalServices = totalServices + 1;
                    var slotDetail = await _context.TimeSlot.Where(u => u.SlotId == item.SlotId).FirstOrDefaultAsync();
                    bookedService.AppointmentDate = slotDetail.SlotDate;
                    bookedService.FromTime = slotDetail.FromTime;
                    bookedService.ToTime = slotDetail.ToTime;
                    vendorId = salonDetail.VendorId;

                    await _context.BookedService.AddAsync(bookedService);
                    await _context.SaveChangesAsync();
                }

                appointmentDetail.BasePrice = basePrice;
                appointmentDetail.FinalPrice = finalPrice;
                appointmentDetail.TotalDiscount = basePrice - finalPrice;
                appointmentDetail.Discount = basePrice - finalPrice;
                appointmentDetail.TotalServices = totalServices;

                _context.Appointment.Update(appointmentDetail);
                await _context.SaveChangesAsync();

                var response = _mapper.Map<AppointmentDetailDTO>(appointmentDetail);

                foreach (var item in inStockCartServices)
                {
                    var updateTimeSlot = await _context.TimeSlot.Where(u => (u.ServiceId == item.ServiceId) && (u.SlotId == item.SlotId)).FirstOrDefaultAsync();
                    updateTimeSlot.SlotCount = updateTimeSlot.SlotCount - 1;
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

                // string motificationMessage = "Dear {0}, \nYou have received a new appointment from {1}.";

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
                notificationSent.NotificationType = NotificationType.Order.ToString();
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

    }
}
