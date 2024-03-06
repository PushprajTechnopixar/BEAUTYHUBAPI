

using AutoMapper;
using BeautyHubAPI.Data;
using BeautyHubAPI.Models.Helper;
using BeautyHubAPI.Models;
using BeautyHubAPI.Repository.IRepository;
using Microsoft.AspNetCore.Identity;
using BeautyHubAPI.Common;
using BeautyHubAPI.Models.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static BeautyHubAPI.Common.GlobalVariables;
using System.Net;
using Microsoft.AspNetCore.Authorization;
using TimeZoneConverter;
using System.Globalization;

namespace BeautyHubAPI.Repository
{
    public class ServiceRepository : IServiceRepository
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
        private readonly ApplointmentListBackgroundService _applointmentListBackgroundService;

        public ServiceRepository(IMapper mapper,
        IUploadRepository uploadRepository,
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
            _response = new();
            _context = context;
            _userManager = userManager;
            _membershipRecordRepository = membershipRecordRepository;
            httpClient = new HttpClient();
            _backgroundService = backgroundService;
            _applointmentListBackgroundService = applointmentListBackgroundService;
        }


        public async Task<Object> addUpdateSalonSchedule(ScheduleDayDTO model)
        {
            try
            {
                var Salon = await _context.SalonDetail.Where(a => a.SalonId == model.salonId).FirstOrDefaultAsync();

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
                        return _response;
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
                            return _response;
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
                                        return _response;
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
                                        return _response;
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
                        return _response;
                    }

                    if (update == 0)
                    {
                        _response.StatusCode = HttpStatusCode.InternalServerError;
                        _response.IsSuccess = false;
                        _response.Data = new { };
                        _response.Messages = "Can't update, while your scheduled timing is booked.";
                        return _response;
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
                return _response;
            }
            catch (Exception ex)
            {
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.IsSuccess = false;
                _response.Data = new { };
                _response.Messages = ResponseMessages.msgSomethingWentWrong + ex.Message;
                return _response;
            }
        }
        public async Task<Object> AddUpdateSalonService(AddUpdateSalonServiceDTO model)
        {
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
                        return _response;
                    }
                }
            }
            if (splitLockTimeStart.Length != splitLockTimeend.Length)
            {
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = false;
                _response.Messages = "Locked start and end time should be same.";
                return _response;
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
            var serviceDetail = await _context.SalonService.Where(u => u.ServiceId == model.serviceId).FirstOrDefaultAsync();
            if (model.serviceId > 0)
            {
                if (serviceDetail == null)
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = ResponseMessages.msgNotFound + "Service";
                    return _response;
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
                                return _response;
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
                                return _response;
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
                    return _response;
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
                    return _response;
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
            return _response;

        }
        public async Task<Object> customerServiceList(SalonServiceFilterationListDTO model, string currentUserId)
        {
            var currentUserDetail = await _userManager.FindByIdAsync(currentUserId);

            var roles = await _userManager.GetRolesAsync(currentUserDetail);

            IQueryable<SalonServiceListDTO> query = _context.SalonService.Select(service => new SalonServiceListDTO { });

            if (string.IsNullOrEmpty(model.serviceType))
            {
                query = from t1 in _context.SalonService
                        join t2 in _context.MainCategory on t1.MainCategoryId equals t2.MainCategoryId

                        where t1.IsDeleted != true
                        where t1.Status == 1
                        where t1.ServiceType == (model.mainCategoryId != 53 ? "Single" : "Package")
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
                            durationInMinutes = t1.DurationInMinutes,
                            status = t1.Status,
                            isSlotAvailable = _context.TimeSlot.Where(a => a.ServiceId == t1.ServiceId && a.Status && a.SlotCount > 0 && !a.IsDeleted)
                                                        .Select(u => u.SlotDate).Distinct().Count(),
                            serviceCountInCart = _context.Cart.Where(a => a.ServiceId == t1.ServiceId && a.CustomerUserId == currentUserId).Sum(a => a.ServiceCountInCart),
                            // Additional properties from other tables
                        };
                if (model.mainCategoryId != 53)
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
                                     durationInMinutes = t1.DurationInMinutes,
                                     ServiceType = t1.ServiceType,
                                     status = t1.Status,
                                     isSlotAvailable = _context.TimeSlot.Where(a => a.ServiceId == t1.ServiceId && a.Status && a.SlotCount > 0 && !a.IsDeleted).Select(u => u.SlotDate).Distinct().Count(),
                                     serviceCountInCart = _context.Cart.Where(a => a.ServiceId == t1.ServiceId && a.CustomerUserId == currentUserId).Sum(a => a.ServiceCountInCart),
                                     // Additional properties from other tables
                                 };

                    query = query.Concat(query1);
                }
            }


            if (model.mainCategoryId > 0)
            {
                query = query.Where(u => u.mainCategoryId == model.mainCategoryId || u.mainCategoryId == 53);
            }
            if (model.subCategoryId > 0)
            {
                query = query.Where(u => u.subCategoryId == model.subCategoryId || u.subCategoryId == 55);
            }
            if (model.salonId > 0)
            {
                var salon = await _context.SalonDetail.FirstOrDefaultAsync(u => u.SalonId == model.salonId);
                if (salon == null)
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = ResponseMessages.msgNotFound + "record.";
                    return _response;
                }
                if (salon.Status == null)
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = ResponseMessages.msgNotFound + "record.";
                    return _response;
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

            var SalonServiceList = query
                .OrderByDescending(u => u.ServiceType)
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

            if (model.Discount > 0 && !string.IsNullOrEmpty(model.MaxOrMinDiscount))
            {
                if (model.Discount > 0 && model.MaxOrMinDiscount == "Max")
                {
                    SalonServiceList = SalonServiceList.Where(x => (x.discountInPercentage < model.Discount)).OrderByDescending(u => u.discountInPercentage).ToList();
                }
                else
                {
                    SalonServiceList = SalonServiceList.Where(x => (x.discountInPercentage > model.Discount)).ToList();
                }
            }

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


            int count = SalonServiceList.Count();
            int CurrentPage = model.pageNumber;
            int PageSize = model.pageSize;
            int TotalCount = count;
            int TotalPages = (int)Math.Ceiling(count / (double)PageSize);
            var items = SalonServiceList.Skip((CurrentPage - 1) * PageSize).Take(PageSize).ToList();
            var previousPage = CurrentPage > 1 ? "Yes" : "No";
            var nextPage = CurrentPage < TotalPages ? "Yes" : "No";

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
                return _response;
            }

            if (model.categoryWise == false)
            {
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Data = obj;
                _response.Messages = ResponseMessages.msgListFoundSuccess;
                return _response;
            }
            else
            {
                var result = query
                        .Where(service => service.status == 1)
                        .OrderBy(service => service.mainCategoryId)
                        .AsEnumerable() // Explicitly load into memory
                        .GroupBy(service => service.mainCategoryName)
                        .Select(groupedServices => new
                        {
                            MainCategoryName = groupedServices.Key,
                            Services = groupedServices.Select(service => new SalonServiceListDTO
                            {
                                serviceName = service.serviceName,
                                serviceId = service.serviceId,
                                vendorId = service.vendorId,
                                salonId = service.salonId,
                                salonName = service.salonName,
                                mainCategoryId = service.mainCategoryId,
                                mainCategoryName = service.mainCategoryName,
                                subCategoryId = service.subCategoryId,
                                subCategoryName = service.subCategoryName,
                                serviceDescription = service.serviceDescription,
                                serviceImage = service.serviceImage,
                                listingPrice = service.listingPrice,
                                basePrice = service.basePrice,
                                favoritesStatus = service.favoritesStatus,
                                discount = service.discount,
                                genderPreferences = service.genderPreferences,
                                ageRestrictions = service.ageRestrictions,
                                ServiceType = service.ServiceType,
                                totalCountPerDuration = service.totalCountPerDuration,
                                durationInMinutes = service.durationInMinutes,
                                status = service.status,
                                isSlotAvailable = service.isSlotAvailable,
                                serviceCountInCart = service.serviceCountInCart,
                                // Additional properties from other tables
                            })
                        })
                    .ToList();

                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Data = result;
                _response.Messages = ResponseMessages.msgListFoundSuccess;
                return _response;
            }


            return _response;
        }
        public async Task<Object> vendorServiceList(SalonServiceFilterationListDTO model, string currentUserId)
        {

            var currentUserDetail = await _userManager.FindByIdAsync(currentUserId);

            var roles = await _userManager.GetRolesAsync(currentUserDetail);

            IQueryable<SalonServiceListDTO> query = _context.SalonService.Select(service => new SalonServiceListDTO { });

            model.serviceType = model.serviceType == null ? "Single" : "Package";
            query = from t1 in _context.SalonService
                    join t2 in _context.MainCategory on t1.MainCategoryId equals t2.MainCategoryId
                    where t1.IsDeleted != true
                    where t1.ServiceType == model.serviceType
                    orderby t1.ServiceId

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
                        discount = t1.Discount,
                        totalCountPerDuration = t1.TotalCountPerDuration,
                        durationInMinutes = t1.DurationInMinutes,
                        genderPreferences = t1.GenderPreferences,
                        ServiceType = t1.ServiceType,
                        ageRestrictions = t1.AgeRestrictions,
                        status = t1.Status
                        // Additional properties from other tables
                    };

            if (model.mainCategoryId > 0)
            {
                query = query.Where(u => u.mainCategoryId == model.mainCategoryId || u.mainCategoryId == 53);
            }
            if (model.subCategoryId > 0)
            {
                query = query.Where(u => u.subCategoryId == model.subCategoryId || u.subCategoryId == 55);
            }
            if (model.salonId > 0)
            {
                var salon = await _context.SalonDetail.FirstOrDefaultAsync(u => u.SalonId == model.salonId);
                if (salon == null)
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = ResponseMessages.msgNotFound + "record.";
                    return _response;
                }
                if (salon.Status == null)
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = ResponseMessages.msgNotFound + "record.";
                    return _response;
                }
                query = query.Where(u => u.salonId == model.salonId);
            }


            var SalonServiceList = query
                .OrderByDescending(u => u.ServiceType)
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

            if (model.Discount > 0 && !string.IsNullOrEmpty(model.MaxOrMinDiscount))
            {
                if (model.Discount > 0 && model.MaxOrMinDiscount == "Max")
                {
                    SalonServiceList = SalonServiceList.Where(x => (x.discountInPercentage < model.Discount)).OrderByDescending(u => u.discountInPercentage).ToList();
                }
                else
                {
                    SalonServiceList = SalonServiceList.Where(x => (x.discountInPercentage > model.Discount)).ToList();
                }
            }

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


            int count = SalonServiceList.Count();
            int CurrentPage = model.pageNumber;
            int PageSize = model.pageSize;
            int TotalCount = count;
            int TotalPages = (int)Math.Ceiling(count / (double)PageSize);
            var items = SalonServiceList.Skip((CurrentPage - 1) * PageSize).Take(PageSize).ToList();
            var previousPage = CurrentPage > 1 ? "Yes" : "No";
            var nextPage = CurrentPage < TotalPages ? "Yes" : "No";

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
                return _response;
            }

            if (model.categoryWise == false)
            {
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Data = obj;
                _response.Messages = ResponseMessages.msgListFoundSuccess;
                return _response;
            }
            else
            {
                var result = query
                        .Where(service => service.status == 1)
                        .OrderBy(service => service.mainCategoryId)
                        .AsEnumerable() // Explicitly load into memory
                        .GroupBy(service => service.mainCategoryName)
                        .Select(groupedServices => new
                        {
                            MainCategoryName = groupedServices.Key,
                            Services = groupedServices.Select(service => new SalonServiceListDTO
                            {
                                serviceName = service.serviceName,
                                serviceId = service.serviceId,
                                vendorId = service.vendorId,
                                salonId = service.salonId,
                                salonName = service.salonName,
                                mainCategoryId = service.mainCategoryId,
                                mainCategoryName = service.mainCategoryName,
                                subCategoryId = service.subCategoryId,
                                subCategoryName = service.subCategoryName,
                                serviceDescription = service.serviceDescription,
                                serviceImage = service.serviceImage,
                                listingPrice = service.listingPrice,
                                basePrice = service.basePrice,
                                favoritesStatus = service.favoritesStatus,
                                discount = service.discount,
                                genderPreferences = service.genderPreferences,
                                ageRestrictions = service.ageRestrictions,
                                ServiceType = service.ServiceType,
                                totalCountPerDuration = service.totalCountPerDuration,
                                durationInMinutes = service.durationInMinutes,
                                status = service.status,
                                isSlotAvailable = service.isSlotAvailable,
                                serviceCountInCart = service.serviceCountInCart,
                                // Additional properties from other tables
                            })
                        })
                    .ToList();

                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Data = result;
                _response.Messages = ResponseMessages.msgListFoundSuccess;
                return _response;
            }

        }
        public async Task<Object> GetSalonServiceDetail(int serviceId, string? serviceType, string currentUserId)

        {
            var currentUserDetail = await _userManager.FindByIdAsync(currentUserId);

            var serviceDetail = await _context.SalonService.FirstOrDefaultAsync(u => u.ServiceId == serviceId);

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
                    serviceResponse.IncludeService = packageServices;
                    serviceResponse.IncludeServiceId = includeService.IncludeServiceId;
                }

            }

            var serviceImageList = new List<ServiceImageDTO>();

            if (!string.IsNullOrEmpty(serviceDetail.ServiceImage1))
            {
                var serviceImageDTO = new ServiceImageDTO();
                serviceImageDTO.salonServiceImage = serviceDetail.ServiceImage1;
                serviceImageList.Add(serviceImageDTO);
            }
            if (!string.IsNullOrEmpty(serviceDetail.ServiceImage2))
            {
                var serviceImageDTO = new ServiceImageDTO();
                serviceImageDTO.salonServiceImage = serviceDetail.ServiceImage2;
                serviceImageList.Add(serviceImageDTO);
            }
            if (!string.IsNullOrEmpty(serviceDetail.ServiceImage3))
            {
                var serviceImageDTO = new ServiceImageDTO();
                serviceImageDTO.salonServiceImage = serviceDetail.ServiceImage3;
                serviceImageList.Add(serviceImageDTO);
            }
            if (!string.IsNullOrEmpty(serviceDetail.ServiceImage4))
            {
                var serviceImageDTO = new ServiceImageDTO();
                serviceImageDTO.salonServiceImage = serviceDetail.ServiceImage4;
                serviceImageList.Add(serviceImageDTO);
            }
            if (!string.IsNullOrEmpty(serviceDetail.ServiceImage5))
            {
                var serviceImageDTO = new ServiceImageDTO();
                serviceImageDTO.salonServiceImage = serviceDetail.ServiceImage5;
                serviceImageList.Add(serviceImageDTO);
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
            serviceResponse.serviceImage = serviceImageList;
            // serviceResponse.isSlotAvailable = _context.TimeSlot.Where(a => a.ServiceId == serviceId && a.Status && a.SlotCount > 0 && !a.IsDeleted)
            //                                             .Select(u => u.SlotDate).Distinct().Count();
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
            return _response;
        }
        public async Task<Object> DeleteSalonService(int serviceId)
        {
            var salonService = await _context.SalonService.Where(x => (x.ServiceId == serviceId) && (x.IsDeleted != true)).FirstOrDefaultAsync();

            List<string> serviceIdList = new List<string>();

            var salonServiceInPackage = await _context.ServicePackage.Select(u => u.IncludeServiceId).ToListAsync();

            foreach (var item in salonServiceInPackage)
            {
                string[] includedIds = item.Split(',');

                // Display the result
                foreach (string id in includedIds)
                {
                    serviceIdList.Add(id);
                }
            }

            if (serviceIdList.Contains(serviceId.ToString()))
            {
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = false;
                _response.Messages = "Unable to delete. The service is currently in use within a package.";
                return _response;
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

            return _response;

        }
        public async Task<Object> SetServiceStatus(SetServiceStatusDTO model)
        {

            var serviceDetails = await _context.SalonService.FirstOrDefaultAsync(u => u.ServiceId == model.serviceId);

            serviceDetails.Status = model.status;
            _context.Update(serviceDetails);
            _context.SaveChanges();

            var getService = await _context.SalonService.FirstOrDefaultAsync(u => u.ServiceId == model.serviceId);
            if (getService != null)
            {
                var response = _mapper.Map<serviceDetailDTO>(getService);
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Data = response;
                _response.Messages = "Service" + ResponseMessages.msgUpdationSuccess;
                return _response;
            }
            else
            {
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = false;
                _response.Data = new Object { };
                _response.Messages = ResponseMessages.msgSomethingWentWrong;
                return _response;
            }
        }
        public async Task<Object> getServiceImageInBase64(int serviceId, string? Status)
        {
            List<string> serviceImageList = new List<string>();

            var serviceDetail = await _context.SalonService.Where(u => u.ServiceId == serviceId).FirstOrDefaultAsync();

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
            _response.Messages = "Service image" + ResponseMessages.msgListFoundSuccess;
            return _response;
        }
        public async Task<Object> getAvailableTimeSlots(int serviceId, string queryDate)
        {

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
                return _response;
            }
            var slotDetail = await _context.TimeSlot
                               .Where(a => a.ServiceId == serviceId && a.Status != false && a.SlotCount > 0 && a.IsDeleted != true && a.SlotDate == searchDate)
                               .ToListAsync();
            // get scheduled days
            var sortedSlots = slotDetail.OrderBy(a => Convert.ToDateTime(a.FromTime)).ToList();

            // Get the current time and add 2 hours to it
            var limitDate = DateTime.Now.AddHours(2);
            var availableSlots = new List<timeSlotsDTO>();

            var ctz = TZConvert.GetTimeZoneInfo("India Standard Time");
            var convrtedZoneDate = TimeZoneInfo.ConvertTimeFromUtc(Convert.ToDateTime(DateTime.UtcNow), ctz);

            foreach (var item in sortedSlots)
            {
                if (searchDate.Date == convrtedZoneDate.Date)
                {
                    var fromTime = (Convert.ToDateTime(item.FromTime).TimeOfDay);
                    var currentTime = convrtedZoneDate.TimeOfDay;
                    var timeDifference = (fromTime.TotalMinutes - currentTime.TotalMinutes);

                    int minutesThreshold = 05; // Set your threshold here
                    if (timeDifference >= minutesThreshold)
                    {
                        availableSlots.Add(_mapper.Map<timeSlotsDTO>(item));
                    }
                }
                else
                {
                    availableSlots.Add(_mapper.Map<timeSlotsDTO>(item));
                }
            }

            if (availableSlots.Any())
            {
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Messages = "Slots shown" + ResponseMessages.msgShownSuccess;
                _response.Data = availableSlots;
                return _response;
            }
            _response.StatusCode = HttpStatusCode.OK;
            _response.IsSuccess = false;
            _response.Messages = ResponseMessages.msgNotFound + "record";
            return _response;


        }
        public async Task<Object> getAvailableDates(int serviceId)
        {


            // get scheduled days
            var slotDetail = await _context.TimeSlot
                                .Where(a => a.ServiceId == serviceId && a.Status != false && a.SlotCount > 0 && a.IsDeleted != true)
                                .Select(u => u.SlotDate)
                                .Distinct()
                                .ToListAsync();

            var availableDates = new List<string>();
            var ctz = TZConvert.GetTimeZoneInfo("India Standard Time");
            var convrtedZoneDate = TimeZoneInfo.ConvertTimeFromUtc(Convert.ToDateTime(DateTime.UtcNow), ctz);
            foreach (var item in slotDetail)
            {
                if (item.Date == convrtedZoneDate.Date)
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
                        var fromTime = (Convert.ToDateTime(item1.FromTime).TimeOfDay);

                        var currentTime = convrtedZoneDate.TimeOfDay;
                        var timeDifference = (fromTime.TotalMinutes - currentTime.TotalMinutes);

                        int minutesThreshold = 05; // Set your threshold here
                        if (timeDifference >= minutesThreshold)
                        {
                            availableSlots.Add(_mapper.Map<timeSlotsDTO>(item1));
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

            var ascendingDates = availableDates.OrderBy(dateString => dateString).ToList();

            if (slotDetail != null)
            {
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Messages = "Dates shown" + ResponseMessages.msgShownSuccess;
                _response.Data = ascendingDates;
                return _response;
            }
            _response.StatusCode = HttpStatusCode.OK;
            _response.IsSuccess = false;
            _response.Messages = ResponseMessages.msgNotFound + "record";
            return _response;

        }
        public async Task<Object> GetScheduledDaysTime(int salonId)
        {
            // get scheduled days
            var SalonSchedule = await _context.SalonSchedule.Where(a => (a.SalonId == salonId) && (a.IsDeleted != true)).FirstOrDefaultAsync();

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
            _response.Messages = "Detail" + ResponseMessages.msgShownSuccess;
            _response.Data = scheduleDayViewModel;
            return _response;

        }
        public async Task<Object> SetSalonServiceFavouriteStatus(SetSalonServiceFavouriteStatusDTO model, string currentUserId)
        {
            var serviceDetail = await _context.SalonService.FirstOrDefaultAsync(u => u.ServiceId == model.serviceId);

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
                    return _response;
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
                    _response.Messages = ResponseMessages.msgNotFound + "record";
                    return _response;
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
                return _response;
            }
            else
            {
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = false;
                _response.Data = new Object { };
                _response.Messages = ResponseMessages.msgSomethingWentWrong;
                return _response;
            }

        }

    }
}