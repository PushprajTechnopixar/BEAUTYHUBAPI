using System;
using System.Collections.Generic;

namespace BeautyHubAPI.Models
{
    public partial class FavouriteSalon
    {
        public int FavouriteSalonId { get; set; }
        public int SalonId { get; set; }
        public string CustomerUserId { get; set; } = null!;
        public DateTime? CreateDate { get; set; }

        public virtual UserDetail CustomerUser { get; set; } = null!;
        public virtual SalonDetail Salon { get; set; } = null!;
    }
}
