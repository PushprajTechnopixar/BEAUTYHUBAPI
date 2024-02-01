using BeautyHubAPI.Data;
using BeautyHubAPI.Models;
using BeautyHubAPI.Models.Dtos;
using Microsoft.EntityFrameworkCore;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TimeZoneConverter;

namespace BeautyHubAPI.JobScheduler
{
    public class EverydayMidnightJob : IJob
    {
        private readonly IServiceProvider _serviceProvider;
        // private ApplicationDbContext dbContext;
        public EverydayMidnightJob(IServiceProvider provider)
        {
            _serviceProvider = provider;
            // dbContext = _dbContext;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    // todo                
                    await UpdateSchedule(dbContext);
                }
            }
            catch
            {
                throw;
            }
            await Task.CompletedTask;
        }

        private async Task UpdateSchedule(ApplicationDbContext dbContext)
        {
            var salonScheduleList = await dbContext.SalonSchedule.Where(u => u.IsDeleted != true).ToListAsync();
            foreach (var SalonScheduleDays in salonScheduleList)
            {
                SalonScheduleDays.UpdateStatus = false;
                dbContext.Update(SalonScheduleDays);
                await dbContext.SaveChangesAsync();

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
                var services = await dbContext.SalonService.Where(u => u.SalonId == SalonScheduleDays.SalonId && u.IsDeleted == false).ToListAsync();
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
                SalonScheduleDays.UpdateStatus = true;
                dbContext.Update(SalonScheduleDays);
                await dbContext.SaveChangesAsync();
            }
        }

    }
}
