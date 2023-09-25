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
        public int? shopId { get; set; }
        public string? searchQuery { get; set; }
    }
    public class OrderFilterationListDTO
    {
        public int pageNumber { get; set; }
        public int pageSize { get; set; }
        public string? vendorId { get; set; }
        public int? shopId { get; set; }
        public string? deliveryType { get; set; }
        public string? paymentStatus { get; set; }
        public DateTime? fromDate { get; set; }
        public DateTime? toDate { get; set; }
        public string? AppointmentStatus { get; set; }
        public string? searchQuery { get; set; }
        public int? isDairyProduct { get; set; }
    }
    public class SubscriptionFilterationListDTO
    {
        public int pageNumber { get; set; }
        public int pageSize { get; set; }
        public int? shopId { get; set; }
        // public string? deliveryType { get; set; }
        public string? subscriptionType { get; set; }
        public DateTime? fromDate { get; set; }
        public DateTime? toDate { get; set; }
        public string? searchQuery { get; set; }
        public string? morningOrEveningOrder { get; set; }
    }

    public class CustomerOrderFilterationListDTO
    {
        public int pageNumber { get; set; }
        public int pageSize { get; set; }
        public string? deliveryType { get; set; }
        public string? paymentStatus { get; set; }
        public DateTime? fromDate { get; set; }
        public DateTime? toDate { get; set; }
        public string? AppointmentStatus { get; set; }
        public string? searchQuery { get; set; }
        public int? isDairyProduct { get; set; }
        public int? isSubscriptionProduct { get; set; }

    }

    public class DistributorEarning
    {
        public int pageNumber { get; set; }
        public int pageSize { get; set; }
        //public string Year { get; set; }
        //public string Month { get; set; }

    }
    public class DeliveryManFilterationDTO
    {
        public int pageNumber { get; set; }
        public int pageSize { get; set; }
        public int shopId { get; set; }
        public string? MorningOrEveningOrder { get; set; }
        public string? AppointmentStatus { get; set; }
        public int? isDairyProduct { get; set; }
        public string? searchQuery { get; set; }
        public DateTime? searchByDate { get; set; }
    }
}
