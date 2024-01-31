using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BeautyHubAPI.Models
{
    public partial class AddCategoryDTO
    {
        public int mainCategoryId { get; set; }
        [Required]
        public string categoryName { get; set; }
        public string categoryDescription { get; set; }
        public int? categoryType { get; set; }
    }
    public partial class UpdateCategoryDTO
    {
        public int mainCategoryId { get; set; }
        public int subCategoryId { get; set; }
        public string categoryName { get; set; }
        public string categoryDescription { get; set; }
        public int? categoryType { get; set; }
    }
    public partial class CategoryDTO
    {
        public int mainCategoryId { get; set; }
        public int SubCategoryId { get; set; }
        public string categoryName { get; set; }
        public string CategoryImage { get; set; }
        public string categoryDescription { get; set; }
        public int? categoryType { get; set; }
        public string? categoryTypeName { get; set; }
        public bool isNext { get; set; } = false;
        public string createDate { get; set; }
        public bool status { get; set; }

    }

    public partial class VendorCategoryDTO
    {
        public int mainCategoryId { get; set; }
        public int SubCategoryId { get; set; }
        public string categoryName { get; set; }
        public string? CategoryImageMale { get; set; }
        public string? CategoryImageFemale { get; set; }
        public string categoryDescription { get; set; }
        public int? categoryType { get; set; }
        public string? categoryTypeName { get; set; }
        public bool isNext { get; set; } = false;
        public string createDate { get; set; }
        public bool status { get; set; }

    }
    public partial class CategoryRequestDTO
    {
        public int? mainCategoryId { get; set; }
        public int? subCategoryId { get; set; }
        public string? maincategoryName { get; set; }
        public string? subcategoryName { get; set; }
        public string? subSubcategoryName { get; set; }
        public string? CategoryImageMale { get; set; }
        public string? CategoryImageFemale { get; set; }
        public int? categorystatus { get; set; }
        public int? categoryType { get; set; }
        public string? createDate { get; set; }
    }

    public partial class GetCategoryRequestDTO
    {
        public int mainCategoryId { get; set; }
        public int? salonId { get; set; }
        public int? categoryType { get; set; }
    }
    public partial class GetCategoryDetailRequestDTO
    {
        public int mainCategoryId { get; set; }
        public int subCategoryId { get; set; }
    }

    public partial class DeleteCategoryDTO
    {
        public int mainCategoryId { get; set; }
        public int subCategoryId { get; set; }
    }
}
