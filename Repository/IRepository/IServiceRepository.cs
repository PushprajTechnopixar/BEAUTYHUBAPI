using BeautyHubAPI.Models;
using BeautyHubAPI.Models.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace BeautyHubAPI.Repository.IRepository
{
    public interface IServiceRepository
    {
        Task<Object> customerServiceList([FromQuery] SalonServiceFilterationListDTO model,string currentUserId);
        Task<Object> vendorServiceList([FromQuery] SalonServiceFilterationListDTO model,string currentUserId);
        Task<serviceDetailDTO> GetSalonServiceDetail(int serviceId, string? serviceType);
        Task<Object> DeleteSalonService(int serviceId);

    }
}
