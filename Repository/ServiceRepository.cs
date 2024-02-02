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

        public async Task<serviceDetailDTO> GetSalonServiceDetail(int serviceId, string? serviceType)
        {
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

            //var roles = await _userManager.GetRolesAsync(currentUserDetail);
            //if (roles[0].ToString() == "Customer")
            //{
            //    // var getCartItems = await _cartRepository.GetAsync(u => (u.CustomerUserId == currentUserId) && (u.ProductId == productDetail.ProductId && u.IsDairyProduct != true && u.IsSubscriptionProduct != true));
            //    // if (getCartItems != null)
            //    // {
            //    //     productResponse.ProductCountInCart = getCartItems.ProductCountInCart;
            //    // }

            //    // var favoritesStatus = await _context.FavouriteService.Where(u => u.ServiceId == serviceId && u.CustomerUserId == currentUserId).FirstOrDefaultAsync();
            //    // serviceResponse.favouriteStatus = favoritesStatus != null ? true : false;
            //}

            var salonDetail = await _context.SalonDetail.Where(u => u.SalonId == serviceResponse.salonId).FirstOrDefaultAsync();
            var vendorDetail = _userManager.FindByIdAsync(salonDetail.VendorId).GetAwaiter().GetResult();
            serviceResponse.vendorName = vendorDetail.FirstName + " " + vendorDetail.LastName;
            serviceResponse.salonName = salonDetail.SalonName;
            serviceResponse.vendorId = salonDetail.VendorId;
            serviceResponse.serviceImage = serivceImageList;
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

            return  serviceResponse ;
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
    }
}
