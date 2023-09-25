using System;
using System.Collections.Generic;

namespace BeautyHubAPI.Models
{
    public partial class CustomerAddress
    {
        public int CustomerAddressId { get; set; }
        public string CustomerUserId { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public string PhoneNumber { get; set; } = null!;
        public string? AlternatePhoneNumber { get; set; }
        public string Pincode { get; set; } = null!;
        public string State { get; set; } = null!;
        public string City { get; set; } = null!;
        public string HouseNoOrBuildingName { get; set; } = null!;
        public string StreetAddresss { get; set; } = null!;
        public string? NearbyLandMark { get; set; }
        public string AddressType { get; set; } = null!;
        public string? AddressLatitude { get; set; }
        public string? AddressLongitude { get; set; }
        public bool Status { get; set; }
        public DateTime CrateDate { get; set; }
        public DateTime? ModifyDate { get; set; }

        public virtual UserDetail CustomerUser { get; set; } = null!;
    }
}
