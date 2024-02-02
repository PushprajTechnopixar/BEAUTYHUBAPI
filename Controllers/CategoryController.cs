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
using BeautyHubAPI.Common;

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
                    _response.Messages = ResponseMessages.msgUserNotFound;
                    return Ok(_response);
                }
                var roles = await _userManager.GetRolesAsync(currentUserDetail);

                MainCategory? mainCategoryDetail = new MainCategory();
                int mainCategoryType = 0;
                var CategoryResponse = new CategoryDTO();

                if (model.mainCategoryId > 0)
                {
                    mainCategoryDetail = await _context.MainCategory.Where(u => (u.MainCategoryId == model.mainCategoryId)).FirstOrDefaultAsync();

                    if (mainCategoryDetail != null)
                    {
                        if (mainCategoryDetail.Male == true && mainCategoryDetail.Female == false)
                        {
                            mainCategoryType = 1;
                        }
                        else if (mainCategoryDetail.Male == false && mainCategoryDetail.Female == true)
                        {
                            mainCategoryType = 2;
                        }
                        else
                        {
                            mainCategoryType = 3;
                        }
                    }
                    else
                    {
                        _response.StatusCode = HttpStatusCode.OK;
                        _response.IsSuccess = false;
                        _response.Messages = ResponseMessages.msgNotFound + "record.";
                        return Ok(_response);
                    }

                    if (mainCategoryType == 1 && model.categoryType == 2)
                    {
                        _response.StatusCode = HttpStatusCode.OK;
                        _response.IsSuccess = false;
                        _response.Messages = "Please enter valid category type.";
                        return Ok(_response);
                    }
                    if (mainCategoryType == 2 && model.categoryType == 1)
                    {
                        _response.StatusCode = HttpStatusCode.OK;
                        _response.IsSuccess = false;
                        _response.Messages = "Please enter valid category type.";
                        return Ok(_response);
                    }
                    if ((mainCategoryType == 1 || mainCategoryType == 2) && model.categoryType == 3)
                    {
                        _response.StatusCode = HttpStatusCode.OK;
                        _response.IsSuccess = false;
                        _response.Messages = "Please enter valid category type.";
                        return Ok(_response);
                    }

                    var checkCategoryName = await _context.SubCategory.Where(u => u.CategoryName.ToLower() == model.categoryName.ToLower()).FirstOrDefaultAsync();
                    if (checkCategoryName != null)
                    {
                        _response.StatusCode = HttpStatusCode.OK;
                        _response.IsSuccess = false;
                        _response.Messages = "Category name already exists.";
                        return Ok(_response);
                    }

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
                    CategoryResponse = _mapper.Map<CategoryDTO>(categoryDetail);
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
                    CategoryResponse = _mapper.Map<CategoryDTO>(categoryDetail);
                }

                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Data = CategoryResponse;
                _response.Messages = "Category" + ResponseMessages.msgAdditionSuccess;
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
                    _response.Messages = ResponseMessages.msgUserNotFound;
                    return Ok(_response);
                }
                var roles = await _userManager.GetRolesAsync(currentUserDetail);

                SubCategory? subCategoryDetail = new SubCategory();
                MainCategory? mainCategoryDetail = new MainCategory();
                int mainCategoryType = 0;
                int subCategoryType = 0;

                if (model.subCategoryId > 0)
                {
                    subCategoryDetail = await _context.SubCategory.Where(u => (u.SubCategoryId == model.subCategoryId)).FirstOrDefaultAsync();
                    mainCategoryDetail = await _context.MainCategory.Where(u => (u.MainCategoryId == subCategoryDetail.MainCategoryId)).FirstOrDefaultAsync();
                }
                if (model.mainCategoryId > 0)
                {
                    mainCategoryDetail = await _context.MainCategory.Where(u => (u.MainCategoryId == model.mainCategoryId)).FirstOrDefaultAsync();
                }

                if (mainCategoryDetail != null)
                {
                    if (mainCategoryDetail.Male == true && mainCategoryDetail.Female != true)
                    {
                        mainCategoryType = 1;
                    }
                    if (mainCategoryDetail.Male == false && mainCategoryDetail.Female == true)
                    {
                        mainCategoryType = 2;
                    }
                    if (mainCategoryDetail.Male == true && mainCategoryDetail.Female == true)
                    {
                        mainCategoryType = 3;
                    }
                }

                if (subCategoryDetail != null)
                {
                    if (subCategoryDetail.Male == true && subCategoryDetail.Female != true)
                    {
                        subCategoryType = 1;
                    }
                    if (subCategoryDetail.Male == false && subCategoryDetail.Female == true)
                    {
                        subCategoryType = 2;
                    }
                    if (subCategoryDetail.Male == true && subCategoryDetail.Female == true)
                    {
                        subCategoryType = 3;
                    }
                }

                var CategoryDetail = new CategoryDTO();
                if (model.subCategoryId > 0)
                {
                    if (subCategoryDetail == null)
                    {
                        _response.StatusCode = HttpStatusCode.OK;
                        _response.IsSuccess = false;
                        _response.Messages = ResponseMessages.msgNotFound + "record.";
                        return Ok(_response);
                    }
                    if (mainCategoryType == 1 && model.categoryType == 2)
                    {
                        _response.StatusCode = HttpStatusCode.OK;
                        _response.IsSuccess = false;
                        _response.Messages = "Please enter valid category type.";
                        return Ok(_response);
                    }
                    if (mainCategoryType == 2 && model.categoryType == 1)
                    {
                        _response.StatusCode = HttpStatusCode.OK;
                        _response.IsSuccess = false;
                        _response.Messages = "Please enter valid category type.";
                        return Ok(_response);
                    }

                    var checkCategoryName = await _context.SubCategory.Where(u => (u.CategoryName.ToLower() == model.categoryName.ToLower()) && (u.SubCategoryId != model.subCategoryId)).FirstOrDefaultAsync();

                    if (checkCategoryName != null)
                    {
                        _response.StatusCode = HttpStatusCode.OK;
                        _response.IsSuccess = false;
                        _response.Messages = "Category name already exists.";
                        return Ok(_response);
                    }
                    subCategoryDetail.CategoryName = model.categoryName;
                    subCategoryDetail.CategoryDescription = model.categoryDescription;
                    subCategoryDetail.ModifiedBy = currentUserId;
                    if (roles[0].ToString() == "SuperAdmin")
                    {
                        subCategoryDetail.CategoryStatus = Convert.ToInt32(Status.Approved);
                    }
                    else
                    {
                        subCategoryDetail.CategoryStatus = Convert.ToInt32(Status.Pending);
                    }
                    if (model.categoryType == 1)
                    {
                        subCategoryDetail.Male = true;
                        subCategoryDetail.Female = false;
                    }
                    if (model.categoryType == 2)
                    {
                        subCategoryDetail.Male = false;
                        subCategoryDetail.Female = true;
                    }
                    if (model.categoryType == 3)
                    {
                        subCategoryDetail.Male = true;
                        subCategoryDetail.Female = true;
                    }

                    _context.Update(subCategoryDetail);
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
                    CategoryDetail = _mapper.Map<CategoryDTO>(subCategoryDetail);

                }
                else
                {
                    if (mainCategoryDetail == null)
                    {
                        _response.StatusCode = HttpStatusCode.OK;
                        _response.IsSuccess = false;
                        _response.Messages = ResponseMessages.msgNotFound + "record.";
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
                    if (model.categoryType != 3)
                    {
                        if (model.categoryType == 1)
                        {
                            var maleSubCategory = await _context.SubCategory.FirstOrDefaultAsync(x => x.MainCategoryId == model.mainCategoryId && x.Female == true);
                            if (maleSubCategory != null)
                            {
                                _response.StatusCode = HttpStatusCode.OK;
                                _response.IsSuccess = false;
                                _response.Messages = "Main category update not allowed if female subcategory is present.";
                                return Ok(_response);
                            }
                        }
                        if (model.categoryType == 2)
                        {
                            var maleSubCategory = await _context.SubCategory.FirstOrDefaultAsync(x => x.MainCategoryId == model.mainCategoryId && x.Male == true);
                            if (maleSubCategory != null)
                            {
                                _response.StatusCode = HttpStatusCode.OK;
                                _response.IsSuccess = false;
                                _response.Messages = "Main category update not allowed if male subcategory is present.";
                                return Ok(_response);
                            }
                        }
                    }

                    _context.Update(mainCategoryDetail);
                    await _context.SaveChangesAsync();

                    mainCategoryDetail.CategoryName = model.categoryName;
                    mainCategoryDetail.CategoryDescription = model.categoryDescription;
                    mainCategoryDetail.ModifiedBy = currentUserId;

                    if (roles[0].ToString() == "SuperAdmin")
                    {
                        mainCategoryDetail.CategoryStatus = Convert.ToInt32(Status.Approved);
                    }
                    else
                    {
                        mainCategoryDetail.CategoryStatus = Convert.ToInt32(Status.Pending);
                    }

                    if (model.categoryType == 1)
                    {
                        mainCategoryDetail.Male = true;
                        mainCategoryDetail.Female = false;
                    }
                    if (model.categoryType == 2)
                    {
                        mainCategoryDetail.Male = false;
                        mainCategoryDetail.Female = true;
                    }
                    if (model.categoryType == 3)
                    {
                        mainCategoryDetail.Male = true;
                        mainCategoryDetail.Female = true;
                    }

                    _context.Update(mainCategoryDetail);
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
                    CategoryDetail = _mapper.Map<CategoryDTO>(mainCategoryDetail);
                }

                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Data = CategoryDetail;
                _response.Messages = "Category" + ResponseMessages.msgUpdationSuccess;
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
                        _response.Messages = ResponseMessages.msgNotFound + "record.";
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
                    _response.Data = new { mainCategoryType = mainCategoryType };
                    _response.Messages = "Category type shown successfully.";
                    return Ok(_response);

                }
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = false;
                _response.Messages = ResponseMessages.msgNotFound + "record.";
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
        ///  Get category list.
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
                    _response.Messages = ResponseMessages.msgUserNotFound;
                    return Ok(_response);
                }
                var roles = await _userManager.GetRolesAsync(currentUserDetail);
                if (roles[0].ToString() == "Vendor")
                {
                    var getSalonDetail = await _context.SalonDetail.Where(u => (u.VendorId == currentUserId)).OrderByDescending(u => u.ModifyDate).ToListAsync();
                    model.salonId = getSalonDetail.FirstOrDefault().SalonId;
                }

                if (roles[0].ToString() == "Customer")
                {
                    List<CategoryDTO> Categories = new List<CategoryDTO>();
                    if (model.mainCategoryId > 0)
                    {
                        if (model.salonId > 0)
                        {
                            var categoryDetail = await _context.SubCategory.Where(u => u.MainCategoryId == model.mainCategoryId && u.CategoryStatus == Convert.ToInt32(Status.Approved)).ToListAsync();
                            if (model.categoryType == 1)
                            {
                                categoryDetail = categoryDetail.Where(u => u.Male == true).ToList();
                            }
                            else if (model.categoryType == 2)
                            {
                                categoryDetail = categoryDetail.Where(u => u.Female == true).ToList();
                            }
                            else if (model.categoryType == 3)
                            {
                                categoryDetail = categoryDetail.Where(u => u.Female == true && u.Male == true).ToList();
                            }
                            else
                            {
                                categoryDetail = categoryDetail;
                            }
                            Categories = new List<CategoryDTO>();
                            foreach (var item in categoryDetail)
                            {
                                var mappedData = _mapper.Map<CategoryDTO>(item);
                                if (model.categoryType > 0)
                                {
                                    mappedData.CategoryImage = (model.categoryType == 1 ? item.CategoryImageMale : item.CategoryImageFemale);
                                }
                                else
                                {
                                    mappedData.CategoryImage = item.CategoryImageMale;
                                }
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
                                var categoryStatus = await _context.VendorCategory.Where(u => (u.SalonId == model.salonId)
                                   && (u.SubCategoryId == item.SubCategoryId)
                                   ).FirstOrDefaultAsync();

                                // if (categoryStatus != null)
                                // {
                                //     if (model.categoryType == 1)
                                //     {
                                //         categoryStatus = categoryStatus.Male == true ? categoryStatus : null;
                                //     }
                                //     else if (model.categoryType == 2)
                                //     {
                                //         categoryStatus = categoryStatus.Female == true ? categoryStatus : null;
                                //     }
                                //     else if (model.categoryType == 3)
                                //     {
                                //         categoryStatus = categoryStatus.Male == true && categoryStatus.Female == true ? categoryStatus : null;
                                //     }
                                //     else
                                //     {
                                //         categoryStatus = categoryStatus;
                                //     }
                                // }
                                if (categoryStatus == null)
                                {
                                    Categories.Add(mappedData);
                                }
                            }
                        }
                        else
                        {
                            var categoryDetail = await _context.SubCategory.Where(u => u.MainCategoryId == model.mainCategoryId && u.CategoryStatus == Convert.ToInt32(Status.Approved)).ToListAsync();

                            if (model.categoryType == 1)
                            {
                                categoryDetail = categoryDetail.Where(u => u.Male == true).ToList();
                            }
                            else if (model.categoryType == 2)
                            {
                                categoryDetail = categoryDetail.Where(u => u.Female == true).ToList();
                            }
                            else if (model.categoryType == 3)
                            {
                                categoryDetail = categoryDetail.Where(u => u.Female == true && u.Male == true).ToList();
                            }
                            else
                            {
                                categoryDetail = categoryDetail;
                            }
                            Categories = new List<CategoryDTO>();
                            foreach (var item in categoryDetail)
                            {
                                var mappedData = _mapper.Map<CategoryDTO>(item);
                                if (model.categoryType > 0)
                                {
                                    mappedData.CategoryImage = (model.categoryType == 1 ? item.CategoryImageMale : item.CategoryImageFemale);
                                }
                                else
                                {
                                    mappedData.CategoryImage = item.CategoryImageMale;
                                }
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
                            var categoryDetail = await _context.MainCategory.Where(u => u.CategoryStatus == Convert.ToInt32(Status.Approved)).ToListAsync();

                            if (model.categoryType == 1)
                            {
                                categoryDetail = categoryDetail.Where(u => u.Male == true).ToList();
                            }
                            else if (model.categoryType == 2)
                            {
                                categoryDetail = categoryDetail.Where(u => u.Female == true).ToList();
                            }
                            else if (model.categoryType == 3)
                            {
                                categoryDetail = categoryDetail.Where(u => u.Male == true && u.Female == true).ToList();
                            }
                            else
                            {
                                categoryDetail = categoryDetail;
                            }
                            Categories = new List<CategoryDTO>();
                            foreach (var item in categoryDetail)
                            {
                                var mappedData = _mapper.Map<CategoryDTO>(item);
                                if (model.categoryType > 0)
                                {
                                    mappedData.CategoryImage = (model.categoryType == 1 ? item.CategoryImageMale : item.CategoryImageFemale);
                                }
                                else
                                {
                                    mappedData.CategoryImage = item.CategoryImageMale;
                                }
                                var subCategoryDetail = new List<SubCategory>();

                                subCategoryDetail = await _context.SubCategory.Where(u => u.MainCategoryId == item.MainCategoryId && u.CategoryStatus == Convert.ToInt32(Status.Approved)).ToListAsync();

                                mappedData.isNext = subCategoryDetail.Count > 0 ? true : false;
                                mappedData.createDate = item.CreateDate.ToString(@"dd-MM-yyyy");
                                mappedData.status = true;

                                if (item.Male == true && item.Female == true)
                                {
                                    mappedData.categoryType = 3;
                                }
                                else if (item.Male == false && item.Female == true)
                                {
                                    mappedData.categoryType = 2;
                                }
                                else if (item.Male == true && item.Female == false)
                                {
                                    mappedData.categoryType = 1;
                                }
                                else
                                {
                                    mappedData.categoryType = 0;
                                }

                                var categoryStatus = await _context.VendorCategory.Where(u => (u.SalonId == model.salonId)
                                   && (u.MainCategoryId == item.MainCategoryId)
                                   ).FirstOrDefaultAsync();

                                // if (categoryStatus == null)
                                // {
                                //     if (model.categoryType == 1)
                                //     {
                                //         categoryStatus = categoryStatus.Male == true ? categoryStatus : null;
                                //     }
                                //     else if (model.categoryType == 2)
                                //     {
                                //         categoryStatus = categoryStatus.Female == true ? categoryStatus : null;
                                //     }
                                //     else if (model.categoryType == 3)
                                //     {
                                //         categoryStatus = categoryStatus.Male == true && categoryStatus.Female == true ? categoryStatus : null;
                                //     }
                                //     else
                                //     {
                                //         categoryStatus = categoryStatus;
                                //     }
                                // }

                                if (categoryStatus == null)
                                {
                                    Categories.Add(mappedData);
                                }
                            }
                        }
                        else
                        {
                            var categoryDetail = await _context.MainCategory.Where(u => u.CategoryStatus == Convert.ToInt32(Status.Approved)).ToListAsync();
                            if (model.categoryType == 1)
                            {
                                categoryDetail = categoryDetail.Where(u => u.Male == true).ToList();
                            }
                            else if (model.categoryType == 2)
                            {
                                categoryDetail = categoryDetail.Where(u => u.Female == true).ToList();
                            }
                            else if (model.categoryType == 3)
                            {
                                categoryDetail = categoryDetail.Where(u => u.Male == true && u.Female == true).ToList();
                            }
                            else
                            {
                                categoryDetail = categoryDetail;
                            }
                            // Categories = (_mapper.Map<List<CategoryDTO>>(categoryDetail));
                            Categories = new List<CategoryDTO>();
                            foreach (var item in categoryDetail)
                            {
                                var mappedData = _mapper.Map<CategoryDTO>(item);
                                if (model.categoryType > 0)
                                {
                                    mappedData.CategoryImage = (model.categoryType == 1 ? item.CategoryImageMale : item.CategoryImageFemale);
                                }
                                else
                                {
                                    mappedData.CategoryImage = item.CategoryImageMale;
                                }
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

                                subCategoryDetail = await _context.SubCategory.Where(u => u.MainCategoryId == item.mainCategoryId && u.CategoryStatus == Convert.ToInt32(Status.Approved)).ToListAsync();

                                item.isNext = subCategoryDetail.Count > 0 ? true : false;
                                item.status = true;
                                item.createDate = (Convert.ToDateTime(item.createDate)).ToString(@"dd-MM-yyyy");
                            }
                        }
                    }

                    if (Categories.Count > 0)
                    {
                        _response.StatusCode = HttpStatusCode.OK;
                        _response.IsSuccess = true;
                        _response.Data = Categories;
                        _response.Messages = "Category" + ResponseMessages.msgFoundSuccess;
                        return Ok(_response);
                    }
                }
                else
                {
                    List<VendorCategoryDTO> Categories = new List<VendorCategoryDTO>();
                    if (model.mainCategoryId > 0)
                    {
                        if (model.salonId > 0)
                        {
                            var categoryDetail = await _context.SubCategory.Where(u => u.MainCategoryId == model.mainCategoryId && u.CategoryStatus == Convert.ToInt32(Status.Approved)).ToListAsync();
                            if (model.categoryType == 1)
                            {
                                categoryDetail = categoryDetail.Where(u => u.Male == true).ToList();
                            }
                            else if (model.categoryType == 2)
                            {
                                categoryDetail = categoryDetail.Where(u => u.Female == true).ToList();
                            }
                            else if (model.categoryType == 3)
                            {
                                categoryDetail = categoryDetail.Where(u => u.Female == true && u.Male == true).ToList();
                            }
                            else
                            {
                                categoryDetail = categoryDetail;
                            }
                            Categories = new List<VendorCategoryDTO>();
                            foreach (var item in categoryDetail)
                            {
                                var mappedData = _mapper.Map<VendorCategoryDTO>(item);
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
                                var categoryStatus = await _context.VendorCategory.Where(u => (u.SalonId == model.salonId)
                                   && (u.SubCategoryId == item.SubCategoryId)
                                   ).FirstOrDefaultAsync();

                                // if (categoryStatus != null)
                                // {
                                //     if (model.categoryType == 1)
                                //     {
                                //         categoryStatus = categoryStatus.Male == true ? categoryStatus : null;
                                //     }
                                //     else if (model.categoryType == 2)
                                //     {
                                //         categoryStatus = categoryStatus.Female == true ? categoryStatus : null;
                                //     }
                                //     else if (model.categoryType == 3)
                                //     {
                                //         categoryStatus = categoryStatus.Male == true && categoryStatus.Female == true ? categoryStatus : null;
                                //     }
                                //     else
                                //     {
                                //         categoryStatus = categoryStatus;
                                //     }
                                // }
                                if (categoryStatus == null)
                                {
                                    Categories.Add(mappedData);
                                }
                            }
                        }
                        else
                        {
                            var categoryDetail = await _context.SubCategory.Where(u => u.MainCategoryId == model.mainCategoryId && u.CategoryStatus == Convert.ToInt32(Status.Approved)).ToListAsync();

                            if (model.categoryType == 1)
                            {
                                categoryDetail = categoryDetail.Where(u => u.Male == true).ToList();
                            }
                            else if (model.categoryType == 2)
                            {
                                categoryDetail = categoryDetail.Where(u => u.Female == true).ToList();
                            }
                            else if (model.categoryType == 3)
                            {
                                categoryDetail = categoryDetail.Where(u => u.Female == true && u.Male == true).ToList();
                            }
                            else
                            {
                                categoryDetail = categoryDetail;
                            }
                            Categories = new List<VendorCategoryDTO>();
                            foreach (var item in categoryDetail)
                            {
                                var mappedData = _mapper.Map<VendorCategoryDTO>(item);

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
                            var categoryDetail = await _context.MainCategory.Where(u => u.CategoryStatus == Convert.ToInt32(Status.Approved)).ToListAsync();

                            if (model.categoryType == 1)
                            {
                                categoryDetail = categoryDetail.Where(u => u.Male == true).ToList();
                            }
                            else if (model.categoryType == 2)
                            {
                                categoryDetail = categoryDetail.Where(u => u.Female == true).ToList();
                            }
                            else if (model.categoryType == 3)
                            {
                                categoryDetail = categoryDetail.Where(u => u.Male == true && u.Female == true).ToList();
                            }
                            else
                            {
                                categoryDetail = categoryDetail;
                            }
                            Categories = new List<VendorCategoryDTO>();
                            foreach (var item in categoryDetail)
                            {
                                var mappedData = _mapper.Map<VendorCategoryDTO>(item);

                                var subCategoryDetail = new List<SubCategory>();

                                subCategoryDetail = await _context.SubCategory.Where(u => u.MainCategoryId == item.MainCategoryId && u.CategoryStatus == Convert.ToInt32(Status.Approved)).ToListAsync();

                                mappedData.isNext = subCategoryDetail.Count > 0 ? true : false;
                                mappedData.createDate = item.CreateDate.ToString(@"dd-MM-yyyy");
                                mappedData.status = true;

                                if (item.Male == true && item.Female == true)
                                {
                                    mappedData.categoryType = 3;
                                }
                                else if (item.Male == false && item.Female == true)
                                {
                                    mappedData.categoryType = 2;
                                }
                                else if (item.Male == true && item.Female == false)
                                {
                                    mappedData.categoryType = 1;
                                }
                                else
                                {
                                    mappedData.categoryType = 0;
                                }

                                var categoryStatus = await _context.VendorCategory.Where(u => (u.SalonId == model.salonId)
                                   && (u.MainCategoryId == item.MainCategoryId)
                                   ).FirstOrDefaultAsync();

                                // if (categoryStatus == null)
                                // {
                                //     if (model.categoryType == 1)
                                //     {
                                //         categoryStatus = categoryStatus.Male == true ? categoryStatus : null;
                                //     }
                                //     else if (model.categoryType == 2)
                                //     {
                                //         categoryStatus = categoryStatus.Female == true ? categoryStatus : null;
                                //     }
                                //     else if (model.categoryType == 3)
                                //     {
                                //         categoryStatus = categoryStatus.Male == true && categoryStatus.Female == true ? categoryStatus : null;
                                //     }
                                //     else
                                //     {
                                //         categoryStatus = categoryStatus;
                                //     }
                                // }

                                if (categoryStatus == null)
                                {
                                    Categories.Add(mappedData);
                                }
                            }
                        }
                        else
                        {
                            var categoryDetail = await _context.MainCategory.Where(u => u.CategoryStatus == Convert.ToInt32(Status.Approved)).ToListAsync();
                            if (model.categoryType == 1)
                            {
                                categoryDetail = categoryDetail.Where(u => u.Male == true).ToList();
                            }
                            else if (model.categoryType == 2)
                            {
                                categoryDetail = categoryDetail.Where(u => u.Female == true).ToList();
                            }
                            else if (model.categoryType == 3)
                            {
                                categoryDetail = categoryDetail.Where(u => u.Male == true && u.Female == true).ToList();
                            }
                            else
                            {
                                categoryDetail = categoryDetail;
                            }
                            // Categories = (_mapper.Map<List<CategoryDTO>>(categoryDetail));
                            Categories = new List<VendorCategoryDTO>();
                            foreach (var item in categoryDetail)
                            {
                                var mappedData = _mapper.Map<VendorCategoryDTO>(item);

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

                                subCategoryDetail = await _context.SubCategory.Where(u => u.MainCategoryId == item.mainCategoryId && u.CategoryStatus == Convert.ToInt32(Status.Approved)).ToListAsync();

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
                        _response.Messages = "Category" + ResponseMessages.msgFoundSuccess;
                        return Ok(_response);
                    }

                }
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = false;
                _response.Messages = ResponseMessages.msgNotFound + "record.";
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
                VendorCategoryDTO Category = new VendorCategoryDTO();
                if (model.subCategoryId > 0)
                {
                    var categoryDetail = await _context.SubCategory.Where(u => (u.SubCategoryId == model.subCategoryId)).FirstOrDefaultAsync();

                    Category = _mapper.Map<VendorCategoryDTO>(categoryDetail);
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
                        _response.Messages = ResponseMessages.msgNotFound + "record.";
                        return Ok(_response);
                    }
                    Category.createDate = (Convert.ToDateTime(Category.createDate)).ToString(@"dd-MM-yyyy");
                }
                else
                {
                    var categoryDetail = await _context.MainCategory.Where(u =>
                    (u.MainCategoryId == model.mainCategoryId)
                    ).FirstOrDefaultAsync();
                    Category = _mapper.Map<VendorCategoryDTO>(categoryDetail);
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
                        _response.Messages = ResponseMessages.msgNotFound + "record.";
                        return Ok(_response);
                    }
                    Category.createDate = (Convert.ToDateTime(Category.createDate)).ToString(@"dd-MM-yyyy");
                }

                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Data = Category;
                _response.Messages = "Category" + ResponseMessages.msgShownSuccess;
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
                    var salonService = await _context.SalonService.Where(u => (u.SubcategoryId == model.subCategoryId) && u.IsDeleted == false).FirstOrDefaultAsync();
                    if (salonService != null)
                    {
                        _response.StatusCode = HttpStatusCode.OK;
                        _response.IsSuccess = false;
                        _response.Messages = "Can't delete, while service is available in this category.";
                        return Ok(_response);
                    }



                    var categoryDetail = await _context.SubCategory.Where(u => (u.SubCategoryId == model.subCategoryId)).FirstOrDefaultAsync();
                    Category = _mapper.Map<CategoryDTO>(categoryDetail);
                    if (Category == null)
                    {
                        _response.StatusCode = HttpStatusCode.OK;
                        _response.IsSuccess = false;
                        _response.Messages = ResponseMessages.msgNotFound + "record.";
                        return Ok(_response);
                    }

                    // delete all service
                    var salonServiceDelete = await _context.SalonService.Where(u => (u.SubcategoryId == model.subCategoryId)).ToListAsync();
                    foreach (var item3 in salonServiceDelete)
                    {
                        var cart = await _context.Cart.Where(u => (u.ServiceId == item3.ServiceId)).ToListAsync();
                        _context.RemoveRange(cart);
                        await _context.SaveChangesAsync();

                        var timeslots = await _context.TimeSlot.Where(u => (u.ServiceId == item3.ServiceId)).ToListAsync();
                        _context.RemoveRange(timeslots);
                        await _context.SaveChangesAsync();

                        var bookedServices = await _context.BookedService.Where(u => (u.ServiceId == item3.ServiceId)).ToListAsync();

                        var appointmentIds = bookedServices.Select(u => u.AppointmentId).Distinct().ToList();
                        _context.RemoveRange(bookedServices);
                        await _context.SaveChangesAsync();

                        var appointments = await _context.Appointment
                                            .Where(u => appointmentIds.Contains(u.AppointmentId))
                                            .ToListAsync();

                        _context.RemoveRange(appointments);
                        await _context.SaveChangesAsync();
                    }

                    var vendorCategory = await _context.VendorCategory.Where(u => u.SubCategoryId == categoryDetail.SubCategoryId).ToListAsync();
                    foreach (var item in vendorCategory)
                    {
                        _context.Remove(item);
                        await _context.SaveChangesAsync();
                    }

                    // Delete s from cart releted to category
                    var cartDetail = await _context.Cart.ToListAsync();
                    foreach (var item in cartDetail)
                    {
                        var Detail = await _context.SalonService.Where(u => (u.ServiceId == item.ServiceId) && (u.SubcategoryId == model.subCategoryId)).ToListAsync();
                        _context.RemoveRange(Detail);
                        await _context.SaveChangesAsync();
                    }
                    _context.SubCategory.Remove(categoryDetail);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    var salonService = await _context.SalonService.Where(u => (u.MainCategoryId == model.mainCategoryId) && u.IsDeleted == false).FirstOrDefaultAsync();
                    if (salonService != null)
                    {
                        _response.StatusCode = HttpStatusCode.OK;
                        _response.IsSuccess = false;
                        _response.Messages = "Can't delete, while service is available in this category.";
                        return Ok(_response);
                    }

                    // delete all service
                    var salonServiceDelete = await _context.SalonService.Where(u => (u.MainCategoryId == model.mainCategoryId)).ToListAsync();
                    foreach (var item3 in salonServiceDelete)
                    {
                        var cart = await _context.Cart.Where(u => (u.ServiceId == item3.ServiceId)).ToListAsync();
                        _context.RemoveRange(cart);
                        await _context.SaveChangesAsync();

                        var timeslots = await _context.TimeSlot.Where(u => (u.ServiceId == item3.ServiceId)).ToListAsync();
                        _context.RemoveRange(timeslots);
                        await _context.SaveChangesAsync();

                        var bookedServices = await _context.BookedService.Where(u => (u.ServiceId == item3.ServiceId)).ToListAsync();

                        var appointmentIds = bookedServices.Select(u => u.AppointmentId).Distinct().ToList();
                        _context.RemoveRange(bookedServices);
                        await _context.SaveChangesAsync();

                        var appointments = await _context.Appointment
                                            .Where(u => appointmentIds.Contains(u.AppointmentId))
                                            .ToListAsync();

                        _context.RemoveRange(appointments);
                        await _context.SaveChangesAsync();
                    }


                    _context.RemoveRange(salonServiceDelete);
                    await _context.SaveChangesAsync();

                    var categoryDetail = await _context.MainCategory.Where(u =>
                    (u.MainCategoryId == model.mainCategoryId)
                    ).FirstOrDefaultAsync();
                    Category = _mapper.Map<CategoryDTO>(categoryDetail);
                    if (Category == null)
                    {
                        _response.StatusCode = HttpStatusCode.OK;
                        _response.IsSuccess = false;
                        _response.Messages = ResponseMessages.msgNotFound + "record.";
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
                    var cartDetail = await _context.Cart.ToListAsync();
                    foreach (var item in cartDetail)
                    {
                        var Detail = await _context.SalonService.Where(u => (u.ServiceId == item.ServiceId) && (u.MainCategoryId == model.mainCategoryId)).ToListAsync();
                        _context.RemoveRange(Detail);
                        await _context.SaveChangesAsync();
                    }
                    _context.Remove(categoryDetail);
                    await _context.SaveChangesAsync();
                }

                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Messages = "Category" + ResponseMessages.msgDeletionSuccess;
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
        [Authorize(Roles = "Vendor,SuperAdmin")]
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

                var user = await _userManager.FindByIdAsync(currentUserId);
                var userRoles = await _userManager.GetRolesAsync(user);

                var mainCategory = await _context.MainCategory.ToListAsync();
                var subCategory = await _context.SubCategory.ToListAsync();

                if (userRoles[0].ToString() == "Vendor")
                {
                    List<CategoryRequestDTO> Category = new List<CategoryRequestDTO>();

                    var maincategories = mainCategory.Where(u => u.CreatedBy == currentUserId).ToList();

                    maincategories = maincategories.OrderByDescending(u => u.CreateDate).ToList();

                    foreach (var item in maincategories)
                    {
                        var maincategory = new CategoryRequestDTO();

                        maincategory.mainCategoryId = item.MainCategoryId;
                        maincategory.maincategoryName = item.CategoryName;
                        maincategory.categorystatus = item.CategoryStatus;
                        maincategory.createDate = item.CreateDate.ToShortDateString();
                        maincategory.CategoryImageMale = item.CategoryImageMale;
                        maincategory.CategoryImageFemale = item.CategoryImageFemale;

                        if (item.Male == true && item.Female == true)
                        {
                            maincategory.categoryType = 3;
                        }
                        if (item.Male == false && item.Female == false)
                        {
                            maincategory.categoryType = 0;
                        }
                        if (item.Male == true && item.Female == false)
                        {
                            maincategory.categoryType = 1;
                        }
                        if (item.Male == false && item.Female == true)
                        {
                            maincategory.categoryType = 2;
                        }

                        Category.Add(maincategory);
                    }

                    var subcategories = subCategory.Where(u => u.CreatedBy == currentUserId).ToList();

                    subcategories = subcategories.OrderByDescending(u => u.CreateDate).ToList();

                    foreach (var item in subcategories)
                    {
                        var subcategory = new CategoryRequestDTO();
                        subcategory.mainCategoryId = item.MainCategoryId;
                        subcategory.subCategoryId = item.SubCategoryId;
                        subcategory.subcategoryName = item.CategoryName;
                        subcategory.categorystatus = item.CategoryStatus;
                        subcategory.createDate = item.CreateDate.ToShortDateString();
                        subcategory.CategoryImageMale = item.CategoryImageMale;
                        subcategory.CategoryImageFemale = item.CategoryImageFemale;

                        if (item.Male == true && item.Female == true)
                        {
                            subcategory.categoryType = 3;
                        }
                        if (item.Male == false && item.Female == false)
                        {
                            subcategory.categoryType = 0;
                        }
                        if (item.Male == true && item.Female == false)
                        {
                            subcategory.categoryType = 1;
                        }
                        if (item.Male == false && item.Female == true)
                        {
                            subcategory.categoryType = 2;
                        }

                        var maincategory = await _context.MainCategory.Where(u => u.MainCategoryId == item.MainCategoryId).FirstOrDefaultAsync();
                        subcategory.maincategoryName = maincategory.CategoryName;
                        Category.Add(subcategory);
                    }
                    if (Category.Count > 0)
                    {
                        _response.StatusCode = HttpStatusCode.OK;
                        _response.IsSuccess = true;
                        _response.Data = Category;
                        _response.Messages = "Category" + ResponseMessages.msgFoundSuccess;
                        return Ok(_response);
                    }
                }
                else
                {
                    List<CategoryRequestDTO> Categories = new List<CategoryRequestDTO>();

                    mainCategory = mainCategory.OrderByDescending(u => u.CreateDate).ToList();

                    foreach (var item in mainCategory)
                    {
                        var maincategory = new CategoryRequestDTO();
                        maincategory.mainCategoryId = item.MainCategoryId;
                        maincategory.maincategoryName = item.CategoryName;
                        maincategory.categorystatus = item.CategoryStatus;
                        maincategory.createDate = item.CreateDate.ToShortDateString();
                        maincategory.CategoryImageMale = item.CategoryImageMale;
                        maincategory.CategoryImageFemale = item.CategoryImageFemale;

                        if (item.Male == true && item.Female == true)
                        {
                            maincategory.categoryType = 3;
                        }
                        if (item.Male == false && item.Female == false)
                        {
                            maincategory.categoryType = 0;
                        }
                        if (item.Male == true && item.Female == false)
                        {
                            maincategory.categoryType = 1;
                        }
                        if (item.Male == false && item.Female == true)
                        {
                            maincategory.categoryType = 2;
                        }
                        Categories.Add(maincategory);
                    }

                    subCategory = subCategory.OrderByDescending(u => u.CreateDate).ToList();

                    foreach (var item in subCategory)
                    {
                        var subcategory = new CategoryRequestDTO();
                        subcategory.mainCategoryId = item.MainCategoryId;
                        subcategory.subCategoryId = item.SubCategoryId;
                        subcategory.subcategoryName = item.CategoryName;
                        subcategory.categorystatus = item.CategoryStatus;
                        subcategory.createDate = item.CreateDate.ToShortDateString();
                        subcategory.CategoryImageMale = item.CategoryImageMale;
                        subcategory.CategoryImageFemale = item.CategoryImageFemale;

                        if (item.Male == true && item.Female == true)
                        {
                            subcategory.categoryType = 3;
                        }
                        if (item.Male == false && item.Female == false)
                        {
                            subcategory.categoryType = 0;
                        }
                        if (item.Male == true && item.Female == false)
                        {
                            subcategory.categoryType = 1;
                        }
                        if (item.Male == false && item.Female == true)
                        {
                            subcategory.categoryType = 2;
                        }

                        var maincategory = await _context.MainCategory.Where(u => u.MainCategoryId == item.MainCategoryId).FirstOrDefaultAsync();
                        subcategory.maincategoryName = maincategory.CategoryName;
                        Categories.Add(subcategory);

                    }
                    if (Categories.Count > 0)
                    {
                        _response.StatusCode = HttpStatusCode.OK;
                        _response.IsSuccess = true;
                        _response.Data = Categories;
                        _response.Messages = "Category" + ResponseMessages.msgFoundSuccess;
                        return Ok(_response);
                    }
                }

                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = false;
                _response.Messages = ResponseMessages.msgNotFound + "record.";
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
                        _response.Messages = ResponseMessages.msgNotFound + "record.";
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
                        _response.Messages = ResponseMessages.msgNotFound + "record.";
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
                _response.Messages = "Category status" + ResponseMessages.msgUpdationSuccess;
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
