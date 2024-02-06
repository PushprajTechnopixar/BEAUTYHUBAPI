using BeautyHubAPI.Models;
using BeautyHubAPI.Models.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace BeautyHubAPI.Repository.IRepository
{
    public interface IServiceRepository
    {

        Task<Object> customerServiceList([FromQuery] SalonServiceFilterationListDTO model, string currentUserId);
        Task<Object> vendorServiceList([FromQuery] SalonServiceFilterationListDTO model, string currentUserId);
        Task<Object> GetSalonServiceDetail(int serviceId, string? serviceType, string currentUserId);
        Task<Object> DeleteSalonService(int serviceId);
        Task<Object> getServiceImageInBase64(int serviceId, string? Status);
        Task<Object> SetServiceStatus(SetServiceStatusDTO model);
        Task<Object> getAvailableTimeSlots(int serviceId, string queryDate);
        Task<Object> getAvailableDates(int serviceId);
        Task<Object> GetScheduledDaysTime(int salonId);
        Task<Object> SetSalonServiceFavouriteStatus(SetSalonServiceFavouriteStatusDTO model, string currentUserId);

    }
}
