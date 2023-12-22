using System;
using System.Collections.Generic;

namespace BeautyHubAPI.Models.Dtos
{
    public partial class AddServiceToCartDTO
    {
        public int serviceId { get; set; }
        public int slotId { get; set; }
    }
    public partial class CartDetailDTO
    {
        public int totalItem { get; set; }
        public double totalMrp { get; set; }
        public double totalDiscount { get; set; }
        public double totalDiscountAmount { get; set; }
        public double totalSellingPrice { get; set; }
        public int salonCount { get; set; }
        public string morningDailyDeliveryTime { get; set; }
        public string eveningDailyDeliveryTime { get; set; }
        public List<CartDetailPerSalonDTO> allCartServices { get; set; }

    }
    public partial class CartDetailPerSalonDTO
    {
        public int salonTotalItem { get; set; }
        public double salonTotalMrp { get; set; }
        public double salonTotalDiscount { get; set; }
        public double salonTotalDiscountAmount { get; set; }
        public double salonTotalSellingPrice { get; set; }
        public string salonName { get; set; }
        public int salonId { get; set; }
        public string distance { get; set; }
        public string duration { get; set; }
        public List<CartServicesDTO> cartServices { get; set; }

    }
    public partial class UnavailableServicesPerSalonDTO
    {
        public string salonName { get; set; }
        public int salonId { get; set; }
        public string distance { get; set; }
        public string duration { get; set; }
        public List<CartServicesDTO> cartServices { get; set; }
    }
    public partial class CartServicesDTO
    {
        public int serviceId { get; set; }
        public string serviceName { get; set; }
        public string serviceImage { get; set; }
        public string? serviceDescription { get; set; }
        public double? basePrice { get; set; }
        public double? discount { get; set; }
        public double listingPrice { get; set; }
        public int durationInMinutes { get; set; }
        public int ServiceCountInCart { get; set; }
        // public int totalCountPerDuration { get; set; }
        public string genderPreferences { get; set; }
        public string ageRestrictions { get; set; }
        public int slotId { get; set; }
        public string slotDate { get; set; }
        public string fromTime { get; set; }
        public string toTime { get; set; }
        public string? serviceType { get; set; }
        public string? statusDisplay { get; set; }
        public bool favoritesStatus { get; set; }
        public bool slotStatus { get; set; }
        public int? isSlotAvailable { get; set; }

    }

}
