using System;
using System.Collections.Generic;

namespace BeautyHubAPI.Models
{
    public partial class Upidetail
    {
        public Upidetail()
        {
            SalonDetail = new HashSet<SalonDetail>();
        }

        public int UpidetailId { get; set; }
        public string? UserId { get; set; }
        public string? Upiid { get; set; }
        public string? Qrcode { get; set; }
        public string? AccountHolderName { get; set; }
        public bool? IsActive { get; set; }
        public bool? IsDeleted { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime? ModifyDate { get; set; }

        public virtual UserDetail? User { get; set; }
        public virtual ICollection<SalonDetail> SalonDetail { get; set; }
    }
}
