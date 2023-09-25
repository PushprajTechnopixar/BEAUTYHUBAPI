namespace BeautyHubAPI.Models
{
    public class FavouriteService
    {
        public int FavouriteServiceId { get; set; }
        public int ServiceId { get; set; }
        public string CustomerUserId { get; set; } = null!;
        public DateTime? CreateDate { get; set; }

        public virtual UserDetail CustomerUser { get; set; } = null!;
        public virtual SalonService Service { get; set; } = null!;
       

    }
}
