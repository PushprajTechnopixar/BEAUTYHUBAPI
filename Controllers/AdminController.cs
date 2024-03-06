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
using System.Net.Http.Headers;
using BeautyHubAPI.Repository;
using BeautyHubAPI.Helpers;
using MimeKit;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Infrastructure;
using GSF;
using BeautyHubAPI.Common;

namespace BeautyHubAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly IUserRepository _userRepo;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly IUploadRepository _uploadRepository;
        private readonly IAdminRepository _adminRepository;
        private readonly IMapper _mapper;
        private readonly IEmailManager _emailSender;
        private readonly IWebHostEnvironment _hostingEnvironment;
        protected APIResponse _response;
        private readonly IBannerRepository _bannerRepository;
        private readonly IMembershipRecordRepository _membershipRecordRepository;
        private readonly IContentRepository _contentRepository;
        private readonly UPIService _upiService;

        public AdminController(
                 IUserRepository userRepo,
                 IAdminRepository adminRepository,
                 IMapper mapper,
                 UserManager<ApplicationUser> userManager,
                 IUploadRepository uploadRepository,
                 IBannerRepository bannerRepository,
                 ApplicationDbContext context,
                 IMembershipRecordRepository membershipRecordRepository,
                 IContentRepository contentRepository,
                 IWebHostEnvironment hostingEnvironment,
                 IEmailManager emailSender,
                 UPIService upiService
                 )
        {
            _userRepo = userRepo;
            _adminRepository = adminRepository;
            _response = new();
            _mapper = mapper;
            _context = context;
            _userManager = userManager;
            _uploadRepository = uploadRepository;
            _bannerRepository = bannerRepository;
            _contentRepository = contentRepository;
            _membershipRecordRepository = membershipRecordRepository;
            _hostingEnvironment = hostingEnvironment;
            _emailSender = emailSender;
            _upiService = upiService;
        }


        #region GetSuperAdminDetail
        /// <summary>
        ///  Get super admin detail.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Authorize(Roles = "SuperAdmin")]
        [Route("GetSuperAdminDetail")]
        public async Task<IActionResult> GetSuperAdminDetail()
        {
            var currentUserId = HttpContext.User.Claims.First().Value;
            var adminDetail = _userManager.FindByIdAsync(currentUserId).GetAwaiter().GetResult();
            if (adminDetail == null)
            {
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = false;
                _response.Messages = ResponseMessages.msgUserNotFound;
                return Ok(_response);
            }

            SuperAdminResponseDTO vendorsalonResponse = new SuperAdminResponseDTO();

            var userToReturn = _context.ApplicationUsers
                        .FirstOrDefault(u => u.Id == currentUserId);
            var userDetail = _context.UserDetail
                        .FirstOrDefault(u => (u.UserId == currentUserId) && (u.IsDeleted == false));
            if (userDetail == null)
            {
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = false;
                _response.Messages = ResponseMessages.msgUserNotFound;
                return Ok(_response);
            }

            var response = await _adminRepository.GetSuperAdminDetail(currentUserId);
            return Ok(response);

          
        }
        #endregion

        #region UpdateSuperAdminDetail
        /// <summary>
        ///  Update super admin detail.
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Authorize(Roles = "SuperAdmin")]
        [Route("UpdateSuperAdminDetail")]
        public async Task<IActionResult> UpdateSuperAdminDetail([FromBody] UpdateSuperAdminDTO model)
        {
            var currentUserId = HttpContext.User.Claims.First().Value;
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
                model.id = currentUserId;
            }
            var adminDetail = _userManager.FindByIdAsync(model.id).GetAwaiter().GetResult();
            if (adminDetail == null)
            {
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = false;
                _response.Messages = ResponseMessages.msgUserNotFound;
                return Ok(_response);
            }

            if ((adminDetail.Email.ToLower() != model.email.ToLower()))
            {
                bool ifUserEmailUnique = _userRepo.IsUniqueEmail(model.email);
                if (!ifUserEmailUnique)
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = "Email already exists.";
                    return Ok(_response);
                }
            }

            if ((adminDetail.PhoneNumber != model.phoneNumber))
            {
                bool ifUserPhoneUnique = _userRepo.IsUniquePhone(model.phoneNumber);
                if (!ifUserPhoneUnique)
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = "Phone already exists.";
                    return Ok(_response);
                }
            }

            if (Gender.Male.ToString() != model.gender && Gender.Female.ToString() != model.gender && Gender.Others.ToString() != model.gender)
            {
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = false;
                _response.Messages = "Please enter valid gender.";
                return Ok(_response);
            }

            var response = await _adminRepository.UpdateSuperAdminDetail(model, currentUserId);
            return Ok(response);
           
        }
        #endregion

        #region GetPaymentOptions
        /// <summary>
        ///  Get payment option.
        /// </summary>
        [HttpGet("GetPaymentOptions")]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Authorize(Roles = "SuperAdmin,Admin,Vendor")]
        public async Task<IActionResult> GetPaymentOptions(int? membershipPlanId)
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

                var planDetail = await _context.MembershipPlan.Where(u => u.MembershipPlanId == membershipPlanId).FirstOrDefaultAsync();
                if (planDetail == null)
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = ResponseMessages.msgNotFound + "plan";
                    return Ok(_response);
                }

                // cartDetail.shopCount = getCartShopIdList.Count;

                var adminDetail = _userManager.FindByEmailAsync("superadmin@beautyhub.com").GetAwaiter().GetResult();
                if (adminDetail == null)
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = ResponseMessages.msgNotFound + "record";
                    return Ok(_response);
                }

                var response = await _adminRepository.GetPaymentOptions(membershipPlanId);
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

        #region AddBanner
        [HttpPost]
        [Route("AddBanner")]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Authorize]
        public async Task<IActionResult> AddBanner([FromForm] AddBannerDTO model)
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

                var response = await _adminRepository.AddBanner(model);
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

        #region UpdateBanner
        [HttpPost]
        [Route("UpdateBanner")]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Authorize]
        public async Task<IActionResult> UpdateBanner([FromForm] UpdateBannerDTO model)
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

                var banner = await _bannerRepository.GetAsync(u => u.BannerId == model.bannerId);
                if (banner == null)
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = ResponseMessages.msgNotFound + "record.";
                    return Ok(_response);
                }
              
                var response = await _adminRepository.UpdateBanner(model);
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

        #region DeleteBanner
        [HttpDelete]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Route("DeleteBanner")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> DeleteBanner(int bannerId)
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
              
                var response = await _adminRepository.DeleteBanner(bannerId);
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

        #region addUpdateMembershipPlan 
        /// <summary>
        /// Add and update membership plan.
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Authorize(Roles = "SuperAdmin")]
        [Route("AddUpdateMembershipPlan")]
        public async Task<IActionResult> addUpdateMembershipPlan(MembershipPlanDTO model)
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

                if (model.gsttype != GSTTypes.Exclusive.ToString() && model.gsttype != GSTTypes.Inclusive.ToString())
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = "Please enter valid GST type.";
                    return Ok(_response);
                }
             
                var response = await _adminRepository.addUpdateMembershipPlan(model);
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

        #region getMembershipPlanList
        /// <summary>
        /// Get membership plan list.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Authorize]
        [Route("getMembershipPlanList")]
        public async Task<IActionResult> getMembershipPlanList(string? searchQuery, string? vendorId, int? planType)
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

                var response = await _adminRepository.getMembershipPlanList(searchQuery, vendorId, planType);
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

        #region getMembershipPlanDetail
        /// <summary>
        /// Get membership plan detail.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Authorize]
        [Route("GetMembershipPlanDetail")]
        public async Task<IActionResult> getMembershipPlanDetail(int membershipPlanId)
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
               
                var response = await _adminRepository.getMembershipPlanDetail(membershipPlanId);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    status = false,
                    data = new { },
                    message = ResponseMessages.msgSomethingWentWrong + ex.Message,
                    code = StatusCodes.Status500InternalServerError
                });
            }
        }
        #endregion

        #region deleteMembershipPlan
        /// <summary>
        /// Delete membership plan.
        /// </summary>
        /// <returns></returns>
        [HttpDelete]
        [Authorize(Roles = "SuperAdmin")]
        [Route("DeleteMembershipPlan")]
        public async Task<IActionResult> deleteMembershipPlan(int membershipPlanId)
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
                var membershipDetail = await _context.MembershipPlan.Where(x => (x.MembershipPlanId == membershipPlanId) && (x.IsDeleted != true)).FirstOrDefaultAsync();
                if (membershipDetail == null)
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = ResponseMessages.msgNotFound + "record.";
                    return Ok(_response);
                }
              
                var response = await _adminRepository.deleteMembershipPlan(membershipPlanId);
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

        #region AddVendor
        /// <summary>
        ///  Add vendor.
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Authorize(Roles = "SuperAdmin,Admin")]
        [Route("AddVendor")]
        public async Task<IActionResult> AddVendor([FromBody] AddVendorSalonDTO model)
        {
            var currentUserId = HttpContext.User.Claims.First().Value;
            var adminDetail = _userManager.FindByIdAsync(currentUserId).GetAwaiter().GetResult();
            if (adminDetail == null)
            {
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = false;
                _response.Messages = ResponseMessages.msgUserNotFound;
                return Ok(_response);
            }

            bool ifUserNameUnique = _userRepo.IsUniqueUser(model.email, model.phoneNumber);
            if (!ifUserNameUnique)
            {
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = false;
                _response.Messages = "Email or phone numeber already exists.";
                return Ok(_response);
            }

            if (Gender.Male.ToString() != model.gender && Gender.Female.ToString() != model.gender && Gender.Others.ToString() != model.gender)
            {
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = false;
                _response.Messages = "Please enter valid gender.";
                return Ok(_response);
            }

            if (SalonType.Male.ToString() != model.salonDetail.First().salonType && SalonType.Female.ToString() != model.salonDetail.First().salonType && SalonType.Unisex.ToString() != model.salonDetail.First().salonType)
            {
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = false;
                _response.Messages = "Please enter valid salon type.";
                return Ok(_response);
            }

            var response = await _adminRepository.AddVendor(model, currentUserId);
            return Ok(response);
        }
        #endregion

        #region UpdateVendor
        /// <summary>
        ///  Update vendor.
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Authorize(Roles = "SuperAdmin,Admin,Vendor")]
        [Route("UpdateVendor")]
        public async Task<IActionResult> UpdateVendor([FromBody] UpdateVendorSalonDTO model)
        {
            var currentUserId = HttpContext.User.Claims.First().Value;
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
                model.vendorId = currentUserId;
            }
            var adminDetail = _userManager.FindByIdAsync(model.vendorId).GetAwaiter().GetResult();
            if (adminDetail == null)
            {
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = false;
                _response.Messages = ResponseMessages.msgUserNotFound;
                return Ok(_response);
            }

            if ((adminDetail.Email.ToLower() != model.email.ToLower()))
            {
                bool ifUserEmailUnique = _userRepo.IsUniqueEmail(model.email);
                if (!ifUserEmailUnique)
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = ResponseMessages.msgEmailAlreadyUsed;
                    return Ok(_response);
                }
            }

            if ((adminDetail.PhoneNumber != model.phoneNumber))
            {
                bool ifUserPhoneUnique = _userRepo.IsUniquePhone(model.phoneNumber);
                if (!ifUserPhoneUnique)
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = "Phone already exists.";
                    return Ok(_response);
                }
            }

            if (Gender.Male.ToString() != model.gender && Gender.Female.ToString() != model.gender && Gender.Others.ToString() != model.gender)
            {
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = false;
                _response.Messages = "Please enter valid gender.";
                return Ok(_response);
            }
          
            var response = await _adminRepository.UpdateVendor(model, currentUserId);
            return Ok(response);
        }
        #endregion

        #region GetVendorList
        /// <summary>
        ///  Get vendor list.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Authorize(Roles = "SuperAdmin,Admin")]
        [Route("GetVendorList")]
        public async Task<IActionResult> GetVendorList([FromQuery] FilterationListDTO model, string? createdBy, string? status, string? salonType)
        {
            var currentUserId = HttpContext.User.Claims.First().Value;
            var adminDetail = _userManager.FindByIdAsync(currentUserId).GetAwaiter().GetResult();
            if (adminDetail == null)
            {
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = false;
                _response.Messages = ResponseMessages.msgUserNotFound;
                return Ok(_response);
            }

          
            var response = await _adminRepository.GetVendorList(model, createdBy, status, salonType, currentUserId);
            return Ok(response);
        }
        #endregion

        #region GetVendorDetail
        /// <summary>
        ///  Get vendor detail.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Authorize(Roles = "SuperAdmin,Admin,Vendor,Distributor")]
        [Route("GetVendorDetail")]
        public async Task<IActionResult> GetVendorDetail(string vendorId)
        {
            var currentUserId = HttpContext.User.Claims.First().Value;
            var adminDetail = _userManager.FindByIdAsync(currentUserId).GetAwaiter().GetResult();
            if (adminDetail == null)
            {
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = false;
                _response.Messages = ResponseMessages.msgUserNotFound;
                return Ok(_response);
            }

            VendorSalonResponseDTO vendorsalonResponse = new VendorSalonResponseDTO();

            var userToReturn = _context.ApplicationUsers
                        .FirstOrDefault(u => u.Id == vendorId);
            var userDetail = _context.UserDetail
                        .FirstOrDefault(u => (u.UserId == vendorId) && (u.IsDeleted == false));
            if (userDetail == null)
            {
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = false;
                _response.Messages = "User does not exists.";
                return Ok(_response);
            }
          
            var response = await _adminRepository.GetVendorDetail(vendorId, currentUserId);
            return Ok(response);
        }
        #endregion

        #region DeleteVendor
        /// <summary>
        ///  Delete vendor.
        /// </summary>
        /// <returns></returns>
        [HttpDelete]
        [Authorize(Roles = "SuperAdmin,Admin,Distributor")]
        [Route("DeleteVendor")]
        public async Task<IActionResult> DeleteVendor([FromQuery] string VendorId)
        {
            var currentUserId = HttpContext.User.Claims.First().Value;
            if (currentUserId == null)
            {
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = false;
                _response.Messages = "Token expired.";
                return Ok(_response);
            }
            if (string.IsNullOrEmpty(VendorId))
            {
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = false;
                _response.Messages = ResponseMessages.msgNotFound + "User";
                return Ok(_response);
            }
            var vendorDetail = _userManager.FindByIdAsync(VendorId).GetAwaiter().GetResult();
            if (vendorDetail == null)
            {
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = false;
                _response.Messages = ResponseMessages.msgUserNotFound;
                return Ok(_response);
            }
           
            var response = await _adminRepository.DeleteVendor(VendorId);
            return Ok(response);

        }
        #endregion

        #region SetVendorStatus
        /// <summary>
        ///  Set vendor status [Pending = 0; Approved = 1; Rejected = 2].
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Authorize(Roles = "SuperAdmin,Distributor,Admin")]
        [Route("SetVendorStatus")]
        public async Task<IActionResult> SetVendorStatus([FromBody] SetVendorStatusDTO model)
        {
            var salonDetails = await _context.SalonDetail.Where(u => (u.VendorId == model.vendorId) && (u.SalonId == model.salonId)).FirstOrDefaultAsync();
            if (salonDetails == null)
            {
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = false;
                _response.Messages = ResponseMessages.msgNotFound + "record.";
                return Ok(_response);
            }
           
            var response = await _adminRepository.SetVendorStatus(model);
            return Ok(response);
        }
        #endregion

        #region AddAdminUser
        /// <summary>
        ///  Add admin user.
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Authorize(Roles = "SuperAdmin")]
        [Route("AddAdminUser")]
        public async Task<IActionResult> AddAdminUser([FromBody] AdminUserRegisterationRequestDTO model)
        {
            bool ifUserNameUnique = _userRepo.IsUniqueUser(model.email, model.phoneNumber);
            if (!ifUserNameUnique)
            {
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = false;
                _response.Messages = "Email or phone numeber already exists.";
                return Ok(_response);
            }

            if (Gender.Male.ToString() != model.gender && Gender.Female.ToString() != model.gender && Gender.Others.ToString() != model.gender)
            {
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = false;
                _response.Messages = "Please enter valid gender.";
                return Ok(_response);
            }

            var response = await _adminRepository.AddAdminUser(model);
            return Ok(response);
        }
        #endregion

        #region UpdateAdminUser
        /// <summary>
        ///  Update admin user.
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Authorize(Roles = "SuperAdmin,Admin")]
        [Route("UpdateAdminUser")]
        public async Task<IActionResult> UpdateAdminUser([FromBody] UpdateAdminUserDTO model)
        {
            var currentUserId = HttpContext.User.Claims.First().Value;
            var currentUserDetail = _userManager.FindByIdAsync(currentUserId).GetAwaiter().GetResult();
            if (currentUserDetail == null)
            {
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = false;
                _response.Messages = ResponseMessages.msgUserNotFound;
                return Ok(_response);
            }
            var roles = await _userManager.GetRolesAsync(currentUserDetail);
            if (roles[0].ToString() == "Admin")
            {
                model.id = currentUserId;
            }
            var adminDetail = _userManager.FindByIdAsync(model.id).GetAwaiter().GetResult();
            if (adminDetail == null)
            {
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = false;
                _response.Messages = "User does not exists.";
                return Ok(_response);
            }
            if ((adminDetail.Email.ToLower() != model.email.ToLower()))
            {
                bool ifUserEmailUnique = _userRepo.IsUniqueEmail(model.email);
                if (!ifUserEmailUnique)
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = "Email already exists.";
                    return Ok(_response);
                }
            }
            if ((adminDetail.PhoneNumber != model.phoneNumber))
            {
                bool ifUserPhoneUnique = _userRepo.IsUniquePhone(model.phoneNumber);
                if (!ifUserPhoneUnique)
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = "Phone already exists.";
                    return Ok(_response);
                }
            }

            if (Gender.Male.ToString() != model.gender && Gender.Female.ToString() != model.gender && Gender.Others.ToString() != model.gender)
            {
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = false;
                _response.Messages = "Please enter valid gender.";
                return Ok(_response);
            }

         
            var response = await _adminRepository.UpdateAdminUser(model, currentUserId);
            return Ok(response);
        }
        #endregion

        #region GetAdminUserList
        /// <summary>
        ///  Get admin user list.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        // [Authorize(Roles = "SuperAdmin")]
        [Route("GetAdminUserList")]
        public async Task<IActionResult> GetAdminUserList([FromQuery] FilterationListDTO? model)
        {
            var adminUsers = await _userManager.GetUsersInRoleAsync(Role.Admin.ToString());
            if (adminUsers.Count < 1)
            {
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = false;
                _response.Messages = ResponseMessages.msgNotFound + "record.";
                return Ok(_response);
            }

            var response = await _adminRepository.GetAdminUserList(model);
            return Ok(response);
        }
        #endregion

        #region GetAdminUserDetail
        /// <summary>
        ///  Get admin user detail.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Authorize(Roles = "SuperAdmin,Admin")]
        [Route("GetAdminUserDetail")]
        public async Task<IActionResult> GetAdminUserDetail([FromQuery] string id)
        {
            var currentUserId = HttpContext.User.Claims.First().Value;
            if (!string.IsNullOrEmpty(id))
            {
                currentUserId = id;
            }
            var adminDetail = _userManager.FindByIdAsync(currentUserId).GetAwaiter().GetResult();
            if (adminDetail == null)
            {
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = false;
                _response.Messages = ResponseMessages.msgUserNotFound;
                return Ok(_response);
            }
            var adminUserProfileDetail = await _context.UserDetail.FirstOrDefaultAsync(u => (u.UserId == adminDetail.Id) && (u.IsDeleted == false));
            if (adminUserProfileDetail == null)
            {
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = false;
                _response.Messages = ResponseMessages.msgUserNotFound;
                return Ok(_response);
            }
           
            var response = await _adminRepository.GetAdminUserDetail(id, currentUserId);
            return Ok(response);

        }
        #endregion

        #region DeleteAdminUser
        /// <summary>
        ///  Delete admin user.
        /// </summary>
        /// <returns></returns>
        [HttpDelete]
        [Authorize(Roles = "SuperAdmin,Admin")]
        [Route("DeleteAdminUser")]
        public async Task<IActionResult> DeleteAdminUser([FromQuery] string id)
        {
            var currentUserId = HttpContext.User.Claims.First().Value;
            if (currentUserId == null)
            {
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = false;
                _response.Messages = "Token expired.";
                return Ok(_response);
            }
            if (string.IsNullOrEmpty(id))
            {
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = false;
                _response.Messages = ResponseMessages.msgNotFound + "User";
                return Ok(_response);
            }
            var adminDetail = _userManager.FindByIdAsync(id).GetAwaiter().GetResult();
            if (adminDetail == null)
            {
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = false;
                _response.Messages = ResponseMessages.msgUserNotFound;
                return Ok(_response);
            }
            var adminUserProfileDetail = await _context.UserDetail.FirstOrDefaultAsync(u => (u.UserId == adminDetail.Id) && (u.IsDeleted == false));
            if (adminUserProfileDetail == null)
            {
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = false;
                _response.Messages = ResponseMessages.msgUserNotFound;
                return Ok(_response);
            }

            var response = await _adminRepository.DeleteAdminUser(id);
            return Ok(response);
        }
        #endregion

    }
}

