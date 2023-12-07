using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace BeautyHubAPI.Models.Dtos
{
    public class UpdateVendorSalonDTO
    {
        // Vendor detail
        public string vendorId { get; set; }
        public string email { get; set; }
        public string firstName { get; set; }
        public string lastName { get; set; }
        public string? gender { get; set; }
        public string? dialCode { get; set; }
        public string phoneNumber { get; set; }
        public int countryId { get; set; }
        public int stateId { get; set; }
        public List<UpdateSalonDTO> salonDetail { get; set; }
        public List<UpdateBankDTO> bankDetail { get; set; }
        public List<UpdateUPIDTO> upiDetail { get; set; }
    }

    public class UpdateSuperAdminDTO
    {
        // Vendor detail
        public string id { get; set; }
        public string email { get; set; }
        public string firstName { get; set; }
        public string lastName { get; set; }
        public string? gender { get; set; }
        public string? dialCode { get; set; }
        public string phoneNumber { get; set; }
        public int countryId { get; set; }
        public int stateId { get; set; }
        public List<UpdateBankDTO> bankDetail { get; set; }
        public string? upiDetail { get; set; }

        // public List<UpdateUPIDTO> upiDetail { get; set; }
    }

    public class UpdateSalonDTO
    {
        // Salon detail
        public int salonId { get; set; }
        public string salonName { get; set; } = null!;
        public string salonType { get; set; } = null!;
        public string? salonDescription { get; set; }
        public string gstnumber { get; set; } = null!;
        public string? businessPAN { get; set; }
        public string? salonAddress { get; set; }
        public string? landmark { get; set; }
        public string? addressLatitude { get; set; }
        public string? addressLongitude { get; set; }
        public string? city { get; set; }
        public string? zip { get; set; }
    }

    public class UpdateBankDTO
    {
        public int bankId { get; set; }
        public string bankAccountHolderName { get; set; } = null!;
        public string bankAccountNumber { get; set; } = null!;
        public string? bankName { get; set; }
        public string ifsc { get; set; } = null!;
        public string? branchName { get; set; }
        public bool? isActive { get; set; }
    }

    public class UpdateUPIDTO
    {
        public int upidetailId { get; set; }
        public string? upiid { get; set; }
        public string? qrcode { get; set; }
        public bool? isActive { get; set; }
        public string? accountHolderName { get; set; }
    }
}
