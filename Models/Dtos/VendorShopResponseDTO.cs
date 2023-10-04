using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace BeautyHubAPI.Models.Dtos
{
    public class VendorSalonResponseDTO
    {
        public string vendorId { get; set; }
        public string email { get; set; }
        public string firstName { get; set; }
        public string lastName { get; set; }
        public string? gender { get; set; }
        public string profilePic { get; set; }
        public string? dialCode { get; set; }
        public string phoneNumber { get; set; }
        public string password { get; set; }
        public int? countryId { get; set; }
        public int? stateId { get; set; }
        public string? countryName { get; set; }
        public string? stateName { get; set; }
        public string createdBy { get; set; }
        public string createdByRole { get; set; }
        public string createdByName { get; set; }
        public List<SalonResponseDTO> salonResponses { get; set; }
        public List<BankResponseDTO> bankResponses { get; set; }
        public List<UPIResponseDTO> upiResponses { get; set; }
        public GetMembershipRecordDTO membershipResponses { get; set; }
    }

    public class SuperAdminResponseDTO
    {
        public string id { get; set; }
        public string email { get; set; }
        public string firstName { get; set; }
        public string lastName { get; set; }
        public string? gender { get; set; }
        public string profilePic { get; set; }
        public string? dialCode { get; set; }
        public string phoneNumber { get; set; }
        public string password { get; set; }
        public int? countryId { get; set; }
        public int? stateId { get; set; }
        public string? countryName { get; set; }
        public string? stateName { get; set; }
        public string createdBy { get; set; }
        public string createdByName { get; set; }
        public List<BankResponseDTO> bankResponses { get; set; }
        public List<UPIResponseDTO> upiResponses { get; set; }
    }

    public class SalonResponseDTO
    {
        public int salonId { get; set; }
        public string salonName { get; set; } = null!;
        public string salonType { get; set; } = null!;
        public string? salonDescription { get; set; }
        public string? salonImage { get; set; }
        public string gstnumber { get; set; } = null!;
        public string? businessPAN { get; set; }
        public string? salonAddress { get; set; }
        public string? landmark { get; set; }
        public string? addressLatitude { get; set; }
        public string? addressLongitude { get; set; }
        public string? distance { get; set; }
        public string? duration { get; set; }
        public string? city { get; set; }
        public string? zip { get; set; }
        public int status { get; set; }
        public string statusDisplay { get; set; }
    }

    public class BankResponseDTO
    {
        public int bankId { get; set; }
        public string bankAccountHolderName { get; set; } = null!;
        public string bankAccountNumber { get; set; } = null!;
        public string? bankName { get; set; }
        public string ifsc { get; set; } = null!;
        public string? branchName { get; set; }
        public bool? isActive { get; set; }
    }

    public class UPIResponseDTO
    {
        public int upidetailId { get; set; }
        public string? userId { get; set; }
        public string? upiid { get; set; }
        public string? qrcode { get; set; }
        public bool? isActive { get; set; }
        // public string? bankName { get; set; }
        public string? accountHolderName { get; set; }
    }
}
