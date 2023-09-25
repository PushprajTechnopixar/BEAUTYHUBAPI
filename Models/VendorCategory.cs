using System;
using System.Collections.Generic;

namespace BeautyHubAPI.Models
{
    public partial class VendorCategory
    {
        public int VendorCategoryId { get; set; }
        public string VendorId { get; set; } = null!;
        public int? SalonId { get; set; }
        public int? MainCategoryId { get; set; }
        public int? SubCategoryId { get; set; }
        public bool? Status { get; set; }
        public DateTime CreateDate { get; set; }
        public bool? Male { get; set; }
        public bool? Female { get; set; }

        public virtual MainCategory? MainCategory { get; set; }
        public virtual SubCategory? SubCategory { get; set; }
        public virtual UserDetail Vendor { get; set; } = null!;
    }
}
