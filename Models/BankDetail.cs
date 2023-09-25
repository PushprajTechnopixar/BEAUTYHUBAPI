using System;
using System.Collections.Generic;

namespace BeautyHubAPI.Models
{
    public partial class BankDetail
    {
        public BankDetail()
        {
            SalonDetail = new HashSet<SalonDetail>();
        }

        public int BankId { get; set; }
        public string? UserId { get; set; }
        public string BankAccountHolderName { get; set; } = null!;
        public string BankAccountNumber { get; set; } = null!;
        public string? BankName { get; set; }
        public string Ifsc { get; set; } = null!;
        public string? BranchName { get; set; }
        public bool? IsActive { get; set; }
        public bool? IsDeleted { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime? ModifyDate { get; set; }

        public virtual UserDetail? User { get; set; }
        public virtual ICollection<SalonDetail> SalonDetail { get; set; }
    }
}
