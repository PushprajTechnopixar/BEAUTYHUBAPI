using BeautyHubAPI.Models;

namespace BeautyHubAPI.Repository.IRepository
{
    public interface IBannerRepository : IRepository<Banner>
    {
        Task<Banner> UpdateBanner(Banner entity);
    }
}
