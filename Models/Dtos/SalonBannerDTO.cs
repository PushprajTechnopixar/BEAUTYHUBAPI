namespace BeautyHubAPI.Models.Dtos
{
  public class AddSalonBannerDTO
  {
    public int? salonId { get; set; }
    public int? mainCategoryId { get; set; }
    public int? subCategoryId { get; set; }
    //  public int? SubSubProductCategoryId { get; set; }
    public string? bannerType { get; set; }
    public int? categoryType { get; set; }
    public IFormFile? bannerImage { get; set; }
  }
  public partial class UpdateSalonBannerDTO
  {
    public int salonBannerId { get; set; }
    public int? salonId { get; set; }
    public int? mainCategoryId { get; set; }
    public int? subCategoryId { get; set; }
    //  public int? SubSubProductCategoryId { get; set; }
    public string? bannerType { get; set; }
    public int? categoryType { get; set; }

    public IFormFile? bannerImage { get; set; }
  }
  public partial class GetSalonBannerDTO
  {
    public int salonBannerId { get; set; }
    public int? salonId { get; set; }
    public int? mainCategoryId { get; set; }
    public int? subCategoryId { get; set; }
    //  public int? subSubProductCategoryId { get; set; }
    public string? mainCategoryName { get; set; }
    public string? subCategoryName { get; set; }
    //  public string? subSubProductCategoryName { get; set; }
    public string? bannerType { get; set; }
    public string? bannerTypeName { get; set; }
    public int? categoryType { get; set; }
    public bool? male { get; set; }
    public bool? female { get; set; }
    public string? bannerImage { get; set; }
    public string? createDate { get; set; }
  }
  public partial class GetSalonBannerrequestDTO
  {
    public int salonId { get; set; }
    public int? mainCategoryId { get; set; }
    public int? subCategoryId { get; set; }
    //  public int? subSubProductCategoryId { get; set; }
    public string? salonBannerType { get; set; }
    public int? categoryType { get; set; }
  }
}
