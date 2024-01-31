using AutoMapper;
using Microsoft.EntityFrameworkCore;
using BeautyHubAPI.Data;
using BeautyHubAPI.Models;
using static BeautyHubAPI.Common.GlobalVariables;
using BeautyHubAPI.Models.Dtos;
using TimeZoneConverter;
using BeautyHubAPI.Models.Helper;
using System.Globalization;

public class ApplointmentListBackgroundService : BackgroundService
{
    private readonly ILogger<ApplointmentListBackgroundService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IMapper _mapper;
    private readonly object _lock = new object(); // Used for thread-safe access to the flag
    private bool _shouldStart = true; // Custom flag to control service start
    private CancellationTokenSource _cancellationTokenSource;
    private bool _shouldRun = true;
    protected APIResponse _response;

    public ApplointmentListBackgroundService(ILogger<ApplointmentListBackgroundService> logger, IMapper mapper, IServiceProvider serviceProvider)
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
    public void StartService()
    {
        // Start the background service
        StartAsync(new CancellationToken()).GetAwaiter().GetResult();
    }
    public void StopServiceOnce()
    {
        _shouldRun = false;
    }

    private async Task UpdateSchedule(ApplicationDbContext _context)
    {
        var ctz = TZConvert.GetTimeZoneInfo("India Standard Time");
        var convrtedZoneDate = TimeZoneInfo.ConvertTimeFromUtc(Convert.ToDateTime(DateTime.UtcNow), ctz);

        List<Appointment>? appointmentList;
        string appointmentTitle = "";
        string appointmentDescription = "";
        int difference = 0;
        // int totalServices = 0;

        appointmentList = await _context.Appointment.Where(x => x.AppointmentStatus == "Scheduled").ToListAsync();

        foreach (var item in appointmentList)
        {
            var bookedServices = await _context.BookedService.Where(u => u.AppointmentId == item.AppointmentId && u.AppointmentStatus == "Scheduled").OrderByDescending(u => u.AppointmentDate).ToListAsync();
            DateTime BookedDateTime = new DateTime();
            int serviceId = 0;
            int timeValue = 0;
            foreach (var item2 in bookedServices)
            {
                double? finalPrice;
                double? discount;

                var slotDetail = await _context.TimeSlot.Where(u => u.SlotId == item2.SlotId).FirstOrDefaultAsync();
                TimeSpan appointmentFromTime = Convert.ToDateTime(slotDetail.FromTime).TimeOfDay;
                string appointmentDate = item2.AppointmentDate.ToString("dd-MM-yyyy");
                DateTime appointmentDateTime = DateTime.ParseExact(appointmentDate, "dd-MM-yyyy", CultureInfo.InvariantCulture);
                appointmentDateTime = appointmentDateTime.Add(appointmentFromTime);
                var serviceDetail = await _context.SalonService.Where(u => u.ServiceId == item2.ServiceId).FirstOrDefaultAsync();
                BookedDateTime = appointmentDateTime;
                TimeSpan timeSpan = convrtedZoneDate - appointmentDateTime;
                difference = Convert.ToInt32(timeSpan.TotalMinutes);
                if (difference > serviceDetail.DurationInMinutes)
                {
                    finalPrice = item2.FinalPrice;
                    discount = item2.Discount;
                    item2.AppointmentStatus = "Cancelled";
                    item2.FinalPrice = 0;
                    item2.Discount = 0;
                    item2.CancelledPrice = item2.CancelledPrice + item2.BasePrice;

                    _context.Update(item2);
                    await _context.SaveChangesAsync();

                    var checBookedServices = await _context.BookedService.Where(u => u.AppointmentId == item.AppointmentId && (u.AppointmentStatus == "Completed" || u.AppointmentStatus == "Scheduled")).FirstOrDefaultAsync();
                    if (checBookedServices == null)
                    {
                        {
                            item.FinalPrice = item.FinalPrice - finalPrice;
                            item.Discount = item.Discount - discount;
                            item.CancelledPrice = item.CancelledPrice + item2.BasePrice;
                            item.AppointmentStatus = "Cancelled";
                        }
                        _context.Update(item);
                        var res = await _context.SaveChangesAsync();
                    }
                    else
                    {
                        {
                            item.FinalPrice = item.FinalPrice - finalPrice;
                            item.Discount = item.Discount - discount;
                            item.CancelledPrice = item.CancelledPrice + item2.BasePrice;
                        }
                        _context.Update(item);
                        await _context.SaveChangesAsync();
                    }
                }
            }
        }

        _logger.LogInformation("Status updated.");
    }
}
