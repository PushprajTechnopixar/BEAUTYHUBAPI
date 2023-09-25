using System;
using System.Collections.Generic;

namespace BeautyHubAPI.Models
{
    public partial class PaymentReceipt
    {
        public PaymentReceipt()
        {
            MembershipRecord = new HashSet<MembershipRecord>();
        }

        public int PaymentReceiptId { get; set; }
        public string? UserId { get; set; }
        public string? PaymentReceiptImage { get; set; }
        public DateTime CreateDate { get; set; }

        public virtual UserDetail? User { get; set; }
        public virtual ICollection<MembershipRecord> MembershipRecord { get; set; }
    }
}
