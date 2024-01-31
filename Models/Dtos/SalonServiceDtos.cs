namespace BeautyHubAPI.Models
{
    public partial class AddUpdateSalonServiceDTO
    {
        public int serviceId { get; set; }
        public int salonId { get; set; }
        public string serviceName { get; set; } = null!;
        // public string? serviceImage { get; set; }
        public string? serviceDescription { get; set; }
        public int? mainCategoryId { get; set; }
        public int? subCategoryId { get; set; }
        public double? basePrice { get; set; }
        public double? discount { get; set; }
        public double listingPrice { get; set; }
        public int durationInMinutes { get; set; }
        public int totalCountPerDuration { get; set; }
        public string genderPreferences { get; set; }
        public string ageRestrictions { get; set; }
        public string? lockTimeStart { get; set; }
        public string? lockTimeEnd { get; set; }
        public int? status { get; set; }
        public string? ServiceType { get; set; }
        public string? IncludeServiceId { get; set; }
    }

    public partial class GetSalonServiceDTO
    {
        public int serviceId { get; set; }
        public int salonId { get; set; }
        public string serviceName { get; set; } = null!;
        public string? serviceImage { get; set; }
        public string? serviceDescription { get; set; }
        public int mainCategoryId { get; set; }
        public int? subCategoryId { get; set; }
        public double? basePrice { get; set; }
        public double? discount { get; set; }
        public double listingPrice { get; set; }
        public int durationInMinutes { get; set; }
        public int totalCountPerDuration { get; set; }
        public int? status { get; set; }
        public string genderPreferences { get; set; } = null!;
        public string ageRestrictions { get; set; } = null!;
        public string createDate { get; set; }
    }

    public class timeSlotsDTO
    {
        public int slotId { get; set; }
        public string fromTime { get; set; } = null!;
        public string toTime { get; set; } = null!;
        public bool status { get; set; }
        public int slotCount { get; set; }
    }

    public class SalonServiceListDTO
    {
        public int serviceId { get; set; }
        public string vendorId { get; set; } = null!;
        public int? salonId { get; set; }
        public string? salonName { get; set; }
        public int? mainCategoryId { get; set; }
        public string? mainCategoryName { get; set; }
        public int? subCategoryId { get; set; }
        public string? subCategoryName { get; set; }
        public string serviceName { get; set; }
        public string? serviceDescription { get; set; }
        public string? serviceImage { get; set; }
        public double? discount { get; set; }
        public double listingPrice { get; set; }
        public double basePrice { get; set; }
        public int? totalCountPerDuration { get; set; }
        public int? durationInMinutes { get; set; }
        public int? serviceCountInCart { get; set; }
        public int? status { get; set; }
        public int? isSlotAvailable { get; set; }
        public string? genderPreferences { get; set; }
        public string? ageRestrictions { get; set; }
        public bool favoritesStatus { get; set; }
        public string? ServiceType { get; set; }
        public double? discountInPercentage { get; set; }
        // public List<SalonPackageServiceListDTO> package { get; set; }
    }

    // public class SalonPackageServiceListDTO
    // {
    //     public int serviceId { get; set; }
    //     public string vendorId { get; set; } = null!;
    //     public int? salonId { get; set; }
    //     public string? salonName { get; set; }
    //     public int? mainCategoryId { get; set; }
    //     public string? mainCategoryName { get; set; }
    //     public int? subCategoryId { get; set; }
    //     public string? subCategoryName { get; set; }
    //     public string serviceName { get; set; }
    //     public string? serviceDescription { get; set; }
    //     public string? serviceImage { get; set; }
    //     public double? discount { get; set; }
    //     public double listingPrice { get; set; }
    //     public double basePrice { get; set; }
    //     public int? totalCountPerDuration { get; set; }
    //     public int? serviceCountInCart { get; set; }
    //     public int? status { get; set; }
    //     public int? isSlotAvailable { get; set; }
    //     public string? genderPreferences { get; set; }
    //     public string? ageRestrictions { get; set; }
    //     public bool favoritesStatus { get; set; }
    // }


    public class serviceDetailDTO
    {
        public int serviceId { get; set; }
        public string vendorId { get; set; } = null!;
        public int? salonId { get; set; }
        public string? salonName { get; set; }
        public string? vendorName { get; set; }
        public string? brandName { get; set; }
        public int? mainCategoryId { get; set; }
        public string? mainCategoryName { get; set; }
        public int? subCategoryId { get; set; }
        public string? subCategoryName { get; set; }
        public string serviceName { get; set; }
        public string? serviceDescription { get; set; }
        public List<ServiceImageDTO>? serviceImage { get; set; }
        public string? serviceIconImage { get; set; }
        public double basePrice { get; set; }
        public double? discount { get; set; }
        public double listingPrice { get; set; }
        public int? status { get; set; }
        public bool? favouriteStatus { get; set; }
        public int? serviceCountInCart { get; set; }
        public int? totalCountPerDuration { get; set; }
        public int? isSlotAvailable { get; set; }
        public string? genderPreferences { get; set; }
        public string? ageRestrictions { get; set; }
        public int DurationInMinutes { get; set; }
        public string? LockTimeStart { get; set; }
        public string? LockTimeEnd { get; set; }
        public string? IncludeServiceId { get; set; }
        public List<IncludeServiceDTO> IncludeService { get; set; }

    }

    public class ServiceFilterationListDTO
    {
        public int? salonId { get; set; }
        public int? mainCategoryId { get; set; }
        public int? subCategoryId { get; set; }
        public int? brandId { get; set; }
        public double? Discount { get; set; }
        public string? DiscountType { get; set; }
        public string? MaxOrMinDiscount { get; set; }
        public string? genderPreferences { get; set; }
        public string? ageRestrictions { get; set; }
        public string? serviceType { get; set; }
        public bool categoryWise { get; set; }
        public string? searchQuery { get; set; }
    }

    public class DashboardServiceFilterationListDTO
    {
        public int? salonId { get; set; }
        public string? genderPreferences { get; set; }
        public string? ageRestrictions { get; set; }
    }

    public class IncludeServiceDTO
    {
        public int serviceId { get; set; }
        public string serviceName { get; set; }
        public string? serviceIconImage { get; set; }
        public double basePrice { get; set; }
        public double? discount { get; set; }
        public double listingPrice { get; set; }
        public string? genderPreferences { get; set; }

    }

    public class ServiceImageDTO
    {
        public string? salonServiceImage { get; set; }
    }

    public class SetSalonServiceFavouriteStatusDTO
    {
        public int serviceId { get; set; }
        public bool status { get; set; }
    }
    public class SetFavouriteSalon
    {
        public int salonId { get; set; }
        public bool status { get; set; }
    }
    public class SetFavouriteService
    {
        public int serviceId { get; set; }
        public bool status { get; set; }
    }
    public class SetServiceStatusDTO
    {
        public int serviceId { get; set; }
        public int status { get; set; }
    }

    public class upcomingScheduleDTO
    {
        public string date { get; set; }
        public string day { get; set; }
        public int slotCount { get; set; }
    }
    public class UpcomingScheduleDetailDTO
    {
        public int serviceId { get; set; }
        public string appointmentDate { get; set; }
        public string serviceName { get; set; }
        public int serviceCountInCart { get; set; }
        public string fromTime { get; set; }
        public string toTime { get; set; }
        public int slotId { get; set; }
        public double listingPrice { get; set; }
        public string serviceImage { get; set; }

    }
    public class cancelUpcomingScheduleDTO
    {
        public int serviceId { get; set; }
        public string appointmentDate { get; set; }
        public bool cancelAllAppointments { get; set; }
    }

    public partial class DashboardSalonServiceCategoryDTO
    {
        public int MainCategoryId { get; set; }
        public int SubCategoryId { get; set; }
        public int SubSubCategoryId { get; set; }
        public string CategoryName { get; set; }
        public string CategoryImage { get; set; }
        public double? Discount { get; set; }
        public string? MaxOrMinDiscount { get; set; }

    }

    public class CustomerDashboardServiceCategoryrDTO
    {
        public string? name { get; set; }
        public string? description { get; set; }
        public string? type { get; set; }
        public int MainCategoryId { get; set; }
        public int SubCategoryId { get; set; }
        public List<DashboardSalonServiceCategoryDTO> dashboarCategory { get; set; }

    }

    public class CustomerDashboardBannerDTO
    {
        public string? name { get; set; }
        public string? description { get; set; }
        public string? type { get; set; }
        public List<GetDashboardSalonBannerDTO> dashboardSalonBanner { get; set; }

    }

    public partial class GetDashboardSalonBannerDTO
    {
        public int salonBannerId { get; set; }
        public int? salonId { get; set; }
        public int? mainCategoryId { get; set; }
        public int? subCategoryId { get; set; }
        // public int? brandId { get; set; }
        public string? mainCategoryName { get; set; }
        public string? subCategoryName { get; set; }
        // public string? brandName { get; set; }
        public string? bannerType { get; set; }
        public bool? male { get; set; }
        public bool? female { get; set; }
        public string? bannerTypeName { get; set; }
        public string? bannerImage { get; set; }
        public string? createDate { get; set; }
    }


    public class CustomerDashboardViewModel
    {
        public CustomerDashboardServiceCategoryrDTO mainCategoryList { get; set; }
        public CustomerDashboardSalonServiceDTO bestPackages { get; set; }
        public CustomerDashboardSalonServiceDTO maxServiceOffer { get; set; }
        public CustomerDashboardSalonServiceDTO minServiceOffer { get; set; }
        public CustomerDashboardSalonServiceDTO categoryWiseServices1 { get; set; }
        public CustomerDashboardSalonServiceDTO categoryWiseServices2 { get; set; }
        public CustomerDashboardSalonServiceDTO recommendedForYou { get; set; }
        public CustomerDashboardSalonServiceDTO categoryWiseServices3 { get; set; }
        public CustomerDashboardSalonServiceDTO categoryWiseServices4 { get; set; }
        public CustomerDashboardSalonServiceDTO suggestedForYou { get; set; }
        public CustomerDashboardSalonServiceDTO youMayLike { get; set; }
        public CustomerDashboardSalonServiceDTO categoryWiseServices5 { get; set; }
        public CustomerDashboardSalonServiceDTO categoryWiseServices6 { get; set; }
        public CustomerDashboardSalonServiceDTO categoryWiseServices7 { get; set; }
        public CustomerDashboardServiceCategoryrDTO subCategoriesOfferMin { get; set; }
        public CustomerDashboardSalonServiceDTO servicesInYourCart { get; set; }
        public CustomerDashboardServiceCategoryrDTO mainCategoriesOfferMin { get; set; }
        public CustomerDashboardSalonServiceDTO newlyLaunched { get; set; }
        public CustomerDashboardBannerDTO categoryBanner1 { get; set; }
        public CustomerDashboardBannerDTO categoryBanner2 { get; set; }
        public CustomerDashboardSalonServiceDTO favourites { get; set; }
        public CustomerDashboardBannerDTO categoryBanner3 { get; set; }
        public CustomerDashboardSalonServiceDTO RecentlyViewed { get; set; }
        public CustomerDashboardBannerDTO categoryBanner4 { get; set; }
        public CustomerDashboardBannerDTO categoryBanner5 { get; set; }
        public CustomerDashboardBannerDTO salonBanner { get; set; }
    }

    public class CustomerDashboardSalonServiceDTO
    {
        public string? name { get; set; }
        public string? description { get; set; }
        public string? type { get; set; }
        public int MainCategoryId { get; set; }
        public int SubCategoryId { get; set; }
        public double? Discount { get; set; }
        public string? MaxOrMinDiscount { get; set; }
        public List<SalonServiceListDTO>? serviceListDTO { get; set; }
    }

}
