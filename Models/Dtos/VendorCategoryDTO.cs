using System;
using System.Collections.Generic;

namespace BeautyHubAPI.Models.Dtos
{
    public class VendorCategoryRequestDTO
    {
        public int salonId { get; set; }
        public int? mainCategoryId { get; set; }
        public int? subCategoryId { get; set; }
        // public int? CategoryType { get; set; }
        public bool Status { get; set; }
    }
    public class CategoryStatusRequestDTO
    {
        public int? mainCategoryId { get; set; }
        public int? subCategoryId { get; set; }
        public int status { get; set; }
    }
}
