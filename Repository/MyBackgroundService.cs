using AutoMapper;
using Microsoft.EntityFrameworkCore;
using BeautyHubAPI.Data;
using BeautyHubAPI.Models;
using static BeautyHubAPI.Common.GlobalVariables;
using BeautyHubAPI.Models.Dtos;
using TimeZoneConverter;

public class MyBackgroundService : BackgroundService
{
    private readonly ILogger<MyBackgroundService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IMapper _mapper;
    private readonly object _lock = new object(); // Used for thread-safe access to the flag
    private bool _shouldStart = true; // Custom flag to control service start
    private CancellationTokenSource _cancellationTokenSource;
    private bool _shouldRun = true;
    int salonIdToUpdate = 0;


    public MyBackgroundService(ILogger<MyBackgroundService> logger, IMapper mapper, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _mapper = mapper;
        _cancellationTokenSource = new CancellationTokenSource();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_shouldRun)
        {
            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                    //todo
                    await UpdateSchedule(dbContext);
                    StopServiceOnce();
                }

                      _logger.LogInformation("Regular service completed.");
            }
            
            catch (Exception ex)
            {
                _logger.LogError($"Error running regular service: {ex.Message}");
            }
            finally
            {
                _shouldRun = false;
            }
        }
    }

    public void StopService()
    {
        _cancellationTokenSource.Cancel();
    }
    public void StartService(int salonId)
    {
        salonIdToUpdate = salonId;
        // Start the background service
        StartAsync(new CancellationToken()).GetAwaiter().GetResult();
    }
    public void StopServiceOnce()
    {
        _shouldRun = false;
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
        var salonScheduleList = await dbContext.SalonSchedule.Where(u => u.IsDeleted != true && u.Status != false && u.UpdateStatus == false && u.SalonId == salonIdToUpdate).ToListAsync();
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

            // update timeslots according to schedule
            var services = await dbContext.SalonService.Where(u => u.SalonId == SalonScheduleDays.SalonId && u.IsDeleted != true).ToListAsync();
            foreach (var item in services)
            {
                var deleteTimeSlot = await dbContext.TimeSlot.Where(u => u.ServiceId == item.ServiceId).ToListAsync();

                foreach (var item3 in deleteTimeSlot)
                {
                    item3.Status = false;
                }
                dbContext.UpdateRange(deleteTimeSlot);
                await dbContext.SaveChangesAsync();
                var ctz = TZConvert.GetTimeZoneInfo("India Standard Time");
                var convrtedZoneDate = TimeZoneInfo.ConvertTimeFromUtc(Convert.ToDateTime(DateTime.UtcNow), ctz);

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

            SalonScheduleDays.UpdateStatus = true;
            dbContext.Update(SalonScheduleDays);
            await dbContext.SaveChangesAsync();
        }
        _logger.LogInformation("Status updated.");
    }
}
