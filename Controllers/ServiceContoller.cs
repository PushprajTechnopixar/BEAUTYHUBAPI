﻿using AutoMapper;
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
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IMembershipRecordRepository _membershipRecordRepository;
        private readonly IWebHostEnvironment _hostingEnvironment;
        private readonly MyBackgroundService _backgroundService;

        public ServiceController(IMapper mapper,
        IUploadRepository uploadRepository,
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IMembershipRecordRepository membershipRecordRepository,

        IWebHostEnvironment hostingEnvironment,
        MyBackgroundService backgroundService
        )
        {
            _mapper = mapper;
            _uploadRepository = uploadRepository;
            _response = new();
            _context = context;
            _userManager = userManager;
            _membershipRecordRepository = membershipRecordRepository;
            httpClient = new HttpClient();
            _backgroundService = backgroundService;
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

                var Salon = await _context.SalonDetail.Where(a => a.SalonId == model.salonId).FirstOrDefaultAsync();
                if (Salon == null)
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = "Not found any Salon.";
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
                        if (modelFromTime > scheduledStartDateTime || modelToTime < scheduledEndDateTime)
                        {
                            if (modelFromTime > scheduledStartDateTime)
                            {
                                var timeSlots = await _context.BookedService
                               .Where(u => u.AppointmentStatus == "Scheduled").ToListAsync();
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
                                var timeSlots = await _context.BookedService
                                .Where(u => u.AppointmentStatus == "Scheduled").ToListAsync();

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

                        SalonScheduleDays.Status = false;
                        SalonScheduleDays.UpdateStatus = false;
                        _context.Update(SalonScheduleDays);
                        await _context.SaveChangesAsync();

                        // scheduledDaysList = new List<string>();
                        // if (SalonScheduleDays.Monday == true)
                        // {
                        //     scheduledDaysList.Add("Monday");
                        // }
                        // if (SalonScheduleDays.Tuesday == true)
                        // {
                        //     scheduledDaysList.Add("Tuesday");
                        // }
                        // if (SalonScheduleDays.Wednesday == true)
                        // {
                        //     scheduledDaysList.Add("Wednesday");
                        // }
                        // if (SalonScheduleDays.Thursday == true)
                        // {
                        //     scheduledDaysList.Add("Thursday");
                        // }
                        // if (SalonScheduleDays.Friday == true)
                        // {
                        //     scheduledDaysList.Add("Friday");
                        // }
                        // if (SalonScheduleDays.Saturday == true)
                        // {
                        //     scheduledDaysList.Add("Saturday");
                        // }
                        // if (SalonScheduleDays.Sunday == true)
                        // {
                        //     scheduledDaysList.Add("Sunday");
                        // }

                        // // update timeslots according to schedule
                        // var services = await _context.SalonService.Where(u => u.SalonId == Salon.SalonId).ToListAsync();
                        // foreach (var item in services)
                        // {
                        //     var deleteTimeSlot = await _context.TimeSlot.Where(u => u.ServiceId == item.ServiceId).ToListAsync();

                        //     foreach (var item3 in deleteTimeSlot)
                        //     {
                        //         item3.Status = false;
                        //     }
                        //     _context.UpdateRange(deleteTimeSlot);
                        //     await _context.SaveChangesAsync();

                        //     int addDay = 0;
                        //     for (int i = 0; i < 7; i++)
                        //     {
                        //         DateTime currentDate = DateTime.Now.AddDays(i);
                        //         string currentDateStr = currentDate.ToString("yyyy-MM-dd");
                        //         string dayName = currentDate.ToString("dddd");

                        //         var existingTimeSlot = _context.TimeSlot
                        //             .Where(u => u.ServiceId == item.ServiceId && u.SlotDate.Date == currentDate.Date)
                        //             .ToList();

                        //         if (!scheduledDaysList.Contains(dayName))
                        //         {
                        //             foreach (var existingSlot in existingTimeSlot)
                        //             {
                        //                 existingSlot.Status = false;
                        //             }

                        //             _context.UpdateRange(existingTimeSlot);
                        //             await _context.SaveChangesAsync();
                        //             continue;
                        //         }

                        //         startDateTime = DateTime.Parse(currentDateStr + " " + startTime);
                        //         endDateTime = DateTime.Parse(currentDateStr + " " + endTime);
                        //         int minutes = item.DurationInMinutes;
                        //         startDateTime = startDateTime.AddMinutes(-minutes);
                        //         endDateTime = endDateTime.AddMinutes(-minutes);

                        //         TimeSpan timeInterval = endDateTime - startDateTime;
                        //         int totalMinutes = (int)timeInterval.TotalMinutes;
                        //         int noOfTimeSlot = totalMinutes / minutes;

                        //         timeList = new List<TimeList>();
                        //         for (int j = 0; j < noOfTimeSlot; j++)
                        //         {
                        //             TimeList obj1 = new TimeList();
                        //             startDateTime = startDateTime.AddMinutes(minutes);
                        //             obj1.time = startDateTime.ToString("hh:mm tt");
                        //             timeList.Add(obj1);
                        //         }

                        //         foreach (var item2 in timeList)
                        //         {
                        //             var timeslot = new TimeSlot
                        //             {
                        //                 ServiceId = item.ServiceId,
                        //                 FromTime = item2.time,
                        //                 ToTime = DateTime.Parse(item2.time).AddMinutes(minutes).ToString("hh:mm tt"),
                        //                 SlotDate = Convert.ToDateTime(currentDate.ToString(@"yyyy-MM-dd")),
                        //                 SlotCount = item.TotalCountPerDuration,
                        //                 Status = true
                        //             };

                        //             bool pass = true;
                        //             var existingTimeSlotDetails = existingTimeSlot.FirstOrDefault(u => u.FromTime == timeslot.FromTime);

                        //             if (!string.IsNullOrEmpty(item.LockTimeStart))
                        //             {
                        //                 string[] splitLockTimeStart = item.LockTimeStart.Split(",");
                        //                 string[] splitLockTimeEnd = item.LockTimeEnd.Split(",");
                        //                 List<DateTime> lockTimeStart = splitLockTimeStart.Select(DateTime.Parse).ToList();
                        //                 List<DateTime> lockTimeEnd = splitLockTimeEnd.Select(DateTime.Parse).ToList();
                        //                 var fromTime = DateTime.Parse(currentDateStr + " " + timeslot.FromTime);
                        //                 var toTime = DateTime.Parse(currentDateStr + " " + timeslot.ToTime);

                        //                 for (int m = 0; m < lockTimeStart.Count; m++)
                        //                 {
                        //                     var chkLockedFrom = DateTime.Parse(currentDateStr + " " + lockTimeStart[m].ToString(@"hh:mm tt"));
                        //                     var chkLockedTo = DateTime.Parse(currentDateStr + " " + lockTimeEnd[m].ToString(@"hh:mm tt"));

                        //                     if ((fromTime <= chkLockedFrom && toTime <= chkLockedFrom) || (fromTime >= chkLockedTo && toTime >= chkLockedTo))
                        //                     {
                        //                         if (existingTimeSlotDetails == null)
                        //                         {
                        //                             await _context.AddAsync(timeslot);
                        //                             await _context.SaveChangesAsync();
                        //                         }
                        //                         else
                        //                         {
                        //                             existingTimeSlotDetails.Status = true;
                        //                             _context.Update(existingTimeSlotDetails);
                        //                             await _context.SaveChangesAsync();
                        //                         }
                        //                     }
                        //                 }
                        //             }
                        //             else
                        //             {
                        //                 if (existingTimeSlotDetails == null)
                        //                 {
                        //                     await _context.AddAsync(timeslot);
                        //                     await _context.SaveChangesAsync();
                        //                 }
                        //                 else
                        //                 {
                        //                     existingTimeSlotDetails.Status = true;
                        //                     _context.Update(existingTimeSlotDetails);
                        //                     await _context.SaveChangesAsync();
                        //                 }
                        //             }
                        //         }
                        //         addDay++;
                        //     }
                        // }
                        _backgroundService.StartService();

                        _response.StatusCode = HttpStatusCode.OK;
                        _response.IsSuccess = true;
                        _response.Messages = "Scheduled updated successfully.";
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
                SalonScheduleDays.UpdateStatus = false;

                _context.SalonSchedule.Add(SalonSchedule);
                _context.SaveChanges();

                _backgroundService.StartService();

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
                _response.Messages = "Secheduled detail saved successfully.";
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
                _response.Messages = "User does not exists.";
                return Ok(_response);
            }
            var roles = await _userManager.GetRolesAsync(currentUserDetail);

            int dairyMainCategoryId = Convert.ToInt32(CategoryName.DairyandEggs);

            IQueryable<SalonServiceListDTO> query = _context.SalonService.Select(service => new SalonServiceListDTO { });

            if (roles[0].ToString() == "Customer")
            {
                if (string.IsNullOrEmpty(model.serviceType))
                {
                    query = from t1 in _context.SalonService
                            join t2 in _context.MainCategory on t1.MainCategoryId equals t2.MainCategoryId

                            where t1.IsDeleted != true
                            where t1.Status == 1
                            // where t6.CustomerUserId == currentUserId
                            orderby t1.MainCategoryId descending
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
                                isSlotAvailable = _context.TimeSlot.Where(a => a.ServiceId == t1.ServiceId && a.Status && a.SlotCount > 0 && !a.IsDeleted)
                                                            .Select(u => u.SlotDate).Distinct().Count(),
                                serviceCountInCart = _context.Cart.Where(a => a.ServiceId == t1.ServiceId && a.CustomerUserId == currentUserId).Sum(a => a.ServiceCountInCart),
                                // Additional properties from other tables
                            };
                    if (model.mainCategoryId != 53 || model.mainCategoryId == null)
                    {
                        var query1 = from t1 in _context.SalonService
                                     join t2 in _context.MainCategory on t1.MainCategoryId equals t2.MainCategoryId

                                     where t1.IsDeleted != true
                                     where t1.Status == 1
                                     where t1.ServiceType == "Package"
                                     // where t6.CustomerUserId == currentUserId
                                     orderby t1.MainCategoryId descending
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
                                         totalCountPerDuration = t1.TotalCountPerDuration,
                                         ServiceType = t1.ServiceType,
                                         isSlotAvailable = _context.TimeSlot.Where(a => a.ServiceId == t1.ServiceId && a.Status && a.SlotCount > 0 && !a.IsDeleted)
                                                                     .Select(u => u.SlotDate).Distinct().Count(),
                                         serviceCountInCart = _context.Cart.Where(a => a.ServiceId == t1.ServiceId && a.CustomerUserId == currentUserId).Sum(a => a.ServiceCountInCart),
                                         // Additional properties from other tables
                                     };

                        _mapper.Map(query1, query);
                    }

                }

                // query = from t1 in _context.SalonService
                //         join t2 in _context.MainCategory on t1.MainCategoryId equals t2.MainCategoryId
                //         where !t1.IsDeleted && t1.Status == 1
                //         orderby t1.ServiceId descending
                //         let salonDetails = _context.SalonDetail.FirstOrDefault(u => u.SalonId == t1.SalonId)
                //         let subCategory = _context.SubCategory.FirstOrDefault(u => u.SubCategoryId == t1.SubcategoryId)
                //         let slotCount = _context.TimeSlot
                //             .Where(a => a.ServiceId == t1.ServiceId && a.Status && a.SlotCount > 0 && !a.IsDeleted)
                //             .Select(u => u.SlotDate)
                //             .Distinct()
                //             .Count()
                //         select new SalonServiceListDTO
                //         {
                //             ServiceName = t1.ServiceName,
                //             ServiceId = t1.ServiceId,
                //             VendorId = salonDetails != null ? salonDetails.VendorId : "",
                //             SalonId = t1.SalonId,
                //             SalonName = salonDetails != null ? salonDetails.SalonName : string.Empty,
                //             MainCategoryId = t1.MainCategoryId,
                //             MainCategoryName = t2.CategoryName,
                //             SubCategoryId = t1.SubcategoryId ?? 0,
                //             SubCategoryName = subCategory != null ? subCategory.CategoryName : string.Empty,
                //             ServiceDescription = t1.ServiceDescription,
                //             ServiceImage1 = t1.ServiceImage1,
                //             ListingPrice = t1.ListingPrice,
                //             BasePrice = (double)t1.BasePrice,
                //             Discount = t1.Discount,
                //             TotalCountPerDuration = t1.TotalCountPerDuration,
                //             IsSlotAvailable = slotCount
                //             // Additional properties from other tables
                //         };

            }
            else
            {
                query = from t1 in _context.SalonService
                        join t2 in _context.MainCategory on t1.MainCategoryId equals t2.MainCategoryId
                        // join t3 in _context.MainProductCategory on t1.MainProductCategoryId equals t3.MainProductCategoryId
                        // join t5 in _context.Brand on t1.BrandId equals t5.BrandId
                        // join t6 in _context.Cart on t1.ProductId equals t6.ProductId

                        where t1.IsDeleted != true
                        where t1.Status == 1
                        where t1.ServiceType == model.serviceType
                        // where t6.CustomerUserId == currentUserId
                        orderby t1.ServiceId

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
                            genderPreferences = t1.GenderPreferences,
                            ServiceType = t1.ServiceType,
                            ageRestrictions = t1.AgeRestrictions
                            // Additional properties from other tables
                        };

                // query = from t1 in _context.SalonService
                //         join t2 in _context.MainCategory on t1.MainCategoryId equals t2.MainCategoryId
                //         join sd in _context.SalonDetail on t1.SalonId equals sd.SalonId into salonDetailsGroup
                //         from salonDetail in salonDetailsGroup.DefaultIfEmpty()
                //         join sc in _context.SubCategory on t1.SubcategoryId equals sc.SubCategoryId into subCategoryGroup
                //         from subCategory in subCategoryGroup.DefaultIfEmpty()
                //         where !t1.IsDeleted && t1.Status == 0
                //         orderby t1.ServiceId
                //         select new SalonServiceListDTO
                //         {
                //             ServiceName = t1.ServiceName,
                //             ServiceId = t1.ServiceId,
                //             VendorId = salonDetail != null ? salonDetail.VendorId : "",
                //             SalonId = t1.SalonId,
                //             SalonName = salonDetail != null ? salonDetail.SalonName : string.Empty,
                //             MainCategoryId = t1.MainCategoryId,
                //             MainCategoryName = t2.CategoryName,
                //             SubCategoryId = t1.SubcategoryId ?? 0,
                //             SubCategoryName = subCategory != null ? subCategory.CategoryName : string.Empty,
                //             ServiceDescription = t1.ServiceDescription,
                //             ServiceImage1 = t1.ServiceImage1,
                //             ListingPrice = t1.ListingPrice,
                //             BasePrice = (double)t1.BasePrice,
                //             Discount = t1.Discount,
                //             TotalCountPerDuration = t1.TotalCountPerDuration,
                //             // Additional properties from other tables
                //         };

            }

            // var products = (await _productRepository.GetAllAsync(u => u.IsDeleted != true)).OrderByDescending(u => u.Modifydate).ToList();

            if (model.mainCategoryId > 0)
            {
                query = query.Where(u => u.mainCategoryId == model.mainCategoryId);
            }
            if (model.subCategoryId > 0)
            {
                query = query.Where(u => u.subCategoryId == model.subCategoryId);
            }
            if (model.salonId > 0)
            {
                var salon = await _context.SalonDetail.FirstOrDefaultAsync(u => u.SalonId == model.salonId);
                if (salon == null)
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = "Not found any record.";
                    return Ok(_response);
                }
                if (salon.Status == null)
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = "Not found any record.";
                    return Ok(_response);
                }
                query = query.Where(u => u.salonId == model.salonId);
            }

            List<int?> customerSalonIds = new List<int?>();
            if (roles[0].ToString() == "Customer")
            {
                customerSalonIds = await _context.CustomerSalon.Where(u => u.CustomerUserId == currentUserId).Select(a => a.SalonId).ToListAsync();
                if (model.salonId < 1)
                {
                    query = query.Where(number => customerSalonIds.Contains(number.salonId));
                }
            }

            // if (products.Count < 1)
            // {
            //     _response.StatusCode = HttpStatusCode.OK;
            //     _response.IsSuccess = false;
            //     _response.Messages = "Not found any record.";
            //     return Ok(_response);
            // }
            // var products = query.ToList();
            var SalonServiceList = query.OrderByDescending(u => u.ServiceType).ToList();

            if (!string.IsNullOrEmpty(model.searchQuery))
            {
                SalonServiceList = SalonServiceList.Where(x => (x.serviceName?.IndexOf(model.searchQuery, StringComparison.OrdinalIgnoreCase) >= 0)
                || (x.salonName?.IndexOf(model.searchQuery, StringComparison.OrdinalIgnoreCase) >= 0)
                || (x.mainCategoryName?.IndexOf(model.searchQuery, StringComparison.OrdinalIgnoreCase) >= 0)
                || (x.subCategoryName?.IndexOf(model.searchQuery, StringComparison.OrdinalIgnoreCase) >= 0)
                ).ToList();
            }
            if (!string.IsNullOrEmpty(model.genderPreferences))
            {
                SalonServiceList = SalonServiceList.Where(x => (x.genderPreferences == model.genderPreferences)).ToList();
            }
            if (!string.IsNullOrEmpty(model.ageRestrictions))
            {
                SalonServiceList = SalonServiceList.Where(x => (x.ageRestrictions == model.ageRestrictions)).ToList();
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
            _response.Messages = "List shown successfully.";
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
                _response.Messages = "User does not exists.";
                return Ok(_response);
            }
            serviceType = string.IsNullOrEmpty(serviceType) ? "Single" : serviceType;

            if (serviceType != "Single" && serviceType != "Package")
            {
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = false;
                _response.Messages = "Please enter valid servivce type.";
                return Ok(_response);
            }

            // var serviceDetail1 = await _context.SalonService.ToListAsync();
            // foreach (var item in serviceDetail1)
            // {
            //     item.ServiceIconImage = item.ServiceImage1;
            //     _context.Update(item);
            //     _context.SaveChanges();
            // }

            var serviceDetail = await _context.SalonService.FirstOrDefaultAsync(u => u.ServiceId == serviceId);
            if (serviceDetail == null)
            {
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = false;
                _response.Messages = "Not found any record.";
                return Ok(_response);
            }

            var serviceResponse = _mapper.Map<serviceDetailDTO>(serviceDetail);

            if (serviceType == "Package")
            {
                var includeService = await _context.ServicePackage.Where(u => u.ServiceId == serviceResponse.serviceId).FirstOrDefaultAsync();
                if (includeService != null)
                {
                    var splittedService = includeService.IncludeServiceId.Split(",");
                    var packageServices = new List<IncludeServiceDTO>();
                    foreach (var item in splittedService)
                    {
                        var packageService = new IncludeServiceDTO();
                        var includeServiceDetail = await _context.SalonService.Where(u => u.ServiceId == Convert.ToInt32(item)).FirstOrDefaultAsync();
                        if (includeServiceDetail != null)
                        {
                            packageServices.Add(_mapper.Map(includeServiceDetail, packageService));
                        }
                    }
                    serviceResponse.IncludeServic = packageServices;
                }

            }

            var serivceImageList = new List<ServiceImageDTO>();

            if (!string.IsNullOrEmpty(serviceDetail.ServiceImage1))
            {
                var serviceImageDTO = new ServiceImageDTO();
                serviceImageDTO.salonServiceImage = serviceDetail.ServiceImage1;
                serivceImageList.Add(serviceImageDTO);
            }
            if (!string.IsNullOrEmpty(serviceDetail.ServiceImage2))
            {
                var serviceImageDTO = new ServiceImageDTO();
                serviceImageDTO.salonServiceImage = serviceDetail.ServiceImage2;
                serivceImageList.Add(serviceImageDTO);
            }
            if (!string.IsNullOrEmpty(serviceDetail.ServiceImage3))
            {
                var serviceImageDTO = new ServiceImageDTO();
                serviceImageDTO.salonServiceImage = serviceDetail.ServiceImage3;
                serivceImageList.Add(serviceImageDTO);
            }
            if (!string.IsNullOrEmpty(serviceDetail.ServiceImage4))
            {
                var serviceImageDTO = new ServiceImageDTO();
                serviceImageDTO.salonServiceImage = serviceDetail.ServiceImage4;
                serivceImageList.Add(serviceImageDTO);
            }
            if (!string.IsNullOrEmpty(serviceDetail.ServiceImage5))
            {
                var serviceImageDTO = new ServiceImageDTO();
                serviceImageDTO.salonServiceImage = serviceDetail.ServiceImage5;
                serivceImageList.Add(serviceImageDTO);
            }

            var roles = await _userManager.GetRolesAsync(currentUserDetail);
            if (roles[0].ToString() == "Customer")
            {
                // var getCartItems = await _cartRepository.GetAsync(u => (u.CustomerUserId == currentUserId) && (u.ProductId == productDetail.ProductId && u.IsDairyProduct != true && u.IsSubscriptionProduct != true));
                // if (getCartItems != null)
                // {
                //     productResponse.ProductCountInCart = getCartItems.ProductCountInCart;
                // }

                // var favoritesStatus = await _context.FavouriteService.Where(u => u.ServiceId == serviceId && u.CustomerUserId == currentUserId).FirstOrDefaultAsync();
                // serviceResponse.favouriteStatus = favoritesStatus != null ? true : false;
            }

            var salonDetail = await _context.SalonDetail.Where(u => u.SalonId == serviceResponse.salonId).FirstOrDefaultAsync();
            var vendorDetail = _userManager.FindByIdAsync(salonDetail.VendorId).GetAwaiter().GetResult();
            serviceResponse.vendorName = vendorDetail.FirstName + " " + vendorDetail.LastName;
            serviceResponse.salonName = salonDetail.SalonName;
            serviceResponse.vendorId = salonDetail.VendorId;
            serviceResponse.serviceImage = serivceImageList;
            serviceResponse.isSlotAvailable = _context.TimeSlot.Where(a => a.ServiceId == serviceId && a.Status && a.SlotCount > 0 && !a.IsDeleted)
                                                        .Select(u => u.SlotDate).Distinct().Count();
            serviceResponse.LockTimeStart = !string.IsNullOrEmpty(serviceResponse.LockTimeStart) ? Convert.ToDateTime(serviceResponse.LockTimeStart).ToString(@"HH:mm") : null;
            serviceResponse.LockTimeEnd = !string.IsNullOrEmpty(serviceResponse.LockTimeEnd) ? Convert.ToDateTime(serviceResponse.LockTimeEnd).ToString(@"HH:mm") : null;
            // if (serviceResponse.BrandId > 0)
            // {
            //     var brandDetail = await _brandRepository.GetAsync(u => u.BrandId == productResponse.BrandId);
            //     productResponse.BrandName = brandDetail != null ? brandDetail.BrandName : null;
            // }
            if (serviceResponse.mainCategoryId > 0)
            {
                var categoryDetail = await _context.MainCategory.FirstOrDefaultAsync(u => u.MainCategoryId == serviceResponse.mainCategoryId);
                serviceResponse.mainCategoryName = categoryDetail != null ? categoryDetail.CategoryName : null;
            }
            if (serviceResponse.subCategoryId > 0)
            {
                var categoryDetail = await _context.SubCategory.FirstOrDefaultAsync(u => u.SubCategoryId == serviceResponse.subCategoryId);
                serviceResponse.subCategoryName = categoryDetail != null ? categoryDetail.CategoryName : null;
            }

            _response.StatusCode = HttpStatusCode.OK;
            _response.IsSuccess = true;
            _response.Data = serviceResponse;
            _response.Messages = "Service detail shown successfully.";
            return Ok(_response);
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
                    _response.Messages = "Not found any record.";
                    return Ok(_response);
                }

                var scheduleDayViewModel = new ScheduleDayResonceDTO();
                scheduleDayViewModel.monday = SalonSchedule.Monday;
                scheduleDayViewModel.tuesday = SalonSchedule.Tuesday;
                scheduleDayViewModel.wednesday = SalonSchedule.Wednesday;
                scheduleDayViewModel.thursday = SalonSchedule.Thursday;
                scheduleDayViewModel.friday = SalonSchedule.Friday;
                scheduleDayViewModel.saturday = SalonSchedule.Saturday;
                scheduleDayViewModel.sunday = SalonSchedule.Sunday;
                scheduleDayViewModel.fromTime = Convert.ToDateTime(SalonSchedule.FromTime).ToString(@"HH:mm");
                scheduleDayViewModel.toTime = Convert.ToDateTime(SalonSchedule.ToTime).ToString(@"HH:mm");
                // scheduleDayViewModel.fromTime = SalonSchedule.FromTime;
                // scheduleDayViewModel.toTime = SalonSchedule.ToTime;
                scheduleDayViewModel.salonId = SalonSchedule.SalonId;
                scheduleDayViewModel.updateStatus = SalonSchedule.UpdateStatus;

                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Messages = "Detail shown successfully.";
                _response.Data = scheduleDayViewModel;
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

                if (model.ageRestrictions != "Kids" && model.ageRestrictions != "Adult")
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = "Please enter valid age limit.";
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
                string[] splitIncludeProduct = model.IncludeServiceId.Split(",");

                foreach (var item in splitIncludeProduct)
                {
                    var ckeckService = await _context.SalonService.Where(u => u.ServiceId == Convert.ToInt32(item)).FirstOrDefaultAsync();
                    if (splitLockTimeStart.Length != splitLockTimeend.Length)
                    {
                        _response.StatusCode = HttpStatusCode.OK;
                        _response.IsSuccess = false;
                        _response.Messages = "Not found selected service for package.";
                        return Ok(_response);
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
                        _response.Messages = "Category is not found.";
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
                        _response.Messages = "Category is not found.";
                        return Ok(_response);
                    }
                    addUpdateServiceEntity.MainCategoryId = isCategoryExist.MainCategoryId;
                }
                if (model.serviceId == 0)
                {
                    await _context.AddAsync(addUpdateServiceEntity);
                    await _context.SaveChangesAsync();

                    var servicePackage = new ServicePackage();
                    servicePackage.ServiceId = addUpdateServiceEntity.ServiceId;
                    servicePackage.IncludeServiceId = model.IncludeServiceId;
                    servicePackage.SalonId = model.salonId;

                    await _context.AddAsync(servicePackage);
                    await _context.SaveChangesAsync();

                    response = _mapper.Map<GetSalonServiceDTO>(addUpdateServiceEntity);

                    message = "Service" + ResponseMessages.msgAdditionSuccess;
                }
                else
                {
                    var serviceDetail = await _context.SalonService.Where(u => u.ServiceId == model.serviceId).FirstOrDefaultAsync();
                    if (serviceDetail == null)
                    {
                        _response.StatusCode = HttpStatusCode.OK;
                        _response.IsSuccess = false;
                        _response.Messages = "Service" + ResponseMessages.msgNotFound;
                        return Ok(_response);
                    }
                    _mapper.Map(model, serviceDetail);
                    _context.Update(serviceDetail);
                    await _context.SaveChangesAsync();

                    var servicePackage = await _context.ServicePackage.FirstOrDefaultAsync(u => u.ServiceId == serviceDetail.ServiceId);
                    if (servicePackage != null)
                    {
                        servicePackage.IncludeServiceId = model.IncludeServiceId;

                        _context.Update(servicePackage);
                        await _context.SaveChangesAsync();
                    }

                    response = _mapper.Map<GetSalonServiceDTO>(serviceDetail);
                    message = "Service" + ResponseMessages.msgUpdationSuccess;
                }

                var deleteTimeSlot = await _context.TimeSlot.Where(u => u.ServiceId == response.serviceId).ToListAsync();

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

                // get scheduled days
                var slotDetail = await _context.TimeSlot
                                    .Where(a => a.ServiceId == serviceId && a.Status != false && a.SlotCount > 0 && a.IsDeleted != true)
                                    .Select(u => u.SlotDate)
                                    .Distinct()
                                    .ToListAsync();

                var availableDates = new List<string>();

                foreach (var item in slotDetail)
                {
                    if (item.Date == DateTime.Now.Date)
                    {
                        // get scheduled days
                        var slotDetail1 = await _context.TimeSlot
                                            .Where(a => a.ServiceId == serviceId && a.Status != false && a.SlotCount > 0 && a.IsDeleted != true && a.SlotDate == DateTime.Now.Date)
                                            .ToListAsync();

                        // Get the current time and add 2 hours to it
                        var limitDate = DateTime.Now.AddHours(2);
                        var availableSlots = new List<timeSlotsDTO>();
                        foreach (var item1 in slotDetail1)
                        {
                            // Assuming item.FromTime is a string representation of a time in "HH:mm" format
                            var fromTime = (Convert.ToDateTime(item1.FromTime).TimeOfDay);

                            // Get the current time as a TimeSpan
                            var currentTime = DateTime.Now.TimeOfDay;

                            // Calculate the time difference between limitDate and fromTime
                            var timeDifference = (fromTime - currentTime).Duration();

                            // Check if the time difference is less than or equal to a certain number of minutes
                            int minutesThreshold = 60; // Set your threshold here
                            if (timeDifference.TotalMinutes! <= minutesThreshold)
                            {
                                availableSlots.Add(_mapper.Map<timeSlotsDTO>(item));
                            }
                        }
                        if (availableSlots.Count > 0)
                        {
                            availableDates.Add(item.ToString(@"dd-MM-yyyy"));
                        }
                    }
                    else
                        availableDates.Add(item.ToString(@"dd-MM-yyyy"));
                }

                if (slotDetail != null)
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = true;
                    _response.Messages = "Dates shown successfully.";
                    _response.Data = availableDates;
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

                // queryDate = Convert.ToDateTime(queryDate.ToString(@"yyyy-MM-dd"));
                string format = "dd-MM-yyyy";
                DateTime searchDate = new DateTime();

                try
                {
                    // Parse the string into a DateTime object using the specified format
                    searchDate = DateTime.ParseExact(queryDate, format, null);
                }
                catch (FormatException)
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = "Invalid date format.";
                    return Ok(_response);
                }

                // get scheduled days
                var slotDetail = await _context.TimeSlot
                                    .Where(a => a.ServiceId == serviceId && a.Status != false && a.SlotCount > 0 && a.IsDeleted != true && a.SlotDate == searchDate)
                                    .ToListAsync();

                // Get the current time and add 2 hours to it
                var limitDate = DateTime.Now.AddHours(2);
                var availableSlots = new List<timeSlotsDTO>();
                foreach (var item in slotDetail)
                {
                    if (searchDate.Date == DateTime.Now.Date)
                    {
                        // Assuming item.FromTime is a string representation of a time in "HH:mm" format
                        var fromTime = (Convert.ToDateTime(item.FromTime).TimeOfDay);

                        // Get the current time as a TimeSpan
                        var currentTime = DateTime.Now.TimeOfDay;

                        // Calculate the time difference between limitDate and fromTime
                        var timeDifference = (fromTime - currentTime).Duration();

                        // Check if the time difference is less than or equal to a certain number of minutes
                        int minutesThreshold = 60; // Set your threshold here
                        if (timeDifference.TotalMinutes! <= minutesThreshold)
                        {
                            availableSlots.Add(_mapper.Map<timeSlotsDTO>(item));
                        }
                    }
                    else
                        availableSlots.Add(_mapper.Map<timeSlotsDTO>(item));
                }

                if (slotDetail != null)
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = true;
                    _response.Messages = "Slots shown successfully.";
                    _response.Data = availableSlots;
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
                    _response.Messages = "Not found any Service.";
                    return Ok(_response);
                }

                var salonServiceFavouriteStatus = await _context.FavouriteService.Where(u => u.ServiceId == model.serviceId && u.CustomerUserId == currentUserId).FirstOrDefaultAsync();
                string msg = "";
                if (model.status == true)
                {
                    if (salonServiceFavouriteStatus != null)
                    {
                        _response.StatusCode = HttpStatusCode.OK;
                        _response.IsSuccess = false;
                        _response.Data = new Object { };
                        _response.Messages = "Already added to favorites.";
                        return Ok(_response);
                    }
                    var addFavouriteService = new FavouriteService();
                    addFavouriteService.CustomerUserId = currentUserId;
                    addFavouriteService.ServiceId = model.serviceId;
                    _context.Add(addFavouriteService);
                    _context.SaveChanges();
                    msg = "Added to favorites.";

                }
                else
                {
                    if (salonServiceFavouriteStatus == null)
                    {
                        _response.StatusCode = HttpStatusCode.OK;
                        _response.IsSuccess = false;
                        _response.Data = new Object { };
                        _response.Messages = "Not found any record.";
                        return Ok(_response);
                    }
                    _context.Remove(salonServiceFavouriteStatus);
                    _context.SaveChanges();
                    msg = "Removed from favorites.";
                }

                var getService = await _context.SalonService.FirstOrDefaultAsync(u => u.ServiceId == model.serviceId);
                var response = _mapper.Map<serviceDetailDTO>(getService);
                var favouriteStatus = await _context.FavouriteService.FirstOrDefaultAsync(u => u.ServiceId == model.serviceId && u.CustomerUserId == currentUserId);
                response.favouriteStatus = favouriteStatus != null ? true : false;

                if (getService != null)
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = true;
                    _response.Data = response;
                    _response.Messages = msg;
                    return Ok(_response);
                }
                else
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Data = new Object { };
                    _response.Messages = ResponseMessages.msgSomethingWentWrong;
                    return Ok(_response);
                }
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
        ///  Get Servive Image In Base64.
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
                _response.Messages = "User does not exists.";
                return Ok(_response);
            }

            List<string> serviceImageList = new List<string>();

            var serviceDetail = await _context.SalonService.Where(u => u.ServiceId == serviceId).FirstOrDefaultAsync();
            if (serviceDetail == null)
            {
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = false;
                _response.Messages = "Not found any record.";
                return Ok(_response);
            }

            if (!string.IsNullOrEmpty(serviceDetail.ServiceImage1))
            {
                var httpClient = new HttpClient();
                // string imageUrl = imgURL + productDetail.ProductImage1;
                string imageUrl = imgURL + serviceDetail.ServiceImage1;
                byte[]? imageBytes;
                try
                {
                    imageBytes = await httpClient.GetByteArrayAsync(imageUrl);
                    if (imageBytes != null)
                    {
                        var base64String = Convert.ToBase64String(imageBytes);
                        var image = imgData + base64String;

                        serviceImageList.Add(image);
                    }
                }
                catch
                {
                }
            }
            if (!string.IsNullOrEmpty(serviceDetail.ServiceImage2))
            {
                var httpClient = new HttpClient();

                string imageUrl = imgURL + serviceDetail.ServiceImage2;
                byte[]? imageBytes;
                try
                {
                    imageBytes = await httpClient.GetByteArrayAsync(imageUrl);
                    if (imageBytes != null)
                    {
                        var base64String = Convert.ToBase64String(imageBytes);
                        var image = imgData + base64String;

                        serviceImageList.Add(image);
                    }
                }
                catch
                {
                }
            }
            if (!string.IsNullOrEmpty(serviceDetail.ServiceImage3))
            {
                var httpClient = new HttpClient();
                // string imageUrl = imgURL + productDetail.ProductImage3;
                string imageUrl = imgURL + serviceDetail.ServiceImage3;
                byte[]? imageBytes;
                try
                {
                    imageBytes = await httpClient.GetByteArrayAsync(imageUrl);
                    if (imageBytes != null)
                    {
                        var base64String = Convert.ToBase64String(imageBytes);
                        var image = imgData + base64String;

                        serviceImageList.Add(image);
                    }
                }
                catch
                {
                }
            }
            if (!string.IsNullOrEmpty(serviceDetail.ServiceImage4))
            {
                var httpClient = new HttpClient();
                // string imageUrl = imgURL + productDetail.ProductImage4;
                string imageUrl = imgURL + serviceDetail.ServiceImage4;
                byte[]? imageBytes;
                try
                {
                    imageBytes = await httpClient.GetByteArrayAsync(imageUrl);
                    if (imageBytes != null)
                    {
                        var base64String = Convert.ToBase64String(imageBytes);
                        var image = imgData + base64String;

                        serviceImageList.Add(image);
                    }
                }
                catch
                {
                }
            }
            if (!string.IsNullOrEmpty(serviceDetail.ServiceImage5))
            {
                var httpClient = new HttpClient();
                // string imageUrl = imgURL + productDetail.ProductImage5;
                string imageUrl = imgURL + serviceDetail.ServiceImage5;
                byte[]? imageBytes;
                try
                {
                    imageBytes = await httpClient.GetByteArrayAsync(imageUrl);
                    if (imageBytes != null)
                    {
                        var base64String = Convert.ToBase64String(imageBytes);
                        var image = imgData + base64String;

                        serviceImageList.Add(image);
                    }
                }
                catch
                {
                }
            }

            _response.StatusCode = HttpStatusCode.OK;
            _response.IsSuccess = true;
            _response.Data = serviceImageList;
            _response.Messages = "Service image list shown successfully.";
            return Ok(_response);
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
                    _response.Messages = "Not found any record.";
                    return Ok(_response);
                }

                var timeSlots = await _context.TimeSlot.Where(x => (x.ServiceId == serviceId)).ToListAsync();
                foreach (var item in timeSlots)
                {
                    item.IsDeleted = true;
                    item.Status = false;
                }
                _context.UpdateRange(timeSlots);
                await _context.SaveChangesAsync();

                var favouriteServices = await _context.FavouriteService.Where(x => (x.ServiceId == serviceId)).ToListAsync();
                _context.RemoveRange(favouriteServices);
                await _context.SaveChangesAsync();

                var cartServices = await _context.Cart.Where(x => (x.ServiceId == serviceId)).ToListAsync();
                _context.RemoveRange(cartServices);
                await _context.SaveChangesAsync();

                salonService.IsDeleted = true;

                _context.Update(salonService);
                await _context.SaveChangesAsync();

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
