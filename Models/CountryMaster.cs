using System;
using System.Collections.Generic;

namespace BeautyHubAPI.Models
{
    public partial class CountryMaster
    {
        public int CountryId { get; set; }
        public string CountryName { get; set; } = null!;
        public string CountryCode { get; set; } = null!;
        public string Timezone { get; set; } = null!;
    }
}
