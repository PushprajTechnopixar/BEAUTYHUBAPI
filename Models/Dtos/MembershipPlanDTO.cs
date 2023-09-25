namespace BeautyHubAPI.Models.Dtos
{
    public class MembershipPlanDTO
    {
        public int membershipPlanId { get; set; }
        public string planName { get; set; }
        public string planDescription { get; set; }
        public double planPrice { get; set; }
        public bool isPopular { get; set; }
        // public string currency { get; set; }
        public int planDuration { get; set; }
        public double? gstinPercentage { get; set; }
        public string? gsttype { get; set; }
        public int? planType { get; set; }
        public double? discountInPercentage { get; set; }
    }

    public partial class GetMembershipPlanDTO
    {
        public int membershipPlanId { get; set; }
        public string planName { get; set; }
        public string planDescription { get; set; }
        public double planPrice { get; set; }
        // public string currency { get; set; } = "USD";
        public int planDuration { get; set; }
        public bool? isPopular { get; set; }
        public string planDurationName { get; set; }
        public double? gstinPercentage { get; set; }
        public string? gsttype { get; set; }
        public double? totalAmount { get; set; }
        public double? gsttax { get; set; }
        public double? discountInPercentage { get; set; }
        public string createDate { get; set; }
        public int? planType { get; set; }
    }

    public partial class GetMembershipPlanListDTO
    {
        public int membershipPlanId { get; set; }
        public string planName { get; set; }
        public string planDescription { get; set; }
        public double planPrice { get; set; }
        // public string currency { get; set; }
        public int planDuration { get; set; }
        public bool? isPopular { get; set; }
        public bool? isActive { get; set; }
        public string planDurationName { get; set; }
        public string createDate { get; set; }
        public string expiryDate { get; set; }
        public double? gstinPercentage { get; set; }
        public string? gsttype { get; set; }
        public double? totalAmount { get; set; }
        public double? gsttax { get; set; }
        public double? discountInPercentage { get; set; }
        public int? planType { get; set; }
    }
}
