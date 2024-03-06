using BeautyHubAPI.Models;
using BeautyHubAPI.Models.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace BeautyHubAPI.Repository.IRepository
{
    public interface IAdminRepository
    {
        Task<Object> GetSuperAdminDetail(string currentUserId);
        Task<Object> UpdateSuperAdminDetail(UpdateSuperAdminDTO model, string currentUserId);
        Task<Object> GetPaymentOptions(int? membershipPlanId);
        Task<Object> AddBanner(AddBannerDTO model);
        Task<Object> UpdateBanner(UpdateBannerDTO model);
        Task<Object> DeleteBanner(int bannerId);
        Task<Object> addUpdateMembershipPlan(MembershipPlanDTO model);
        Task<Object> getMembershipPlanList(string? searchQuery, string? vendorId, int? planType);
        Task<Object> getMembershipPlanDetail(int membershipPlanId);
        Task<Object> deleteMembershipPlan(int membershipPlanId);
        Task<Object> AddVendor(AddVendorSalonDTO model, string currentUserId);
        Task<Object> UpdateVendor(UpdateVendorSalonDTO model, string currentUserId);
        Task<Object> GetVendorList(FilterationListDTO model, string? createdBy, string? status, string? salonType, string currentUserId);
        Task<Object> GetVendorDetail(string vendorId, string currentUserId);
        Task<Object> DeleteVendor(string VendorId);
        Task<Object> SetVendorStatus(SetVendorStatusDTO model);
        Task<Object> AddAdminUser(AdminUserRegisterationRequestDTO model);
        Task<Object> UpdateAdminUser(UpdateAdminUserDTO model, string currentUserId);
        Task<Object> GetAdminUserList(FilterationListDTO? model);
        Task<Object> GetAdminUserDetail(string id, string currentUserId);
        Task<Object> DeleteAdminUser(string id);
    }
}
