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



        public async Task<Object> customerServiceList([FromQuery] SalonServiceFilterationListDTO model, string currentUserId)
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

        public async Task<Object> vendorServiceList([FromQuery] SalonServiceFilterationListDTO model, string currentUserId)
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

            return serviceResponse;
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