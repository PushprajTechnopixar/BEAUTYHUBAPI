using BeautyHubAPI.Models;
using BeautyHubAPI.Models.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace BeautyHubAPI.Repository.IRepository
{
    public interface IAdminRepository
    {
        Task<Object> GetSuperAdminDetail(string currentUserId);
        Task<Object> UpdateSuperAdminDetail([FromBody] UpdateSuperAdminDTO model, string currentUserId);
        Task<Object> GetPaymentOptions(int? membershipPlanId);
        Task<Object> AddBanner([FromForm] AddBannerDTO model);
        Task<Object> UpdateBanner([FromForm] UpdateBannerDTO model);
        Task<Object> DeleteBanner(int bannerId);
        Task<Object> addUpdateMembershipPlan(MembershipPlanDTO model);
        Task<Object> getMembershipPlanList(string? searchQuery, string? vendorId, int? planType);
        Task<Object> getMembershipPlanDetail(int membershipPlanId);
        Task<Object> deleteMembershipPlan(int membershipPlanId);
        Task<Object> AddVendor([FromBody] AddVendorSalonDTO model, string currentUserId);
        Task<Object> UpdateVendor([FromBody] UpdateVendorSalonDTO model, string currentUserId);
        Task<Object> GetVendorList([FromQuery] FilterationListDTO model, string? createdBy, string? status, string? salonType, string currentUserId);
        Task<Object> GetVendorDetail(string vendorId, string currentUserId);
        Task<Object> DeleteVendor([FromQuery] string VendorId);
        Task<Object> SetVendorStatus([FromBody] SetVendorStatusDTO model);
        Task<Object> AddAdminUser([FromBody] AdminUserRegisterationRequestDTO model);
        Task<Object> UpdateAdminUser([FromBody] UpdateAdminUserDTO model, string currentUserId);
        Task<Object> GetAdminUserList([FromQuery] FilterationListDTO? model);
        Task<Object> GetAdminUserDetail([FromQuery] string id, string currentUserId);
        Task<Object> DeleteAdminUser([FromQuery] string id);
    }
}
