using System;
using System.Collections.Generic;

namespace BeautyHubAPI.Models
{
    public partial class CityMaster
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public int StateId { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime? ModifyDate { get; set; }

        public virtual StateMaster State { get; set; } = null!;
    }
}
