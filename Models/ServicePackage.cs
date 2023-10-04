using System;
using System.Collections.Generic;

namespace BeautyHubAPI.Models
{
    public partial class ServicePackage
    {
        public int ServicePackageId { get; set; }
        public int ServiceId { get; set; }
        public string IncludeServiceId { get; set; } = null!;
        public int SalonId { get; set; }
        public DateTime? CreateDate { get; set; }

        public virtual SalonService PackageService { get; set; } = null!;
    }
}
