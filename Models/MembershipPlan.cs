using System;
using System.Collections.Generic;

namespace BeautyHubAPI.Models
{
    public partial class MembershipPlan
    {
        public MembershipPlan()
        {
            MembershipRecord = new HashSet<MembershipRecord>();
        }

        public int MembershipPlanId { get; set; }
        public string PlanName { get; set; } = null!;
        public string? PlanDescription { get; set; }
        public double PlanPrice { get; set; }
        public int PlanDuration { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public bool? IsPopular { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime? ModifyDate { get; set; }
        public double? GstinPercentage { get; set; }
        public string? Gsttype { get; set; }
        public double? TotalAmount { get; set; }
        public double? Gsttax { get; set; }
        public double? DiscountInPercentage { get; set; }
        public int? PlanType { get; set; }

        public virtual ICollection<MembershipRecord> MembershipRecord { get; set; }
    }
}
