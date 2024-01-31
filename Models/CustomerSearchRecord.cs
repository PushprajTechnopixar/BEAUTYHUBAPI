using System;
using System.Collections.Generic;

namespace BeautyHubAPI.Models
{
    public partial class CustomerSearchRecord
    {
        public int RecordId { get; set; }
        public string? CustomerUserId { get; set; }
        public string? CustomerSearchItem { get; set; }
        public int? MaincategoryId { get; set; }
        public int? SubcategoryId { get; set; }
        public DateTime CreateDate { get; set; }

        public virtual UserDetail? CustomerUser { get; set; }
    }
}
