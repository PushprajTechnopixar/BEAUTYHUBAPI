using BeautyHubAPI.Models;

namespace BeautyHubAPI.Repository.IRepository
{
    public interface IServiceRepository
    {
        Task<serviceDetailDTO> GetSalonServiceDetail(int serviceId, string? serviceType);
        Task<Object> DeleteSalonService(int serviceId);
    }
}
