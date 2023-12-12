using System;
using System.Collections.Generic;

namespace BeautyHubAPI.Models
{
    public partial class Appointment
    {
        public Appointment()
        {
            BookedService = new HashSet<BookedService>();
        }

        public int AppointmentId { get; set; }
        public string CustomerUserId { get; set; } = null!;
        public int? ServiceInvoiceId { get; set; }
        public string? TransactionId { get; set; }
        public int? CouponId { get; set; }
        public string CustomerFirstName { get; set; } = null!;
        public string? CustomerLastName { get; set; }
        public string? AppointmentStatus { get; set; }
        public string? PaymentMethod { get; set; }
        public string? PaymentStatus { get; set; }
        public double? Cgst { get; set; }
        public double? Igst { get; set; }
        public double? Sgst { get; set; }
        public double? BasePrice { get; set; }
        public double? TotalPrice { get; set; }
        public double? FinalPrice { get; set; }
        public double? CancelledPrice { get; set; }
        public double? Discount { get; set; }
        public double? TotalDiscount { get; set; }
        public int? TotalServices { get; set; }
        public string? CustomerAddress { get; set; }
        public DateTime CreateDate { get; set; }
        public string? CancelledBy { get; set; }
        public string? PaymentReceipt { get; set; }
        public string? PhoneNumber { get; set; }
        public bool? IsUpdated { get; set; }

        public virtual ICollection<BookedService> BookedService { get; set; }
    }
}
