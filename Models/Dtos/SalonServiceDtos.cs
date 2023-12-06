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

    public class IncludeServiceDTO
    {
        public int serviceId { get; set; }
        public string serviceName { get; set; }
        public string? serviceIconImage { get; set; }
        public double basePrice { get; set; }
        public double? discount { get; set; }
        public double listingPrice { get; set; }
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
    public class SetServiceStatusDTO
    {
        public int serviceId { get; set; }
        public int status { get; set; }
    }
}
