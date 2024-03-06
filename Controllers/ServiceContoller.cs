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
using System.Numerics;
using Microsoft.AspNetCore.Builder.Extensions;
using Newtonsoft.Json.Serialization;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.IdentityModel.Tokens;
using System.Net.Http;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
using GSF.Collections;
using GSF;
using Org.BouncyCastle.Tls.Crypto;
using BeautyHubAPI.Common;
using System.Xml.Linq;

namespace BeautyHubAPI.Controllers
{
    [Route("api/Service")]
    [ApiController]
    public class ServiceController : ControllerBase
    {
        private readonly IMapper _mapper;
        protected APIResponse _response;
        private readonly HttpClient httpClient;
        private readonly IUploadRepository _uploadRepository;
        private readonly IServiceRepository _serviceRepository;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IMembershipRecordRepository _membershipRecordRepository;
        private readonly IWebHostEnvironment _hostingEnvironment;
        private readonly MyBackgroundService _backgroundService;
        private readonly ApplointmentListBackgroundService _applointmentListBackgroundService;

        public ServiceController(IMapper mapper,
        IUploadRepository uploadRepository,
        IServiceRepository serviceRepository,
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IMembershipRecordRepository membershipRecordRepository,

        IWebHostEnvironment hostingEnvironment,
        MyBackgroundService backgroundService,
        ApplointmentListBackgroundService applointmentListBackgroundService
        )
        {
            _mapper = mapper;
            _uploadRepository = uploadRepository;
            _serviceRepository = serviceRepository;
            _response = new();
            _context = context;
            _userManager = userManager;
            _membershipRecordRepository = membershipRecordRepository;
            httpClient = new HttpClient();
            _backgroundService = backgroundService;
            _applointmentListBackgroundService = applointmentListBackgroundService;
        }

        #region addUpdateSalonSchedule
        /// <summary>
        /// Add Salon Schedule.
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Authorize(Roles = "Vendor")]
        [Route("addUpdateSalonSchedule")]
        public async Task<IActionResult> addUpdateSalonSchedule([FromBody] ScheduleDayDTO model)
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

