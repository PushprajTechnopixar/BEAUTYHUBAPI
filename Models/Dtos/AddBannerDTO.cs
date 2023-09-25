namespace BeautyHubAPI.Models.Dtos
{
    public class AddBannerDTO
    {
        public IFormFile bannerImage { get; set; }
    }
    public class UpdateBannerDTO
    {
        public int bannerId { get; set; }
        public IFormFile bannerImage { get; set; }
    }
}
