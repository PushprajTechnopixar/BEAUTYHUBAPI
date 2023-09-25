using BeautyHubAPI.Data;
using BeautyHubAPI.Models;
using BeautyHubAPI.Repository.IRepository;

namespace BeautyHubAPI.Repository
{
    public class BannerRepository : Repository<Banner>, IBannerRepository
    {
        private readonly ApplicationDbContext _context;
        public BannerRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<Banner> UpdateBanner(Banner entity)
        {
            _context.Banner.Update(entity);
            await _context.SaveChangesAsync();
            return entity;
        }
    }
    
}
