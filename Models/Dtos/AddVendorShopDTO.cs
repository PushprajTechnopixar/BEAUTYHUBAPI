using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace BeautyHubAPI.Models.Dtos
{
    public class AddVendorSalonDTO
    {
        // Vendor detail
        public int? membershipRecordId { get; set; }
        [Required]
        [EmailAddress]
        public string email { get; set; }
        [Required]
        public string firstName { get; set; }
        public string lastName { get; set; }
        [Required]
        public string? gender { get; set; }
        [Required]
        public string? dialCode { get; set; }
        public string? deviceType { get; set; }
        [Required]
        public string phoneNumber { get; set; }
        public int countryId { get; set; }
        public int stateId { get; set; }
        public List<AddSalonDTO> salonDetail { get; set; }
        public List<AddBankDTO> bankDetail { get; set; }
        public List<AddUPIDTO> upiDetail { get; set; }
    }

    public class AddSalonDTO
    {
        // Salon detail
        public string salonName { get; set; } = null!;
        public string salonType { get; set; } = null!;
        public string? salonDescription { get; set; }
        public string gstnumber { get; set; } = null!;
        public string? businessPAN { get; set; }
        public string? salonAddress { get; set; }
        public string? landmark { get; set; }
        [Required]
        public string? addressLatitude { get; set; }
        [Required]
        public string? addressLongitude { get; set; }
        public string? city { get; set; }
        public string? zip { get; set; }
    }

    public class AddBankDTO
    {
        public string bankAccountHolderName { get; set; } = null!;
        public string bankAccountNumber { get; set; } = null!;
        public string? bankName { get; set; }
        public string ifsc { get; set; } = null!;
        public bool? isActive { get; set; }
        public string? branchName { get; set; }
    }

    public class AddUPIDTO
    {
        // public string? userId { get; set; }
        public string? upiid { get; set; }
        public string? accountHolderName { get; set; }
        public bool? isActive { get; set; }
    }

    public class VendorListDTO
    {
        public string vendorId { get; set; }
        public int salonId { get; set; }
        public string vendorName { get; set; }
        public string salonName { get; set; }
        public string salonType { get; set; }
        public string profilePic { get; set; }
        public string statusDisplay { get; set; }
        public string createdBy { get; set; }
        public string createdByRole { get; set; }
        public string createdById { get; set; }
        public string createDate { get; set; }
    }

    public class DistributorEarningDTO
    {
        public string vendorId { get; set; }
        public string vendorName { get; set; }
        public string createDate { get; set; }
        public string createdBy { get; set; }
        public string planName { get; set; }
        public double rewardAmount { get; set; }
    }

    public class DistributorEarningResponse
    {
        public List<DistributorEarningDTO> earningResponse { get; set; }

        public string totalEarning { get; set; }
    }
}
