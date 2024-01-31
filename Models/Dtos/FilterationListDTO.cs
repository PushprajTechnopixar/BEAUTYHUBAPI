namespace BeautyHubAPI.Models.Dtos
{
    public class FilterationListDTO
    {
        public int pageNumber { get; set; }
        public int pageSize { get; set; }
        public string? searchQuery { get; set; }
    }
    public class NullableFilterationListDTO
    {
        public int? pageNumber { get; set; }
        public int? pageSize { get; set; }
        public string? searchQuery { get; set; }
    }
    public class SalonServiceFilterationListDTO
    {
        public int pageNumber { get; set; }
        public int pageSize { get; set; }
        public int? salonId { get; set; }
        public int? mainCategoryId { get; set; }
        public int? subCategoryId { get; set; }
        public string? searchQuery { get; set; }
        public string? genderPreferences { get; set; }
        public string? ageRestrictions { get; set; }
        public string? serviceType { get; set; }
        public bool categoryWise { get; set; }
        public double? Discount { get; set; }
        public string? MaxOrMinDiscount { get; set; }
    }

    public class CategoryWiseServiceFilterationListDTO
    {
        public int salonId { get; set; }
        public string? genderPreferences { get; set; }
        public string? ageRestrictions { get; set; }
    }
    public class InventoryProductFilterationListDTO
    {
        public int pageNumber { get; set; }
        public int pageSize { get; set; }
        public int? mainProductCategoryId { get; set; }
        public int? subProductCategoryId { get; set; }
        public int? subSubProductCategoryId { get; set; }
        public int? brandId { get; set; }
        public string? productType { get; set; }
        public int? favoritesStatus { get; set; }
        public string? searchQuery { get; set; }
    }
    public class CollectionFilterationListDTO
    {
        public int pageNumber { get; set; }
        public int pageSize { get; set; }
        public string? vendorId { get; set; }
        public int? salonId { get; set; }
        public string? searchQuery { get; set; }
    }
    public class OrderFilterationListDTO
    {
        public int pageNumber { get; set; }
        public int pageSize { get; set; }
        public string? vendorId { get; set; }
        public int? salonId { get; set; }
        public string? paymentStatus { get; set; }
        public string? fromDate { get; set; }
        public string? toDate { get; set; }
        public int? sortDateBy { get; set; }
        public string? appointmentStatus { get; set; }
        public string? searchQuery { get; set; }
    }
    public class SubscriptionFilterationListDTO
    {
        public int pageNumber { get; set; }
        public int pageSize { get; set; }
        public int? salonId { get; set; }
        // public string? deliveryType { get; set; }
        public string? subscriptionType { get; set; }
        public DateTime? fromDate { get; set; }
        public DateTime? toDate { get; set; }
        public string? searchQuery { get; set; }
        public string? morningOrEveningOrder { get; set; }
    }

    public class CustomerAppointmentFilterationListDTO
    {
        public int pageNumber { get; set; }
        public int pageSize { get; set; }
        public string? paymentStatus { get; set; }
        public string? fromDate { get; set; }
        public string? toDate { get; set; }
        public int? sortDateBy { get; set; }
        public string? appointmentStatus { get; set; }
        public string? searchQuery { get; set; }
        public int? liveLocation { get; set; }
    }

    public class DistributorEarning
    {
        public int pageNumber { get; set; }
        public int pageSize { get; set; }
        //public string Year { get; set; }
        //public string Month { get; set; }

    }

}
