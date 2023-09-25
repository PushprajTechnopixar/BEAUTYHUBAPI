using System;
using System.Collections.Generic;

namespace BeautyHubAPI.Models
{
    public partial class CustomerSalon
    {
        public int CustomerSalonId { get; set; }
        public string? CustomerUserId { get; set; }
        public int? SalonId { get; set; }
        public bool? Status { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime? ModifyDate { get; set; }

        public virtual UserDetail? CustomerUser { get; set; }
        public virtual SalonDetail? Salon { get; set; }
    }
}
