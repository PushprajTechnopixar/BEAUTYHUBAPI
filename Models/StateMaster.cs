using System;
using System.Collections.Generic;

namespace BeautyHubAPI.Models
{
    public partial class StateMaster
    {
        public StateMaster()
        {
            CityMaster = new HashSet<CityMaster>();
        }

        public int StateId { get; set; }
        public string StateName { get; set; } = null!;
        public int CountryId { get; set; }

        public virtual ICollection<CityMaster> CityMaster { get; set; }
    }
}
