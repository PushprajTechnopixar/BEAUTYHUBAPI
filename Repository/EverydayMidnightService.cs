using AutoMapper;
using Microsoft.EntityFrameworkCore;
using BeautyHubAPI.Data;
using BeautyHubAPI.Models;
using static BeautyHubAPI.Common.GlobalVariables;
using BeautyHubAPI.Models.Dtos;
using TimeZoneConverter;

public class EverydayMidnightService : BackgroundService
{
    private readonly ILogger<EverydayMidnightService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IMapper _mapper;
    private readonly object _lock = new object(); // Used for thread-safe access to the flag
    private bool _shouldStart = true; // Custom flag to control service start
    private CancellationTokenSource _cancellationTokenSource;


    public EverydayMidnightService(ILogger<EverydayMidnightService> logger, IMapper mapper, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _mapper = mapper;
        _cancellationTokenSource = new CancellationTokenSource();
    }

    // protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    // {
    //     // Calculate the time until 12:00 AM
    //     var now = DateTimeOffset.Now;
    //     var nextRunTime = now.Date.AddDays(1).AddHours(0); // 12:00 AM

    //     // Calculate the delay until the next run
    //     var delay = nextRunTime - now;

    //     if (delay < TimeSpan.Zero)
    //     {
    //         // If the calculated delay is negative, schedule it for the next day
    //         delay = TimeSpan.FromHours(24) + delay;
    //     }

    //     _logger.LogInformation($"Next run scheduled at {nextRunTime}. Waiting for {delay.TotalHours} hours.");

    //     await Task.Delay(delay, stoppingToken);

    //     // Service code here
    //     using (var scope = _serviceProvider.CreateScope())
    //     {
    //         var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    //         await UpdateSchedule(dbContext);
    //     }

    //     _logger.LogInformation("Everyday midnight service completed.");
    // }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Calculate the time until 12:00 AM
        var now = DateTimeOffset.UtcNow;
        var ctz = TZConvert.GetTimeZoneInfo("India Standard Time");
        var convrtedZoneDate = TimeZoneInfo.ConvertTimeFromUtc(now.DateTime, ctz);
        var nextRunTime = now.Date.AddDays(1).AddHours(0); // 12:00 AM

        // Calculate the delay until the next run
        var delay = nextRunTime - now;

        if (delay < TimeSpan.Zero)
        {
            // If the calculated delay is negative, schedule it for the next day
            delay = TimeSpan.FromHours(24) + delay;
        }

        // delay = TimeSpan.FromSeconds(10);

