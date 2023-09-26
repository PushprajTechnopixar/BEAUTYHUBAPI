using AutoMapper;
using BeautyHubAPI.Data;
using BeautyHubAPI.Models;
using BeautyHubAPI.Models.Dtos;
using BeautyHubAPI.Models.Helper;
using BeautyHubAPI.Repository.IRepository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;
using BeautyHubAPI.Helpers;
using Microsoft.AspNetCore.Authorization;
using BeautyHubAPI.Repository;
using System.Net.Http.Headers;
using static BeautyHubAPI.Common.GlobalVariables;
using TimeZoneConverter;
using OfficeOpenXml;
using System.IO;
using Microsoft.AspNetCore.Identity;
using BeautyHubAPI.Firebase;
using BeautyHubAPI.Dtos;
using System.Globalization;
using System.Text;
using MimeKit.Encodings;
using Newtonsoft.Json;
using RestSharp;

namespace BeautyHubAPI.Controllers
{
    [Route("api/Vendor")]
    [ApiController]
    public class VendorController : ControllerBase
    {
        private readonly IMapper _mapper;
        protected APIResponse _response;
        private readonly HttpClient httpClient;
        private readonly IUploadRepository _uploadRepository;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IMembershipRecordRepository _membershipRecordRepository;
        private readonly IWebHostEnvironment _hostingEnvironment;
        private readonly IMobileMessagingClient _mobileMessagingClient;

        public VendorController(IMapper mapper,
        IUploadRepository uploadRepository,
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IMembershipRecordRepository membershipRecordRepository,
        IMobileMessagingClient mobileMessagingClient,

        IWebHostEnvironment hostingEnvironment
        )
        {
            _mapper = mapper;
            _uploadRepository = uploadRepository;
            _response = new();
            _context = context;
            _userManager = userManager;
            _membershipRecordRepository = membershipRecordRepository;
            _mobileMessagingClient = mobileMessagingClient;
            httpClient = new HttpClient();

        }

        #region buyMembershipPlan
        /// <summary>
        /// Buy membership plan.
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Authorize]
        [Route("buyMembershipPlan")]
        public async Task<IActionResult> buyServicePlan(buyMembershipPlanDTO model)
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

                if (!string.IsNullOrEmpty(model.createdBy))
                {
                    var createdBy = _userManager.FindByIdAsync(model.createdBy).GetAwaiter().GetResult();
                    if (createdBy == null)
                    {
                        _response.StatusCode = HttpStatusCode.OK;
                        _response.IsSuccess = false;
                        _response.Messages = "User is is not valid.";
                        return Ok(_response);
                    }
                }

                if (!string.IsNullOrEmpty(model.vendorId))
                {
                    var vendorDetail = _userManager.FindByIdAsync(model.vendorId).GetAwaiter().GetResult();
                    if (vendorDetail == null)
                    {
                        _response.StatusCode = HttpStatusCode.OK;
                        _response.IsSuccess = false;
                        _response.Messages = "User is is not valid.";
                        return Ok(_response);
                    }
                    var userProfileDetail = await _context.UserDetail.Where(u => u.UserId == vendorDetail.Id).FirstOrDefaultAsync();
                    model.createdBy = userProfileDetail.CreatedBy;
                }

