using System;
using System.Collections.Generic;

namespace BeautyHubAPI.Models
{
    public partial class TimeSlot
    {
        public TimeSlot()
        {
            Cart = new HashSet<Cart>();
        }
        public int SlotId { get; set; }
        public int ServiceId { get; set; }
        public DateTime SlotDate { get; set; }
        public string FromTime { get; set; } = null!;
        public string ToTime { get; set; } = null!;
        public bool Status { get; set; }
        public int SlotCount { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime ModifyDate { get; set; }

        public virtual SalonService Service { get; set; } = null!;
        public virtual ICollection<Cart> Cart { get; set; }
    }
}
