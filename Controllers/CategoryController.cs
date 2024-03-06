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
        private readonly ICategoryRepository _categoryRepository;
        private readonly IMapper _mapper;
        protected APIResponse _response;
        public CategoryController(
            IMapper mapper,
            UserManager<ApplicationUser> userManager,
             ICategoryRepository categoryRepository,
            ApplicationDbContext context
            )
        {
            _response = new();
            _mapper = mapper;
            _categoryRepository = categoryRepository;
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

                var response = await _categoryRepository.AddCategory(model, currentUserId);
                return Ok(response);
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

                var response = await _categoryRepository.UpdateCategory(model, currentUserId);
                return Ok(response);
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


                var response = await _categoryRepository.GetSubCategoryType(mainCategoryId);
                return Ok(response);

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

                var response = await _categoryRepository.GetCategoryList(model, currentUserId);
                return Ok(response);
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
                var response = await _categoryRepository.GetCategoryDetail(model);
                return Ok(response);
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
                var response = await _categoryRepository.DeleteCategory(model);
                return Ok(response);
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


                var response = await _categoryRepository.GetCategoryRequests(currentUserId);
                return Ok(response);
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


                var response = await _categoryRepository.SetCategoryStatus(model);
                return Ok(response);
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
