using System.ComponentModel.DataAnnotations;

namespace BeautyHubAPI.Models.Dtos
{
    public class ForgotPasswordDTO
    {
        [Required]
        public string email { get; set; }
        public bool isVerify { get; set; }
    }
    public class PhoneOTPDTO
    {
        [Required]
        public string dialCode { get; set; }
        [Required]
        public string phoneNumber { get; set; }
        public bool isVerify { get; set; }
    }
}
