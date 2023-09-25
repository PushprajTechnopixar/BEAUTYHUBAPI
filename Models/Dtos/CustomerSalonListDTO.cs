namespace BeautyHubAPI.Dtos
{
    public class CustomerSalonListDTO
    {
        public int salonId { get; set; }
        public string? salonType { get; set; }
        public string salonName { get; set; } = null!;
        public string vendorName { get; set; } = null!;
        public string? salonDescription { get; set; }
        public string? salonImage { get; set; }
        public string gstnumber { get; set; } = null!;
        public string? salonAddress { get; set; }
        public string? landmark { get; set; }
        public string? addressLatitude { get; set; }
        public string? addressLongitude { get; set; }
        public string? distance { get; set; }
        public string? duration { get; set; }
        // public string? morningDailyDeliveryTime { get; set; }
        // public string? morningOrderByTiming { get; set; }
        // public string? eveningDailyDeliveryTime { get; set; }
        // public string? eveningOrderByTiming { get; set; }
        public string? city { get; set; }
        public string? zip { get; set; }
        public bool isSalonAdded { get; set; }
        public bool favoritesStatus { get; set; }
        // public bool salonSubscriptionStatus { get; set; }
        // public double? walletAmount { get; set; }
        // public double? minimumWalletAmount { get; set; }
        // public string? walletPaymenyStatus { get; set; }
    }

    public class CustomerSalonBannersDTO
    {
        public int salonId { get; set; }
        public int mainProductCategoryId { get; set; }
        public int subProductCategoryId { get; set; }
        public int subSubProductCategoryId { get; set; }
        public string? salonBanner { get; set; }
    }

    public class AllCustomerSalonList
    {
        public List<CustomerSalonListDTO> customerSalonList { get; set; }
        public List<CustomerSalonListDTO> nearByCustomerSalonList { get; set; }
    }

    public class SetAreaCodeForCustomer
    {
        public int salonId { get; set; }
        public int areaCodeId { get; set; }
        public string customerUserId { get; set; }
    }
}
