using System;
using System.Collections.Generic;

namespace BeautyHubAPI.Models
{
    public partial class SalonDetail
    {
        public SalonDetail()
        {
            Cart = new HashSet<Cart>();
            SalonBanner = new HashSet<SalonBanner>();
            CustomerSalon = new HashSet<CustomerSalon>();
            SalonSchedule = new HashSet<SalonSchedule>();
            SalonService = new HashSet<SalonService>();
            FavouriteSalon = new HashSet<FavouriteSalon>();
        }

        public int SalonId { get; set; }
        public string VendorId { get; set; } = null!;
        public int? BankId { get; set; }
        public string SalonName { get; set; } = null!;
        public string? SalonDescription { get; set; }
        public string? SalonImage { get; set; }
        public string Gstnumber { get; set; } = null!;
        public string? BusinessPan { get; set; }
        public string? SalonAddress { get; set; }
        public string? Landmark { get; set; }
        public string? City { get; set; }
        public string? Zip { get; set; }
        public int? Status { get; set; }
        public string? AddressLatitude { get; set; }
        public string? AddressLongitude { get; set; }
        public bool? IsDeleted { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime? ModifyDate { get; set; }
        public string? Upiid { get; set; }
        public string? PaymentQrcode { get; set; }
        public int? UpidetailId { get; set; }
        public bool? InventoryAdded { get; set; }
        public bool? HomerServiceStatus { get; set; }
        public string SalonType { get; set; } = null!;

        public virtual BankDetail? Bank { get; set; }
        public virtual ICollection<Cart> Cart { get; set; }
        public virtual Upidetail? Upidetail { get; set; }
        public virtual ICollection<SalonBanner> SalonBanner { get; set; }
        public virtual ICollection<CustomerSalon> CustomerSalon { get; set; }
        public virtual ICollection<SalonSchedule> SalonSchedule { get; set; }
        public virtual ICollection<SalonService> SalonService { get; set; }
        public virtual ICollection<FavouriteSalon> FavouriteSalon { get; set; }
    }
}
