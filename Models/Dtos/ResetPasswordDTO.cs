using System.ComponentModel.DataAnnotations;

namespace BeautyHubAPI.Models.Dtos
{
    public class ResetPasswordDTO
    {
        public string email { get; set; }
        public string phoneNumber { get; set; }
        [Required]
        public string newPassword { get; set; }
    }
    public class ChangePasswordDTO
    {
        [Required]
        public string oldPassword { get; set; }
        [Required]
        public string newPassword { get; set; }
    }
}
