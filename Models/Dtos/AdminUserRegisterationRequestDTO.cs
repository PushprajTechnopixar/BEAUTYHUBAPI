using System.ComponentModel.DataAnnotations;

namespace BeautyHubAPI.Models.Dtos
{
    public class AdminUserRegisterationRequestDTO
    {
        [Required]
        [EmailAddress]
        public string email { get; set; }
        [Required]
        public string firstName { get; set; }
        public string lastName { get; set; }
        [Required]
        public string gender { get; set; }
        [Required]
        public string? dialCode { get; set; }
        [Required]
        public string phoneNumber { get; set; }
        public string? deviceType { get; set; }
        public int countryId { get; set; }
        public int stateId { get; set; }
    }

    public class UpdateAdminUserDTO
    {
        public string? id { get; set; }
        public string email { get; set; }
        public string firstName { get; set; }
        public string lastName { get; set; }
        public string gender { get; set; }
        public string? dialCode { get; set; }
        public string phoneNumber { get; set; }
        public int? countryId { get; set; }
        public int? stateId { get; set; }
    }

    public class AdminUserDTO
    {
        public string id { get; set; }
        public string email { get; set; }
        public string firstName { get; set; }
        public string lastName { get; set; }
        public string gender { get; set; }
        public string? dialCode { get; set; }
        public string phoneNumber { get; set; }
        public string? deviceType { get; set; }
        public string profilepic { get; set; }
        public string role { get; set; }
        public string password { get; set; }
    }
}
