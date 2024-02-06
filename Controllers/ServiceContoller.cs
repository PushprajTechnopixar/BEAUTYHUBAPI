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

                // save scheduled time slot
                string startTime = model.fromTime;
                string endTime = model.toTime;

                List<TimeList> timeList = new List<TimeList>();

                var indiaDate = DateTime.Now.ToString(@"yyyy-MM-dd");

                var startDateTime = Convert.ToDateTime(indiaDate + " " + startTime);
                var endDateTime = Convert.ToDateTime(indiaDate + " " + endTime);

                model.fromTime = startDateTime.ToString(@"hh\:mm tt");
                model.toTime = endDateTime.ToString(@"hh\:mm tt");

                int update = 1;
                var SalonScheduleDays = await _context.SalonSchedule.Where(a => a.SalonId == model.salonId).FirstOrDefaultAsync();
                if (SalonScheduleDays != null)
                {
                    if (SalonScheduleDays.UpdateStatus == false)
                    {
                        _response.StatusCode = HttpStatusCode.OK;
                        _response.IsSuccess = false;
                        _response.Messages = "Please wait while the schedule is updating.";
                        return Ok(_response);
                    }
                    var scheduledDaysList = new List<string>();
                    if (SalonScheduleDays.Monday == true)
                    {
                        scheduledDaysList.Add("Monday");
                    }
                    if (SalonScheduleDays.Tuesday == true)
                    {
                        scheduledDaysList.Add("Tuesday");
                    }
                    if (SalonScheduleDays.Wednesday == true)
                    {
                        scheduledDaysList.Add("Wednesday");
                    }
                    if (SalonScheduleDays.Thursday == true)
                    {
                        scheduledDaysList.Add("Thursday");
                    }
                    if (SalonScheduleDays.Friday == true)
                    {
                        scheduledDaysList.Add("Friday");
                    }
                    if (SalonScheduleDays.Saturday == true)
                    {
                        scheduledDaysList.Add("Saturday");
                    }
                    if (SalonScheduleDays.Sunday == true)
                    {
                        scheduledDaysList.Add("Sunday");
                    }

                    var daysList = new List<string>();
                    if (model.monday == true)
                    {
                        daysList.Add("Monday");
                    }
                    if (model.tuesday == true)
                    {
                        daysList.Add("Tuesday");
                    }
                    if (model.wednesday == true)
                    {
                        daysList.Add("Wednesday");
                    }
                    if (model.thursday == true)
                    {
                        daysList.Add("Thursday");
                    }
                    if (model.friday == true)
                    {
                        daysList.Add("Friday");
                    }
                    if (model.saturday == true)
                    {
                        daysList.Add("Saturday");
                    }
                    if (model.sunday == true)
                    {
                        daysList.Add("Sunday");
                    }

                    List<string> modelTimeList = new List<string>();
                    foreach (var addTime in timeList)
                    {
                        modelTimeList.Add(addTime.time);
                    }

                    // foreach (var appointmentDetail in appointmentDetails)
                    // {
                    //     var bookingDay = appointmentDetail.BookingDate.ToString("dddd");
                    //     var bookingTime = appointmentDetail.BookingTime;

                    //     if (!daysList.Contains(bookingDay) || !modelTimeList.Contains(bookingTime))
                    //     {
                    //         update = 0;
                    //     }
                    // }

                    if (update == 1)
                    {
                        var scheduledStartDateTime = Convert.ToDateTime(indiaDate + " " + SalonScheduleDays.FromTime);
                        var scheduledEndDateTime = Convert.ToDateTime(indiaDate + " " + SalonScheduleDays.ToTime);
                        var modelFromTime = Convert.ToDateTime(indiaDate + " " + model.fromTime);
                        var modelToTime = Convert.ToDateTime(indiaDate + " " + model.toTime);
                        var timeSlots = await _context.BookedService.Where(u => u.AppointmentStatus == "Scheduled" && u.SalonId == model.salonId).ToListAsync();
                        var bookeddates = timeSlots.DistinctBy(u => u.AppointmentDate);
                        var bookeddays = new List<string>();
                        foreach (var item in bookeddates)
                        {
                            bookeddays.Add(Convert.ToDateTime(item.AppointmentDate).DayOfWeek.ToString());
                        }
                        var remainingBookedDays = bookeddays.Except(daysList);
                        if (remainingBookedDays.Any())
                        {
                            _response.StatusCode = HttpStatusCode.OK;
                            _response.IsSuccess = false;
                            _response.Messages = "Can't update while an appointment is scheduled.";
                            return Ok(_response);
                        }
                        if (modelFromTime > scheduledStartDateTime || modelToTime < scheduledEndDateTime)
                        {
                            if (modelFromTime > scheduledStartDateTime)
                            {
                                foreach (var item in timeSlots)
                                {
                                    var scheduledToTime = Convert.ToDateTime(indiaDate + " " + item.ToTime);
                                    var scheduledFromTime = Convert.ToDateTime(indiaDate + " " + item.FromTime);
                                    if (scheduledFromTime < modelFromTime)
                                    {
                                        _response.StatusCode = HttpStatusCode.OK;
                                        _response.IsSuccess = false;
                                        _response.Messages = "Can't update while an appointment is scheduled.";
                                        return Ok(_response);
                                    }
                                }
                            }
                            if (modelToTime < scheduledEndDateTime)
                            {
                                foreach (var item in timeSlots)
                                {
                                    var scheduledToTime = Convert.ToDateTime(indiaDate + " " + item.ToTime);
                                    var scheduledFromTime = Convert.ToDateTime(indiaDate + " " + item.FromTime);
                                    if (scheduledToTime > modelToTime)
                                    {
                                        _response.StatusCode = HttpStatusCode.OK;
                                        _response.IsSuccess = false;
                                        _response.Messages = "Can't update while an appointment is scheduled.";
                                        return Ok(_response);
                                    }
                                }
                            }

                        }

                        SalonScheduleDays.Monday = model.monday;
                        SalonScheduleDays.Tuesday = model.tuesday;
                        SalonScheduleDays.Wednesday = model.wednesday;
                        SalonScheduleDays.Thursday = model.thursday;
                        SalonScheduleDays.Friday = model.friday;
                        SalonScheduleDays.Saturday = model.saturday;
                        SalonScheduleDays.Sunday = model.sunday;
                        SalonScheduleDays.FromTime = model.fromTime;
                        SalonScheduleDays.ToTime = model.toTime;

                        SalonScheduleDays.Status = true;
                        SalonScheduleDays.UpdateStatus = false;

                        _context.Update(SalonScheduleDays);
                        var res = await _context.SaveChangesAsync();

                        _backgroundService.StartService(model.salonId);

                        _response.StatusCode = HttpStatusCode.OK;
                        _response.IsSuccess = true;
                        _response.Messages = "Scheduled" + ResponseMessages.msgUpdationSuccess;
                        return Ok(_response);
                    }

                    if (update == 0)
                    {
                        _response.StatusCode = HttpStatusCode.InternalServerError;
                        _response.IsSuccess = false;
                        _response.Data = new { };
                        _response.Messages = "Can't update, while your scheduled timing is booked.";
                        return Ok(_response);
                    }
                }

                // Add Salon Schedule
                var SalonSchedule = new SalonSchedule();
                SalonSchedule.SalonId = model.salonId;
                SalonSchedule.Monday = model.monday;
                SalonSchedule.Tuesday = model.tuesday;
                SalonSchedule.Wednesday = model.wednesday;
                SalonSchedule.Thursday = model.thursday;
                SalonSchedule.Friday = model.friday;
                SalonSchedule.Saturday = model.saturday;
                SalonSchedule.Sunday = model.sunday;
                SalonSchedule.FromTime = model.fromTime;
                SalonSchedule.ToTime = model.toTime;
                SalonSchedule.Status = false;
                SalonSchedule.UpdateStatus = false;

                _context.SalonSchedule.Add(SalonSchedule);
                _context.SaveChanges();

                _backgroundService.StartService(model.salonId);

                var scheduledDays = new ScheduleDayResonceDTO();
                scheduledDays.monday = SalonSchedule.Monday;
                scheduledDays.tuesday = SalonSchedule.Tuesday;
                scheduledDays.wednesday = SalonSchedule.Wednesday;
                scheduledDays.thursday = SalonSchedule.Thursday;
                scheduledDays.friday = SalonSchedule.Friday;
                scheduledDays.saturday = SalonSchedule.Saturday;
                scheduledDays.sunday = SalonSchedule.Sunday;
                scheduledDays.fromTime = SalonSchedule.FromTime;
                scheduledDays.toTime = SalonSchedule.ToTime;
                scheduledDays.updateStatus = SalonSchedule.UpdateStatus;

                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Messages = "Scheduled detail" + ResponseMessages.msgDataSavedSuccess;
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

                //  var scheduleDayViewModel = new ScheduleDayResonceDTO();
                //  scheduleDayViewModel.monday = SalonSchedule.Monday;
                //  scheduleDayViewModel.tuesday = SalonSchedule.Tuesday;
                //  scheduleDayViewModel.wednesday = SalonSchedule.Wednesday;
                //  scheduleDayViewModel.thursday = SalonSchedule.Thursday;
                //  scheduleDayViewModel.friday = SalonSchedule.Friday;
                //  scheduleDayViewModel.saturday = SalonSchedule.Saturday;
                //  scheduleDayViewModel.sunday = SalonSchedule.Sunday;
                //  scheduleDayViewModel.fromTime = Convert.ToDateTime(SalonSchedule.FromTime).ToString(@"HH:mm");
                //  scheduleDayViewModel.toTime = Convert.ToDateTime(SalonSchedule.ToTime).ToString(@"HH:mm");
                //  // scheduleDayViewModel.fromTime = SalonSchedule.FromTime;
                //  // scheduleDayViewModel.toTime = SalonSchedule.ToTime;
                //  scheduleDayViewModel.salonId = SalonSchedule.SalonId;
                //  scheduleDayViewModel.updateStatus = SalonSchedule.UpdateStatus;
                //
                //  _response.StatusCode = HttpStatusCode.OK;
                //  _response.IsSuccess = true;
                //  _response.Messages = "Detail" + ResponseMessages.msgShownSuccess;
                //  _response.Data = scheduleDayViewModel;
                //  return Ok(_response);

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

                string[] splitLockTimeStart = model.lockTimeStart.Split(",");
                string[] splitLockTimeend = model.lockTimeEnd.Split(",");
                string[] splitIncludeProduct = new string[0];
                if (!string.IsNullOrEmpty(model.IncludeServiceId))
                {
                    splitIncludeProduct = model.IncludeServiceId.Split(",");

                    foreach (var item in splitIncludeProduct)
                    {
                        var ckeckService = await _context.SalonService.Where(u => u.ServiceId == Convert.ToInt32(item)).FirstOrDefaultAsync();
                        if (splitLockTimeStart.Length != splitLockTimeend.Length)
                        {
                            _response.StatusCode = HttpStatusCode.OK;
                            _response.IsSuccess = false;
                            _response.Messages = ResponseMessages.msgNotFound + "selected service for package.";
                            return Ok(_response);
                        }
                    }
                }

                if (splitLockTimeStart.Length != splitLockTimeend.Length)
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = "Locked start and end time should be same.";
                    return Ok(_response);
                }

                List<DateTime> lockTimeStart = new List<DateTime>();
                List<DateTime> lockTimend = new List<DateTime>();

                if (!string.IsNullOrEmpty(model.lockTimeStart))
                {
                    for (int l = 0; l < splitLockTimeStart.Length; l++)
                    {
                        lockTimeStart.Add(Convert.ToDateTime(splitLockTimeStart[l]));
                        lockTimend.Add(Convert.ToDateTime(splitLockTimeend[l]));
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
                var serviceDetail = await _context.SalonService.Where(u => u.ServiceId == model.serviceId).FirstOrDefaultAsync();
                if (model.serviceId > 0)
                {
                    if (serviceDetail == null)
                    {
                        _response.StatusCode = HttpStatusCode.OK;
                        _response.IsSuccess = false;
                        _response.Messages = ResponseMessages.msgNotFound + "Service";
                        return Ok(_response);
                    }

                    string indiaDate = DateTime.Now.ToString(@"yyyy-MM-dd");
                    var lockStartDateTime = Convert.ToDateTime(indiaDate + " " + serviceDetail.LockTimeStart);
                    var lockEndDateTime = Convert.ToDateTime(indiaDate + " " + serviceDetail.LockTimeEnd);
                    var modelFromTime = Convert.ToDateTime(indiaDate + " " + model.lockTimeStart);
                    var modelToTime = Convert.ToDateTime(indiaDate + " " + model.lockTimeEnd);
                    var timeSlots = await _context.BookedService.Where(u => u.AppointmentStatus == "Scheduled" && u.ServiceId == model.serviceId && u.SalonId == model.salonId).ToListAsync();

                    if (modelFromTime > lockStartDateTime || modelToTime < lockEndDateTime)
                    {

                        if (modelFromTime <= lockStartDateTime || modelFromTime >= lockStartDateTime)
                        {
                            foreach (var item in timeSlots)
                            {
                                var scheduledToTime = Convert.ToDateTime(indiaDate + " " + item.ToTime);
                                var scheduledFromTime = Convert.ToDateTime(indiaDate + " " + item.FromTime);
                                var fromtime = Convert.ToDateTime(indiaDate + " " + model.lockTimeStart);
                                var totime = Convert.ToDateTime(indiaDate + " " + model.lockTimeEnd);
                                if (scheduledFromTime <= modelFromTime && scheduledToTime >= modelFromTime)
                                {
                                    _response.StatusCode = HttpStatusCode.OK;
                                    _response.IsSuccess = false;
                                    _response.Messages = "Can't update while an appointment is scheduled.";
                                    return Ok(_response);
                                }
                            }
                        }
                        if (modelToTime <= lockEndDateTime || modelToTime >= lockEndDateTime)
                        {
                            foreach (var item in timeSlots)
                            {
                                var scheduledToTime = Convert.ToDateTime(indiaDate + " " + item.ToTime);
                                var scheduledFromTime = Convert.ToDateTime(indiaDate + " " + item.FromTime);
                                var fromtime = Convert.ToDateTime(indiaDate + " " + model.lockTimeStart);
                                var totime = Convert.ToDateTime(indiaDate + " " + model.lockTimeEnd);
                                if (scheduledToTime > modelToTime && scheduledToTime <= modelToTime)
                                {
                                    _response.StatusCode = HttpStatusCode.OK;
                                    _response.IsSuccess = false;
                                    _response.Messages = "Can't update while an appointment is scheduled.";
                                    return Ok(_response);
                                }
                            }
                        }

                    }

                }

                var scheduledDaysList = new List<string>();
                if (scheduleDetail.Monday == true)
                {
                    scheduledDaysList.Add("Monday");
                }
                if (scheduleDetail.Tuesday == true)
                {
                    scheduledDaysList.Add("Tuesday");
                }
                if (scheduleDetail.Wednesday == true)
                {
                    scheduledDaysList.Add("Wednesday");
                }
                if (scheduleDetail.Thursday == true)
                {
                    scheduledDaysList.Add("Thursday");
                }
                if (scheduleDetail.Friday == true)
                {
                    scheduledDaysList.Add("Friday");
                }
                if (scheduleDetail.Saturday == true)
                {
                    scheduledDaysList.Add("Saturday");
                }
                if (scheduleDetail.Sunday == true)
                {
                    scheduledDaysList.Add("Sunday");
                }

                model.mainCategoryId = model.mainCategoryId == null ? model.mainCategoryId = 0 : model.mainCategoryId;
                model.subCategoryId = model.subCategoryId == null ? model.subCategoryId = 0 : model.subCategoryId;

                model.listingPrice = (double)(model.basePrice - model.discount);

                // var userDetail = _userManager.FindByIdAsync(currentUserId).GetAwaiter().GetResult();
                // if (userDetail != null)
                // {
                //     var roles = await _userManager.GetRolesAsync(userDetail);
                //     if (roles[0] == "SuperAdmin")
                //     {
                //         model.status = 1;
                //     }
                // }
                model.status = 1;

                var addUpdateServiceEntity = _mapper.Map<SalonService>(model);
                GetSalonServiceDTO? response = new GetSalonServiceDTO();
                var message = "";

                if (model.mainCategoryId > 0)
                {
                    var isCategoryExist = await _context.MainCategory.Where(u => u.MainCategoryId == model.mainCategoryId).FirstOrDefaultAsync();
                    if (isCategoryExist == null)
                    {
                        _response.StatusCode = HttpStatusCode.OK;
                        _response.IsSuccess = false;
                        _response.Messages = ResponseMessages.msgNotFound + "Category";
                        return Ok(_response);
                    }
                }
                if (model.subCategoryId > 0)
                {
                    var isCategoryExist = await _context.SubCategory.Where(u => u.SubCategoryId == model.subCategoryId).FirstOrDefaultAsync();
                    if (isCategoryExist == null)
                    {
                        _response.StatusCode = HttpStatusCode.OK;
                        _response.IsSuccess = false;
                        _response.Messages = ResponseMessages.msgNotFound + "Category";
                        return Ok(_response);
                    }
                    addUpdateServiceEntity.MainCategoryId = isCategoryExist.MainCategoryId;
                }
                if (model.serviceId == 0)
                {
                    await _context.AddAsync(addUpdateServiceEntity);
                    await _context.SaveChangesAsync();

                    if (splitIncludeProduct.Count() > 0)
                    {
                        var servicePackage = new ServicePackage();
                        servicePackage.ServiceId = addUpdateServiceEntity.ServiceId;
                        servicePackage.IncludeServiceId = model.IncludeServiceId;
                        servicePackage.SalonId = model.salonId;

                        await _context.AddAsync(servicePackage);
                        await _context.SaveChangesAsync();
                    }

                    response = _mapper.Map<GetSalonServiceDTO>(addUpdateServiceEntity);

                    message = "Service" + ResponseMessages.msgAdditionSuccess;
                }
                else
                {
                    _mapper.Map(model, serviceDetail);
                    _context.Update(serviceDetail);
                    await _context.SaveChangesAsync();

                    if (splitIncludeProduct.Count() > 0)
                    {
                        var servicePackage = await _context.ServicePackage.FirstOrDefaultAsync(u => u.ServiceId == serviceDetail.ServiceId);
                        if (servicePackage != null)
                        {
                            servicePackage.IncludeServiceId = model.IncludeServiceId;

                            _context.Update(servicePackage);
                            await _context.SaveChangesAsync();
                        }
                    }

                    response = _mapper.Map<GetSalonServiceDTO>(serviceDetail);
                    message = "Service" + ResponseMessages.msgUpdationSuccess;
                }

                var deleteTimeSlot = _context.TimeSlot.Where(u => u.ServiceId == response.serviceId);

                foreach (var item3 in deleteTimeSlot)
                {
                    item3.Status = false;
                }
                _context.UpdateRange(deleteTimeSlot);
                await _context.SaveChangesAsync();

                int addDay = 0;
                for (int i = 0; i < 7; i++)
                {
                    DateTime currentDate = DateTime.Now.AddDays(i);
                    string currentDateStr = currentDate.ToString("yyyy-MM-dd");
                    string dayName = currentDate.ToString("dddd");

                    var existingTimeSlot = _context.TimeSlot
                        .Where(u => u.ServiceId == response.serviceId && u.SlotDate.Date == currentDate.Date)
                        .ToList();

                    if (!scheduledDaysList.Contains(dayName))
                    {
                        foreach (var existingSlot in existingTimeSlot)
                        {
                            existingSlot.Status = false;
                        }

                        _context.UpdateRange(existingTimeSlot);
                        await _context.SaveChangesAsync();
                        continue;
                    }

                    DateTime startDateTime = DateTime.Parse(currentDateStr + " " + scheduleDetail.FromTime);
                    DateTime endDateTime = DateTime.Parse(currentDateStr + " " + scheduleDetail.ToTime);
                    int minutes = response.durationInMinutes;
                    startDateTime = startDateTime.AddMinutes(-minutes);
                    endDateTime = endDateTime.AddMinutes(-minutes);

                    TimeSpan timeInterval = endDateTime - startDateTime;
                    int totalMinutes = (int)timeInterval.TotalMinutes;
                    int noOfTimeSlot = totalMinutes / minutes;

                    var timeList = new List<TimeList>();
                    for (int j = 0; j < noOfTimeSlot; j++)
                    {
                        TimeList obj1 = new TimeList();
                        startDateTime = startDateTime.AddMinutes(minutes);
                        obj1.time = startDateTime.ToString("hh:mm tt");
                        timeList.Add(obj1);
                    }

                    foreach (var item2 in timeList)
                    {
                        var timeslot = new TimeSlot
                        {
                            ServiceId = response.serviceId,
                            FromTime = item2.time,
                            ToTime = DateTime.Parse(item2.time).AddMinutes(minutes).ToString("hh:mm tt"),
                            SlotDate = Convert.ToDateTime(currentDate.ToString(@"yyyy-MM-dd")),
                            SlotCount = response.totalCountPerDuration,
                            Status = true
                        };

                        bool pass = true;
                        var existingTimeSlotDetails = existingTimeSlot.FirstOrDefault(u => u.FromTime == timeslot.FromTime);
                        if (!string.IsNullOrEmpty(model.lockTimeStart))
                        {
                            for (int m = 0; m < lockTimeStart.Count; m++)
                            {
                                var chkLockedFrom = DateTime.Parse(currentDateStr + " " + lockTimeStart[m].ToString(@"hh:mm tt"));
                                var chkLockedTo = DateTime.Parse(currentDateStr + " " + lockTimend[m].ToString(@"hh:mm tt"));
                                var fromTime = DateTime.Parse(currentDateStr + " " + timeslot.FromTime);
                                var toTime = DateTime.Parse(currentDateStr + " " + timeslot.ToTime);
                                if ((fromTime <= chkLockedFrom && toTime <= chkLockedFrom) || (fromTime >= chkLockedTo && toTime >= chkLockedTo))
                                {
                                    if (existingTimeSlotDetails == null)
                                    {
                                        await _context.AddAsync(timeslot);
                                        await _context.SaveChangesAsync();
                                    }
                                    else
                                    {
                                        existingTimeSlotDetails.Status = true;
                                        _context.Update(existingTimeSlotDetails);
                                        await _context.SaveChangesAsync();
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (existingTimeSlotDetails == null)
                            {
                                await _context.AddAsync(timeslot);
                                await _context.SaveChangesAsync();
                            }
                            else
                            {
                                existingTimeSlotDetails.Status = true;
                                _context.Update(existingTimeSlotDetails);
                                await _context.SaveChangesAsync();
                            }
                        }
                    }
                    addDay++;
                }

                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Data = response;
                _response.Messages = message;
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

                //// get scheduled days
                //var slotDetail = await _context.TimeSlot
                //                    .Where(a => a.ServiceId == serviceId && a.Status != false && a.SlotCount > 0 && a.IsDeleted != true)
                //                    .Select(u => u.SlotDate)
                //                    .Distinct()
                //                    .ToListAsync();

                //var availableDates = new List<string>();
                //var ctz = TZConvert.GetTimeZoneInfo("India Standard Time");
                //var convrtedZoneDate = TimeZoneInfo.ConvertTimeFromUtc(Convert.ToDateTime(DateTime.UtcNow), ctz);
                //foreach (var item in slotDetail)
                //{
                //    if (item.Date == convrtedZoneDate.Date)
                //    {
                //        // get scheduled days
                //        var slotDetail1 = await _context.TimeSlot
                //                            .Where(a => a.ServiceId == serviceId && a.Status != false && a.SlotCount > 0 && a.IsDeleted != true && a.SlotDate == DateTime.Now.Date)
                //                            .ToListAsync();

                //        // Get the current time and add 2 hours to it
                //        var limitDate = DateTime.Now.AddHours(2);
                //        var availableSlots = new List<timeSlotsDTO>();
                //        foreach (var item1 in slotDetail1)
                //        {
                //            var fromTime = (Convert.ToDateTime(item1.FromTime).TimeOfDay);

                //            var currentTime = convrtedZoneDate.TimeOfDay;
                //            var timeDifference = (fromTime.TotalMinutes - currentTime.TotalMinutes);

                //            int minutesThreshold = 05; // Set your threshold here
                //            if (timeDifference >= minutesThreshold)
                //            {
                //                availableSlots.Add(_mapper.Map<timeSlotsDTO>(item1));
                //            }
                //        }
                //        if (availableSlots.Count > 0)
                //        {
                //            availableDates.Add(item.ToString(@"dd-MM-yyyy"));
                //        }
                //    }
                //    else
                //        availableDates.Add(item.ToString(@"dd-MM-yyyy"));
                //}

                //if (slotDetail != null)
                //{
                //    _response.StatusCode = HttpStatusCode.OK;
                //    _response.IsSuccess = true;
                //    _response.Messages = "Dates shown" + ResponseMessages.msgShownSuccess;
                //    _response.Data = availableDates;
                //    return Ok(_response);
                //}
                //_response.StatusCode = HttpStatusCode.OK;
                //_response.IsSuccess = false;
                //_response.Messages = ResponseMessages.msgNotFound + "record";
                //return Ok(_response);
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

                //// queryDate = Convert.ToDateTime(queryDate.ToString(@"yyyy-MM-dd"));
                //string format = "dd-MM-yyyy";
                //DateTime searchDate = new DateTime();

                //try
                //{
                //    // Parse the string into a DateTime object using the specified format
                //    searchDate = DateTime.ParseExact(queryDate, format, null);
                //}
                //catch (FormatException)
                //{
                //    _response.StatusCode = HttpStatusCode.OK;
                //    _response.IsSuccess = false;
                //    _response.Messages = "Invalid date format.";
                //    return Ok(_response);
                //}
                //var slotDetail = await _context.TimeSlot
                //                   .Where(a => a.ServiceId == serviceId && a.Status != false && a.SlotCount > 0 && a.IsDeleted != true && a.SlotDate == searchDate)
                //                   .ToListAsync();
                //// get scheduled days
                //var sortedSlots = slotDetail.OrderBy(a => Convert.ToDateTime(a.FromTime)).ToList();

                //// Get the current time and add 2 hours to it
                //var limitDate = DateTime.Now.AddHours(2);
                //var availableSlots = new List<timeSlotsDTO>();

                //var ctz = TZConvert.GetTimeZoneInfo("India Standard Time");
                //var convrtedZoneDate = TimeZoneInfo.ConvertTimeFromUtc(Convert.ToDateTime(DateTime.UtcNow), ctz);

                //foreach (var item in sortedSlots)
                //{
                //    if (searchDate.Date == convrtedZoneDate.Date)
                //    {
                //        var fromTime = (Convert.ToDateTime(item.FromTime).TimeOfDay);
                //        var currentTime = convrtedZoneDate.TimeOfDay;
                //        var timeDifference = (fromTime.TotalMinutes - currentTime.TotalMinutes);

                //        int minutesThreshold = 05; // Set your threshold here
                //        if (timeDifference >= minutesThreshold)
                //        {
                //            availableSlots.Add(_mapper.Map<timeSlotsDTO>(item));
                //        }
                //    }
                //    else
                //    {
                //        availableSlots.Add(_mapper.Map<timeSlotsDTO>(item));
                //    }
                //}

                //if (availableSlots.Any())
                //{
                //    _response.StatusCode = HttpStatusCode.OK;
                //    _response.IsSuccess = true;
                //    _response.Messages = "Slots shown" + ResponseMessages.msgShownSuccess;
                //    _response.Data = availableSlots;
                //    return Ok(_response);
                //}
                //_response.StatusCode = HttpStatusCode.OK;
                //_response.IsSuccess = false;
                //_response.Messages = ResponseMessages.msgNotFound + "record";
                //return Ok(_response);

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

                //  var salonServiceFavouriteStatus = await _context.FavouriteService.Where(u => u.ServiceId == model.serviceId && u.CustomerUserId == //currentUserId).FirstOrDefaultAsync();
                //  string msg = "";
                //  if (model.status == true)
                //  {
                //      if (salonServiceFavouriteStatus != null)
                //      {
                //          _response.StatusCode = HttpStatusCode.OK;
                //          _response.IsSuccess = false;
                //          _response.Data = new Object { };
                //          _response.Messages = "Already added to favorites.";
                //          return Ok(_response);
                //      }
                //      var addFavouriteService = new FavouriteService();
                //      addFavouriteService.CustomerUserId = currentUserId;
                //      addFavouriteService.ServiceId = model.serviceId;
                //      _context.Add(addFavouriteService);
                //      _context.SaveChanges();
                //      msg = "Added to favorites.";
                //
                //  }
                //  else
                //  {
                //      if (salonServiceFavouriteStatus == null)
                //      {
                //          _response.StatusCode = HttpStatusCode.OK;
                //          _response.IsSuccess = false;
                //          _response.Data = new Object { };
                //          _response.Messages = ResponseMessages.msgNotFound + "record";
                //          return Ok(_response);
                //      }
                //      _context.Remove(salonServiceFavouriteStatus);
                //      _context.SaveChanges();
                //      msg = "Removed from favorites.";
                //  }
                //
                //  var getService = await _context.SalonService.FirstOrDefaultAsync(u => u.ServiceId == model.serviceId);
                //  var response = _mapper.Map<serviceDetailDTO>(getService);
                //  var favouriteStatus = await _context.FavouriteService.FirstOrDefaultAsync(u => u.ServiceId == model.serviceId && u.CustomerUserId == //currentUserId);
                //  response.favouriteStatus = favouriteStatus != null ? true : false;
                //
                //  if (getService != null)
                //  {
                //      _response.StatusCode = HttpStatusCode.OK;
                //      _response.IsSuccess = true;
                //      _response.Data = response;
                //      _response.Messages = msg;
                //      return Ok(_response);
                //  }
                //  else
                //  {
                //      _response.StatusCode = HttpStatusCode.OK;
                //      _response.IsSuccess = false;
                //      _response.Data = new Object { };
                //      _response.Messages = ResponseMessages.msgSomethingWentWrong;
                //      return Ok(_response);
                //  }
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

            //List<string> serviceImageList = new List<string>();

            //if (!string.IsNullOrEmpty(serviceDetail.ServiceImage1))
            //{
            //    var httpClient = new HttpClient();
            //    // string imageUrl = imgURL + productDetail.ProductImage1;
            //    string imageUrl = imgURL + serviceDetail.ServiceImage1;
            //    byte[]? imageBytes;
            //    try
            //    {
            //        imageBytes = await httpClient.GetByteArrayAsync(imageUrl);
            //        if (imageBytes != null)
            //        {
            //            var base64String = Convert.ToBase64String(imageBytes);
            //            var image = imgData + base64String;

            //            serviceImageList.Add(image);
            //        }
            //    }
            //    catch
            //    {
            //    }
            //}
            //if (!string.IsNullOrEmpty(serviceDetail.ServiceImage2))
            //{
            //    var httpClient = new HttpClient();

            //    string imageUrl = imgURL + serviceDetail.ServiceImage2;
            //    byte[]? imageBytes;
            //    try
            //    {
            //        imageBytes = await httpClient.GetByteArrayAsync(imageUrl);
            //        if (imageBytes != null)
            //        {
            //            var base64String = Convert.ToBase64String(imageBytes);
            //            var image = imgData + base64String;

            //            serviceImageList.Add(image);
            //        }
            //    }
            //    catch
            //    {
            //    }
            //}
            //if (!string.IsNullOrEmpty(serviceDetail.ServiceImage3))
            //{
            //    var httpClient = new HttpClient();
            //    // string imageUrl = imgURL + productDetail.ProductImage3;
            //    string imageUrl = imgURL + serviceDetail.ServiceImage3;
            //    byte[]? imageBytes;
            //    try
            //    {
            //        imageBytes = await httpClient.GetByteArrayAsync(imageUrl);
            //        if (imageBytes != null)
            //        {
            //            var base64String = Convert.ToBase64String(imageBytes);
            //            var image = imgData + base64String;

            //            serviceImageList.Add(image);
            //        }
            //    }
            //    catch
            //    {
            //    }
            //}
            //if (!string.IsNullOrEmpty(serviceDetail.ServiceImage4))
            //{
            //    var httpClient = new HttpClient();
            //    // string imageUrl = imgURL + productDetail.ProductImage4;
            //    string imageUrl = imgURL + serviceDetail.ServiceImage4;
            //    byte[]? imageBytes;
            //    try
            //    {
            //        imageBytes = await httpClient.GetByteArrayAsync(imageUrl);
            //        if (imageBytes != null)
            //        {
            //            var base64String = Convert.ToBase64String(imageBytes);
            //            var image = imgData + base64String;

            //            serviceImageList.Add(image);
            //        }
            //    }
            //    catch
            //    {
            //    }
            //}
            //if (!string.IsNullOrEmpty(serviceDetail.ServiceImage5))
            //{
            //    var httpClient = new HttpClient();
            //    // string imageUrl = imgURL + productDetail.ProductImage5;
            //    string imageUrl = imgURL + serviceDetail.ServiceImage5;
            //    byte[]? imageBytes;
            //    try
            //    {
            //        imageBytes = await httpClient.GetByteArrayAsync(imageUrl);
            //        if (imageBytes != null)
            //        {
            //            var base64String = Convert.ToBase64String(imageBytes);
            //            var image = imgData + base64String;

            //            serviceImageList.Add(image);
            //        }
            //    }
            //    catch
            //    {
            //    }
            //}

            //_response.StatusCode = HttpStatusCode.OK;
            //_response.IsSuccess = true;
            //_response.Data = serviceImageList;
            //_response.Messages = "Service image" + ResponseMessages.msgListFoundSuccess;
            //return Ok(_response);
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

                //serviceDeatils.Status = model.status;
                //_context.Update(serviceDeatils);
                //_context.SaveChanges();

                //var getService = await _context.SalonService.FirstOrDefaultAsync(u => u.ServiceId == model.serviceId);
                //if (getService != null)
                //{
                //    var response = _mapper.Map<serviceDetailDTO>(getService);
                //    _response.StatusCode = HttpStatusCode.OK;
                //    _response.IsSuccess = true;
                //    _response.Data = response;
                //    _response.Messages = "Service" + ResponseMessages.msgUpdationSuccess;
                //    return Ok(_response);
                //}
                //else
                //{
                //    _response.StatusCode = HttpStatusCode.OK;
                //    _response.IsSuccess = false;
                //    _response.Data = new Object { };
                //    _response.Messages = ResponseMessages.msgSomethingWentWrong;
                //    return Ok(_response);
                //}

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
