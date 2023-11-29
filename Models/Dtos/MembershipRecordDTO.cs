using System;
using System.Collections.Generic;

namespace BeautyHubAPI.Models.Dtos
{
    public partial class buyMembershipPlanDTO
    {
        public string? vendorId { get; set; }
        public int? salonId { get; set; }
        public int membershipPlanId { get; set; }
        // public long transactionId { get; set; }
        public int paymentReceiptId { get; set; }
        public string paymentMethod { get; set; }
        public string? createdBy { get; set; }
    }
    public partial class GetMembershipRecordDTO
    {
        public int membershipRecordId { get; set; }
        public int membershipPlanId { get; set; }
        public string planName { get; set; }
        public bool planStatus { get; set; }
        public string expiryDate { get; set; }
        public string createDate { get; set; }
        // public string priceCurrency { get; set; }
        public double totalAmount { get; set; }
        public long? transactionId { get; set; }
    }

    public partial class GetMembershipPlanDetailByTransactionId
    {
        public long transactionId { get; set; }
        public double transactionAmount { get; set; }
        public bool transactionStatus { get; set; }
        public int membershipRecordId { get; set; }
        public int membershipPlanId { get; set; }
        public string planName { get; set; }
        public bool planStatus { get; set; }
        // public string expiryDate { get; set; }
        // public string createDate { get; set; }
    }
}
