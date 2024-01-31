using System;
using System.Collections.Generic;

namespace BeautyHubAPI.Models
{
    public partial class UserDetail
    {
        public UserDetail()
        {
            Cart = new HashSet<Cart>();
            FavouriteService = new HashSet<FavouriteService>();
            BankDetail = new HashSet<BankDetail>();
            MembershipRecord = new HashSet<MembershipRecord>();
            PaymentReceipt = new HashSet<PaymentReceipt>();
            Upidetail = new HashSet<Upidetail>();
            CustomerSalon = new HashSet<CustomerSalon>();
            CustomerAddress = new HashSet<CustomerAddress>();
            VendorCategory = new HashSet<VendorCategory>();
            NotificationSent = new HashSet<NotificationSent>();
            FavouriteSalon = new HashSet<FavouriteSalon>();
            CustomerSearchRecord = new HashSet<CustomerSearchRecord>();

        }

        public string UserId { get; set; } = null!;
        public string? Gender { get; set; } = null!;
        public string? Pan { get; set; }
        public int? CountryId { get; set; }
        public int? StateId { get; set; }
        public string? DialCode { get; set; }
        public int? EmailOtp { get; set; }
        public string? ProfilePic { get; set; }
        public string? DeviceType { get; set; }
        public string? CreatedBy { get; set; }
        public bool? IsDeleted { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime? ModifyDate { get; set; }
        public string? Fcmtoken { get; set; }
        public bool IsNotificationEnabled { get; set; }
        public string? AddressLongitude { get; set; }
        public string? AddressLatitude { get; set; }

        public virtual ICollection<Cart> Cart { get; set; }
        public virtual ICollection<FavouriteService> FavouriteService { get; set; }
        public virtual ICollection<BankDetail> BankDetail { get; set; }
        public virtual ICollection<MembershipRecord> MembershipRecord { get; set; }
        public virtual ICollection<PaymentReceipt> PaymentReceipt { get; set; }
        public virtual ICollection<Upidetail> Upidetail { get; set; }
        public virtual ICollection<CustomerSalon> CustomerSalon { get; set; }
        public virtual ICollection<CustomerAddress> CustomerAddress { get; set; }
        public virtual ICollection<VendorCategory> VendorCategory { get; set; }
        public virtual ICollection<NotificationSent> NotificationSent { get; set; }
        public virtual ICollection<FavouriteSalon> FavouriteSalon { get; set; }
        public virtual ICollection<CustomerSearchRecord> CustomerSearchRecord { get; set; }

    }
}
