using System;
using System.Collections.Generic;

namespace BeautyHubAPI.Models
{
    public partial class SalonBanner
    {
        public int SalonBannerId { get; set; }
        public int? SalonId { get; set; }
        public int? MainCategoryId { get; set; }
        public int? SubCategoryId { get; set; }
        public string? BannerType { get; set; }
        public bool? Male { get; set; }
        public bool? Female { get; set; }
        public string? BannerImage { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime? ModifyDate { get; set; }

        public virtual MainCategory? MainCategory { get; set; }
        public virtual SalonDetail? Salon { get; set; }
        public virtual SubCategory? SubCategory { get; set; }
    }
}
