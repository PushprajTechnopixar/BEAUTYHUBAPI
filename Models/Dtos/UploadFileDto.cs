using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace BeautyHubAPI.Models
{
    public class UploadFileDto
    {
        public IFormFile File { get; set; }
        public string Id { get; set; }
    }
    public class UploadProfilePicDto
    {
        public IFormFile profilePic { get; set; }
        public string id { get; set; }
    }
    public partial class UploadCategoryImageDTO
    {
        public int? mainCategoryId { get; set; }
        public int? subCategoryId { get; set; }
        //  public int? SubSubProductCategoryId { get; set; }
        public IFormFile? CategoryImageMale { get; set; }
        public IFormFile? CategoryImageFemale { get; set; }
    }
    public partial class UploadBrandImageDTO
    {
        public int brandId { get; set; }
        public IFormFile brandImage { get; set; }
    }
    public partial class UploadBannerImageDTO
    {
        public int bannerId { get; set; }
        public IFormFile bannerImage { get; set; }
    }
    public partial class UploadSalonImageDTO
    {
        public int salonId { get; set; }
        public IFormFile? salonImage { get; set; }
    }
    public partial class UploadPaymentReceipt
    {
        public IFormFile? paymentReceipt { get; set; }
    }
    public partial class UploadQRImage
    {
        public string upidetailIds { get; set; }
        public List<IFormFile>? qrcode { get; set; }

    }
    public partial class UploadProductImageInBulkDTO
    {
        public string productIds { get; set; }
        public List<IFormFile>? productImage { get; set; }
    }
    public partial class UploadServiceImageDTO
    {
        public int serviceId { get; set; }
        // public string? Status { get; set; }
        public List<IFormFile>? salonServiceImage { get; set; }
    }
    public partial class UploadServiceIconImageDTO
    {
        public int serviceId { get; set; }
        // public string? Status { get; set; }
        public IFormFile? salonServiceIconImage { get; set; }
    }
    public partial class UploadCollectionImageDTO
    {
        public int collectionId { get; set; }
        public List<IFormFile>? collectionImage { get; set; }
    }

    public partial class GenerateImageLinkInBulkDTO
    {
        public List<IFormFile>? productImage { get; set; }
    }
    public class UploadBulkProductDTO
    {
        public int? salonId { get; set; }
        public IFormFile? excelFile { get; set; }
    }
}
