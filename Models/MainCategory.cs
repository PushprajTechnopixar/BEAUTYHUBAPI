using System;
using System.Collections.Generic;

namespace BeautyHubAPI.Models
{
    public partial class MainCategory
    {
        public MainCategory()
        {
            SalonBanner = new HashSet<SalonBanner>();
            SubCategory = new HashSet<SubCategory>();
            VendorCategory = new HashSet<VendorCategory>();
            SalonService = new HashSet<SalonService>();
        }

        public int MainCategoryId { get; set; }
        public string CategoryName { get; set; } = null!;
        public string CategoryDescription { get; set; } = null!;
        public string? CategoryImageMale { get; set; }
        public string? CategoryImageFemale { get; set; }
        public int? CategoryStatus { get; set; }
        public string CreatedBy { get; set; } = null!;
        public string? ModifiedBy { get; set; }
        public bool? Male { get; set; }
        public bool? Female { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime? ModifyDate { get; set; }

        public virtual ICollection<SalonBanner> SalonBanner { get; set; }
        public virtual ICollection<SubCategory> SubCategory { get; set; }
        public virtual ICollection<VendorCategory> VendorCategory { get; set; }
        public virtual ICollection<SalonService> SalonService { get; set; }
    }
}
