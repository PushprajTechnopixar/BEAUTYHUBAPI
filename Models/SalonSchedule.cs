using System;
using System.Collections.Generic;

namespace BeautyHubAPI.Models
{
    public partial class SalonSchedule
    {
        public int SalonScheduleId { get; set; }
        public int SalonId { get; set; }
        public bool? Monday { get; set; }
        public bool? Tuesday { get; set; }
        public bool? Wednesday { get; set; }
        public bool? Thursday { get; set; }
        public bool? Friday { get; set; }
        public bool? Saturday { get; set; }
        public bool? Sunday { get; set; }
        public string? FromTime { get; set; }
        public string? ToTime { get; set; }
        public bool IsAvailable { get; set; }
        public int? DeletedBy { get; set; }
        public DateTime DeletedDate { get; set; }
        public bool IsDeleted { get; set; }
        public bool Status { get; set; }
        public bool UpdateStatus { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? ModifyDate { get; set; }

        public virtual SalonDetail Salon { get; set; } = null!;
    }
}
