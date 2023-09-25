
using System.ComponentModel.DataAnnotations;

namespace BeautyHubAPI.Dtos;
public class VerifyPhoneModel
{
    [Required]
    public string dialCode { get; set; }
    [Required]
    public string phoneNumber { get; set; }
    [Required]
    public string otp { get; set; }
}
public class PhoneModel
{
    [Required]
    public string dialCode { get; set; }
    [Required]
    public string phoneNumber { get; set; }
    public bool isVerify { get; set; }
}
public class BasicProductInfo
{
    [Required]
    public string productId { get; set; }
    [Required]
    public string inStock { get; set; }
}