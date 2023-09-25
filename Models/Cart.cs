using System;
using System.Collections.Generic;

namespace BeautyHubAPI.Models
{
    public partial class Cart
    {
        public int CartId { get; set; }
        public int ServiceId { get; set; }
        public string CustomerUserId { get; set; } = null!;
        public int? SlotId { get; set; }
        public int? SalonId { get; set; }
        public int? ServiceCountInCart { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime? ModifyDate { get; set; }

        public virtual UserDetail CustomerUser { get; set; } = null!;
        public virtual SalonDetail? Salon { get; set; }
        public virtual SalonService Service { get; set; } = null!;
        public virtual TimeSlot? Slot { get; set; }
    }
}
