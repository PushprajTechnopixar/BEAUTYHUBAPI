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
        // int totalServices = 0;

        appointmentList = _context.Appointment.Where(x => x.AppointmentStatus == "Scheduled").ToList();

        foreach (var item in appointmentList)
        {
            var bookedServices = await _context.BookedService.Where(u => u.AppointmentId == item.AppointmentId && u.AppointmentStatus == "Scheduled").OrderByDescending(u => u.AppointmentDate).ToListAsync();
            DateTime BookedDateTime = new DateTime();
            int serviceId = 0;
            int timeValue = 0;
            foreach (var item2 in bookedServices)
            {
                var slotDetail = await _context.TimeSlot.Where(u => u.SlotId == item2.SlotId).FirstOrDefaultAsync();
                TimeSpan appointmentFromTime = Convert.ToDateTime(slotDetail.FromTime).TimeOfDay;
                string appointmentDate = item2.AppointmentDate.ToString("dd-MM-yyyy");
                DateTime appointmentDateTime = DateTime.ParseExact(appointmentDate, "dd-MM-yyyy", CultureInfo.InvariantCulture);
                appointmentDateTime = appointmentDateTime.Add(appointmentFromTime);
                BookedDateTime = appointmentDateTime;
                TimeSpan timeSpan = convrtedZoneDate - appointmentDateTime;
                int difference = Convert.ToInt32(timeSpan.TotalMinutes);
                if (difference > 5)
                {
                    item2.AppointmentStatus = "Cancelled";
                }
            }
            if (BookedDateTime.Date > convrtedZoneDate.Date)
            {
                _context.UpdateRange(bookedServices);
                _context.SaveChanges();
                var checBookedServices = await _context.BookedService.Where(u => u.AppointmentId == item.AppointmentId && u.AppointmentStatus == "Completed").FirstOrDefaultAsync();
                if (checBookedServices == null)
                {
                    foreach (var item3 in bookedServices)
                    {
                        item3.FinalPrice = 0;
                        item3.Discount = 0;
                        item3.AppointmentStatus = "Cancelled";
                        item3.CancelledPrice = item3.TotalPrice;
                    }
                    _context.UpdateRange(bookedServices);
                    _context.SaveChanges();

                    {
                        item.FinalPrice = 0;
                        item.Discount = 0;
                        item.AppointmentStatus = "Cancelled";
                        item.CancelledPrice = item.TotalPrice;
                    }

                    _context.Update(item);
                    _context.SaveChangesAsync();
                }
            }
        }

        _logger.LogInformation("Status updated.");
    }

}
