using System;
using System.Collections.Generic;

namespace BeautyHubAPI.Models
{
    public partial class SalonService
    {
        public SalonService()
        {
            TimeSlot = new HashSet<TimeSlot>();
            FavouriteService = new HashSet<FavouriteService>();
            Cart = new HashSet<Cart>();
            ServicePackage = new HashSet<ServicePackage>();
        }

        public int ServiceId { get; set; }
        public int SalonId { get; set; }
        public string ServiceName { get; set; } = null!;
        public string? ServiceIconImage { get; set; }
        public string? ServiceImage1 { get; set; }
        public string? ServiceImage2 { get; set; }
        public string? ServiceImage3 { get; set; }
        public string? ServiceImage4 { get; set; }
        public string? ServiceImage5 { get; set; }
        public string? ServiceDescription { get; set; }
        public int MainCategoryId { get; set; }
        public int? SubcategoryId { get; set; }
        public double? BasePrice { get; set; }
        public double? Discount { get; set; }
        public double ListingPrice { get; set; }
        public int DurationInMinutes { get; set; }
        public int TotalCountPerDuration { get; set; }
        public string GenderPreferences { get; set; } = null!;
        public string AgeRestrictions { get; set; } = null!;
        public int? Status { get; set; }
        public string? LockTimeStart { get; set; }
        public string? LockTimeEnd { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime ModifyDate { get; set; }
        public string? ServiceType { get; set; }

        public virtual ICollection<FavouriteService> FavouriteService { get; set; }
        public virtual MainCategory MainCategory { get; set; } = null!;
        public virtual SalonDetail Salon { get; set; } = null!;
        //public virtual SubCategory? Subcategory { get; set; }
        public virtual ICollection<TimeSlot> TimeSlot { get; set; }
        public virtual ICollection<Cart> Cart { get; set; }
        public virtual ICollection<ServicePackage> ServicePackage { get; set; }

    }
}
