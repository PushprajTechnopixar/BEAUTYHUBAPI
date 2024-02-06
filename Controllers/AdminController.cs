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
            var mappedData = _mapper.Map<SuperAdminResponseDTO>(userToReturn);
            var createdByDetail = _context.ApplicationUsers.FirstOrDefault(u => u.Id == currentUserId);
            mappedData.createdByName = createdByDetail.FirstName + " " + createdByDetail.LastName;
            mappedData.id = userToReturn.Id;
            mappedData.createdBy = userDetail.CreatedBy;
            mappedData.gender = userDetail.Gender;
            var roles = await _userManager.GetRolesAsync(userToReturn);
            mappedData.countryId = userDetail.CountryId;
            mappedData.stateId = userDetail.StateId;
            mappedData.gender = userDetail.Gender;
            mappedData.profilePic = userDetail.ProfilePic;
            mappedData.dialCode = userDetail.DialCode;

            if (userDetail.CountryId != null && userDetail.StateId != null)
            {
                var countryDetail = await _contentRepository.GetCountryById(userDetail.CountryId);
                var stateDetail = await _contentRepository.GetStateById(userDetail.StateId);
                mappedData.countryName = countryDetail.countryName;
                mappedData.stateName = stateDetail.stateName;
            }

            var banks = await _context.BankDetail.Where(u => u.UserId == currentUserId).ToListAsync();
            banks = banks.OrderByDescending(u => u.ModifyDate).ToList();
            var banksResponse = new List<BankResponseDTO>();
            foreach (var item in banks)
            {
                var bankDetail = _mapper.Map<BankResponseDTO>(item);
                banksResponse.Add(bankDetail);
            }

            var getupiList = await _context.Upidetail.Where(u => u.UserId == currentUserId).ToListAsync();
            var upiResponse = _mapper.Map<List<UPIResponseDTO>>(getupiList);

            mappedData.bankResponses = banksResponse;
            mappedData.upiResponses = upiResponse;

            _response.StatusCode = HttpStatusCode.OK;
            _response.IsSuccess = true;
            _response.Data = mappedData;
            _response.Messages = "Detail" + ResponseMessages.msgShownSuccess;
            return Ok(_response);
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
            // var upiCheck = model.upiDetail.Where(u => u.isActive == true).ToList();
            // if (upiCheck.Count > 1)
            // {
            //     _response.StatusCode = HttpStatusCode.OK;
            //     _response.IsSuccess = false;
            //     _response.Messages = "Can't activate two UPI accounts simultaneously.";
            //     return Ok(_response);
            // }
            var bankStatusCheck = model.bankDetail.Where(u => u.isActive == true).ToList();
            if (bankStatusCheck.Count > 1)
            {
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = false;
                _response.Messages = "Can't activate two UPI accounts simultaneously.";
                return Ok(_response);
            }
            var user = _mapper.Map(model, adminDetail);
            await _userManager.UpdateAsync(user);

            // Update user detail
            UserDetail? userdetail = await _context.UserDetail.Where(u => u.UserId == model.id).FirstOrDefaultAsync();
            userdetail.CountryId = model.countryId;
            userdetail.StateId = model.stateId;
            userdetail.DialCode = model.dialCode;
            userdetail.Gender = model.gender;

            // userdetail.ModifyDate = DateTime.Now;
            _context.Update(userdetail);
            await _context.SaveChangesAsync();

            SuperAdminResponseDTO superAdminDetail = new SuperAdminResponseDTO();
            _mapper.Map(user, superAdminDetail);
            _mapper.Map(userdetail, superAdminDetail);

            if (userdetail.CountryId != null && userdetail.StateId != null)
            {
                var countryDetail = await _contentRepository.GetCountryById(userdetail.CountryId);
                var stateDetail = await _contentRepository.GetStateById(userdetail.StateId);
                superAdminDetail.countryName = countryDetail.countryName;
                superAdminDetail.stateName = stateDetail.stateName;
            }

            // Update bank detail
            var existingBankList = await _context.BankDetail.Where(u => u.UserId == user.Id).ToArrayAsync();
            List<int> previousBanks = existingBankList.Select(u => u.BankId).ToList();
            List<int> updateBanks = model.bankDetail.Select(u => u.bankId).ToList();
            List<int> subtractedListToRemove = previousBanks.Except(updateBanks).ToList();
            foreach (var item in subtractedListToRemove)
            {
                var removeBankId = await _context.BankDetail.Where(u => u.BankId == item).FirstOrDefaultAsync();
                _context.Remove(removeBankId);
                await _context.SaveChangesAsync();
            }
            foreach (var item in model.bankDetail)
            {
                BankDetail bankDetail = await _context.BankDetail.Where(u => u.BankId == item.bankId).FirstOrDefaultAsync();
                if (bankDetail != null)
                {
                    var updateBank = _mapper.Map(item, bankDetail);
                    _context.Update(updateBank);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    var addBank = _mapper.Map<BankDetail>(item);
                    addBank.UserId = user.Id;
                    await _context.BankDetail.AddAsync(addBank);
                    await _context.SaveChangesAsync();
                }
            }
            var banks = await _context.BankDetail.Where(u => u.UserId == model.id).ToListAsync();
            var banksResponse = _mapper.Map<List<BankResponseDTO>>(banks);

            // Update upi detail
            var existingupiList = await _context.Upidetail.Where(u => u.UserId == user.Id).ToListAsync();
            // List<int> previousUPIs = existingupiList.Select(u => u.UpidetailId).ToList();
            // List<int> updateUPIs = model.upiDetail.Select(u => u.upidetailId).ToList();
            // List<int> subtractedUPIListToRemove = previousUPIs.Except(updateUPIs).ToList();
            // foreach (var item in subtractedUPIListToRemove)
            // {
            //     var removeUPIId = await _context.Upidetail.Where(u => u.UpidetailId == item).FirstOrDefaultAsync();
            //     _context.Remove(removeUPIId);
            //     await _context.SaveChangesAsync();
            // }
            // foreach (var item in model.upiDetail)
            // {
            //     var upiDetail = await _context.Upidetail.Where(u => u.UpidetailId == item.upidetailId).FirstOrDefaultAsync();
            //     if (upiDetail == null)
            //     {
            //         var addUPI = _mapper.Map<Upidetail>(item);
            //         addUPI.UserId = user.Id;
            //         await _context.Upidetail.AddAsync(addUPI);
            //         await _context.SaveChangesAsync();
            //     }
            //     else
            //     {
            //         var updateUPI = _mapper.Map(item, upiDetail);
            //         _context.Upidetail.Update(updateUPI);
            //         await _context.SaveChangesAsync();
            //     }
            // }
            // var getupiList = await _context.Upidetail.Where(u => u.UserId == model.id).ToListAsync();
            // var upiResponse = _mapper.Map<List<UPIResponseDTO>>(getupiList);

            superAdminDetail.bankResponses = banksResponse;
            // superAdminDetail.upiResponses = upiResponse;

            if (superAdminDetail == null)
            {
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = false;
                _response.Messages = "Error while updating.";
                return Ok(_response);
            }
            _response.StatusCode = HttpStatusCode.OK;
            _response.IsSuccess = true;
            _response.Data = superAdminDetail;
            _response.Messages = "profile" + ResponseMessages.msgUpdationSuccess;
            return Ok(_response);
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

                var upiDetail = await _context.Upidetail.Where(u => u.UserId == adminDetail.Id && u.IsActive == true).FirstOrDefaultAsync();
                // if (upiDetail == null)
                // {
                //     _response.StatusCode = HttpStatusCode.OK;
                //     _response.IsSuccess = false;
                //     _response.Messages = "Not found any record.";
                //     return Ok(_response);
                // }
                var bankDetail = await _context.BankDetail.Where(u => u.UserId == adminDetail.Id && u.IsActive == true).FirstOrDefaultAsync();

                PaymentOptionsDTO paymentOptions = new PaymentOptionsDTO();

                if (upiDetail != null)
                {
                    paymentOptions.upiId = upiDetail.Upiid;
                    paymentOptions.qrCode = upiDetail.Qrcode;

                    // Get the necessary payment details
                    string upiId = upiDetail.Upiid;
                    decimal amount = (decimal)planDetail.PlanPrice;
                    string description = "Vendor registartion";
                    string name = "BeautyHub";
                    string mc = "";
                    string trr = CommonMethod.GenerateOrderId();

                    // Build the UPI URI
                    string upiUri = _upiService.BuildUPIUri(upiId, name, mc, amount, description);

                    paymentOptions.directLink = upiUri;
                    paymentOptions.paytmLink = paymentOptions.directLink.Replace($"upi://pay", $"paytm://upi/pay");
                    paymentOptions.gpayLink = paymentOptions.directLink.Replace($"upi://pay", $"gpay://upi/pay");
                    paymentOptions.phonePeLink = paymentOptions.directLink.Replace($"upi://pay", $"phonepay://upi/pay");
                }
                paymentOptions.accountDetail = _mapper.Map<AccountDetailDTO>(bankDetail);


                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Data = paymentOptions;
                _response.Messages = "Payment option" + ResponseMessages.msgShownSuccess;
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

                var banner = _mapper.Map<Banner>(model);

                var documentFile = ContentDispositionHeaderValue.Parse(model.bannerImage.ContentDisposition).FileName.Trim('"');
                documentFile = CommonMethod.EnsureCorrectFilename(documentFile);
                documentFile = CommonMethod.RenameFileName(documentFile);

                var documentPath = bannerImageContainer + documentFile;
                bool uploadStatus = await _uploadRepository.UploadFilesToServer(
                        model.bannerImage,
                        bannerImageContainer,
                        documentFile
                    );
                banner.BannerImage = documentPath;
                banner.BannerType = BannerType.Home.ToString();

                await _bannerRepository.CreateEntity(banner);

                var getBanner = await _bannerRepository.GetAsync(u => u.BannerId == banner.BannerId);

                if (getBanner != null)
                {
                    var bannerDetail = _mapper.Map<BannerDTO>(getBanner);
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = true;
                    _response.Data = bannerDetail;
                    _response.Messages = "Banner" + ResponseMessages.msgAdditionSuccess;
                    return Ok(_response);
                }
                else
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Data = new Object { };
                    _response.Messages = ResponseMessages.msgSomethingWentWrong;
                    return Ok(_response);
                }
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
                // Delete previous file
                //if (!string.IsNullOrEmpty(banner.BannerImage))
                //{
                //    var chk = await _uploadRepository.DeleteFilesFromServer("FileToSave/" + banner.BannerImage);
                //}

                var documentFile = ContentDispositionHeaderValue.Parse(model.bannerImage.ContentDisposition).FileName.Trim('"');
                documentFile = CommonMethod.EnsureCorrectFilename(documentFile);
                documentFile = CommonMethod.RenameFileName(documentFile);

                var documentPath = bannerImageContainer + documentFile;
                bool uploadStatus = await _uploadRepository.UploadFilesToServer(
                        model.bannerImage,
                        bannerImageContainer,
                        documentFile
                    );
                banner.BannerImage = documentPath;
                banner.BannerType = BannerType.Home.ToString();

                await _bannerRepository.UpdateBanner(banner);
                var bannerDetail = _mapper.Map<BannerDTO>(banner);

                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Data = bannerDetail;
                _response.Messages = "Banner" + ResponseMessages.msgUpdationSuccess;
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
                var getBanner = await _bannerRepository.GetAsync(u => u.BannerId == bannerId);

                if (getBanner != null)
                {
                    await _bannerRepository.RemoveEntity(getBanner);
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = true;
                    _response.Messages = "Banner" + ResponseMessages.msgDeletionSuccess;
                    return Ok(_response);
                }
                else
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Data = new Object { };
                    _response.Messages = ResponseMessages.msgNotFound + "record";
                    return Ok(_response);
                }
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
                var mapData = _mapper.Map<MembershipPlan>(model);
                if (model.gsttype == GSTTypes.Exclusive.ToString())
                {
                    if (model.discountInPercentage > 0)
                    {
                        mapData.PlanPrice = (double)(mapData.PlanPrice - (mapData.PlanPrice * mapData.DiscountInPercentage) / 100);
                    }
                    mapData.TotalAmount = mapData.PlanPrice + (mapData.PlanPrice * mapData.GstinPercentage) / 100;
                    mapData.Gsttax = mapData.TotalAmount - mapData.PlanPrice;
                }
                else
                {
                    mapData.TotalAmount = mapData.PlanPrice;
                    mapData.Gsttax = mapData.PlanPrice - (mapData.PlanPrice / (1 + (mapData.GstinPercentage / 100)));
                    mapData.PlanPrice = (double)(mapData.PlanPrice / (1 + (mapData.GstinPercentage / 100)));
                    if (model.discountInPercentage > 0)
                    {
                        mapData.PlanPrice = (double)(mapData.PlanPrice - (mapData.PlanPrice / (1 + (mapData.GstinPercentage / 100))));
                    }
                }

                if (model.membershipPlanId < 1)
                {
                    // Add membership plan
                    _context.Add(mapData);
                    _context.SaveChanges();

                    // set popular is false for other plans
                    if (mapData.IsPopular == true)
                    {
                        var updatePopularPlan = await _context.MembershipPlan.Where(a => (a.MembershipPlanId != mapData.MembershipPlanId) && (a.IsPopular == true)).FirstOrDefaultAsync();
                        if (updatePopularPlan != null)
                        {
                            updatePopularPlan.IsPopular = false;
                            _context.Update(updatePopularPlan);
                            _context.SaveChanges();
                        }
                    }
                    var responseData = _mapper.Map<GetMembershipPlanDTO>(mapData);
                    responseData.planDurationName = ((TimePeriod)mapData.PlanDuration).ToString();

                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = true;
                    _response.Data = responseData;
                    _response.Messages = "Plan" + ResponseMessages.msgAdditionSuccess;
                    return Ok(_response);
                }
                else
                {
                    // Update membership plan
                    var updateMembershipPlan = await _context.MembershipPlan.Where(a => (a.MembershipPlanId == model.membershipPlanId) && (a.IsDeleted != true)).FirstOrDefaultAsync();
                    if (updateMembershipPlan != null)
                    {
                        mapData = _mapper.Map(model, updateMembershipPlan);
                        _context.Update(mapData);
                        _context.SaveChanges();

                        // set popular is false for other plans
                        if (mapData.IsPopular == true)
                        {
                            var updatePopularPlan = await _context.MembershipPlan.Where(a => (a.MembershipPlanId != mapData.MembershipPlanId) && (a.IsPopular == true) && (a.IsDeleted != true)).FirstOrDefaultAsync();
                            if (updatePopularPlan != null)
                            {
                                updatePopularPlan.IsPopular = false;
                                _context.Update(updatePopularPlan);
                                _context.SaveChangesAsync();
                            }
                        }

                        var responseData = _mapper.Map<GetMembershipPlanDTO>(mapData);
                        responseData.planDurationName = ((TimePeriod)mapData.PlanDuration).ToString();

                        _response.StatusCode = HttpStatusCode.OK;
                        _response.IsSuccess = true;
                        _response.Data = responseData;
                        _response.Messages = "Plan" + ResponseMessages.msgUpdationSuccess;
                        return Ok(_response);
                    }
                    else
                    {
                        _response.StatusCode = HttpStatusCode.OK;
                        _response.IsSuccess = false;
                        _response.Messages = ResponseMessages.msgNotFound + "record.";
                        return Ok(_response);
                    }
                }
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

                // // set status false when expire
                List<MembershipRecord>? updateMembership = new List<MembershipRecord>();
                var updateMembershipRecord = await _membershipRecordRepository.GetAllAsync(a => a.PlanStatus != true);
                var shopUpdates = new List<SalonDetail>(); // Collect shop updates

                foreach (var item in updateMembershipRecord)
                {
                    if (DateTime.UtcNow > item.ExpiryDate)
                    {
                        item.PlanStatus = false;
                        updateMembership.Add(item);

                        var shop = _context.SalonDetail.FirstOrDefault(u => u.SalonId == item.SalonId);
                        if (shop != null)
                        {
                            shop.Status = (int)Status.Expired;
                            shopUpdates.Add(shop); // Collect shop updates
                        }
                    }
                }

                // Outside the loop, save all changes to the context
                _context.UpdateRange(updateMembership);
                _context.UpdateRange(shopUpdates);
                await _context.SaveChangesAsync();

                await _membershipRecordRepository.UpdateMembershipRecord(updateMembership);

                var membershipPlans = _context.MembershipPlan.Where(a => (a.IsDeleted != true)).OrderByDescending(a => a.IsPopular).ToList();

                var responseList = new List<GetMembershipPlanListDTO>();
                foreach (var membershipPlan in membershipPlans)
                {
                    var mapData = _mapper.Map<GetMembershipPlanListDTO>(membershipPlan);
                    mapData.createDate = membershipPlan.CreateDate.ToString(@"dd-MM-yyyy");
                    mapData.planDurationName = ((TimePeriod)mapData.planDuration).ToString();

                    responseList.Add(mapData);
                }

                if (responseList.Count > 0)
                {
                    if (true)
                    {
                        if (!string.IsNullOrEmpty(vendorId))
                        {
                            var membershipRecord = await _context.MembershipRecord.Where(a => a.PlanStatus == true && a.VendorId == vendorId).FirstOrDefaultAsync();
                            if (membershipRecord != null)
                            {
                                var activePlan = responseList.Where(u => u.membershipPlanId == membershipRecord.MembershipPlanId).FirstOrDefault();
                                activePlan.isActive = true;
                                activePlan.expiryDate = membershipRecord.ExpiryDate.ToString(@"dd-MM-yyyy");
                            }
                        }
                    }
                    if (!string.IsNullOrEmpty(searchQuery))
                    {
                        responseList = responseList.Where(a => (a.planName.ToLower().Contains(searchQuery.ToLower())
                        || (a.planDurationName.ToLower().Contains(searchQuery.ToLower()))
                        )).ToList();
                    }
                    if (planType > 0)
                    {
                        responseList = responseList.Where(a => (a.planType == planType)).ToList();
                    }
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = true;
                    _response.Data = responseList;
                    _response.Messages = "Plan list" + ResponseMessages.msgShownSuccess;
                    return Ok(_response);
                }
                else
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = ResponseMessages.msgNotFound + "record.";
                    return Ok(_response);
                }
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
                var membershipPlan = await _context.MembershipPlan.Where(a => (a.MembershipPlanId == membershipPlanId) && (a.IsDeleted != true)).FirstOrDefaultAsync();
                if (membershipPlan != null)
                {
                    var mapData = _mapper.Map<GetMembershipPlanDTO>(membershipPlan);
                    mapData.createDate = membershipPlan.CreateDate.ToString(@"dd-MM-yyyy");
                    mapData.planDurationName = ((TimePeriod)mapData.planDuration).ToString();

                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = true;
                    _response.Data = mapData;
                    _response.Messages = "Plan detail" + ResponseMessages.msgAdditionSuccess;
                    return Ok(_response);
                }
                else
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = ResponseMessages.msgNotFound + "record.";
                    return Ok(_response);
                }
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
                // // set plan status false for subscriber of this plan
                var membershipRecords = await _context.MembershipRecord.Where(x => (x.MembershipPlanId == membershipPlanId)).ToListAsync();

                // // check record is null or not
                // if (membershipRecords != null)
                // {
                //     foreach (var item in membershipRecords)
                //     {
                //         // set status false
                //         item.PlanStatus = false;

                //         //save changes
                //         _context.Update(item);
                //         await _context.SaveChangesAsync();
                //     }
                // }

                // delete plan
                membershipDetail.IsDeleted = true;

                _context.Update(membershipDetail);
                await _context.SaveChangesAsync();

                // var response = _mapper.Map<MembershipPlanDTO>(membershipDetail);
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Messages = "Plan" + ResponseMessages.msgDeletionSuccess;
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

            var upiCheck = model.upiDetail.Where(u => u.isActive == true).ToList();
            if (upiCheck.Count > 1)
            {
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = false;
                _response.Messages = "Can't active two upi at a time.";
                return Ok(_response);
            }
            var bankStatusCheck = model.bankDetail.Where(u => u.isActive == true).ToList();
            if (bankStatusCheck.Count > 1)
            {
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = false;
                _response.Messages = "Can't active two bank at a time.";
                return Ok(_response);
            }
            var membershipRecord = await _membershipRecordRepository.GetAsync(u => u.MembershipRecordId == model.membershipRecordId);

            if (membershipRecord == null)
            {
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = false;
                _response.Messages = "Please enter valid membership id.";
                return Ok(_response);
            }

            var vendorDetail = await _userRepo.VendorRegistration(model, currentUserId);

            // Add bank detail
            foreach (var item in model.bankDetail)
            {
                BankDetail bankDetailMapped = _mapper.Map<BankDetail>(item);
                bankDetailMapped.UserId = vendorDetail.vendorId;
                await _context.AddAsync(bankDetailMapped);
                await _context.SaveChangesAsync();
            }

            // Add upi detail
            foreach (var item in model.upiDetail)
            {
                Upidetail upiDetailMapped = _mapper.Map<Upidetail>(item);
                upiDetailMapped.UserId = vendorDetail.vendorId;
                await _context.AddAsync(upiDetailMapped);
                await _context.SaveChangesAsync();
            }

            var banks = await _context.BankDetail.Where(u => u.UserId == vendorDetail.vendorId).ToListAsync();
            banks = banks.Where(u => u.UserId == vendorDetail.vendorId).ToList();
            var banksResponse = new List<BankResponseDTO>();
            foreach (var item in banks)
            {
                var mappedData = _mapper.Map<BankResponseDTO>(item);
                banksResponse.Add(mappedData);
            }
            // Add shop detail
            foreach (var item in model.salonDetail)
            {
                SalonDetail salonDetailMapped = _mapper.Map<SalonDetail>(item);
                var roles = await _userManager.GetRolesAsync(adminDetail);
                if ((roles[0] == Role.SuperAdmin.ToString()))
                {
                    salonDetailMapped.Status = Convert.ToInt32(Status.Approved);
                }

                salonDetailMapped.VendorId = vendorDetail.vendorId;
                var membershipPlan = await _context.MembershipPlan.Where(u => u.MembershipPlanId == membershipRecord.MembershipPlanId).FirstOrDefaultAsync();

                await _context.SalonDetail.AddAsync(salonDetailMapped);
                await _context.SaveChangesAsync();

                if (membershipRecord != null)
                {
                    membershipRecord.VendorId = vendorDetail.vendorId;
                    membershipRecord.CreatedBy = vendorDetail.createdBy;
                    membershipRecord.SalonId = salonDetailMapped.SalonId;
                    _context.Update(membershipRecord);
                    await _context.SaveChangesAsync();
                }
            }
            var salons = await _context.SalonDetail.Where(u => u.VendorId == vendorDetail.vendorId).ToListAsync();
            // _backgroundService.StartService();

            var salonsResponse = new List<SalonResponseDTO>();
            foreach (var item in salons)
            {
                var mappedData = _mapper.Map<SalonResponseDTO>(item);
                salonsResponse.Add(mappedData);
            }
            var upiIds = await _context.Upidetail.Where(u => u.UserId == vendorDetail.vendorId).ToListAsync();
            var upiIdList = new List<UPIResponseDTO>();
            foreach (var item in upiIds)
            {
                var mappedData = _mapper.Map<UPIResponseDTO>(item);
                upiIdList.Add(mappedData);
            }

            vendorDetail.salonResponses = salonsResponse;
            vendorDetail.bankResponses = banksResponse;
            vendorDetail.upiResponses = upiIdList;

            if (vendorDetail == null)
            {
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = false;
                _response.Messages = "Error while adding.";
                return Ok(_response);
            }
            // Send email here

            var pathToFile =
                _hostingEnvironment.WebRootPath
                + Path.DirectorySeparatorChar.ToString()
                + mainTemplatesContainer
                + Path.DirectorySeparatorChar.ToString()
                + emailTemplatesContainer
                + Path.DirectorySeparatorChar.ToString()
                + vendor_registration;

            var name =
                vendorDetail.firstName + " " + vendorDetail.lastName
                ?? string.Empty + " " + vendorDetail.lastName;
            var body = new BodyBuilder();
            using (StreamReader reader = System.IO.File.OpenText(pathToFile))
            {
                body.HtmlBody = reader.ReadToEnd();
            }
            string messageBody = body.HtmlBody;
            messageBody = messageBody.Replace("{vendorName}", name);
            messageBody = messageBody.Replace("{link}", "http://beautyhubtest.s3-website.ap-south-1.amazonaws.com/");
            messageBody = messageBody.Replace("{pasword}", vendorDetail.password);
            messageBody = messageBody.Replace("{email}", vendorDetail.email);

            await _emailSender.SendEmailAsync(
                email: vendorDetail.email,
                subject: "Congratulations! Your Vendor Registration on BeautyHub is Complete!",
                htmlMessage: messageBody
            );
            _response.StatusCode = HttpStatusCode.OK;
            _response.IsSuccess = true;
            _response.Data = vendorDetail;
            _response.Messages = "Vendor detail" + ResponseMessages.msgAdditionSuccess;
            return Ok(_response);
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
            var upiCheck = model.upiDetail.Where(u => u.isActive == true).ToList();
            if (upiCheck.Count > 1)
            {
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = false;
                _response.Messages = "Can't activate two UPI accounts simultaneously.";
                return Ok(_response);
            }
            var bankStatusCheck = model.bankDetail.Where(u => u.isActive == true).ToList();
            if (bankStatusCheck.Count > 1)
            {
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = false;
                _response.Messages = "Can't activate two bank accounts simultaneously.";
                return Ok(_response);
            }
            var user = _mapper.Map(model, adminDetail);
            await _userManager.UpdateAsync(user);

            // Update user detail
            UserDetail? userdetail = await _context.UserDetail.FirstOrDefaultAsync(u => u.UserId == model.vendorId);
            userdetail.CountryId = model.countryId;
            userdetail.StateId = model.stateId;
            userdetail.DialCode = model.dialCode;
            userdetail.Gender = model.gender;

            // userdetail.ModifyDate = DateTime.Now;
            _context.Update(userdetail);
            await _context.SaveChangesAsync();

            VendorSalonResponseDTO vendorDetail = new VendorSalonResponseDTO();
            _mapper.Map(user, vendorDetail);
            _mapper.Map(userdetail, vendorDetail);

            var createdByDetail = _userManager.FindByIdAsync(userdetail.CreatedBy).GetAwaiter().GetResult();
            vendorDetail.createdByName = createdByDetail.FirstName + " " + createdByDetail.LastName;
            if (userdetail.CountryId != null && userdetail.StateId != null)
            {
                var countryDetail = await _contentRepository.GetCountryById(userdetail.CountryId);
                var stateDetail = await _contentRepository.GetStateById(userdetail.StateId);
                vendorDetail.countryName = countryDetail.countryName;
                vendorDetail.stateName = stateDetail.stateName;
            }

            // salonDetail salonDetail = await _context.SalonDetail.Where.GetAsync(u => u.ShopId == model.salonDetail.FirstOrDefault().ShopId);
            // salonDetail.BankId = null;
            // salonDetail.UpidetailId = null;
            // await _context.SalonDetail.Where.UpdatesalonDetail(salonDetail);
            var existingShopList = await _context.SalonDetail.Where(u => u.VendorId == user.Id).ToListAsync();
            // null bank detail and upidetail
            foreach (var item in existingShopList)
            {
                item.BankId = null;
                item.UpidetailId = null;

                _context.Update(item);
                await _context.SaveChangesAsync();
            }

            // Update bank detail
            var existingBankList = await _context.BankDetail.Where(u => u.UserId == user.Id).ToListAsync();
            List<int> previousBanks = existingBankList.Select(u => u.BankId).ToList();
            List<int> updateBanks = model.bankDetail.Select(u => u.bankId).ToList();
            List<int> subtractedListToRemove = previousBanks.Except(updateBanks).ToList();
            foreach (var item in subtractedListToRemove)
            {
                var removeBankId = await _context.BankDetail.Where(u => u.BankId == item).FirstOrDefaultAsync();
                _context.Remove(removeBankId);
                await _context.SaveChangesAsync();
            }
            foreach (var item in model.bankDetail)
            {
                BankDetail bankDetail = await _context.BankDetail.Where(u => u.BankId == item.bankId).FirstOrDefaultAsync();
                if (bankDetail != null)
                {
                    var updateBank = _mapper.Map(item, bankDetail);
                    _context.BankDetail.Update(updateBank);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    var addBank = _mapper.Map<BankDetail>(item);
                    addBank.UserId = user.Id;
                    await _context.BankDetail.AddAsync(addBank);
                    await _context.SaveChangesAsync();
                }
            }

            var banks = await _context.BankDetail.Where(u => u.UserId == model.vendorId).ToListAsync();
            var banksResponse = _mapper.Map<List<BankResponseDTO>>(banks);

            // Update upi detail
            var existingupiList = await _context.Upidetail.Where(u => u.UserId == user.Id).ToListAsync();
            List<int> previousUPIs = existingupiList.Select(u => u.UpidetailId).ToList();
            List<int> updateUPIs = model.upiDetail.Select(u => u.upidetailId).ToList();
            List<int> subtractedUPIListToRemove = previousUPIs.Except(updateUPIs).ToList();
            foreach (var item in subtractedUPIListToRemove)
            {
                var removeUPIId = await _context.Upidetail.Where(u => u.UpidetailId == item).FirstOrDefaultAsync();
                _context.Remove(removeUPIId);
                await _context.SaveChangesAsync();
            }
            foreach (var item in model.upiDetail)
            {
                var upiDetail = await _context.Upidetail.Where(u => u.UpidetailId == item.upidetailId).FirstOrDefaultAsync();
                if (upiDetail == null)
                {
                    var addUPI = _mapper.Map<Upidetail>(item);
                    addUPI.UserId = user.Id;
                    await _context.AddAsync(addUPI);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    var updateUPI = _mapper.Map(item, upiDetail);
                    _context.Update(updateUPI);
                    await _context.SaveChangesAsync();
                }
            }

            var getupiList = await _context.Upidetail.Where(u => u.UserId == model.vendorId).ToListAsync();
            var upiResponse = _mapper.Map<List<UPIResponseDTO>>(getupiList);

            // add bank and upi detail for salons
            var getupiDetail = await _context.Upidetail.Where(u => u.UserId == model.vendorId && u.IsActive == true).FirstOrDefaultAsync();
            var activeBank = await _context.BankDetail.Where(u => u.UserId == model.vendorId && u.IsActive == true).FirstOrDefaultAsync();

            // Update shop detail
            List<int> previousSalon = existingShopList.Select(u => u.SalonId).ToList();
            List<int> updateSalon = model.salonDetail.Select(u => u.salonId).ToList();
            List<int> subtractedListToRemoveShop = previousSalon.Except(updateSalon).ToList();
            foreach (var item in subtractedListToRemoveShop)
            {
                var removeShopId = await _context.SalonDetail.Where(u => u.SalonId == item).FirstOrDefaultAsync();
                _context.SalonDetail.Remove(removeShopId);
                await _context.SaveChangesAsync();
            }
            foreach (var item in model.salonDetail)
            {
                var addUpdatesalonDetail = await _context.SalonDetail.Where(u => u.SalonId == item.salonId).FirstOrDefaultAsync();
                if (addUpdatesalonDetail != null)
                {
                    var updateShop = _mapper.Map(item, addUpdatesalonDetail);
                    if (getupiDetail != null)
                    {
                        updateShop.UpidetailId = getupiDetail.UpidetailId;
                    }
                    if (activeBank != null)
                    {
                        updateShop.BankId = activeBank.BankId;
                    }
                    roles = await _userManager.GetRolesAsync(currentUserDetail);
                    if ((roles[0] == Role.SuperAdmin.ToString()))
                    {
                        updateShop.Status = Convert.ToInt32(Status.Approved);
                    }
                    // else
                    //     updateShop.Status = Convert.ToInt32(Status.Pending);

                    _context.SalonDetail.Update(addUpdatesalonDetail);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    var addSalon = _mapper.Map<SalonDetail>(item);
                    addSalon.VendorId = user.Id;
                    if (getupiDetail != null)
                    {
                        addSalon.UpidetailId = getupiDetail.UpidetailId;
                    }
                    if (activeBank != null)
                    {
                        addSalon.BankId = activeBank.BankId;
                    }
                    roles = await _userManager.GetRolesAsync(currentUserDetail);
                    if ((roles[0] == Role.SuperAdmin.ToString()))
                    {
                        addSalon.Status = Convert.ToInt32(Status.Approved);
                    }
                    // else
                    //     addSalon.Status = Convert.ToInt32(Status.Pending);

                    await _context.AddAsync(addSalon);
                    await _context.SaveChangesAsync();
                }
            }

            var salons = await _context.SalonDetail.Where(u => u.VendorId == model.vendorId).ToListAsync();
            var salonsResponse = _mapper.Map<List<SalonResponseDTO>>(salons);
            foreach (var item in salonsResponse)
            {
                item.statusDisplay = item.status == 1 ? "Approved" : item.status == 2 ? "Rejected" : "Pending";
            }

            // get membership record
            var membershipRecord = await _membershipRecordRepository.GetAsync(u => u.VendorId == model.vendorId && u.PlanStatus == true);
            if (membershipRecord != null)
            {
                var responseData = _mapper.Map<GetMembershipRecordDTO>(membershipRecord);
                var planDetail = await _context.MembershipPlan.Where(u => u.MembershipPlanId == membershipRecord.MembershipPlanId).FirstOrDefaultAsync();
                responseData.transactionId = membershipRecord.TransactionId;
                responseData.totalAmount = (double)planDetail.TotalAmount;
                responseData.planName = planDetail.PlanName;

                responseData.createDate = membershipRecord.CreateDate.ToString(@"dd-MM-yyyy");
                responseData.expiryDate = membershipRecord.ExpiryDate.ToString(@"dd-MM-yyyy");

                vendorDetail.membershipResponses = responseData;
            }

            vendorDetail.salonResponses = salonsResponse;
            vendorDetail.bankResponses = banksResponse;
            vendorDetail.upiResponses = upiResponse;

            if (vendorDetail == null)
            {
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = false;
                _response.Messages = "Error while adding.";
                return Ok(_response);
            }
            _response.StatusCode = HttpStatusCode.OK;
            _response.IsSuccess = true;
            _response.Data = vendorDetail;
            _response.Messages = "Vendor detail" + ResponseMessages.msgUpdationSuccess;
            return Ok(_response);
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

            var shopList = await _context.SalonDetail.Where(u => u.IsDeleted != true).ToListAsync();
            shopList = shopList.OrderByDescending(u => u.ModifyDate).ToList();
            List<VendorListDTO> vendorList = new List<VendorListDTO>();
            foreach (var item in shopList)
            {
                var vendorDetail = _userManager.FindByIdAsync(item.VendorId).GetAwaiter().GetResult();
                var vendorProfileDetail = await _context.UserDetail.FirstOrDefaultAsync(u => u.UserId == item.VendorId);

                var userDetail = await _context.UserDetail.FirstOrDefaultAsync(u => u.UserId == vendorDetail.Id);
                var adminUser = _userManager.FindByIdAsync(userDetail.CreatedBy).GetAwaiter().GetResult();
                var roleName = (await _userManager.GetRolesAsync(adminUser)).FirstOrDefault();
                VendorListDTO vendor = new VendorListDTO()
                {
                    salonName = item.SalonName,
                    salonType = item.SalonType,
                    salonId = item.SalonId,
                    statusDisplay = item.Status == 1 ? "Approved" : (item.Status == 2 ? "Rejected" : "Pending"),
                    createDate = item.CreateDate.ToString(@"dd-MM-yyyy"),
                    vendorName = vendorDetail.FirstName + " " + vendorDetail.LastName,
                    createdBy = adminUser.FirstName + " " + adminDetail.LastName,
                    createdById = adminUser.Id,
                    vendorId = vendorDetail.Id,
                    profilePic = vendorProfileDetail.ProfilePic,
                    createdByRole = roleName
                    // ModifyDate = vendorProfileDetail.ModifyDate
                };
                vendorList.Add(vendor);
            }

            if (!string.IsNullOrEmpty(status))
            {
                vendorList = vendorList.Where(u => u.statusDisplay.ToLower() == status.ToLower()).ToList();
            }

            if (!string.IsNullOrEmpty(salonType))
            {
                vendorList = vendorList.Where(u => u.salonType.ToLower() == salonType.ToLower()).ToList();
            }

            if (!string.IsNullOrEmpty(model.searchQuery))
            {
                vendorList = vendorList.Where(u => u.vendorName.ToLower().Contains(model.searchQuery.ToLower())
                || u.salonName.ToLower().Contains(model.searchQuery.ToLower())
                || u.statusDisplay.ToLower().Contains(model.searchQuery.ToLower())
                || u.createdBy.ToLower().Contains(model.searchQuery.ToLower())
                ).ToList();
            }

            var roles = await _userManager.GetRolesAsync(adminDetail);
            if ((roles.FirstOrDefault() == Role.Admin.ToString()))
            {
                createdBy = currentUserId;
            }

            if (!string.IsNullOrEmpty(createdBy))
            {
                vendorList = vendorList.Where(u => u.createdById.ToLower().Contains(createdBy)
                ).ToList();
            }

            // Get's No of Rows Count   
            int count = vendorList.Count();

            // Parameter is passed from Query string if it is null then it default Value will be pageNumber:1  
            int CurrentPage = model.pageNumber;

            // Parameter is passed from Query string if it is null then it default Value will be pageSize:20  
            int PageSize = model.pageSize;

            // Display TotalCount to Records to User  
            int TotalCount = count;

            // Calculating Totalpage by Dividing (No of Records / Pagesize)  
            int TotalPages = (int)Math.Ceiling(count / (double)PageSize);

            // Returns List of Customer after applying Paging   
            var items = vendorList.Skip((CurrentPage - 1) * PageSize).Take(PageSize).ToList();

            // if CurrentPage is greater than 1 means it has previousPage  
            var previousPage = CurrentPage > 1 ? "Yes" : "No";

            // if TotalPages is greater than CurrentPage means it has nextPage  
            var nextPage = CurrentPage < TotalPages ? "Yes" : "No";

            // Returing List of Customers Collections  
            FilterationResponseModel<VendorListDTO> obj = new FilterationResponseModel<VendorListDTO>();
            obj.totalCount = TotalCount;
            obj.pageSize = PageSize;
            obj.currentPage = CurrentPage;
            obj.totalPages = TotalPages;
            obj.previousPage = previousPage;
            obj.nextPage = nextPage;
            obj.searchQuery = string.IsNullOrEmpty(model.searchQuery) ? "no parameter passed" : model.searchQuery;
            obj.dataList = items.ToList();

            if (obj == null)
            {
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = false;
                _response.Messages = "Error while adding.";
                return Ok(_response);
            }
            _response.StatusCode = HttpStatusCode.OK;
            _response.IsSuccess = true;
            _response.Data = obj;
            _response.Messages = ResponseMessages.msgListFoundSuccess;
            return Ok(_response);
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
            var mappedData = _mapper.Map<VendorSalonResponseDTO>(userToReturn);
            var createdByDetail = _context.ApplicationUsers.FirstOrDefault(u => u.Id == currentUserId);
            mappedData.createdByName = createdByDetail.FirstName + " " + createdByDetail.LastName;
            mappedData.vendorId = userToReturn.Id;
            mappedData.createdBy = userDetail.CreatedBy;
            mappedData.gender = userDetail.Gender;
            // var roles = await _userManager.GetRolesAsync(userToReturn);
            var createdByRoleName = await _userManager.GetRolesAsync(userToReturn);
            mappedData.countryId = userDetail.CountryId;
            mappedData.stateId = userDetail.StateId;
            mappedData.gender = userDetail.Gender;
            mappedData.profilePic = userDetail.ProfilePic;
            mappedData.dialCode = userDetail.DialCode;
            mappedData.createdByRole = createdByRoleName.FirstOrDefault();

            if (userDetail.CountryId != null && userDetail.StateId != null)
            {
                var countryDetail = await _contentRepository.GetCountryById(userDetail.CountryId);
                var stateDetail = await _contentRepository.GetStateById(userDetail.StateId);
                mappedData.countryName = countryDetail != null ? countryDetail.countryName : null;
                mappedData.stateName = stateDetail != null ? stateDetail.stateName : null;
            }

            var banksResponse = new List<BankResponseDTO>();

            var salons = await _context.SalonDetail.Where(u => u.VendorId == vendorId).ToListAsync();
            salons = salons.OrderByDescending(u => u.ModifyDate).Take(1).ToList();
            var salonsResponse = new List<SalonResponseDTO>();
            foreach (var item in salons)
            {
                var salonData = _mapper.Map<SalonResponseDTO>(item);
                salonData.statusDisplay = item.Status == 1 ? "Approved" : item.Status == 2 ? "Rejected" : "Pending";
                salonsResponse.Add(salonData);

                var banks = await _context.BankDetail.Where(u => u.UserId == vendorId).ToListAsync();
                banks = banks.OrderByDescending(u => u.ModifyDate).Take(1).ToList();

                foreach (var item1 in banks)
                {
                    var bankDetail = _mapper.Map<BankResponseDTO>(item1);
                    banksResponse.Add(bankDetail);
                }
                var getupiList = await _context.Upidetail.Where(u => u.UserId == vendorId).ToListAsync();
                var upiResponse = _mapper.Map<List<UPIResponseDTO>>(getupiList);
                mappedData.upiResponses = upiResponse;

                // get membership record
                var membershipRecord = await _membershipRecordRepository.GetAsync(u => u.VendorId == item.VendorId && u.PlanStatus == true);
                if (membershipRecord != null)
                {
                    var responseData = _mapper.Map<GetMembershipRecordDTO>(membershipRecord);
                    var planDetail = await _context.MembershipPlan.Where(u => u.MembershipPlanId == membershipRecord.MembershipPlanId).FirstOrDefaultAsync();
                    responseData.transactionId = membershipRecord.TransactionId;
                    responseData.totalAmount = (double)planDetail.TotalAmount;
                    responseData.planName = planDetail.PlanName;

                    responseData.createDate = membershipRecord.CreateDate.ToString(@"dd-MM-yyyy");
                    responseData.expiryDate = membershipRecord.ExpiryDate.ToString(@"dd-MM-yyyy");

                    mappedData.membershipResponses = responseData;
                }
            }

            mappedData.salonResponses = salonsResponse;
            mappedData.bankResponses = banksResponse;

            _response.StatusCode = HttpStatusCode.OK;
            _response.IsSuccess = true;
            _response.Data = mappedData;
            _response.Messages = "Detail" + ResponseMessages.msgShownSuccess;
            return Ok(_response);
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
            var vendorProfileDetail = await _context.UserDetail.Where(u => (u.UserId == vendorDetail.Id) && (u.IsDeleted == false)).FirstOrDefaultAsync();
            if (vendorProfileDetail == null)
            {
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = false;
                _response.Messages = ResponseMessages.msgUserNotFound;
                return Ok(_response);
            }

            if (vendorProfileDetail != null)
            {
                vendorProfileDetail.IsDeleted = true;
                _context.UserDetail.Update(vendorProfileDetail);
                await _context.SaveChangesAsync();
            }

            var salonDetail = await _context.SalonDetail.Where(u => u.VendorId == vendorProfileDetail.UserId).ToListAsync();
            foreach (var item in salonDetail)
            {
                // var customerSalonToDelete = await _context.CustomerSalon
                // .Where(u => u.ShopId == item.ShopId)
                // .ToListAsync();

                // _context.CustomerSalon.RemoveRange(customerSalonToDelete);
                // await _context.SaveChangesAsync();

                item.IsDeleted = true;
                // item.ModifyDate = DateTime.Now;
                _context.Update(item);
                await _context.SaveChangesAsync();

            }

            var bankDetail = await _context.BankDetail.Where(u => u.UserId == vendorProfileDetail.UserId).ToListAsync();
            foreach (var item in bankDetail)
            {
                item.IsDeleted = true;
                // item.ModifyDate = DateTime.Now;
                _context.Update(item);
                await _context.SaveChangesAsync();
            }
            // var productIds = await _context.Product.Where(u => u.VendorId == vendorDetail.Id)
            //         .Select(p => p.ProductId).ToListAsync();

            // var cartProductsToDelete = await _context.Cart
            //     .Where(u => productIds.Contains(u.ProductId))
            //     .ToListAsync();

            // _context.Cart.RemoveRange(cartProductsToDelete);
            // await _context.SaveChangesAsync();

            // var favouriteProductsToDelete = await _context.FavouriteProduct
            //     .Where(u => productIds.Contains(u.ProductId))
            //     .ToListAsync();

            // _context.FavouriteProduct.RemoveRange(favouriteProductsToDelete);
            // await _context.SaveChangesAsync();

            // await _context.Product
            //     .Where(u => productIds.Contains(u.ProductId))
            //     .ForEachAsync(p =>
            //     {
            //         p.Status = Convert.ToInt32(ServiceStatus.InActive);
            //         p.IsDeleted = true;
            //     });

            // await _context.SaveChangesAsync();

            _response.StatusCode = HttpStatusCode.OK;
            _response.IsSuccess = true;
            _response.Messages = ResponseMessages.msgDeletionSuccess;
            return Ok(_response);
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
            salonDetails.Status = model.status;
            // salonDetails.ModifyDate = DateTime.Now;
            _context.Update(salonDetails);
            await _context.SaveChangesAsync();

            var shopResponse = _mapper.Map<SalonResponseDTO>(salonDetails);
            shopResponse.statusDisplay = salonDetails.Status == 1 ? "Approved" : salonDetails.Status == 2 ? "Rejected" : "Pending";

            _response.StatusCode = HttpStatusCode.OK;
            _response.IsSuccess = true;
            _response.Data = shopResponse;
            _response.Messages = "Status" + ResponseMessages.msgUpdationSuccess;
            return Ok(_response);
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

            var user = await _userRepo.AdminUserRegistration(model);
            if (user == null)
            {
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = false;
                _response.Messages = "Error while registering.";
                return Ok(_response);
            }

            // Send email here

            var pathToFile =
                _hostingEnvironment.WebRootPath
                + Path.DirectorySeparatorChar.ToString()
                + mainTemplatesContainer
                + Path.DirectorySeparatorChar.ToString()
                + emailTemplatesContainer
                + Path.DirectorySeparatorChar.ToString()
                + admin_user_registration;

            var name =
                user.firstName + " " + user.lastName
                ?? string.Empty + " " + user.lastName;
            var body = new BodyBuilder();
            using (StreamReader reader = System.IO.File.OpenText(pathToFile))
            {
                body.HtmlBody = reader.ReadToEnd();
            }
            string messageBody = body.HtmlBody;
            messageBody = messageBody.Replace("{adminName}", name);
            messageBody = messageBody.Replace("{link}", "https://salonnearme.com/");
            messageBody = messageBody.Replace("{pasword}", user.password);
            messageBody = messageBody.Replace("{email}", user.email);

            await _emailSender.SendEmailAsync(
                email: user.email,
                subject: "Congratulations! Registration on BeautyHub is Complete!",
                htmlMessage: messageBody
            );

            _response.StatusCode = HttpStatusCode.OK;
            _response.IsSuccess = true;
            _response.Data = user;
            _response.Messages = ResponseMessages.msgUserRegisterSuccess;
            return Ok(_response);
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

            var user = _mapper.Map(model, adminDetail);

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                // Update user detail
                UserDetail? userdetail = await _context.UserDetail.FirstOrDefaultAsync(u => u.UserId == model.id);
                userdetail = _mapper.Map(model, userdetail);
                // userdetail.ModifyDate = DateTime.Now;
                _context.Update(userdetail);
                _context.SaveChanges();

                var userToReturn = _context.ApplicationUsers
                    .FirstOrDefault(u => u.Email.ToLower() == model.email.ToLower());
                var userProfileToReturn = await _context.UserDetail.FirstOrDefaultAsync(u => u.UserId == model.id);

                var mappedData = _mapper.Map<UserDetailDTO>(userToReturn);
                _mapper.Map(userProfileToReturn, mappedData);

                if (userProfileToReturn.CountryId != null && userProfileToReturn.StateId != null)
                {
                    var countryDetail = await _contentRepository.GetCountryById(userProfileToReturn.CountryId);
                    var stateDetail = await _contentRepository.GetStateById(userProfileToReturn.StateId);
                    mappedData.countryName = countryDetail.countryName;
                    mappedData.stateName = stateDetail.stateName;
                }
                if (mappedData.id == null)
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = "Error while updating.";
                    return Ok(_response);
                }
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Data = mappedData;
                _response.Messages = ResponseMessages.msgUpdationSuccess;
                return Ok(_response);
            }
            _response.StatusCode = HttpStatusCode.OK;
            _response.IsSuccess = false;
            _response.Messages = "Error while updating.";
            return Ok(_response);
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

            List<AdminUserListDTO> adminUserList = new List<AdminUserListDTO>();
            foreach (var item in adminUsers)
            {
                var adminUserDetail = _userManager.FindByIdAsync(item.Id).GetAwaiter().GetResult();
                var adminUserProfileDetail = await _context.UserDetail.FirstOrDefaultAsync(u => (u.UserId == item.Id) && (u.IsDeleted == false));
                if (adminUserProfileDetail != null)
                {
                    var mappedData = _mapper.Map<AdminUserListDTO>(item);
                    mappedData.profilepic = adminUserProfileDetail.ProfilePic;
                    mappedData.gender = adminUserProfileDetail.Gender;
                    mappedData.modifyDate = adminUserProfileDetail.ModifyDate;
                    adminUserList.Add(mappedData);
                }
            }

            adminUserList = adminUserList.OrderByDescending(u => u.modifyDate).ToList();

            if (!string.IsNullOrEmpty(model.searchQuery))
            {
                adminUserList = adminUserList.Where(u => u.firstName.ToLower().Contains(model.searchQuery.ToLower())
                || u.email.ToLower().Contains(model.searchQuery.ToLower())
                ).ToList();
            }

            // Get's No of Rows Count   
            int count = adminUserList.Count();

            // Parameter is passed from Query string if it is null then it default Value will be pageNumber:1  
            int CurrentPage = model.pageNumber;

            // Parameter is passed from Query string if it is null then it default Value will be pageSize:20  
            int PageSize = model.pageSize;

            // Display TotalCount to Records to User  
            int TotalCount = count;

            // Calculating Totalpage by Dividing (No of Records / Pagesize)  
            int TotalPages = (int)Math.Ceiling(count / (double)PageSize);

            // Returns List of Customer after applying Paging   
            var items = adminUserList.Skip((CurrentPage - 1) * PageSize).Take(PageSize).ToList();

            // if CurrentPage is greater than 1 means it has previousPage  
            var previousPage = CurrentPage > 1 ? "Yes" : "No";

            // if TotalPages is greater than CurrentPage means it has nextPage  
            var nextPage = CurrentPage < TotalPages ? "Yes" : "No";

            // Returing List of Customers Collections  
            FilterationResponseModel<AdminUserListDTO> obj = new FilterationResponseModel<AdminUserListDTO>();
            obj.totalCount = TotalCount;
            obj.pageSize = PageSize;
            obj.currentPage = CurrentPage;
            obj.totalPages = TotalPages;
            obj.previousPage = previousPage;
            obj.nextPage = nextPage;
            obj.searchQuery = string.IsNullOrEmpty(model.searchQuery) ? "no parameter passed" : model.searchQuery;
            obj.dataList = items.ToList();

            if (obj == null)
            {
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = false;
                _response.Messages = "Error while adding.";
                return Ok(_response);
            }
            _response.StatusCode = HttpStatusCode.OK;
            _response.IsSuccess = true;
            _response.Data = obj;
            _response.Messages = ResponseMessages.msgListFoundSuccess;
            return Ok(_response);
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
            var mappedData = _mapper.Map<UserDetailDTO>(adminDetail);
            mappedData.profilePic = adminUserProfileDetail.ProfilePic;
            mappedData.gender = adminUserProfileDetail.Gender;
            if (adminUserProfileDetail.CountryId != null && adminUserProfileDetail.StateId != null)
            {
                var countryDetail = await _contentRepository.GetCountryById(adminUserProfileDetail.CountryId);
                var stateDetail = await _contentRepository.GetStateById(adminUserProfileDetail.StateId);
                mappedData.countryName = countryDetail.countryName;
                mappedData.stateName = stateDetail.stateName;
                mappedData.pan = adminUserProfileDetail.Pan;
                mappedData.profilePic = adminUserProfileDetail.ProfilePic;
                mappedData.countryId = adminUserProfileDetail.CountryId;
                mappedData.stateId = adminUserProfileDetail.StateId;
                mappedData.dialCode = adminUserProfileDetail.DialCode;
            }

            if (mappedData == null)
            {
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = false;
                _response.Messages = "Error while retrieving.";
                return Ok(_response);
            }
            _response.StatusCode = HttpStatusCode.OK;
            _response.IsSuccess = true;
            _response.Data = mappedData;
            _response.Messages = "Detail" + ResponseMessages.msgShownSuccess;
            return Ok(_response);
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

            adminUserProfileDetail.IsDeleted = true;
            // adminUserProfileDetail.ModifyDate = DateTime.Now;
            _context.Update(adminUserProfileDetail);
            _context.SaveChanges();

            _response.StatusCode = HttpStatusCode.OK;
            _response.IsSuccess = true;
            _response.Messages = ResponseMessages.msgDeletionSuccess;
            return Ok(_response);
        }
        #endregion

    }
}

