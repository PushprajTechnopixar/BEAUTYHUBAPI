using AutoMapper;
using BeautyHubAPI.Models.Dtos;
using BeautyHubAPI.Models.Helper;
using BeautyHubAPI.Repository.IRepository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Net;
using static BeautyHubAPI.Common.GlobalVariables;
using BeautyHubAPI.Models;
using BeautyHubAPI.Data;
using Microsoft.EntityFrameworkCore;
using BeautyHubAPI.Helpers;
using System.Globalization;

namespace BeautyHubAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoryController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        protected APIResponse _response;
        public CategoryController(
            IMapper mapper,
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context
            )
        {
            _response = new();
            _mapper = mapper;
            _context = context;
            _userManager = userManager;
        }

        #region AddCategory
        /// <summary>
        ///  Add  category.
        /// </summary>
        [HttpPost("AddCategory")]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Authorize]
        public async Task<IActionResult> AddCategory([FromBody] AddCategoryDTO model)
        {
            try
            {
                string currentUserId = (HttpContext.User.Claims.First().Value);
                if (string.IsNullOrEmpty(currentUserId))
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = "Token expired.";
                    return Ok(_response);
                }

                model.categoryType = model.categoryType == null ? 0 : model.categoryType;
                if (model.categoryType == 0)
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = "Please select salon type.";
                    return Ok(_response);
                }

                var currentUserDetail = _userManager.FindByIdAsync(currentUserId).GetAwaiter().GetResult();
                if (currentUserDetail == null)
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = "User does not exists.";
                    return Ok(_response);
                }
                var roles = await _userManager.GetRolesAsync(currentUserDetail);

                var CategoryDetail = new CategoryDTO();

                if (model.mainCategoryId > 0)
                {
                    var categoryDetail = _mapper.Map<SubCategory>(model);
                    categoryDetail.CreatedBy = currentUserId;
                    if (model.categoryType == 1)
                    {
                        categoryDetail.Male = true;
                        categoryDetail.Female = false;
                    }
                    if (model.categoryType == 2)
                    {
                        categoryDetail.Male = false;
                        categoryDetail.Female = true;
                    }
                    if (model.categoryType == 3)
                    {
                        categoryDetail.Male = true;
                        categoryDetail.Female = true;
                    }
                    if (roles[0].ToString() == "SuperAdmin")
                    {
                        categoryDetail.CategoryStatus = Convert.ToInt32(Status.Approved);
                    }
                    else
                    {
                        categoryDetail.CategoryStatus = Convert.ToInt32(Status.Pending);
                    }
                    var checkCategoryName = await _context.SubCategory.Where(u => u.CategoryName.ToLower() == model.categoryName.ToLower()).FirstOrDefaultAsync();
                    if (checkCategoryName != null)
                    {
                        _response.StatusCode = HttpStatusCode.OK;
                        _response.IsSuccess = false;
                        _response.Messages = "Category name already exists.";
                        return Ok(_response);
                    }
                    await _context.AddAsync(categoryDetail);
                    await _context.SaveChangesAsync();

                    var mainsCategory = await _context.MainCategory.FirstOrDefaultAsync(x => x.MainCategoryId == model.mainCategoryId);
                    int mainCategoryType = 0;
                    if (mainsCategory.Male == true && mainsCategory.Female == true)
                    {
                        mainCategoryType = 3;
                    }
                    else if (mainsCategory.Male == true && mainsCategory.Female == false)
                    {
                        mainCategoryType = 1;
                    }
                    else
                    {
                        mainCategoryType = 2;
                    }
                    if (mainCategoryType == 3)
                    {
                        await _context.AddAsync(mainsCategory);
                        await _context.SaveChangesAsync();
                    }
                    else 
                    {
                        if (model.categoryType != mainCategoryType)
                        {
                            _response.StatusCode = HttpStatusCode.OK;
                            _response.IsSuccess = false;
                            _response.Messages = "Please select valid Category type";
                            return Ok(_response);
                        }
                        
                    }
                    if (roles[0].ToString() == "SuperAdmin")
                    {
                        var SalonDetail = await _context.SalonDetail.Where(u => u.IsDeleted != true).ToListAsync();
                        foreach (var item in SalonDetail)
                        {
                            var vendorCategory = new VendorCategory();
                            vendorCategory.SalonId = item.SalonId;
                            vendorCategory.VendorId = item.VendorId;
                            vendorCategory.SubCategoryId = categoryDetail.SubCategoryId;
                            vendorCategory.MainCategoryId = null;
                            if (model.categoryType == 1)
                            {
                                vendorCategory.Male = true;
                                vendorCategory.Female = false;
                            }
                            if (model.categoryType == 2)
                            {
                                vendorCategory.Male = false;
                                vendorCategory.Female = true;
                            }
                            if (model.categoryType == 3)
                            {
                                vendorCategory.Male = true;
                                vendorCategory.Female = true;
                            }
                           
                            await _context.AddAsync(vendorCategory);
                            await _context.SaveChangesAsync();
                        }
                    }
                    CategoryDetail = _mapper.Map<CategoryDTO>(categoryDetail);

                }
                else
                {
                    var categoryDetail = _mapper.Map<MainCategory>(model);
                    categoryDetail.CreatedBy = currentUserId;
                    if (roles[0].ToString() == "SuperAdmin")
                    {
                        categoryDetail.CategoryStatus = Convert.ToInt32(Status.Approved);
                    }
                    else
                    {
                        categoryDetail.CategoryStatus = Convert.ToInt32(Status.Pending);
                    }
                    var checkCategoryName = await _context.MainCategory.Where(u => u.CategoryName.ToLower() == model.categoryName.ToLower()).FirstOrDefaultAsync();
                    if (checkCategoryName != null)
                    {
                        _response.StatusCode = HttpStatusCode.OK;
                        _response.IsSuccess = false;
                        _response.Messages = "Category name already exists.";
                        return Ok(_response);
                    }
                    if (model.categoryType == 1)
                    {
                        categoryDetail.Male = true;
                        categoryDetail.Female = false;
                    }
                    if (model.categoryType == 2)
                    {
                        categoryDetail.Male = false;
                        categoryDetail.Female = true;
                    }
                    if (model.categoryType == 3)
                    {
                        categoryDetail.Male = true;
                        categoryDetail.Female = true;
                    }
                    await _context.AddAsync(categoryDetail);
                    await _context.SaveChangesAsync();

                    if (roles[0].ToString() == "SuperAdmin")
                    {
                        var SalonDetail = await _context.SalonDetail.Where(u => u.IsDeleted != true).ToListAsync();
                        foreach (var item in SalonDetail)
                        {
                            var vendorCategory = new VendorCategory();
                            vendorCategory.SalonId = item.SalonId;
                            vendorCategory.VendorId = item.VendorId;
                            vendorCategory.MainCategoryId = categoryDetail.MainCategoryId;
                            vendorCategory.SubCategoryId = null;
                            if (model.categoryType == 1)
                            {
                                vendorCategory.Male = true;
                                vendorCategory.Female = false;
                            }
                            if (model.categoryType == 2)
                            {
                                vendorCategory.Male = false;
                                vendorCategory.Female = true;
                            }
                            if (model.categoryType == 3)
                            {
                                vendorCategory.Male = true;
                                vendorCategory.Female = true;
                            }

                            await _context.AddAsync(vendorCategory);
                            await _context.SaveChangesAsync();
                        }
                       
                    }
                    CategoryDetail = _mapper.Map<CategoryDTO>(categoryDetail); 
                }
               
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Data = CategoryDetail;
                _response.Messages = "Category added successfully.";
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.IsSuccess = false;
                _response.Messages = ex.Message;
                return Ok(_response);
            }
        }
        #endregion

        #region UpdateCategory
        /// <summary>
        ///  Update  category.
        /// </summary>
        [HttpPost("UpdateCategory")]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> UpdateCategory([FromBody] UpdateCategoryDTO model)
        {
            try
            {
                string currentUserId = (HttpContext.User.Claims.First().Value);
                if (string.IsNullOrEmpty(currentUserId))
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = "Token expired.";
                    return Ok(_response);
                }
                var currentUserDetail = _userManager.FindByIdAsync(currentUserId).GetAwaiter().GetResult();
                if (currentUserDetail == null)
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = "User does not exists.";
                    return Ok(_response);
                }
                var roles = await _userManager.GetRolesAsync(currentUserDetail);


                var CategoryDetail = new CategoryDTO();
                if (model.subCategoryId > 0)
                {
                    var categoryDetail = await _context.SubCategory.Where(u => (u.SubCategoryId == model.subCategoryId)).FirstOrDefaultAsync();
                    if (categoryDetail == null)
                    {
                        _response.StatusCode = HttpStatusCode.OK;
                        _response.IsSuccess = false;
                        _response.Messages = "Record not found.";
                        return Ok(_response);
                    }
                    if (categoryDetail.Male == true && categoryDetail.Female != true)
                    {
                        if (model.categoryType == 2)
                        {
                            _response.StatusCode = HttpStatusCode.OK;
                            _response.IsSuccess = false;
                            _response.Messages = "Please enter valid category type.";
                            return Ok(_response);
                        }
                    }
                    if (categoryDetail.Female == true && categoryDetail.Male != true)
                    {
                        if (model.categoryType == 1)
                        {
                            _response.StatusCode = HttpStatusCode.OK;
                            _response.IsSuccess = false;
                            _response.Messages = "Please enter valid category type.";
                            return Ok(_response);
                        }
                    }
                    var checkCategoryName = await _context.SubCategory.Where(u => (u.CategoryName.ToLower() == model.categoryName.ToLower()) && (u.SubCategoryId != model.subCategoryId)).FirstOrDefaultAsync();

                    if (checkCategoryName != null)
                    {
                        _response.StatusCode = HttpStatusCode.OK;
                        _response.IsSuccess = false;
                        _response.Messages = "Category name already exists.";
                        return Ok(_response);
                    }
                    categoryDetail.CategoryName = model.categoryName;
                    categoryDetail.CategoryDescription = model.categoryDescription;
                    categoryDetail.ModifiedBy = currentUserId;
                    if (roles[0].ToString() == "SuperAdmin")
                    {
                        categoryDetail.CategoryStatus = Convert.ToInt32(Status.Approved);
                    }
                    else
                    {
                        categoryDetail.CategoryStatus = Convert.ToInt32(Status.Pending);
                    }
                    if (model.categoryType == 1)
                    {
                        categoryDetail.Male = true;
                        categoryDetail.Female = false;
                    }
                    if (model.categoryType == 2)
                    {
                        categoryDetail.Male = false;
                        categoryDetail.Female = true;
                    }
                    if (model.categoryType == 3)
                    {
                        categoryDetail.Male = true;
                        categoryDetail.Female = true;
                    }

                    _context.Update(categoryDetail);
                    await _context.SaveChangesAsync();
                    // if (roles[0].ToString() == "SuperAdmin")
                    // {
                    //     var SalonDetail = await _SalonDetailRepository.GetAllAsync(u => u.IsDeleted != true);
                    //     foreach (var item in SalonDetail)
                    //     {
                    //         var vendorCategory = new VendorCategory();
                    //         vendorCategory.SalonId = item.SalonId;
                    //         vendorCategory.VendorId = item.VendorId;
                    //         vendorCategory.SubCategoryId = categoryDetail.SubCategoryId;
                    //         vendorCategory.MainCategoryId = null;
                    //         vendorCategory.SubSubCategoryId = null;

                    //         await _vendorCategoryRepository.CreateEntity(vendorCategory);
                    //     }
                    // }
                    CategoryDetail = _mapper.Map<CategoryDTO>(categoryDetail);

                }
                else
                {
                    var categoryDetail = await _context.MainCategory.Where(u => u.MainCategoryId == model.mainCategoryId).FirstOrDefaultAsync();
                    if (categoryDetail == null)
                    {
                        _response.StatusCode = HttpStatusCode.OK;
                        _response.IsSuccess = false;
                        _response.Messages = "Record not found.";
                        return Ok(_response);
                    }
                    var checkCategoryName = await _context.MainCategory.Where(u => (u.CategoryName.ToLower() == model.categoryName.ToLower()) && (u.MainCategoryId != model.mainCategoryId)).FirstOrDefaultAsync();

                    if (checkCategoryName != null)
                    {
                        _response.StatusCode = HttpStatusCode.OK;
                        _response.IsSuccess = false;
                        _response.Messages = "Category name already exists.";
                        return Ok(_response);
                    }
                    categoryDetail.CategoryName = model.categoryName;
                    categoryDetail.CategoryDescription = model.categoryDescription;
                    categoryDetail.ModifiedBy = currentUserId;
                    if (roles[0].ToString() == "SuperAdmin")
                    {
                        categoryDetail.CategoryStatus = Convert.ToInt32(Status.Approved);
                    }
                    else
                    {
                        categoryDetail.CategoryStatus = Convert.ToInt32(Status.Pending);
                    }
                    if (model.categoryType == 1)
                    {
                        categoryDetail.Male = true;
                        categoryDetail.Female = false;
                    }
                    if (model.categoryType == 2)
                    {
                        categoryDetail.Male = false;
                        categoryDetail.Female = true;
                    }
                    if (model.categoryType == 3)
                    {
                        categoryDetail.Male = true;
                        categoryDetail.Female = true;
                    }

                    var subCategory = await _context.MainCategory.FirstOrDefaultAsync(x => x.MainCategoryId == model.mainCategoryId);
                    int subCategoryType = 0;
                    if (subCategory.Male == true && subCategory.Female == true)
                    {
                        subCategoryType = 3;
                    }
                    else if (subCategory.Male == false && subCategory.Female == true)
                    {
                        subCategoryType = 1;
                    }
                    else
                    {
                        subCategoryType = 2;
                    }
                    if (subCategoryType == 3)
                    {
                         _context.Update(subCategory);
                        await _context.SaveChangesAsync();
                    }

                    //if (model.categoryType == subCategoryType)
                    //{
                    //    _response.StatusCode = HttpStatusCode.OK;
                    //    _response.IsSuccess = false;
                    //    _response.Messages = "Updating the primary category is restricted when a subcategory under the category exists.";
                    //    return Ok(_response);
                    //}

                    _context.Update(categoryDetail);
                    await _context.SaveChangesAsync();

                    // if (roles[0].ToString() == "SuperAdmin")
                    // {
                    //     var SalonDetail = await _SalonDetailRepository.GetAllAsync(u => u.IsDeleted != true);
                    //     foreach (var item in SalonDetail)
                    //     {
                    //         var vendorCategory = new VendorCategory();
                    //         vendorCategory.SalonId = item.SalonId;
                    //         vendorCategory.VendorId = item.VendorId;
                    //         vendorCategory.MainCategoryId = categoryDetail.MainCategoryId;
                    //         vendorCategory.SubSubCategoryId = null;
                    //         vendorCategory.SubCategoryId = null;

                    //         await _vendorCategoryRepository.CreateEntity(vendorCategory);
                    //     }
                    // }
                    CategoryDetail = _mapper.Map<CategoryDTO>(categoryDetail);
                }

                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Data = CategoryDetail;
                _response.Messages = "Category updated successfully.";
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.IsSuccess = false;
                _response.Messages = ex.Message;
                return Ok(_response);
            }
        }
        #endregion

        #region GetSubCategoryType
        /// <summary>
        ///  Get SubCategory Type.
        /// </summary>
        [HttpGet("GetSubCategoryType")]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Authorize]
        public async Task<IActionResult> GetSubCategoryType(int mainCategoryId)
        {
            try
            {
                string currentUserId = (HttpContext.User.Claims.First().Value);
                if (string.IsNullOrEmpty(currentUserId))
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = "Token expired.";
                    return Ok(_response);
                }
                if (mainCategoryId > 0)
                {
                    var mainsCategory = await _context.MainCategory.FirstOrDefaultAsync(x => x.MainCategoryId == mainCategoryId);
                    if (mainsCategory == null)
                    {
                        _response.StatusCode = HttpStatusCode.OK;
                        _response.IsSuccess = false;
                        _response.Messages = "Not found any record.";
                        return Ok(_response);
                    }
                    int mainCategoryType = 0;
                    if (mainsCategory.Male == true && mainsCategory.Female == true)
                    {
                        mainCategoryType = 3;
                    }
                    else if (mainsCategory.Male == true && mainsCategory.Female == false)
                    {
                        mainCategoryType = 1;
                    }
                    else
                    {
                        mainCategoryType = 2;
                    }
                  

                    _response.StatusCode = HttpStatusCode.OK;
                     _response.IsSuccess = true;
                    _response.Data = new  {mainCategoryType =mainCategoryType };
                    _response.Messages = "Category type shown successfully.";
                     return Ok(_response);

                }
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = false;
                _response.Messages = "Not found any record.";
                return Ok(_response);


            }
            catch (Exception ex)
            {
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.IsSuccess = false;
                _response.Messages = ex.Message;
                return Ok(_response);
            }
        }
        #endregion

        #region GetCategoryList
        /// <summary>
        ///  Get  category list.
        /// </summary>
        [HttpGet("GetCategoryList")]
        [Authorize]
        public async Task<IActionResult> GetCategoryList([FromQuery] GetCategoryRequestDTO model)
        {
            try
            {
                string currentUserId = (HttpContext.User.Claims.First().Value);
                if (string.IsNullOrEmpty(currentUserId))
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = "Token expired.";
                    return Ok(_response);
                }

                model.categoryType = model.categoryType == null ? 0 : model.categoryType;
                // get top Salon detail for login vendor
                var currentUserDetail = _userManager.FindByIdAsync(currentUserId).GetAwaiter().GetResult();
                if (currentUserDetail == null)
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = "User does not exists.";
                    return Ok(_response);
                }
                var roles = await _userManager.GetRolesAsync(currentUserDetail);
                if (roles[0].ToString() == "Vendor")
                {
                    var getSalonDetail = await _context.SalonDetail.Where(u => (u.VendorId == currentUserId)).OrderByDescending(u => u.ModifyDate).ToListAsync();
                    model.salonId = getSalonDetail.FirstOrDefault().SalonId;
                }

                List<CategoryDTO> Categories = new List<CategoryDTO>();
                if (model.mainCategoryId > 0)
                {
                    if (model.salonId > 0)
                    {
                        var categoryDetail = new List<SubCategory>();
                        if (model.categoryType == 0)
                        {
                            categoryDetail = await _context.SubCategory.Where(u => u.MainCategoryId == model.mainCategoryId && u.CategoryStatus == Convert.ToInt32(Status.Approved)).ToListAsync();

                        }
                        else if (model.categoryType == 1)
                        {
                            categoryDetail = await _context.SubCategory.Where(u => u.MainCategoryId == model.mainCategoryId && u.CategoryStatus == Convert.ToInt32(Status.Approved)
                           && (u.Male == true)
                           //    && (u.Female == false)
                           ).ToListAsync();
                        }
                        else if (model.categoryType == 2)
                        {
                            categoryDetail = await _context.SubCategory.Where(u => u.MainCategoryId == model.mainCategoryId && u.CategoryStatus == Convert.ToInt32(Status.Approved)
                           //    && (u.Male == false)
                           && (u.Female == true)
                           ).ToListAsync();
                        }
                        else
                        {
                            categoryDetail = await _context.SubCategory.Where(u => u.MainCategoryId == model.mainCategoryId && u.CategoryStatus == Convert.ToInt32(Status.Approved)
                           && (u.Male == true)
                           && (u.Female == true)
                           ).ToListAsync();
                        }
                        Categories = _mapper.Map<List<CategoryDTO>>(categoryDetail);
                        foreach (var item in Categories)
                        {
                            item.status = true;
                            item.createDate = (Convert.ToDateTime(item.createDate)).ToString(@"dd-MM-yyyy");
                        }
                        Categories = new List<CategoryDTO>();
                        foreach (var item in categoryDetail)
                        {
                            var mappedData = _mapper.Map<CategoryDTO>(item);
                            if (item.Male == true && item.Female == true)
                            {
                                mappedData.categoryType = 3;
                            }
                            if (item.Male == false && item.Female == false)
                            {
                                mappedData.categoryType = 0;
                            }
                            if (item.Male == true && item.Female == false)
                            {
                                mappedData.categoryType = 1;
                            }
                            if (item.Male == false && item.Female == true)
                            {
                                mappedData.categoryType = 2;
                            }
                            mappedData.createDate = item.CreateDate.ToString(@"dd-MM-yyyy");
                            mappedData.status = true;
                            var categoryStatus = new VendorCategory();
                            if (model.categoryType == 0)
                            {
                                categoryStatus = await _context.VendorCategory.Where(u => (u.SalonId == model.salonId)
                               && (u.SubCategoryId == item.SubCategoryId)
                               ).FirstOrDefaultAsync();
                            }
                            else if (model.categoryType == 1)
                            {
                                categoryStatus = await _context.VendorCategory.Where(u => (u.SalonId == model.salonId)
                               && (u.SubCategoryId == item.SubCategoryId)
                               && (u.Male == true)
                               //    && (u.Female == false)
                               ).FirstOrDefaultAsync();
                            }
                            else if (model.categoryType == 2)
                            {
                                categoryStatus = await _context.VendorCategory.Where(u => (u.SalonId == model.salonId)
                               && (u.SubCategoryId == item.SubCategoryId)
                               //    && (u.Male == false)
                               && (u.Female == true)
                               ).FirstOrDefaultAsync();
                            }
                            else
                            {
                                categoryStatus = await _context.VendorCategory.Where(u => (u.SalonId == model.salonId)
                               && (u.SubCategoryId == item.SubCategoryId)
                               && (u.Male == true)
                               && (u.Female == true)
                               ).FirstOrDefaultAsync();
                            }
                            if (categoryStatus == null)
                            {
                                Categories.Add(mappedData);
                            }
                        }
                    }
                    else
                    {
                        var categoryDetail = new List<SubCategory>();
                        if (model.categoryType == 0)
                        {
                            categoryDetail = await _context.SubCategory.Where(u => u.MainCategoryId == model.mainCategoryId && u.CategoryStatus == Convert.ToInt32(Status.Approved)).ToListAsync();

                        }
                        else if (model.categoryType == 1)
                        {
                            categoryDetail = await _context.SubCategory.Where(u => u.MainCategoryId == model.mainCategoryId && u.CategoryStatus == Convert.ToInt32(Status.Approved)
                           && (u.Male == true)
                           //    && (u.Female == false)
                           ).ToListAsync();
                        }
                        else if (model.categoryType == 2)
                        {
                            categoryDetail = await _context.SubCategory.Where(u => u.MainCategoryId == model.mainCategoryId && u.CategoryStatus == Convert.ToInt32(Status.Approved)
                           //    && (u.Male == false)
                           && (u.Female == true)
                           ).ToListAsync();
                        }
                        else
                        {
                            categoryDetail = await _context.SubCategory.Where(u => u.MainCategoryId == model.mainCategoryId && u.CategoryStatus == Convert.ToInt32(Status.Approved)
                           && (u.Male == true)
                           && (u.Female == true)
                           ).ToListAsync();
                        }
                        Categories = new List<CategoryDTO>();
                        foreach (var item in categoryDetail)
                        {
                            var mappedData = _mapper.Map<CategoryDTO>(item);
                            mappedData.status = true;
                            // mappedData.createDate = (Convert.ToDateTime(item.CreateDate)).ToString(@"dd-MM-yyyy");
                            mappedData.createDate = item.CreateDate.ToString(@"dd-MM-yyyy");

                            if (item.Male == true && item.Female == true)
                            {
                                mappedData.categoryType = 3;
                            }
                            if (item.Male == false && item.Female == false)
                            {
                                mappedData.categoryType = 0;
                            }
                            if (item.Male == true && item.Female == false)
                            {
                                mappedData.categoryType = 1;
                            }
                            if (item.Male == false && item.Female == true)
                            {
                                mappedData.categoryType = 2;
                            }
                            Categories.Add(mappedData);
                        }
                    }
                }
                else
                {
                    if (model.salonId > 0)
                    {
                        var categoryDetail = new List<MainCategory>();
                        if (model.categoryType == 0)
                        {
                            categoryDetail = await _context.MainCategory.Where(u => u.CategoryStatus == Convert.ToInt32(Status.Approved)).ToListAsync();
                        }
                        else if (model.categoryType == 1)
                        {
                            categoryDetail = await _context.MainCategory.Where(u => u.CategoryStatus == Convert.ToInt32(Status.Approved)
                           && (u.Male == true)
                           //    && (u.Female == false)
                           ).ToListAsync();
                        }
                        else if (model.categoryType == 2)
                        {
                            categoryDetail = await _context.MainCategory.Where(u => u.CategoryStatus == Convert.ToInt32(Status.Approved)
                           //    && (u.Male == false)
                           && (u.Female == true)
                           ).ToListAsync();
                        }
                        else
                        {
                            categoryDetail = await _context.MainCategory.Where(u => u.CategoryStatus == Convert.ToInt32(Status.Approved)
                           && (u.Male == true)
                           && (u.Female == true)
                           ).ToListAsync();
                        }
                        Categories = new List<CategoryDTO>();
                        foreach (var item in categoryDetail)
                        {
                            var mappedData = _mapper.Map<CategoryDTO>(item);
                            var subCategoryDetail = new List<SubCategory>();
                            if (model.categoryType == 0)
                            {
                                subCategoryDetail = await _context.SubCategory.Where(u => u.MainCategoryId == item.MainCategoryId && u.CategoryStatus == Convert.ToInt32(Status.Approved)).ToListAsync();

                            }
                            else if (model.categoryType == 1)
                            {
                                subCategoryDetail = await _context.SubCategory.Where(u => u.MainCategoryId == item.MainCategoryId && u.CategoryStatus == Convert.ToInt32(Status.Approved)
                               && (u.Male == true)
                               && (u.Female == false)
                               ).ToListAsync();
                            }
                            else if (model.categoryType == 2)
                            {
                                subCategoryDetail = await _context.SubCategory.Where(u => u.MainCategoryId == item.MainCategoryId && u.CategoryStatus == Convert.ToInt32(Status.Approved)
                               && (u.Male == false)
                               && (u.Female == true)
                               ).ToListAsync();
                            }
                            else
                            {
                                subCategoryDetail = await _context.SubCategory.Where(u => u.MainCategoryId == item.MainCategoryId && u.CategoryStatus == Convert.ToInt32(Status.Approved)
                               && (u.Male == true)
                               && (u.Female == true)
                               ).ToListAsync();
                            }
                            mappedData.isNext = subCategoryDetail.Count > 0 ? true : false;
                            mappedData.createDate = item.CreateDate.ToString(@"dd-MM-yyyy");
                            mappedData.status = true;
                            if (item.Male == true && item.Female == true)
                            {
                                mappedData.categoryType = 3;
                            }
                            if (item.Male == false && item.Female == false)
                            {
                                mappedData.categoryType = 0;
                            }
                            if (item.Male == true && item.Female == false)
                            {
                                mappedData.categoryType = 1;
                            }
                            if (item.Male == false && item.Female == true)
                            {
                                mappedData.categoryType = 2;
                            }

                            var categoryStatus = new VendorCategory();
                            if (model.categoryType == 0)
                            {
                                categoryStatus = await _context.VendorCategory.Where(u => (u.SalonId == model.salonId)
                               && (u.MainCategoryId == item.MainCategoryId)
                               ).FirstOrDefaultAsync();
                            }
                            else if (model.categoryType == 1)
                            {
                                categoryStatus = await _context.VendorCategory.Where(u => (u.SalonId == model.salonId)
                               && (u.MainCategoryId == item.MainCategoryId)
                               && (u.Male == true)
                               && (u.Female == false)
                               ).FirstOrDefaultAsync();
                            }
                            else if (model.categoryType == 2)
                            {
                                categoryStatus = await _context.VendorCategory.Where(u => (u.SalonId == model.salonId)
                               && (u.MainCategoryId == item.MainCategoryId)
                               && (u.Male == false)
                               && (u.Female == true)
                               ).FirstOrDefaultAsync();
                            }
                            else
                            {
                                categoryStatus = await _context.VendorCategory.Where(u => (u.SalonId == model.salonId)
                               && (u.MainCategoryId == item.MainCategoryId)
                               && (u.Male == true)
                               && (u.Female == true)
                               ).FirstOrDefaultAsync();
                            }
                            if (categoryStatus == null)
                            {
                                Categories.Add(mappedData);
                            }
                        }
                    }
                    else
                    {
                        var categoryDetail = new List<MainCategory>();
                        if (model.categoryType == 0)
                        {
                            categoryDetail = await _context.MainCategory.Where(u => u.CategoryStatus == Convert.ToInt32(Status.Approved)
                           ).ToListAsync();
                        }
                        else if (model.categoryType == 1)
                        {
                            categoryDetail = await _context.MainCategory.Where(u => u.CategoryStatus == Convert.ToInt32(Status.Approved)
                           && (u.Male == true)
                           //    && (u.Female == false)
                           ).ToListAsync();
                        }
                        else if (model.categoryType == 2)
                        {
                            categoryDetail = await _context.MainCategory.Where(u => u.CategoryStatus == Convert.ToInt32(Status.Approved)
                           //    && (u.Male == false)
                           && (u.Female == true)
                           ).ToListAsync();
                        }
                        else
                        {
                            categoryDetail = await _context.MainCategory.Where(u => u.CategoryStatus == Convert.ToInt32(Status.Approved)
                           && (u.Male == true)
                           && (u.Female == true)
                           ).ToListAsync();
                        }
                        // Categories = (_mapper.Map<List<CategoryDTO>>(categoryDetail));
                        Categories = new List<CategoryDTO>();
                        foreach (var item in categoryDetail)
                        {
                            var mappedData = _mapper.Map<CategoryDTO>(item);
                            mappedData.status = true;
                            //mappedData.createDate = item.CreateDate.ToString(@"dd-MM-yyyy");
                            if (item.Male == true && item.Female == true)
                            {
                                mappedData.categoryType = 3;
                            }
                            if (item.Male == false && item.Female == false)
                            {
                                mappedData.categoryType = 0;
                            }
                            if (item.Male == true && item.Female == false)
                            {
                                mappedData.categoryType = 1;
                            }
                            if (item.Male == false && item.Female == true)
                            {
                                mappedData.categoryType = 2;
                            }
                            Categories.Add(mappedData);
                        }
                        foreach (var item in Categories)
                        {
                            var subCategoryDetail = new List<SubCategory>();
                            if (model.categoryType == 0)
                            {
                                subCategoryDetail = await _context.SubCategory.Where(u => u.MainCategoryId == item.mainCategoryId && u.CategoryStatus == Convert.ToInt32(Status.Approved)).ToListAsync();
                            }
                            else if (model.categoryType == 1)
                            {
                                subCategoryDetail = await _context.SubCategory.Where(u => u.MainCategoryId == item.mainCategoryId && u.CategoryStatus == Convert.ToInt32(Status.Approved)
                               //    && (u.Male == true)
                               && (u.Female == false)
                               ).ToListAsync();
                            }
                            else if (model.categoryType == 2)
                            {
                                subCategoryDetail = await _context.SubCategory.Where(u => u.MainCategoryId == item.mainCategoryId && u.CategoryStatus == Convert.ToInt32(Status.Approved)
                               //    && (u.Male == false)
                               && (u.Female == true)
                               ).ToListAsync();
                            }
                            else
                            {
                                subCategoryDetail = await _context.SubCategory.Where(u => u.MainCategoryId == item.mainCategoryId && u.CategoryStatus == Convert.ToInt32(Status.Approved)
                               && (u.Male == true)
                               && (u.Female == true)
                               ).ToListAsync();
                            }

                            item.isNext = subCategoryDetail.Count > 0 ? true : false;
                            item.status = true;
                            item.createDate = (Convert.ToDateTime(item.createDate)).ToString(@"dd-MM-yyyy");
                        }
                    }
                }

                if (roles[0].ToString() == "Vendor")
                {
                    Categories = Categories.Where(u => u.mainCategoryId != 53).ToList();
                }

                if (Categories.Count > 0)
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = true;
                    _response.Data = Categories;
                    _response.Messages = "Category shown successfully.";
                    return Ok(_response);
                }
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = false;
                _response.Messages = "Record not found.";
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.IsSuccess = false;
                _response.Messages = ex.Message;
                return Ok(_response);
            }
        }
        #endregion

        #region GetCategoryDetail
        /// <summary>
        ///  Get  category list.
        /// </summary>
        [HttpGet("GetCategoryDetail")]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Authorize]
        public async Task<IActionResult> GetCategoryDetail([FromQuery] GetCategoryDetailRequestDTO model)
        {
            try
            {
                CategoryDTO Category = new CategoryDTO();
                if (model.subCategoryId > 0)
                {
                    var categoryDetail = await _context.SubCategory.Where(u => (u.SubCategoryId == model.subCategoryId)).FirstOrDefaultAsync();

                    Category = _mapper.Map<CategoryDTO>(categoryDetail);
                    if (categoryDetail.Male == true && categoryDetail.Female == true)
                    {
                        Category.categoryType = 3;
                    }
                    if (categoryDetail.Male == false && categoryDetail.Female == false)
                    {
                        Category.categoryType = 0;
                    }
                    if (categoryDetail.Male == true && categoryDetail.Female == false)
                    {
                        Category.categoryType = 1;
                    }
                    if (categoryDetail.Male == false && categoryDetail.Female == true)
                    {
                        Category.categoryType = 2;
                    }
                    if (Category == null)
                    {
                        _response.StatusCode = HttpStatusCode.OK;
                        _response.IsSuccess = false;
                        _response.Messages = "Record not found.";
                        return Ok(_response);
                    }
                    Category.createDate = (Convert.ToDateTime(Category.createDate)).ToString(@"dd-MM-yyyy");
                }
                else
                {
                    var categoryDetail = await _context.MainCategory.Where(u =>
                    (u.MainCategoryId == model.mainCategoryId)
                    ).FirstOrDefaultAsync();
                    Category = _mapper.Map<CategoryDTO>(categoryDetail);
                    if (categoryDetail.Male == true && categoryDetail.Female == true)
                    {
                        Category.categoryType = 3;
                    }
                    if (categoryDetail.Male == false && categoryDetail.Female == false)
                    {
                        Category.categoryType = 0;
                    }
                    if (categoryDetail.Male == true && categoryDetail.Female == false)
                    {
                        Category.categoryType = 1;
                    }
                    if (categoryDetail.Male == false && categoryDetail.Female == true)
                    {
                        Category.categoryType = 2;
                    }
                    if (Category == null)
                    {
                        _response.StatusCode = HttpStatusCode.OK;
                        _response.IsSuccess = false;
                        _response.Messages = "Record not found.";
                        return Ok(_response);
                    }
                    Category.createDate = (Convert.ToDateTime(Category.createDate)).ToString(@"dd-MM-yyyy");
                }

                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Data = Category;
                _response.Messages = "Category detail shown successfully.";
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.IsSuccess = false;
                _response.Messages = ex.Message;
                return Ok(_response);
            }
        }
        #endregion

        #region DeleteCategory
        /// <summary>
        ///  Delete  category.
        /// </summary>
        [HttpDelete("DeleteCategory")]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Authorize]
        public async Task<IActionResult> DeleteCategory([FromQuery] DeleteCategoryDTO model)
        {
            try
            {
                CategoryDTO Category = new CategoryDTO();
                if (model.subCategoryId > 0)
                {
                    // var categoryDetail = await _context.Inventory.Where(u => (u.SubCategoryId == model.subCategoryId)).FirstOrDefaultAsync();
                    // if (categoryDetail != null)
                    // {
                    //     _response.StatusCode = HttpStatusCode.OK;
                    //     _response.IsSuccess = false;
                    //     _response.Messages = "Can't delete,  is added with this category.";
                    //     return Ok(_response);
                    // }
                    var categoryDetail = await _context.SubCategory.Where(u => (u.SubCategoryId == model.subCategoryId)).FirstOrDefaultAsync();
                    Category = _mapper.Map<CategoryDTO>(categoryDetail);
                    if (Category == null)
                    {
                        _response.StatusCode = HttpStatusCode.OK;
                        _response.IsSuccess = false;
                        _response.Messages = "Record not found.";
                        return Ok(_response);
                    }


                    var vendorCategory = await _context.VendorCategory.Where(u => u.SubCategoryId == categoryDetail.SubCategoryId).ToListAsync();
                    foreach (var item in vendorCategory)
                    {
                        _context.Remove(item);
                        await _context.SaveChangesAsync();
                    }

                    // // Delete s from cart releted to category
                    // var cartDetail = await _cartRepository.GetAllAsync();
                    // foreach (var item in cartDetail)
                    // {
                    //     var Detail = await _context.Inventory.Where(u => (u.Id == item.Id) && (u.SubCategoryId == model.subCategoryId)).ToListAsync();
                    //     foreach (var item1 in Detail)
                    //     {
                    //         await _cartRepository.RemoveEntity(item);
                    //     }
                    // }

                    _context.SubCategory.Remove(categoryDetail);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    // var categoryDetail = await _context.Inventory.Where(u => (u.MainCategoryId == model.mainCategoryId)).FirstOrDefaultAsync();
                    // if (categoryDetail != null)
                    // {
                    //     _response.StatusCode = HttpStatusCode.OK;
                    //     _response.IsSuccess = false;
                    //     _response.Messages = "Can't delete,  is added with this category.";
                    //     return Ok(_response);
                    // }
                    var categoryDetail = await _context.MainCategory.Where(u =>
                    (u.MainCategoryId == model.mainCategoryId)
                    ).FirstOrDefaultAsync();
                    Category = _mapper.Map<CategoryDTO>(categoryDetail);
                    if (Category == null)
                    {
                        _response.StatusCode = HttpStatusCode.OK;
                        _response.IsSuccess = false;
                        _response.Messages = "Record not found.";
                        return Ok(_response);
                    }
                    var subCategoryDetail = await _context.SubCategory.Where(u => (u.MainCategoryId == model.mainCategoryId)
                        ).ToListAsync();
                    foreach (var item in subCategoryDetail)
                    {

                        var vendorSubCategoryDetail = await _context.VendorCategory.Where(u => u.SubCategoryId == item.SubCategoryId).ToListAsync();
                        foreach (var item2 in vendorSubCategoryDetail)
                        {
                            _context.VendorCategory.Remove(item2);
                            await _context.SaveChangesAsync();
                        }
                        _context.Remove(item);
                        await _context.SaveChangesAsync();
                    }
                    var vendorCategory = await _context.VendorCategory.Where(u => u.MainCategoryId == categoryDetail.MainCategoryId).ToListAsync();
                    foreach (var item in vendorCategory)
                    {
                        _context.Remove(item);
                        await _context.SaveChangesAsync();
                    }

                    // Delete s from cart releted to category
                    // var cartDetail = await _cartRepository.GetAllAsync();
                    // foreach (var item in cartDetail)
                    // {
                    //     var Detail = await _context.Inventory.Where(u => (u.Id == item.Id) && (u.MainCategoryId == model.mainCategoryId)).ToListAsync();
                    //     foreach (var item1 in Detail)
                    //     {
                    //         await _cartRepository.RemoveEntity(item);
                    //     }
                    // }
                    _context.Remove(categoryDetail);
                    await _context.SaveChangesAsync();
                }

                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Messages = "Category deleted successfully.";
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.IsSuccess = false;
                _response.Messages = ex.Message;
                return Ok(_response);
            }
        }
        #endregion

        #region GetCategoryRequests
        /// <summary>
        ///  Get requested  category list.
        /// </summary>
        [HttpGet("GetCategoryRequests")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> GetCategoryRequests()
        {
            try
            {
                string currentUserId = (HttpContext.User.Claims.First().Value);
                if (string.IsNullOrEmpty(currentUserId))
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = "Token expired.";
                    return Ok(_response);
                }

                List<CategoryRequestDTO> Categories = new List<CategoryRequestDTO>();
                var mainCategories = await _context.MainCategory.Where(u => u.CategoryStatus != Convert.ToInt32(Status.Approved)).ToListAsync();
                mainCategories = mainCategories.OrderByDescending(u => u.CreateDate).ToList();
                foreach (var item in mainCategories)
                {
                    var mainCategory = new CategoryRequestDTO();
                    mainCategory.mainCategoryId = item.MainCategoryId;
                    mainCategory.maincategoryName = item.CategoryName;
                    mainCategory.categorystatus = item.CategoryStatus;
                    if (item.Male == true && item.Female == true)
                    {
                        mainCategory.categoryType = 3;
                    }
                    if (item.Male == false && item.Female == false)
                    {
                        mainCategory.categoryType = 0;
                    }
                    if (item.Male == true && item.Female == false)
                    {
                        mainCategory.categoryType = 1;
                    }
                    if (item.Male == false && item.Female == true)
                    {
                        mainCategory.categoryType = 2;
                    }
                    Categories.Add(mainCategory);
                }
                var subCategories = await _context.SubCategory.Where(u => u.CategoryStatus != Convert.ToInt32(Status.Approved)).ToListAsync();
                subCategories = subCategories.OrderByDescending(u => u.CreateDate).ToList();
                foreach (var item in subCategories)
                {
                    var subCategory = new CategoryRequestDTO();
                    subCategory.subCategoryId = item.SubCategoryId;
                    subCategory.subcategoryName = item.CategoryName;
                    subCategory.categorystatus = item.CategoryStatus;
                    if (item.Male == true && item.Female == true)
                    {
                        subCategory.categoryType = 3;
                    }
                    if (item.Male == false && item.Female == false)
                    {
                        subCategory.categoryType = 0;
                    }
                    if (item.Male == true && item.Female == false)
                    {
                        subCategory.categoryType = 1;
                    }
                    if (item.Male == false && item.Female == true)
                    {
                        subCategory.categoryType = 2;
                    }
                    var mainCategory = await _context.MainCategory.Where(u => u.MainCategoryId == item.MainCategoryId).FirstOrDefaultAsync();
                    subCategory.maincategoryName = mainCategory.CategoryName;
                    Categories.Add(subCategory);
                }
                if (Categories.Count > 0)
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = true;
                    _response.Data = Categories;
                    _response.Messages = "Category shown successfully.";
                    return Ok(_response);
                }
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = false;
                _response.Messages = "Record not found.";
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.IsSuccess = false;
                _response.Messages = ex.Message;
                return Ok(_response);
            }
        }
        #endregion

        #region SetCategoryStatus
        /// <summary>
        /// Set category status .
        /// </summary>
        [HttpPost]
        [Route("SetCategoryStatus")]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> SetCategoryStatus([FromBody] CategoryStatusRequestDTO model)
        {
            try
            {
                string currentUserId = (HttpContext.User.Claims.First().Value);
                if (string.IsNullOrEmpty(currentUserId))
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = "Token expired.";
                    return Ok(_response);
                }

                if (model.mainCategoryId > 0)
                {
                    var categoryDetail = await _context.MainCategory.Where(u => u.MainCategoryId == model.mainCategoryId).FirstOrDefaultAsync();
                    if (categoryDetail == null)
                    {
                        _response.StatusCode = HttpStatusCode.OK;
                        _response.IsSuccess = false;
                        _response.Messages = "Not found any record.";
                        return Ok(_response);
                    }
                    categoryDetail.CategoryStatus = model.status;


                    _context.Update(categoryDetail);
                    await _context.SaveChangesAsync();

                    // update to vendor category table for each Salon
                    if (model.status == Convert.ToInt32(Status.Approved))
                    {
                        var SalonDetail = await _context.SalonDetail.Where(u => u.IsDeleted != true).ToListAsync();
                        foreach (var item in SalonDetail)
                        {
                            var vendorCategory = new VendorCategory();
                            vendorCategory.SalonId = item.SalonId;
                            vendorCategory.VendorId = item.VendorId;
                            vendorCategory.MainCategoryId = categoryDetail.MainCategoryId;
                            vendorCategory.SubCategoryId = null;
                            vendorCategory.Male = categoryDetail.Male;
                            vendorCategory.Female = categoryDetail.Female;

                            await _context.AddAsync(vendorCategory);
                            await _context.SaveChangesAsync();
                        }
                    }
                }
                else
                {
                    var categoryDetail = await _context.SubCategory.Where(u => u.SubCategoryId == model.subCategoryId).FirstOrDefaultAsync();
                    if (categoryDetail == null)
                    {
                        _response.StatusCode = HttpStatusCode.OK;
                        _response.IsSuccess = false;
                        _response.Messages = "Not found any record.";
                        return Ok(_response);
                    }
                    categoryDetail.CategoryStatus = model.status;
                    categoryDetail.Male = categoryDetail.Male;
                    categoryDetail.Female = categoryDetail.Female;

                    _context.Update(categoryDetail);
                    await _context.SaveChangesAsync();

                    // update to vendor category table for each Salon
                    if (model.status == Convert.ToInt32(Status.Approved))
                    {
                        var SalonDetail = await _context.SalonDetail.Where(u => u.IsDeleted != true).ToListAsync();
                        foreach (var item in SalonDetail)
                        {
                            var vendorCategory = new VendorCategory();
                            vendorCategory.SalonId = item.SalonId;
                            vendorCategory.VendorId = item.VendorId;
                            vendorCategory.SubCategoryId = categoryDetail.SubCategoryId;
                            vendorCategory.MainCategory = null;
                            vendorCategory.Male = categoryDetail.Male;
                            vendorCategory.Female = categoryDetail.Female;

                            await _context.AddAsync(vendorCategory);
                            await _context.SaveChangesAsync();
                        }
                    }
                }

                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Messages = "Category status updated successfully.";
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.IsSuccess = false;
                _response.Data = new { };
                _response.Messages = ResponseMessages.msgSomethingWentWrong + ex.Message;
                return Ok(_response);
            }
        }
        #endregion



    }
}