                var planDetail = await _context.MembershipPlan.Where(a => (a.MembershipPlanId == model.membershipPlanId) && (a.IsDeleted != true)).FirstOrDefaultAsync();
                if (planDetail == null)
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = "Not found any record.";
                    return Ok(_response);
                }

                var membershipRecord = new MembershipRecord();

                if (!string.IsNullOrEmpty(model.paymentMethod))
                {
                    if (model.paymentMethod == PaymentMethod.PayByUPI.ToString()
                    || model.paymentMethod == PaymentMethod.Acc_Ifsc.ToString()
                    )
                    {
                        if (model.paymentReceiptId > 0)
                        {
                            var paymentReceipt = await _context.PaymentReceipt.Where(u => u.PaymentReceiptId == model.paymentReceiptId).FirstOrDefaultAsync();
                            if (paymentReceipt == null)
                            {
                                _response.StatusCode = HttpStatusCode.OK;
                                _response.IsSuccess = false;
                                _response.Messages = "Not found any record.";
                                return Ok(_response);
                            }
                            membershipRecord.PaymentReceiptId = paymentReceipt.PaymentReceiptId;
                            membershipRecord.PaymentMethod = model.paymentMethod;
                        }
                        else
                        {
                            _response.StatusCode = HttpStatusCode.OK;
                            _response.IsSuccess = false;
                            _response.Messages = "Please upload payment receipt.";
                            return Ok(_response);
                        }
                    }
                    else
                    {
                        if (model.paymentMethod != PaymentMethod.InCash.ToString())
                        {
                            _response.StatusCode = HttpStatusCode.OK;
                            _response.IsSuccess = false;
                            _response.Messages = "Please select valid payment method.";
                            return Ok(_response);
                        }
                        else
                        {
                            membershipRecord.PaymentMethod = PaymentMethod.InCash.ToString();
                        }
                    }
                }
                else
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = "Please select valid payment method.";
                    return Ok(_response);
                }

                // if (model.transactionId > 0)
                // {
                //     var paymentReceipt = await _context.TransactionDetail.Where(u => u.TransactionId == model.transactionId).FirstOrDefaultAsync();
                //     if (paymentReceipt == null)
                //     {
                //         _response.StatusCode = HttpStatusCode.OK;
                //         _response.IsSuccess = false;
                //         _response.Messages = "Not found any record.";
                //         return Ok(_response);
                //     }
                //     var checkTransactionDetail = await _context.MembershipRecord.Where(u => u.TransactionId == paymentReceipt.TransactionId && u.PlanStatus == true).FirstOrDefaultAsync();
                //     if (checkTransactionDetail != null)
                //     {
                //         _response.StatusCode = HttpStatusCode.OK;
                //         _response.IsSuccess = false;
                //         _response.Messages = "Please enter valid transaction id.";
                //         return Ok(_response);
                //     }
                //     if (paymentReceipt.Amount != planDetail.TotalAmount)
                //     {
                //         _response.StatusCode = HttpStatusCode.OK;
                //         _response.IsSuccess = false;
                //         _response.Messages = "Please enter valid transaction id.";
                //         return Ok(_response);
                //     }
                //     membershipRecord.TransactionId = paymentReceipt.TransactionId;
                // }
                // else
                // {
                //     _response.StatusCode = HttpStatusCode.OK;
                //     _response.IsSuccess = false;
                //     _response.Messages = "Please enter transaction id.";
                //     return Ok(_response);
                // }

                membershipRecord.CreatedBy = model.createdBy;
                membershipRecord.VendorId = model.vendorId;
                membershipRecord.MembershipPlanId = model.membershipPlanId;
                membershipRecord.PlanStatus = true;
                membershipRecord.SalonId = model.salonId > 1 ? model.salonId : null;

                await _membershipRecordRepository.CreateEntity(membershipRecord);

                if (planDetail.PlanDuration == Convert.ToInt32(TimePeriod.Monthly))
                {
                    membershipRecord.ExpiryDate = membershipRecord.CreateDate.AddMonths(1).AddDays(-1);
                }
                else if (planDetail.PlanDuration == Convert.ToInt32(TimePeriod.Quarterly))
                {
                    membershipRecord.ExpiryDate = membershipRecord.CreateDate.AddMonths(3).AddDays(-1);
                }
                else if (planDetail.PlanDuration == Convert.ToInt32(TimePeriod.Semi_Annually))
                {
                    membershipRecord.ExpiryDate = membershipRecord.CreateDate.AddMonths(6).AddDays(-1);
                }
                else if (planDetail.PlanDuration == Convert.ToInt32(TimePeriod.Annually))
                {
                    membershipRecord.ExpiryDate = membershipRecord.CreateDate.AddYears(1).AddDays(-1);
                }
                else
                {
                    membershipRecord.ExpiryDate = membershipRecord.CreateDate;
                }

                await _membershipRecordRepository.UpdateMembershipRecord(membershipRecord);

                var responseData = _mapper.Map<GetMembershipRecordDTO>(membershipRecord);

                responseData.transactionId = membershipRecord.TransactionId;
                responseData.totalAmount = (double)planDetail.TotalAmount;
                var membershipPlan = await _context.MembershipPlan.Where(u => u.MembershipPlanId == membershipRecord.MembershipPlanId).FirstOrDefaultAsync();

                responseData.planName = membershipPlan.PlanName;

                responseData.createDate = membershipRecord.CreateDate.ToString(@"dd-MM-yyyy");
                responseData.expiryDate = membershipRecord.ExpiryDate.ToString(@"dd-MM-yyyy");

                var oldMembershipRecord = await _membershipRecordRepository.GetAllAsync(a => (a.VendorId == membershipRecord.VendorId) && (a.MembershipRecordId != membershipRecord.MembershipRecordId) && (a.PlanStatus == true));
                foreach (var item in oldMembershipRecord)
                {
                    item.PlanStatus = false;
                    await _membershipRecordRepository.UpdateMembershipRecord(item);
                }

                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Data = responseData;
                _response.Messages = "Plan booked successfully";
                return Ok(_response);
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

        #region GetVendorCategoryList
        /// <summary>
        ///  Get vendor  category list.
        /// </summary>
        [HttpGet("GetVendorCategoryList")]
        [Authorize(Roles = "Vendor,SuperAdmin,Admin")]
        public async Task<IActionResult> GetVendorCategoryList([FromQuery] GetCategoryRequestDTO model)
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
                List<CategoryDTO> Categories = new List<CategoryDTO>();

                if (model.mainCategoryId > 0)
                {
                    if (model.salonId > 0)
                    {
                        var categoryDetail = new List<SubCategory>();
                        if (model.categoryType == 0)
                        {
                            categoryDetail = await _context.SubCategory.Where(u => (u.MainCategoryId == model.mainCategoryId)
                           && u.CategoryStatus == Convert.ToInt32(Status.Approved)
                           ).ToListAsync();
                        }
                        else if (model.categoryType == 1)
                        {
                            categoryDetail = await _context.SubCategory.Where(u => (u.MainCategoryId == model.mainCategoryId)
                            && u.CategoryStatus == Convert.ToInt32(Status.Approved)
                           && (u.Male == true)
                           //    && (u.Female == false)
                           ).ToListAsync();
                        }
                        else if (model.categoryType == 2)
                        {
                            categoryDetail = await _context.SubCategory.Where(u => (u.MainCategoryId == model.mainCategoryId)
                            && u.CategoryStatus == Convert.ToInt32(Status.Approved)
                           //    && (u.Male == false)
                           && (u.Female == true)
                           ).ToListAsync();
                        }
                        else
                        {
                            categoryDetail = await _context.SubCategory.Where(u => (u.MainCategoryId == model.mainCategoryId)
                            && u.CategoryStatus == Convert.ToInt32(Status.Approved)
                           && (u.Male == true)
                           && (u.Female == true)
                           ).ToListAsync();
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
                            mappedData.createDate = (Convert.ToDateTime(item.CreateDate)).ToString(@"dd-MM-yyyy");

                            var categoryStatus = await _context.VendorCategory.Where(u => u.SubCategoryId == item.SubCategoryId
                            && u.SalonId == model.salonId
                            // && u.Male == item.Male
                            // && u.Female == item.Female
                            ).FirstOrDefaultAsync();
                            if (categoryStatus == null)
                            {
                                mappedData.status = true;
                            }
                            else
                            {
                                mappedData.status = false;
                            }
                            Categories.Add(mappedData);
                        }
                    }
                    else
                    {
                        var categoryDetail = new List<SubCategory>();
                        if (model.categoryType == 0)
                        {
                            categoryDetail = await _context.SubCategory.Where(u => (u.MainCategoryId == model.mainCategoryId)
                           && u.CategoryStatus == Convert.ToInt32(Status.Approved)
                           ).ToListAsync();
                        }
                        else if (model.categoryType == 1)
                        {
                            categoryDetail = await _context.SubCategory.Where(u => (u.MainCategoryId == model.mainCategoryId)
                            && u.CategoryStatus == Convert.ToInt32(Status.Approved)
                           && (u.Male == true)
                           //    && (u.Female == false)
                           ).ToListAsync();
                        }
                        else if (model.categoryType == 2)
                        {
                            categoryDetail = await _context.SubCategory.Where(u => (u.MainCategoryId == model.mainCategoryId)
                            && u.CategoryStatus == Convert.ToInt32(Status.Approved)
                           //    && (u.Male == false)
                           && (u.Female == true)
                           ).ToListAsync();
                        }
                        else
                        {
                            categoryDetail = await _context.SubCategory.Where(u => (u.MainCategoryId == model.mainCategoryId)
                            && u.CategoryStatus == Convert.ToInt32(Status.Approved)
                           && (u.Male == true)
                           && (u.Female == true)
                           ).ToListAsync();
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
                            mappedData.createDate = (Convert.ToDateTime(item.CreateDate)).ToString(@"dd-MM-yyyy");

                            var categoryStatus = await _context.VendorCategory.Where(u => u.SubCategoryId == item.SubCategoryId
                            && u.VendorId == currentUserId
                            // && u.Male == item.Male
                            // && u.Female == item.Female
                            ).FirstOrDefaultAsync();
                            if (categoryStatus == null)
                            {
                                mappedData.status = true;
                            }
                            else
                            {
                                mappedData.status = false;
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
                        Categories = new List<CategoryDTO>();
                        foreach (var item in categoryDetail)
                        {
                            var mappedData = _mapper.Map<CategoryDTO>(item);
                            mappedData.createDate = (Convert.ToDateTime(item.CreateDate)).ToString(@"dd-MM-yyyy");

                            var subCategoryDetail = new List<SubCategory>();
                            if (model.categoryType == 0)
                            {
                                subCategoryDetail = await _context.SubCategory.Where(u => u.MainCategoryId == item.MainCategoryId && u.CategoryStatus == Convert.ToInt32(Status.Approved)).ToListAsync();

                            }
                            else if (model.categoryType == 1)
                            {
                                subCategoryDetail = await _context.SubCategory.Where(u => u.MainCategoryId == item.MainCategoryId && u.CategoryStatus == Convert.ToInt32(Status.Approved)
                               && (u.Male == true)
                               //    && (u.Female == false)
                               ).ToListAsync();
                            }
                            else if (model.categoryType == 2)
                            {
                                subCategoryDetail = await _context.SubCategory.Where(u => u.MainCategoryId == item.MainCategoryId && u.CategoryStatus == Convert.ToInt32(Status.Approved)
                               //    && (u.Male == false)
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
                            mappedData.isNext = subCategoryDetail.Count > 0 ? true : false;

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
                               //    && (u.Female == false)
                               ).FirstOrDefaultAsync();
                            }
                            else if (model.categoryType == 2)
                            {
                                categoryStatus = await _context.VendorCategory.Where(u => (u.SalonId == model.salonId)
                               && (u.MainCategoryId == item.MainCategoryId)
                               && (u.Male == false)
                               //    && (u.Female == true)
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
                                mappedData.status = true;
                            }
                            else
                            {
                                mappedData.status = false;
                            }
                            Categories.Add(mappedData);
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
                        Categories = new List<CategoryDTO>();
                        foreach (var item in categoryDetail)
                        {
                            var mappedData = _mapper.Map<CategoryDTO>(item);
                            mappedData.createDate = (Convert.ToDateTime(item.CreateDate)).ToString(@"dd-MM-yyyy");

                            var subCategoryDetail = new List<SubCategory>();
                            if (model.categoryType == 0)
                            {
                                subCategoryDetail = await _context.SubCategory.Where(u => u.MainCategoryId == item.MainCategoryId && u.CategoryStatus == Convert.ToInt32(Status.Approved)).ToListAsync();

                            }
                            else if (model.categoryType == 1)
                            {
                                subCategoryDetail = await _context.SubCategory.Where(u => u.MainCategoryId == item.MainCategoryId && u.CategoryStatus == Convert.ToInt32(Status.Approved)
                               && (u.Male == true)
                               //    && (u.Female == false)
                               ).ToListAsync();
                            }
                            else if (model.categoryType == 2)
                            {
                                subCategoryDetail = await _context.SubCategory.Where(u => u.MainCategoryId == item.MainCategoryId && u.CategoryStatus == Convert.ToInt32(Status.Approved)
                               //    && (u.Male == false)
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
                            mappedData.isNext = subCategoryDetail.Count > 0 ? true : false;
                            Categories.Add(mappedData);
                        }
                    }
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

        #region SetVendorCategoryStatus
        /// <summary>
        /// Set vendor category status.
        /// </summary>
        [HttpPost]
        [Route("SetVendorCategoryStatus")]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Authorize(Roles = "Vendor")]
        public async Task<IActionResult> SetVendorCategoryStatus([FromBody] VendorCategoryRequestDTO model)
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

                if (model.salonId > 0)
                {
                    var salonDetail = await _context.SalonDetail.Where(u => u.SalonId == model.salonId).FirstOrDefaultAsync();
                    if (salonDetail == null)
                    {
                        _response.StatusCode = HttpStatusCode.OK;
                        _response.IsSuccess = false;
                        _response.Messages = "Not found any Salon.";
                        return Ok(_response);
                    }
                }

                if (model.Status == false)
                {
                    var addVendor = _mapper.Map<VendorCategory>(model);
                    addVendor.VendorId = currentUserId;
                    if (model.mainCategoryId > 0)
                    {
                        var vendorCategory = new VendorCategory();
                        // if (model.categoryType == 0)
                        // {
                        vendorCategory = await _context.VendorCategory.Where(u => (u.SalonId == model.salonId)
                       && (u.MainCategoryId == model.mainCategoryId)
                       ).FirstOrDefaultAsync();
                        // }
                        // else if (model.categoryType == 1)
                        // {
                        //     vendorCategory = await _context.VendorCategory.Where(u => (u.SalonId == model.salonId)
                        //    && (u.MainCategoryId == model.mainCategoryId)
                        //    && (u.Male == true)
                        //    && (u.Female == false)
                        //    ).FirstOrDefaultAsync();
                        // }
                        // else if (model.categoryType == 2)
                        // {
                        //     vendorCategory = await _context.VendorCategory.Where(u => (u.SalonId == model.salonId)
                        //    && (u.MainCategoryId == model.mainCategoryId)
                        //    && (u.Male == false)
                        //    && (u.Female == true)
                        //    ).FirstOrDefaultAsync();
                        // }
                        // else
                        // {
                        //     vendorCategory = await _context.VendorCategory.Where(u => (u.SalonId == model.salonId)
                        //    && (u.MainCategoryId == model.mainCategoryId)
                        //    && (u.Male == true)
                        //    && (u.Female == true)
                        //    ).FirstOrDefaultAsync();
                        // }

                        if (vendorCategory != null)
                        {
                            _response.StatusCode = HttpStatusCode.OK;
                            _response.IsSuccess = false;
                            _response.Messages = "Category has already Deactive.";
                            return Ok(_response);
                        }

                        var category = new MainCategory();
                        // if (model.categoryType == 0)
                        // {
                        vendorCategory = await _context.VendorCategory.Where(u => (u.MainCategoryId == model.mainCategoryId)
                       ).FirstOrDefaultAsync();
                        // }
                        // else if (model.categoryType == 1)
                        // {
                        //     vendorCategory = await _context.VendorCategory.Where(u => (u.MainCategoryId == model.mainCategoryId)
                        //    && (u.Male == true)
                        //    && (u.Female == false)
                        //    ).FirstOrDefaultAsync();
                        // }
                        // else if (model.categoryType == 2)
                        // {
                        //     vendorCategory = await _context.VendorCategory.Where(u => (u.MainCategoryId == model.mainCategoryId)
                        //    && (u.Male == false)
                        //    && (u.Female == true)
                        //    ).FirstOrDefaultAsync();
                        // }
                        // else
                        // {
                        //     vendorCategory = await _context.VendorCategory.Where(u => (u.MainCategoryId == model.mainCategoryId)
                        //    && (u.Male == true)
                        //    && (u.Female == true)
                        //    ).FirstOrDefaultAsync();
                        // }

                        if (category == null)
                        {
                            _response.StatusCode = HttpStatusCode.OK;
                            _response.IsSuccess = false;
                            _response.Messages = "Not found any category.";
                            return Ok(_response);
                        }

                        // var cartDetail = await _cartRepository.GetAllAsync(u => u.SalonId == model.salonId);
                        // foreach (var item in cartDetail)
                        // {
                        //     var Detail = (await _Repository.GetAllAsync(u => (u.Id == item.Id))).FirstOrDefault();
                        //     if (Detail != null)
                        //     {
                        //         var inventoryDetail = await _context.Inventory.Where(u => u.Id == Detail.InventoryId && (u.MainCategoryId == model.mainCategoryId)).FirstOrDefaultAsync();

                        //         if (inventoryDetail != null)
                        //         {
                        //             await _cartRepository.RemoveEntity(item);
                        //         }
                        //     }
                        // }
                        addVendor.MainCategoryId = addVendor.MainCategoryId > 0 ? addVendor.MainCategoryId : null;
                        addVendor.SubCategoryId = null;
                        await _context.AddAsync(addVendor);
                        await _context.SaveChangesAsync();
                    }
                    else
                    {
                        var vendorCategory = new VendorCategory();
                        // if (model.categoryType == 0)
                        // {
                        vendorCategory = await _context.VendorCategory.Where(u => (u.SalonId == model.salonId)
                       && (u.SubCategoryId == model.subCategoryId)
                       ).FirstOrDefaultAsync();
                        // }
                        // else if (model.categoryType == 1)
                        // {
                        //     vendorCategory = await _context.VendorCategory.Where(u => (u.SalonId == model.salonId)
                        //    && (u.SubCategoryId == model.subCategoryId)
                        //    && (u.Male == true)
                        //    && (u.Female == false)
                        //    ).FirstOrDefaultAsync();
                        // }
                        // else if (model.categoryType == 2)
                        // {
                        //     vendorCategory = await _context.VendorCategory.Where(u => (u.SalonId == model.salonId)
                        //    && (u.SubCategoryId == model.subCategoryId)
                        //    && (u.Male == false)
                        //    && (u.Female == true)
                        //    ).FirstOrDefaultAsync();
                        // }
                        // else
                        // {
                        //     vendorCategory = await _context.VendorCategory.Where(u => (u.SalonId == model.salonId)
                        //    && (u.SubCategoryId == model.subCategoryId)
                        //    && (u.Male == true)
                        //    && (u.Female == true)
                        //    ).FirstOrDefaultAsync();
                        // }

                        if (vendorCategory != null)
                        {
                            _response.StatusCode = HttpStatusCode.OK;
                            _response.IsSuccess = false;
                            _response.Messages = "Category has already Deactive.";
                            return Ok(_response);
                        }

                        var category = new SubCategory();

                        // if (model.categoryType == 0)
                        // {
                        vendorCategory = await _context.VendorCategory.Where(u => (u.SubCategoryId == model.subCategoryId)
                       ).FirstOrDefaultAsync();
                        // }
                        // else if (model.categoryType == 1)
                        // {
                        //     vendorCategory = await _context.VendorCategory.Where(u => (u.SubCategoryId == model.subCategoryId)
                        //    && (u.Male == true)
                        //    && (u.Female == false)
                        //    ).FirstOrDefaultAsync();
                        // }
                        // else if (model.categoryType == 2)
                        // {
                        //     vendorCategory = await _context.VendorCategory.Where(u => (u.SubCategoryId == model.subCategoryId)
                        //    && (u.Male == false)
                        //    && (u.Female == true)
                        //    ).FirstOrDefaultAsync();
                        // }
                        // else
                        // {
                        //     vendorCategory = await _context.VendorCategory.Where(u => (u.SubCategoryId == model.subCategoryId)
                        //    && (u.Male == true)
                        //    && (u.Female == true)
                        //    ).FirstOrDefaultAsync();
                        // }

                        if (category == null)
                        {
                            _response.StatusCode = HttpStatusCode.OK;
                            _response.IsSuccess = false;
                            _response.Messages = "Not found any category.";
                            return Ok(_response);
                        }
                        // var cartDetail = await _cartRepository.GetAllAsync(u => u.SalonId == model.salonId);
                        // foreach (var item in cartDetail)
                        // {
                        //     var Detail = (await _Repository.GetAllAsync(u => (u.Id == item.Id))).FirstOrDefault();
                        //     if (Detail != null)
                        //     {
                        //         var inventoryDetail = await _context.Inventory.Where(u => u.Id == Detail.InventoryId && (u.SubSubCategoryId == model.SubSubCategoryId)).FirstOrDefaultAsync();

                        //         if (inventoryDetail != null)
                        //         {
                        //             await _cartRepository.RemoveEntity(item);
                        //         }
                        //     }
                        // }
                        // addVendor.MainCategoryId = Category.MainCategoryId;
                        addVendor.MainCategoryId = null;
                        addVendor.SubCategoryId = addVendor.SubCategoryId > 0 ? addVendor.SubCategoryId : null;
                        await _context.VendorCategory.AddAsync(addVendor);
                        await _context.SaveChangesAsync();
                    }
                }
                else
                {
                    if (model.mainCategoryId > 0)
                    {
                        var vendorCategory = new VendorCategory();
                        // if (model.categoryType == 0)
                        // {
                        vendorCategory = await _context.VendorCategory.Where(u => (u.SalonId == model.salonId)
                       && (u.MainCategoryId == model.mainCategoryId)
                       ).FirstOrDefaultAsync();
                        // }
                        // else if (model.categoryType == 1)
                        // {
                        //     vendorCategory = await _context.VendorCategory.Where(u => (u.SalonId == model.salonId)
                        //    && (u.MainCategoryId == model.mainCategoryId)
                        //    && (u.Male == true)
                        //    && (u.Female == false)
                        //    ).FirstOrDefaultAsync();
                        // }
                        // else if (model.categoryType == 2)
                        // {
                        //     vendorCategory = await _context.VendorCategory.Where(u => (u.SalonId == model.salonId)
                        //    && (u.MainCategoryId == model.mainCategoryId)
                        //    && (u.Male == false)
                        //    && (u.Female == true)
                        //    ).FirstOrDefaultAsync();
                        // }
                        // else
                        // {
                        //     vendorCategory = await _context.VendorCategory.Where(u => (u.SalonId == model.salonId)
                        //    && (u.MainCategoryId == model.mainCategoryId)
                        //    && (u.Male == true)
                        //    && (u.Female == true)
                        //    ).FirstOrDefaultAsync();
                        // }

                        if (vendorCategory == null)
                        {
                            _response.StatusCode = HttpStatusCode.OK;
                            _response.IsSuccess = false;
                            _response.Messages = "Category has already active.";
                            return Ok(_response);
                        }
                        _context.Remove(vendorCategory);
                        await _context.SaveChangesAsync();
                    }
                    else
                    {
                        var vendorCategory = new VendorCategory();
                        // if (model.categoryType == 0)
                        // {
                        vendorCategory = await _context.VendorCategory.Where(u => (u.SalonId == model.salonId)
                       && (u.SubCategoryId == model.subCategoryId)
                       ).FirstOrDefaultAsync();
                        // }
                        // else if (model.categoryType == 1)
                        // {
                        //     vendorCategory = await _context.VendorCategory.Where(u => (u.SalonId == model.salonId)
                        //    && (u.SubCategoryId == model.subCategoryId)
                        //    && (u.Male == true)
                        //    && (u.Female == false)
                        //    ).FirstOrDefaultAsync();
                        // }
                        // else if (model.categoryType == 2)
                        // {
                        //     vendorCategory = await _context.VendorCategory.Where(u => (u.SalonId == model.salonId)
                        //    && (u.SubCategoryId == model.subCategoryId)
                        //    && (u.Male == false)
                        //    && (u.Female == true)
                        //    ).FirstOrDefaultAsync();
                        // }
                        // else
                        // {
                        //     vendorCategory = await _context.VendorCategory.Where(u => (u.SalonId == model.salonId)
                        //    && (u.SubCategoryId == model.subCategoryId)
                        //    && (u.Male == true)
                        //    && (u.Female == true)
                        //    ).FirstOrDefaultAsync();
                        // }

                        if (vendorCategory == null)
                        {
                            _response.StatusCode = HttpStatusCode.OK;
                            _response.IsSuccess = false;
                            _response.Messages = "Category has already active.";
                            return Ok(_response);
                        }
                        _context.Remove(vendorCategory);
                        await _context.SaveChangesAsync();
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

        #region AddSalonBanner
        /// <summary>
        /// Add SalonBanner {SalonBanner, SalonCategoryBanner}.
        /// </summary>
        [HttpPost]
        [Route("AddSalonBanner")]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Authorize(Roles = "SuperAdmin,Admin,Vendor")]
        public async Task<IActionResult> AddSalonBanner([FromForm] AddSalonBannerDTO model)
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

                if (model.bannerImage == null)
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = "Please choose image.";
                    return Ok(_response);
                }

                model.mainCategoryId = model.mainCategoryId == null ? model.mainCategoryId = 0 : model.mainCategoryId;
                model.subCategoryId = model.subCategoryId == null ? model.subCategoryId = 0 : model.subCategoryId;

                if (model.bannerType != BannerType.SalonBanner.ToString() && model.bannerType != BannerType.SalonCategoryBanner.ToString())
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = "Please enter correct banner type.";
                    return Ok(_response);
                }

                if (model.bannerType == BannerType.SalonBanner.ToString() && (model.mainCategoryId > 0))
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = "Please enter correct banner type.";
                    return Ok(_response);
                }

                if (model.bannerType == BannerType.SalonCategoryBanner.ToString())
                {
                    if ((model.mainCategoryId == 0 && model.subCategoryId == 0))
                    {
                        _response.StatusCode = HttpStatusCode.OK;
                        _response.IsSuccess = false;
                        _response.Messages = "Please enter correct banner type.";
                        return Ok(_response);
                    }
                }

                if (model.subCategoryId > 0)
                {
                    var getCategoryDetail = await _context.SubCategory.FirstOrDefaultAsync(u => u.SubCategoryId == model.subCategoryId);
                    if (getCategoryDetail == null)
                    {
                        _response.StatusCode = HttpStatusCode.OK;
                        _response.IsSuccess = false;
                        _response.Messages = "Category not found.";
                        return Ok(_response);
                    }
                }
                else
                {
                    model.subCategoryId = null;
                }
                if (model.mainCategoryId > 0)
                {
                    var getCategoryDetail = await _context.MainCategory.FirstOrDefaultAsync(u => u.MainCategoryId == model.mainCategoryId);
                    if (getCategoryDetail == null)
                    {
                        _response.StatusCode = HttpStatusCode.OK;
                        _response.IsSuccess = false;
                        _response.Messages = "Category not found.";
                        return Ok(_response);
                    }
                }
                else
                {
                    model.mainCategoryId = null;
                }

                var SalonBanner = _mapper.Map<SalonBanner>(model);

                var documentFile = ContentDispositionHeaderValue.Parse(model.bannerImage.ContentDisposition).FileName.Trim('"');
                documentFile = CommonMethod.EnsureCorrectFilename(documentFile);
                documentFile = CommonMethod.RenameFileName(documentFile);

                var documentPath = SalonBannerImageContainer + documentFile;
                bool uploadStatus = await _uploadRepository.UploadFilesToServer(
                        model.bannerImage,
                        SalonBannerImageContainer,
                        documentFile
                    );
                SalonBanner.BannerImage = documentPath;
                _context.Add(SalonBanner);
                _context.SaveChanges();

                var getSalonBanner = await _context.SalonBanner.FirstOrDefaultAsync(u => u.SalonBannerId == SalonBanner.SalonBannerId);

                if (getSalonBanner != null)
                {
                    var SalonBannerDetail = _mapper.Map<GetSalonBannerDTO>(getSalonBanner);
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = true;
                    _response.Data = SalonBannerDetail;
                    _response.Messages = "SalonBanner" + ResponseMessages.msgAdditionSuccess;
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

        #region UpdateSalonBanner
        /// <summary>
        /// Update Salon Banner.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Route("UpdateSalonBanner")]
        [Authorize(Roles = "SuperAdmin,Admin,Vendor")]
        public async Task<IActionResult> UpdateSalonBanner([FromForm] UpdateSalonBannerDTO model)
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

                model.mainCategoryId = model.mainCategoryId == null ? model.mainCategoryId = 0 : model.mainCategoryId;
                model.subCategoryId = model.subCategoryId == null ? model.subCategoryId = 0 : model.subCategoryId;



                if (model.bannerType != BannerType.SalonBanner.ToString() && model.bannerType != BannerType.SalonCategoryBanner.ToString())
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = "Please enter correct banner type.";
                    return Ok(_response);
                }

                if (model.bannerType == BannerType.SalonBanner.ToString() && (model.mainCategoryId != 0))
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = "Please enter correct banner type.";
                    return Ok(_response);
                }

                if (model.bannerType == BannerType.SalonCategoryBanner.ToString())
                {
                    if ((model.mainCategoryId == 0 && model.subCategoryId == 0))
                    {
                        _response.StatusCode = HttpStatusCode.OK;
                        _response.IsSuccess = false;
                        _response.Messages = "Please enter correct banner type.";
                        return Ok(_response);
                    }
                }
                if (model.subCategoryId > 0)
                {
                    var getCategoryDetail = await _context.SubCategory.FirstOrDefaultAsync(u => u.SubCategoryId == model.subCategoryId);
                    if (getCategoryDetail == null)
                    {
                        _response.StatusCode = HttpStatusCode.OK;
                        _response.IsSuccess = false;
                        _response.Messages = "Category not found.";
                        return Ok(_response);
                    }
                }
                else
                {
                    model.subCategoryId = null;
                }
                if (model.mainCategoryId > 0)
                {
                    var getCategoryDetail = await _context.MainCategory.FirstOrDefaultAsync(u => u.MainCategoryId == model.mainCategoryId);
                    if (getCategoryDetail == null)
                    {
                        _response.StatusCode = HttpStatusCode.OK;
                        _response.IsSuccess = false;
                        _response.Messages = "Category not found.";
                        return Ok(_response);
                    }
                }
                else
                {
                    model.mainCategoryId = null;
                }

                var updteSalonBanner = await _context.SalonBanner.FirstOrDefaultAsync(u => u.SalonBannerId == model.salonBannerId);
                var oldBannerImage = updteSalonBanner.BannerImage;
                _mapper.Map(model, updteSalonBanner);
                if (model.bannerImage != null)
                {
                    // Delete previous file
                    if (!string.IsNullOrEmpty(updteSalonBanner.BannerImage))
                    {
                        var chk = await _uploadRepository.DeleteFilesFromServer("FileToSave/" + updteSalonBanner.BannerImage);
                    }
                    var documentFile = ContentDispositionHeaderValue.Parse(model.bannerImage.ContentDisposition).FileName.Trim('"');
                    documentFile = CommonMethod.EnsureCorrectFilename(documentFile);
                    documentFile = CommonMethod.RenameFileName(documentFile);

                    var documentPath = SalonBannerImageContainer + documentFile;
                    bool uploadStatus = await _uploadRepository.UploadFilesToServer(
                            model.bannerImage,
                            SalonBannerImageContainer,
                            documentFile
                        );
                    updteSalonBanner.BannerImage = documentPath;
                }
                else
                {
                    updteSalonBanner.BannerImage = oldBannerImage;
                }
                _context.Update(updteSalonBanner);
                _context.SaveChanges();

                if (updteSalonBanner != null)
                {
                    var SalonBannerDetail = _mapper.Map<GetSalonBannerDTO>(updteSalonBanner);
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = true;
                    _response.Data = SalonBannerDetail;
                    _response.Messages = "SalonBanner" + ResponseMessages.msgUpdationSuccess;
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

        #region DeleteSalonBanner
        /// <summary>
        /// Delete salon banner.
        /// </summary>
        [HttpDelete]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Route("DeleteSalonBanner")]
        [Authorize(Roles = "SuperAdmin,Admin,Vendor")]
        public async Task<IActionResult> DeleteSalonBanner(int salonBannerId)
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
                var getSalonBanner = await _context.SalonBanner.FirstOrDefaultAsync(u => u.SalonBannerId == salonBannerId);

                if (getSalonBanner != null)
                {
                    _context.Remove(getSalonBanner);
                    _context.SaveChanges();

                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = true;
                    _response.Messages = "SalonBanner" + ResponseMessages.msgDeletionSuccess;
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

        #region GetSalonBannerDetail
        /// <summary>
        /// Get SalonBanner.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Route("GetSalonBannerDetail")]
        [Authorize(Roles = "SuperAdmin,Admin,Vendor,Customer")]
        public async Task<IActionResult> GetSalonBannerDetail(int salonBannerId)
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
                var getSalonBanner = await _context.SalonBanner.FirstOrDefaultAsync(u => u.SalonBannerId == salonBannerId);

                if (getSalonBanner != null)
                {
                    var SalonBannerDetail = _mapper.Map<GetSalonBannerDTO>(getSalonBanner);
                    if (SalonBannerDetail.subCategoryId > 0)
                    {
                        var categoryDetail = await _context.SubCategory.FirstOrDefaultAsync(u => u.SubCategoryId == SalonBannerDetail.subCategoryId);
                        SalonBannerDetail.subCategoryName = categoryDetail.CategoryName;
                    }
                    if (SalonBannerDetail.mainCategoryId > 0)
                    {
                        var categoryDetail = await _context.MainCategory.FirstOrDefaultAsync(u => u.MainCategoryId == SalonBannerDetail.mainCategoryId);
                        SalonBannerDetail.mainCategoryName = categoryDetail.CategoryName;
                    }

                    SalonBannerDetail.createDate = Convert.ToDateTime(SalonBannerDetail.createDate).ToString(@"dd-MM-yyyy");
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = true;
                    _response.Data = SalonBannerDetail;
                    _response.Messages = "SalonBanner detail" + ResponseMessages.msgShownSuccess;
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

        #region GetSalonBannerList
        /// <summary>
        ///  Get SalonBanner list.
        /// </summary>
        [HttpGet]
        [Route("GetSalonBannerList")]
        [Authorize]
        public async Task<IActionResult> GetSalonBannerList([FromQuery] GetSalonBannerrequestDTO model)
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
                var salonBanners = new List<SalonBanner>();
                if (string.IsNullOrEmpty(model.salonBannerType))
                {
                    if (model.subCategoryId > 0)
                    {
                        salonBanners = await _context.SalonBanner.Where(u => (u.SalonId == model.salonId) && (u.SubCategoryId == model.subCategoryId)).ToListAsync();
                    }
                    else if (model.mainCategoryId > 0)
                    {
                        salonBanners = await _context.SalonBanner.Where(u => (u.SalonId == model.salonId) && (u.MainCategoryId == model.mainCategoryId)).ToListAsync();
                    }
                    else
                    {
                        salonBanners = await _context.SalonBanner.Where(u => (u.SalonId == model.salonId) && (u.BannerType == BannerType.SalonBanner.ToString())).ToListAsync();
                    }
                }
                else
                {
                    if (model.salonBannerType == BannerType.SalonBanner.ToString())
                    {
                        salonBanners = await _context.SalonBanner.Where(u => (u.SalonId == model.salonId) && (u.BannerType == BannerType.SalonBanner.ToString())).ToListAsync();
                    }
                    else if (model.salonBannerType == BannerType.SalonBanner.ToString())
                    {
                        salonBanners = await _context.SalonBanner.Where(u => (u.SalonId == model.salonId) && (u.BannerType == BannerType.SalonCategoryBanner.ToString())).ToListAsync();
                    }
                    else
                    {
                        salonBanners = await _context.SalonBanner.Where(u => u.SalonId == model.salonId).ToListAsync();
                    }
                }
                if (salonBanners == null)
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = "Not found any record.";
                    return Ok(_response);
                }
                salonBanners = salonBanners.OrderByDescending(u => u.ModifyDate).ToList();
                List<GetSalonBannerDTO> SalonBannerList = _mapper.Map<List<GetSalonBannerDTO>>(salonBanners);
                foreach (var item in SalonBannerList)
                {
                    item.createDate = Convert.ToDateTime(item.createDate).ToString(@"dd-MM-yyyy");
                    if (item.subCategoryId > 0)
                    {
                        var categoryDetail = await _context.SubCategory.FirstOrDefaultAsync(u => u.SubCategoryId == item.subCategoryId);
                        item.subCategoryName = categoryDetail.CategoryName;
                    }
                    if (item.mainCategoryId > 0)
                    {
                        var categoryDetail = await _context.MainCategory.FirstOrDefaultAsync(u => u.MainCategoryId == item.mainCategoryId);
                        item.mainCategoryName = categoryDetail.CategoryName;
                    }
                    if (item.bannerType == BannerType.SalonBanner.ToString())
                    {
                        item.bannerTypeName = "Salon Banner";
                    }
                    if (item.bannerType == BannerType.SalonCategoryBanner.ToString())
                    {
                        item.bannerTypeName = "Salon Category Banner";
                    }
                }

                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Data = SalonBannerList;
                _response.Messages = "SalonBanner list shown successfully.";
                return Ok(_response);
            }
            catch (System.Exception ex)
            {

                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.IsSuccess = false;
                _response.Data = new { };
                _response.Messages = ResponseMessages.msgSomethingWentWrong + ex.Message;
                return Ok(_response);
            }
        }
        #endregion

        #region SetPaymentStatus
        /// <summary>
        /// Set payment status {OnHold, Paid, Unpaid, Refunded}.
        /// </summary>
        [HttpPost]
        [Route("SetPaymentStatus")]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Authorize(Roles = "Vendor")]
        public async Task<IActionResult> SetPaymentStatus(SetPaymentStatusDTO model)
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

                if (model.paymentStatus != PaymentStatus.OnHold.ToString()
                && model.paymentStatus != PaymentStatus.Paid.ToString()
                && model.paymentStatus != PaymentStatus.Unpaid.ToString()
                && model.paymentStatus != PaymentStatus.Refunded.ToString())
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = "Please select a valid status.";
                    return Ok(_response);
                }

                var appointmentDetail = await _context.Appointment.Where(u => u.AppointmentId == model.appointmentId).FirstOrDefaultAsync();
                if (appointmentDetail == null)
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Data = new Object { };
                    _response.Messages = "Not found any order.";
                    return Ok(_response);
                }

                if (appointmentDetail.AppointmentStatus == AppointmentStatus.Confirmed.ToString())
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Data = new Object { };
                    _response.Messages = "Can't change Appointment status after confirmed.";
                    return Ok(_response);
                }

                if (appointmentDetail.PaymentStatus == PaymentStatus.Paid.ToString())
                {
                    if (model.paymentStatus == PaymentStatus.OnHold.ToString()
                    || model.paymentStatus == PaymentStatus.Unpaid.ToString())
                    {
                        _response.StatusCode = HttpStatusCode.OK;
                        _response.IsSuccess = false;
                        _response.Data = new Object { };
                        _response.Messages = "Please enter valid payment status.";
                        return Ok(_response);
                    }
                }

                if (appointmentDetail.PaymentStatus == PaymentStatus.Refunded.ToString())
                {
                    if (model.paymentStatus == PaymentStatus.OnHold.ToString()
                    || model.paymentStatus == PaymentStatus.Paid.ToString()
                    || model.paymentStatus == PaymentStatus.Unpaid.ToString())
                    {
                        _response.StatusCode = HttpStatusCode.OK;
                        _response.IsSuccess = false;
                        _response.Data = new Object { };
                        _response.Messages = "Please enter valid payment status.";
                        return Ok(_response);
                    }
                }

                appointmentDetail.PaymentStatus = model.paymentStatus;
                _context.Appointment.Update(appointmentDetail);
                await _context.SaveChangesAsync();
                // send Notification

                string motificationMessage = "";

                if (appointmentDetail.PaymentStatus == PaymentStatus.Unpaid.ToString())
                {
                    motificationMessage = "Payment remain unpaid.";
                }
                else if (appointmentDetail.PaymentStatus == PaymentStatus.Paid.ToString())
                {
                    motificationMessage = "Paid successfully.";
                }
                else if (appointmentDetail.PaymentStatus == PaymentStatus.OnHold.ToString())
                {
                    motificationMessage = "Your payment is on hold.";
                }
                else
                {
                    motificationMessage = "Your payment has been refunded.";
                }

                var user = await _context.UserDetail.Where(a => (a.UserId == appointmentDetail.CustomerUserId) && (a.IsDeleted != true)).FirstOrDefaultAsync();
                var userprofileDetail = _userManager.FindByIdAsync(user.UserId).GetAwaiter().GetResult();
                var token = user.Fcmtoken;
                var title = "Payment Status";
                var description = String.Format("Hi {0},\n{1}", userprofileDetail.FirstName, motificationMessage);
                if (!string.IsNullOrEmpty(token))
                {
                    // if (user.IsNotificationEnabled == true)
                    // {
                    var resp = await _mobileMessagingClient.SendNotificationAsync(token, title, description);
                    // if (!string.IsNullOrEmpty(resp))
                    // {
                    // update notification sent
                    var notificationSent = new NotificationSent();
                    notificationSent.Title = title;
                    notificationSent.Description = description;
                    notificationSent.NotificationType = NotificationType.Order.ToString();
                    notificationSent.UserId = user.UserId;

                    await _context.AddAsync(notificationSent);
                    await _context.SaveChangesAsync();
                    // }
                    // }
                }

                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                // _response.Data = response;
                _response.Messages = "Payment status" + ResponseMessages.msgUpdationSuccess;
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
