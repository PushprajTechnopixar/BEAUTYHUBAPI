using System;
using System.Collections.Generic;

namespace BeautyHubAPI.Models
{
    public partial class BookedService
    {
        public int BookedServiceId { get; set; }
        public int? CouponId { get; set; }
        public int AppointmentId { get; set; }
        public int ServiceId { get; set; }
        public string? ServiceName { get; set; }
        public string? ServiceImage { get; set; }
        public double? BasePrice { get; set; }
        public double? ListingPrice { get; set; }
        public double? Discount { get; set; }
        public double? TotalDiscount { get; set; }
        public double? FinalPrice { get; set; }
        public double? CancelledPrice { get; set; }
        public double? TotalPrice { get; set; }
        public string? VendorId { get; set; }
        public int? SalonId { get; set; }
        public string? VendorName { get; set; }
        public string? SalonName { get; set; }
        public DateTime AppointmentDate { get; set; }
        public string FromTime { get; set; } = null!;
        public string ToTime { get; set; } = null!;
        public int? DurationInMinutes { get; set; }
        public int? ServiceCountInCart { get; set; }
        public DateTime CreateDate { get; set; }
        public string? AppointmentStatus { get; set; }
        public int? SlotId { get; set; }

        public virtual Appointment Appointment { get; set; } = null!;
    }
}