        while (!stoppingToken.IsCancellationRequested)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                await UpdateSchedule(dbContext);
            }
            // Delay for a certain duration before checking the flag again
            await Task.Delay(delay, stoppingToken); // Polling interval
            // await Task.Delay(TimeSpan.FromHours(60), stoppingToken);
            _logger.LogInformation("Everyday midnight service completed.");
        }
    }

    public void StopService()
    {
        _cancellationTokenSource.Cancel();
    }
    public void StartService()
    {
        // Start the background service
        StartAsync(new CancellationToken()).GetAwaiter().GetResult();
    }

    // public void StartService()
    // {
    //     lock (_lock)
    //     {
    //         _shouldStart = true;
    //     }
    // }

    // public void StopService()
    // {
    //     lock (_lock)
    //     {
    //         _shouldStart = false;
    //     }
    // }

    private async Task UpdateSchedule(ApplicationDbContext dbContext)
    {
        var salonScheduleList = await dbContext.SalonSchedule.Where(u => u.IsDeleted != true).ToListAsync();
        foreach (var SalonScheduleDays in salonScheduleList)
        {
            SalonScheduleDays.Status = true;
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

            var ctz = TZConvert.GetTimeZoneInfo("India Standard Time");
            var convrtedZoneDate = TimeZoneInfo.ConvertTimeFromUtc(Convert.ToDateTime(DateTime.UtcNow), ctz).AddHours(1);

            // update timeslots according to schedule
            var services = await dbContext.SalonService.Where(u => u.SalonId == SalonScheduleDays.SalonId && u.IsDeleted != true).ToListAsync();
            foreach (var item in services)
            {
                var deleteTimeSlot = await dbContext.TimeSlot.Where(u => u.ServiceId == item.ServiceId && u.Status != false).ToListAsync();

                foreach (var item3 in deleteTimeSlot)
                {
                    item3.Status = false;
                }
                dbContext.UpdateRange(deleteTimeSlot);
                await dbContext.SaveChangesAsync();

                int addDay = 0;
                for (int i = 0; i < 7; i++)
                {
                    DateTime currentDate = convrtedZoneDate.AddDays(i);
                    string currentDateStr = currentDate.ToString("yyyy-MM-dd");
                    string dayName = currentDate.ToString("dddd");

                    var existingTimeSlot = dbContext.TimeSlot
                        .Where(u => u.ServiceId == item.ServiceId && u.SlotDate.Date == currentDate.Date)
                        .ToList();

                    if (!scheduledDaysList.Contains(dayName))
                    {
                        foreach (var existingSlot in existingTimeSlot)
                        {
                            existingSlot.Status = false;
                        }

                        dbContext.UpdateRange(existingTimeSlot);
                        await dbContext.SaveChangesAsync();
                        continue;
                    }

                    var startDateTime = DateTime.Parse(currentDateStr + " " + SalonScheduleDays.FromTime);
                    var endDateTime = DateTime.Parse(currentDateStr + " " + SalonScheduleDays.ToTime);
                    int minutes = item.DurationInMinutes;
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
                            ServiceId = item.ServiceId,
                            FromTime = item2.time,
                            ToTime = DateTime.Parse(item2.time).AddMinutes(minutes).ToString("hh:mm tt"),
                            SlotDate = Convert.ToDateTime(currentDate.ToString(@"yyyy-MM-dd")),
                            SlotCount = item.TotalCountPerDuration,
                            Status = true
                        };

                        bool pass = true;
                        var existingTimeSlotDetails = existingTimeSlot.FirstOrDefault(u => u.FromTime == timeslot.FromTime);

                        if (!string.IsNullOrEmpty(item.LockTimeStart))
                        {
                            string[] splitLockTimeStart = item.LockTimeStart.Split(",");
                            string[] splitLockTimeEnd = item.LockTimeEnd.Split(",");
                            List<DateTime> lockTimeStart = splitLockTimeStart.Select(DateTime.Parse).ToList();
                            List<DateTime> lockTimeEnd = splitLockTimeEnd.Select(DateTime.Parse).ToList();
                            var fromTime = DateTime.Parse(currentDateStr + " " + timeslot.FromTime);
                            var toTime = DateTime.Parse(currentDateStr + " " + timeslot.ToTime);

                            for (int m = 0; m < lockTimeStart.Count; m++)
                            {
                                var chkLockedFrom = DateTime.Parse(currentDateStr + " " + lockTimeStart[m].ToString(@"hh:mm tt"));
                                var chkLockedTo = DateTime.Parse(currentDateStr + " " + lockTimeEnd[m].ToString(@"hh:mm tt"));

                                if ((fromTime <= chkLockedFrom && toTime <= chkLockedFrom) || (fromTime >= chkLockedTo && toTime >= chkLockedTo))
                                {
                                    if (existingTimeSlotDetails == null)
                                    {
                                        await dbContext.AddAsync(timeslot);
                                        await dbContext.SaveChangesAsync();
                                    }
                                    else
                                    {
                                        existingTimeSlotDetails.Status = true;
                                        dbContext.Update(existingTimeSlotDetails);
                                        await dbContext.SaveChangesAsync();
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (existingTimeSlotDetails == null)
                            {
                                await dbContext.AddAsync(timeslot);
                                await dbContext.SaveChangesAsync();
                            }
                            else
                            {
                                existingTimeSlotDetails.Status = true;
                                dbContext.Update(existingTimeSlotDetails);
                                await dbContext.SaveChangesAsync();
                            }
                        }
                    }
                    addDay++;
                }
            }

            var backDateTimeSlots = dbContext.BookedService
                                 .Where(u => u.AppointmentDate.Date < convrtedZoneDate.Date &&
                                             (u.AppointmentStatus != "Completed" || u.AppointmentStatus != "Cancelled"))
                                 .ToList();
            // var backDateTimeSlots = dbContext.TimeSlot
            //         .Where(t => t.SlotDate.Date <= convrtedZoneDate.Date && t.SlotDate.Date > convrtedZoneDate.Date.AddDays(-1) && t.Status != false)
            //         .ToList().ToList();

            foreach (var timeSlot in backDateTimeSlots)
            {
                var timeSlotDetail = dbContext.TimeSlot.Where(u => u.SlotId == timeSlot.SlotId).FirstOrDefault();

                timeSlotDetail.Status = false;

                dbContext.UpdateRange(timeSlotDetail);
                await dbContext.SaveChangesAsync();

                var backDateBookedService = dbContext.BookedService
                    .Where(u => u.SlotId == timeSlot.SlotId &&
                                (u.AppointmentStatus == "Scheduled"))
                    .FirstOrDefault();

                if (backDateBookedService != null)
                {
                    backDateBookedService.AppointmentStatus = "Cancelled";
                    backDateBookedService.CancelledPrice = backDateBookedService.TotalPrice;
                    backDateBookedService.FinalPrice = 0;
                    backDateBookedService.Discount = 0;

                    dbContext.Update(backDateBookedService);
                    await dbContext.SaveChangesAsync();
                    var checkScheduledAppointment = dbContext.BookedService
                                        .Where(u => u.AppointmentId == timeSlot.AppointmentId &&
                                                    (u.AppointmentStatus == "Scheduled"))
                                        .FirstOrDefault();
                    if (checkScheduledAppointment == null)
                    {
                        var backAppointmentService = dbContext.Appointment
                            .Where(u => u.AppointmentId == backDateBookedService.AppointmentId &&
                                        (u.AppointmentStatus == "Scheduled"))
                            .FirstOrDefault();

                        if (backAppointmentService != null)
                        {
                            {
                                backAppointmentService.FinalPrice = 0;
                                backAppointmentService.Discount = 0;
                                backAppointmentService.AppointmentStatus = "Cancelled";
                                backAppointmentService.CancelledPrice = backAppointmentService.TotalPrice;
                            }

                            dbContext.Update(backAppointmentService);
                            await dbContext.SaveChangesAsync();
                        }
                    }
                }
            }
            // Remove product from cart
            var cartServices = dbContext.Cart.ToList();
            foreach (var item in cartServices)
            {
                var timeSlotDate = dbContext.TimeSlot.Where(u => u.SlotId == item.SlotId).FirstOrDefault();
                if (timeSlotDate.SlotDate.Date < convrtedZoneDate.Date)
                {
                    dbContext.RemoveRange(cartServices);
                    await dbContext.SaveChangesAsync();
                }
            }
            _logger.LogInformation("Status updated.");
        }
    }
}
