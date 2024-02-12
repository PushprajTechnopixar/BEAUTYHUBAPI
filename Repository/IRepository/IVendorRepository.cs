using BeautyHubAPI.Models;
using BeautyHubAPI.Models.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace BeautyHubAPI.Repository.IRepository
{
    public interface IVendorRepository
    {
        Task<Object> buyServicePlan(buyMembershipPlanDTO model, string currentUserId);
        Task<Object> GetVendorCategoryList([FromQuery] GetCategoryRequestDTO model, string currentUserId);
        Task<Object> SetVendorCategoryStatus([FromBody] VendorCategoryRequestDTO model, string currentUserId);
        Task<Object> AddSalonBanner([FromForm] AddSalonBannerDTO model);
        Task<Object> UpdateSalonBanner([FromForm] UpdateSalonBannerDTO model);
        Task<Object> DeleteSalonBanner(int salonBannerId);
        Task<Object> GetSalonBannerDetail(int salonBannerId);
        Task<Object> GetSalonBannerList([FromQuery] GetSalonBannerrequestDTO model);
        Task<Object> GetVendorAppointmentList([FromQuery] OrderFilterationListDTO model);
        Task<Object> GetVendorAppointmentDetail(int appointmentId, string currentUserId);
        Task<Object> SetAppointmentStatus(SetAppointmentStatusDTO model);
        Task<Object> SetPaymentStatus(SetPaymentStatusDTO model);
        Task<Object> MarkAppointmentAsRead(ReadStatusDTO model);
        Task<Object> UpComingSchedule(int salonId);
        Task<Object> UpComingScheduleDetail(string queryDate);
    }
}
