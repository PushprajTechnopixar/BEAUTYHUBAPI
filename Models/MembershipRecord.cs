using System;
using System.Collections.Generic;

namespace BeautyHubAPI.Models
{
    public partial class MembershipRecord
    {
        public int MembershipRecordId { get; set; }
        public string? VendorId { get; set; }
        public int MembershipPlanId { get; set; }
        public bool PlanStatus { get; set; }
        public string? CreatedBy { get; set; }
        public int? PaymentReceiptId { get; set; }
        public string? PaymentMethod { get; set; }
        public DateTime ExpiryDate { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime? ModifyDate { get; set; }
        public int? SalonId { get; set; }
        public long? TransactionId { get; set; }

        public virtual UserDetail? CreatedByNavigation { get; set; }
        public virtual MembershipPlan MembershipPlan { get; set; } = null!;
        public virtual PaymentReceipt? PaymentReceipt { get; set; }
    }
}
