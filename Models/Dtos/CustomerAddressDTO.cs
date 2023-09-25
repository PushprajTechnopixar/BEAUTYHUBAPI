namespace BeautyHubAPI.Models.Dtos
{
    public class CustomerAddressDTO
    {
        public int customerAddressId { get; set; }
        public string customerUserId { get; set; } = null!;
        public string fullName { get; set; } = null!;
        public string phoneNumber { get; set; } = null!;
        public string? alternatePhoneNumber { get; set; }
        public string pincode { get; set; } = null!;
        public string state { get; set; } = null!;
        public string city { get; set; } = null!;
        public string houseNoOrBuildingName { get; set; } = null!;
        public string streetAddresss { get; set; } = null!;
        public string? nearbyLandMark { get; set; }
        public string addressType { get; set; } = null!;
        public string? addressLatitude { get; set; }
        public string? addressLongitude { get; set; }
        public bool status { get; set; }
        public string? distance { get; set; }
        public string? duration { get; set; }
        public DateTime crateDate { get; set; }
    }

    public class AddCustomerAddressRequestDTO
    {
        public string fullName { get; set; } = null!;
        public string phoneNumber { get; set; } = null!;
        public string? alternatePhoneNumber { get; set; }
        public string pincode { get; set; } = null!;
        public string state { get; set; } = null!;
        public string city { get; set; } = null!;
        public string houseNoOrBuildingName { get; set; } = null!;
        public string streetAddresss { get; set; } = null!;
        public string? nearbyLandMark { get; set; }
        public string addressType { get; set; } = null!;
        public string? addressLatitude { get; set; }
        public string? addressLongitude { get; set; }
    }

    public class UpdateCustomerAddressRequestDTO
    {
        public int customerAddressId { get; set; }
        public string fullName { get; set; } = null!;
        public string phoneNumber { get; set; } = null!;
        public string? alternatePhoneNumber { get; set; }
        public string pincode { get; set; } = null!;
        public string state { get; set; } = null!;
        public string city { get; set; } = null!;
        public string houseNoOrBuildingName { get; set; } = null!;
        public string streetAddresss { get; set; } = null!;
        public string? nearbyLandMark { get; set; }
        public string addressType { get; set; } = null!;
        public string? addressLatitude { get; set; }
        public string? addressLongitude { get; set; }
    }

    public class SerCustomerAddressRequestStatusDTO
    {
        public int customerAddressId { get; set; }
        public bool status { get; set; }
    }
}