                if ((!string.IsNullOrEmpty(model.fromTime) && string.IsNullOrEmpty(model.toTime))
                || (string.IsNullOrEmpty(model.fromTime) && !string.IsNullOrEmpty(model.toTime))
                )
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = "Please enter both the from and to time.";
                    return Ok(_response);
                }

                var Salon = await _context.SalonDetail.Where(a => a.SalonId == model.salonId).FirstOrDefaultAsync();
                if (Salon == null)
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = ResponseMessages.msgNotFound + "Salon";
                    return Ok(_response);
                }

                if (!CommonMethod.IsValidTime24Format(model.fromTime) || !CommonMethod.IsValidTime24Format(model.toTime))
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = "Please enter time in 24 hour format (Ex. 15:00).";
                    return Ok(_response);
                }
                else
                {
                    model.fromTime = Convert.ToDateTime(model.fromTime).ToString(@"hh:mm tt");
                    model.toTime = Convert.ToDateTime(model.toTime).ToString(@"hh:mm tt");
                }

                var response = await _serviceRepository.addUpdateSalonSchedule(model);
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

        #region GetSalonServiceList
        /// <summary>
        //  Get Salon Service list.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Authorize(Roles = "SuperAdmin,Admin,Vendor,Customer")]
        [Route("GetSalonServiceList")]
        public async Task<IActionResult> GetSalonServiceList([FromQuery] SalonServiceFilterationListDTO model)
        {
            string currentUserId = (HttpContext.User.Claims.First().Value);
            model.searchQuery = string.IsNullOrEmpty(model.searchQuery) ? null : (model.searchQuery).TrimEnd();
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

            var roles = await _userManager.GetRolesAsync(currentUserDetail);

            if (roles[0].ToString() == "Customer")
            {

                var customerServiceList = await _serviceRepository.customerServiceList(model, currentUserId);
                return Ok(customerServiceList);

            }

            var serviceList = await _serviceRepository.vendorServiceList(model, currentUserId);
            return Ok(serviceList);

        }
        #endregion

        #region GetSalonServiceListPro
        /// <summary>
        //  Get Salon Service list.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Authorize(Roles = "SuperAdmin,Admin,Vendor,Customer")]
        [Route("GetSalonServiceListPro")]
        public async Task<IActionResult> GetSalonServiceListPro([FromQuery] SalonServiceFilterationListDTO model)
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
            var roles = await _userManager.GetRolesAsync(currentUserDetail);

            model.serviceType = model.serviceType == null ? "Single" : "Package";
            model.categoryWise = model.categoryWise == null ? false : model.categoryWise;
            model.mainCategoryId = model.mainCategoryId == null ? 0 : model.mainCategoryId;
            IQueryable<SalonServiceListDTO> query = _context.SalonService.Select(service => new SalonServiceListDTO { });

            if (roles[0].ToString() == "Customer")
            {
                // var customerSalonIds = await _context.CustomerSalon.Where(u => u.CustomerUserId == currentUserId).Select(a => a.SalonId).ToListAsync();

                query = from t1 in _context.SalonService
                        join t2 in _context.MainCategory on t1.MainCategoryId equals t2.MainCategoryId
                        where t1.IsDeleted == false
                        && t1.Status == 1
                        && (!string.IsNullOrEmpty(model.genderPreferences) ? t1.GenderPreferences == model.genderPreferences : t1.GenderPreferences == "Male" || t1.GenderPreferences == "Female")
                        && (model.mainCategoryId > 0 ? (t1.MainCategoryId == model.mainCategoryId || t1.MainCategoryId == 53) : (t1.MainCategoryId > 0 || t1.MainCategoryId == 53))
                        && (model.subCategoryId > 0 ? t1.SubcategoryId == model.subCategoryId : t1.SubcategoryId > 0)
                        && (model.salonId > 0 ? t1.SalonId == model.salonId : t1.SalonId > 0)
                        && (!string.IsNullOrEmpty(model.ageRestrictions) ? t1.AgeRestrictions == model.ageRestrictions : t1.AgeRestrictions == "Adult" || t1.GenderPreferences == "Kids")
                        // where customerSalonIds.Contains(t1.SalonId)
                        orderby t1.ServiceType descending
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
                            isSlotAvailable = _context.TimeSlot.Where(a => a.ServiceId == t1.ServiceId && a.Status && a.SlotCount > 0 && !a.IsDeleted)
                                                        .Select(u => u.SlotDate).Distinct().Count(),
                            serviceCountInCart = _context.Cart.Where(a => a.ServiceId == t1.ServiceId && a.CustomerUserId == currentUserId).Sum(a => a.ServiceCountInCart),
                        };
            }
            else
            {
                model.serviceType = model.serviceType == null ? "Single" : "Package";
                query = from t1 in _context.SalonService
                        join t2 in _context.MainCategory on t1.MainCategoryId equals t2.MainCategoryId
                        where t1.Status == 1
                        && (!string.IsNullOrEmpty(model.genderPreferences) ? t1.GenderPreferences == model.genderPreferences : t1.GenderPreferences == "Male" || t1.GenderPreferences == "Female")
                        && (model.mainCategoryId > 0 ? t1.MainCategoryId == model.mainCategoryId : t1.MainCategoryId > 0)
                        && (model.subCategoryId > 0 ? t1.SubcategoryId == model.subCategoryId : t1.SubcategoryId > 0)
                        && (model.salonId > 0 ? t1.SalonId == model.salonId : t1.SalonId > 0)
                        && (!string.IsNullOrEmpty(model.ageRestrictions) ? t1.AgeRestrictions == model.ageRestrictions : t1.AgeRestrictions == "Adult" || t1.GenderPreferences == "Kids")
                        && t1.IsDeleted != true
                        where t1.ServiceType == model.serviceType
                        // orderby t1.ServiceId
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
                            //  favoritesStatus = (_context.FavouriteProduct.Where(u => u.ProductId == t1.ProductId && u.CustomerUserId == currentUserId)).FirstOrDefault() != null ? true : false,
                            discount = t1.Discount,
                            totalCountPerDuration = t1.TotalCountPerDuration,
                            durationInMinutes = t1.DurationInMinutes,
                            genderPreferences = t1.GenderPreferences,
                            ServiceType = t1.ServiceType,
                            ageRestrictions = t1.AgeRestrictions,
                            status = t1.Status,
                            // Additional properties from other tables
                        };
            }

            List<SalonServiceListDTO>? SalonServiceList = query.ToList();
            if (!string.IsNullOrEmpty(model.searchQuery))
            {
                SalonServiceList = SalonServiceList.Where(x => (x.serviceName?.IndexOf(model.searchQuery, StringComparison.OrdinalIgnoreCase) >= 0)
                || (x.salonName?.IndexOf(model.searchQuery, StringComparison.OrdinalIgnoreCase) >= 0)
                || (x.mainCategoryName?.IndexOf(model.searchQuery, StringComparison.OrdinalIgnoreCase) >= 0)
                || (x.subCategoryName?.IndexOf(model.searchQuery, StringComparison.OrdinalIgnoreCase) >= 0)
                ).ToList();
            }

            // Get's No of Rows Count   
            int count = SalonServiceList.Count();

            // Parameter is passed from Query string if it is null then it default Value will be pageNumber:1  
            int CurrentPage = model.pageNumber;

            // Parameter is passed from Query string if it is null then it default Value will be pageSize:20  
            int PageSize = model.pageSize;

            // Display TotalCount to Records to User  
            int TotalCount = count;

            // Calculating Totalpage by Dividing (No of Records / Pagesize)  
            int TotalPages = (int)Math.Ceiling(count / (double)PageSize);

            // Returns List of Customer after applying Paging   
            var items = SalonServiceList.Skip((CurrentPage - 1) * PageSize).Take(PageSize).ToList();

            // if CurrentPage is greater than 1 means it has previousPage  
            var previousPage = CurrentPage > 1 ? "Yes" : "No";

            // if TotalPages is greater than CurrentPage means it has nextPage  
            var nextPage = CurrentPage < TotalPages ? "Yes" : "No";

            //  // Returing List of Customers Collections  
            FilterationResponseModel<SalonServiceListDTO> obj = new FilterationResponseModel<SalonServiceListDTO>();
            obj.totalCount = TotalCount;
            obj.pageSize = PageSize;
            obj.currentPage = CurrentPage;
            obj.totalPages = TotalPages;
            obj.previousPage = previousPage;
            obj.nextPage = nextPage;
            obj.searchQuery = string.IsNullOrEmpty(model.searchQuery) ? "no parameter passed" : model.searchQuery;
            obj.dataList = items.ToList();

            _applointmentListBackgroundService.StartService();

            if (obj == null)
            {
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = false;
                _response.Messages = ResponseMessages.msgSomethingWentWrong;
                return Ok(_response);
            }

            _response.StatusCode = HttpStatusCode.OK;
            _response.IsSuccess = true;
            _response.Data = obj;
            _response.Messages = ResponseMessages.msgListFoundSuccess;
            return Ok(_response);
        }
        #endregion

        #region GetSalonServiceDetail
        /// <summary>
        ///  Get Salon Service detail.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Authorize(Roles = "SuperAdmin,Admin,Vendor,Customer")]
        [Route("GetSalonServiceDetail")]
        public async Task<IActionResult> GetSalonServiceDetail(int serviceId, string? serviceType)
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

            serviceType = string.IsNullOrEmpty(serviceType) ? "Single" : serviceType;

            if (serviceType != "Single" && serviceType != "Package")
            {
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = false;
                _response.Messages = "Please enter valid service type.";
                return Ok(_response);
            }

            var serviceDetail = await _context.SalonService.FirstOrDefaultAsync(u => u.ServiceId == serviceId);
            if (serviceDetail == null)
            {
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = false;
                _response.Messages = ResponseMessages.msgNotFound + "record";
                return Ok(_response);
            }

            var serviceResponse = await _serviceRepository.GetSalonServiceDetail(serviceId, serviceType, currentUserId);
            return Ok(serviceResponse);

        }
        #endregion

        #region getScheduledDaysTime
        /// <summary>
        /// Get Scheduled Days and Time.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Authorize(Roles = "Vendor")]
        [Route("getScheduledDaysTime")]
        public async Task<IActionResult> GetScheduledDaysTime(int salonId)
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
                // get scheduled days
                var SalonSchedule = await _context.SalonSchedule.Where(a => (a.SalonId == salonId) && (a.IsDeleted != true)).FirstOrDefaultAsync();
                if (SalonSchedule == null)
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = ResponseMessages.msgNotFound + "record";
                    return Ok(_response);
                }

                var response = await _serviceRepository.GetScheduledDaysTime(salonId);
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

        #region AddUpdateSalonService
        /// <summary>
        /// Add Service.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Route("AddUpdateSalonService")]
        [Authorize(Roles = "SuperAdmin,Admin,Vendor")]
        public async Task<IActionResult> AddUpdateSalonService([FromBody] AddUpdateSalonServiceDTO model)
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

                if (model.genderPreferences != "Male" && model.genderPreferences != "Female" && model.genderPreferences != "Common")
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = "Please enter valid gender.";
                    return Ok(_response);
                }

                if (!string.IsNullOrEmpty(model.ServiceType))
                {
                    if (model.ServiceType == "Package")
                    {
                        model.mainCategoryId = 53;
                        model.subCategoryId = 55;
                        if (string.IsNullOrEmpty(model.IncludeServiceId))
                        {
                            _response.StatusCode = HttpStatusCode.OK;
                            _response.IsSuccess = false;
                            _response.Messages = "Please enter service id for package.";
                            return Ok(_response);
                        }
                    }
                    else
                    {
                        model.ServiceType = "Single";
                    }
                }
                else
                {
                    model.ServiceType = "Single";
                }

                if (model.ageRestrictions != "Kids" && model.ageRestrictions != "Adult")
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = "Please enter valid age limit.";
                    return Ok(_response);
                }
                if ((!string.IsNullOrEmpty(model.lockTimeStart) && string.IsNullOrEmpty(model.lockTimeEnd))
                || (string.IsNullOrEmpty(model.lockTimeStart) && !string.IsNullOrEmpty(model.lockTimeEnd))
                )
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = "Please enter start and end time.";
                    return Ok(_response);
                }
                if (!string.IsNullOrEmpty(model.lockTimeStart) && !string.IsNullOrEmpty(model.lockTimeEnd))
                {
                    // if (!CommonMethod.IsValidTimeFormat(model.lockTimeStart) || !CommonMethod.IsValidTimeFormat(model.lockTimeEnd))
                    // {
                    //     _response.StatusCode = HttpStatusCode.OK;
                    //     _response.IsSuccess = false;
                    //     _response.Messages = "Please enter time in correct format(Ex. 10:00 AM).";
                    //     return Ok(_response);
                    // }

                    if (!CommonMethod.IsValidTime24Format(model.lockTimeStart) || !CommonMethod.IsValidTime24Format(model.lockTimeEnd))
                    {
                        _response.StatusCode = HttpStatusCode.OK;
                        _response.IsSuccess = false;
                        _response.Messages = "Please enter time in correct format(Ex. 15:30).";
                        return Ok(_response);
                    }
                    else
                    {
                        model.lockTimeStart = Convert.ToDateTime(model.lockTimeStart).ToString("hh:mm tt");
                        model.lockTimeEnd = Convert.ToDateTime(model.lockTimeEnd).ToString("hh:mm tt");
                    }
                }


                var scheduleDetail = await _context.SalonSchedule.Where(u => u.SalonId == model.salonId).FirstOrDefaultAsync();
                if (scheduleDetail == null)
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = "Can't add service before schedule.";
                    return Ok(_response);
                }


                var response = await _serviceRepository.AddUpdateSalonService(model);
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

        #region getAvailableDates
        /// <summary>
        /// Get Available Dates.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Authorize(Roles = "Customer,Vendor")]
        [Route("getAvailableDates")]
        public async Task<IActionResult> getAvailableDates(int serviceId)
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

                var response = await _serviceRepository.getAvailableDates(serviceId);
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

        #region getAvailableTimeSlots
        /// <summary>
        /// Get available slots.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Authorize(Roles = "Customer,Vendor")]
        [Route("getAvailableTimeSlots")]
        public async Task<IActionResult> getAvailableTimeSlots(int serviceId, string queryDate)
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

                var response = await _serviceRepository.getAvailableTimeSlots(serviceId, queryDate);
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

        #region SetSalonServiceFavouriteStatus
        /// <summary>
        /// Set Salon Service favourite status.
        /// </summary>
        [HttpPost]
        [Route("SetSalonServiceFavouriteStatus")]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> SetSalonServiceFavouriteStatus(SetSalonServiceFavouriteStatusDTO model)
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

                var serviceDetail = await _context.SalonService.FirstOrDefaultAsync(u => u.ServiceId == model.serviceId);
                if (serviceDetail == null)
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Data = new Object { };
                    _response.Messages = ResponseMessages.msgNotFound + "Service";
                    return Ok(_response);
                }


                var response = await _serviceRepository.SetSalonServiceFavouriteStatus(model, currentUserId);
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

        #region getServiceImageInBase64
        /// <summary>
        ///  Get Service Image In Base64.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Authorize(Roles = "SuperAdmin,Admin,Vendor,Customer")]
        [Route("getServiceImageInBase64")]
        public async Task<IActionResult> getServiceImageInBase64(int serviceId, string? Status)
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

            var serviceDetail = await _context.SalonService.Where(u => u.ServiceId == serviceId).FirstOrDefaultAsync();
            if (serviceDetail == null)
            {
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = false;
                _response.Messages = ResponseMessages.msgNotFound + "record";
                return Ok(_response);
            }

            var response = await _serviceRepository.getServiceImageInBase64(serviceId, Status);
            return Ok(response);

           
        }
        #endregion

        #region SetServiceStatus
        /// <summary>
        /// Set service status 
        /// </summary>
        [HttpPost]
        [Route("SetServiceStatus")]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Authorize(Roles = "SuperAdmin,Admin,Vendor")]
        public async Task<IActionResult> SetServiceStatus(SetServiceStatusDTO model)
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

                if (model.status != Convert.ToInt32(ServiceStatus.Active)
                && model.status != Convert.ToInt32(ServiceStatus.Pending)
                && model.status != Convert.ToInt32(ServiceStatus.InActive))
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = "Please select a valid status.";
                    return Ok(_response);
                }

                var serviceDeatils = await _context.SalonService.FirstOrDefaultAsync(u => u.ServiceId == model.serviceId);
                if (serviceDeatils == null)
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Data = new Object { };
                    _response.Messages = ResponseMessages.msgNotFound + "service";
                    return Ok(_response);
                }


                var response = await _serviceRepository.SetServiceStatus(model);
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

        #region deleteSalonService
        /// <summary>
        ///  Delete Salon Service.
        /// </summary>
        [HttpDelete("DeleteSalonService")]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Authorize(Roles = "SuperAdmin,Admin,Vendor")]
        public async Task<IActionResult> DeleteSalonService(int serviceId)
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

                var salonService = await _context.SalonService.Where(x => (x.ServiceId == serviceId) && (x.IsDeleted != true)).FirstOrDefaultAsync();
                if (salonService == null)
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = ResponseMessages.msgNotFound + "record";
                    return Ok(_response);
                }

                var service = await _serviceRepository.DeleteSalonService(serviceId);

                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Messages = "Service is" + ResponseMessages.msgDeletionSuccess;
                return Ok(_response);

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
