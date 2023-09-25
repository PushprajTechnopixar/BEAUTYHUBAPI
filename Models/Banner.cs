using System;
using System.Collections.Generic;

namespace BeautyHubAPI.Models
{
    public partial class Banner
    {
        public int BannerId { get; set; }
        public string? BannerImage { get; set; }
        public string? BannerType { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime? ModifyDate { get; set; }
    }
}
