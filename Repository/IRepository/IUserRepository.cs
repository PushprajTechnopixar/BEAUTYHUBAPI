using BeautyHubAPI.Models;
using BeautyHubAPI.Models.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace BeautyHubAPI.Repository.IRepository
{
    public interface IUserRepository
    {
        Task<ApplicationUser> IsValidUser(string EmailOrPhone);
        bool IsUniqueUser(string username, string phoneNumber);
        bool IsUniqueEmail(string email);
        bool IsUniquePhone(string phoneNumber);
        Task<LoginResponseDTO> Login(LoginRequestDTO loginRequestDTO);
        Task<LoginResponseDTO> Register(RegisterationRequestDTO registerationRequestDTO);
        Task<LoginResponseDTO> PhoneLogin(PhoneLoginRequestDTO model);
        Task<IActionResult> ResetPassword(ResetPasswordDTO model);
        Task<VendorSalonResponseDTO> VendorRegistration(AddVendorSalonDTO addVendorDTO, string currentUserId);
        Task<AdminUserDTO> AdminUserRegistration(AdminUserRegisterationRequestDTO registerationRequestDTO);

    }
}
