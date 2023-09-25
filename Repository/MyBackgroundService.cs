// using AutoMapper;
// using Microsoft.EntityFrameworkCore;
// using BeautyHubAPI.Data;
// using BeautyHubAPI.Models;
// using static BeautyHubAPI.Common.GlobalVariables;

// public class MyBackgroundService : BackgroundService
// {
//     private readonly ILogger<MyBackgroundService> _logger;
//     private readonly IServiceProvider _serviceProvider;
//     private readonly IMapper _mapper;
//     private readonly object _lock = new object(); // Used for thread-safe access to the flag
//     private bool _shouldStart = true; // Custom flag to control service start
//     private CancellationTokenSource _cancellationTokenSource;


//     public MyBackgroundService(ILogger<MyBackgroundService> logger, IMapper mapper, IServiceProvider serviceProvider)
//     {
//         _logger = logger;
//         _serviceProvider = serviceProvider;
//         _mapper = mapper;
//         _cancellationTokenSource = new CancellationTokenSource();
//     }

//     protected override async Task ExecuteAsync(CancellationToken stoppingToken)
//     {
//         _logger.LogInformation("Background service is running at: {time}", DateTimeOffset.Now);

//         while (!stoppingToken.IsCancellationRequested)
//         {
//             using (var scope = _serviceProvider.CreateScope())
//             {
//                 var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

//                 await AddProductToVendorInventory(dbContext);
//             }
//             // Delay for a certain duration before checking the flag again
//             await Task.Delay(TimeSpan.FromHours(60), stoppingToken);
//         }
//         _logger.LogInformation("Background service completed.");
//     }

//     public void StopService()
//     {
//         _cancellationTokenSource.Cancel();
//     }
//     public void StartService()
//     {
//         // Start the background service
//         StartAsync(new CancellationToken()).GetAwaiter().GetResult();
//     }

//     // public void StartService()
//     // {
//     //     lock (_lock)
//     //     {
//     //         _shouldStart = true;
//     //     }
//     // }

//     // public void StopService()
//     // {
//     //     lock (_lock)
//     //     {
//     //         _shouldStart = false;
//     //     }
//     // }

//     private async Task AddProductToVendorInventory(ApplicationDbContext dbContext)
//     {
//         var salonDetails = await dbContext.salonDetail.Where(u => u.InventoryAdded != true).ToListAsync();
//         foreach (var salonDetail in salonDetails)
//         {
//             if (salonDetail != null)
//             {
//                 // var inventoryProducts = await dbContext.ProductInventory.Where(u => u.ProductId > 612).ToListAsync();
//                 var inventoryProducts = await dbContext.ProductInventory.Where(u => u.Status == 1).ToListAsync();

//                 foreach (var item in inventoryProducts)
//                 {
//                     var product = _mapper.Map<Product>(item);
//                     product.VendorId = salonDetail.VendorId;
//                     product.ShippingCharges = 0;
//                     product.WaitingDays = 0;
//                     product.ShopId = salonDetail.ShopId;
//                     // product.Skuid = item.Skuid + salonDetail.ShopId.ToString();
//                     product.ProductId = 0;
//                     product.ProductType = ProductType.Main.ToString();
//                     product.ProductInventoryId = item.ProductId;

//                     await dbContext.Product.AddAsync(product);
//                     await dbContext.SaveChangesAsync();
//                 }

//                 salonDetail.InventoryAdded = true;
//                 dbContext.salonDetail.Update(salonDetail);
//                 await dbContext.SaveChangesAsync();
//             }
//         }
//         _logger.LogInformation("Added products.");
//     }


// }
