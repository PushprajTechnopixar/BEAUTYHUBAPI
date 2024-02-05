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
using BeautyHubAPI.Common;

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
                    _response.Messages = ResponseMessages.msgUserNotFound;
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
                    _response.Messages = ResponseMessages.msgNotFound + "record.";
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
                                _response.Messages = ResponseMessages.msgNotFound + "record.";
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

                _context.Update(membershipRecord);
                await _context.SaveChangesAsync();

                var responseData = _mapper.Map<GetMembershipRecordDTO>(membershipRecord);

                responseData.transactionId = membershipRecord.TransactionId;
                responseData.totalAmount = (double)planDetail.TotalAmount;
                var membershipPlan = await _context.MembershipPlan.Where(u => u.MembershipPlanId == membershipRecord.MembershipPlanId).FirstOrDefaultAsync();

                responseData.planName = membershipPlan.PlanName;

                responseData.createDate = membershipRecord.CreateDate.ToString(@"dd-MM-yyyy");
                responseData.expiryDate = membershipRecord.ExpiryDate.ToString(@"dd-MM-yyyy");

                var oldMembershipRecord = await _membershipRecordRepository.GetAllAsync(a => (a.VendorId == membershipRecord.VendorId) && (a.MembershipRecordId != membershipRecord.MembershipRecordId) && (a.PlanStatus == true));
                List<MembershipRecord>? updateMembership = new List<MembershipRecord>();
                foreach (var item in oldMembershipRecord)
                {
                    item.PlanStatus = false;
                    updateMembership.Add(item);
                }
                await _membershipRecordRepository.UpdateMembershipRecord(updateMembership);

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
                List<VendorCategoryDTO> Categories = new List<VendorCategoryDTO>();

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
                        Categories = new List<VendorCategoryDTO>();
                        foreach (var item in categoryDetail)
                        {
                            var mappedData = _mapper.Map<VendorCategoryDTO>(item);
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
                        Categories = new List<VendorCategoryDTO>();
                        foreach (var item in categoryDetail)
                        {
                            var mappedData = _mapper.Map<VendorCategoryDTO>(item);
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

                Categories = Categories.Where(u => u.mainCategoryId != 53).ToList();
                foreach (var item in Categories)
                {
                    // item.categoryTypeName = 
                    if (item.categoryType == 1)
                    {
                        item.categoryTypeName = "Male";
                    }
                    else if (item.categoryType == 2)
                    {
                        item.categoryTypeName = "Female";
                    }
                    else
                    {
                        item.categoryTypeName = "Male & Female";
                    }
                }

                if (Categories.Count > 0)
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = true;
                    _response.Data = Categories;
                    _response.Messages = "Category" + ResponseMessages.msgListFoundSuccess;
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
                        _response.Messages = ResponseMessages.msgNotFound + "Salon.";
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
                            _response.Messages = ResponseMessages.msgNotFound + "category.";
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
                        _response.Messages = ResponseMessages.msgNotFound + "Category";
                        return Ok(_response);
                    }
                    else
                    {
                        var categoryType = 0;
                        if (getCategoryDetail.Male == true && getCategoryDetail.Female == false)
                        {
                            categoryType = 1;
                        }
                        if (getCategoryDetail.Male == false && getCategoryDetail.Female == true)
                        {
                            categoryType = 2;
                        }
                        else
                        {
                            categoryType = 3;
                        }
                        if (categoryType != 3)
                        {
                            if (model.categoryType != categoryType)
                            {
                                _response.StatusCode = HttpStatusCode.OK;
                                _response.IsSuccess = false;
                                _response.Messages = "Please enter valid category type.";
                                return Ok(_response);
                            }
                        }

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
                        _response.Messages = ResponseMessages.msgNotFound + "Category";
                        return Ok(_response);
                    }
                    else
                    {
                        var categoryType = 0;
                        if (getCategoryDetail.Male == true && getCategoryDetail.Female == false)
                        {
                            categoryType = 1;
                        }
                        if (getCategoryDetail.Male == false && getCategoryDetail.Female == true)
                        {
                            categoryType = 2;
                        }
                        else
                        {
                            categoryType = 3;
                        }
                        if (categoryType != 3)
                        {
                            if (model.categoryType != categoryType)
                            {
                                _response.StatusCode = HttpStatusCode.OK;
                                _response.IsSuccess = false;
                                _response.Messages = "Please enter valid category type.";
                                return Ok(_response);
                            }
                        }

                    }
                }
                else
                {
                    model.mainCategoryId = null;
                }

                var SalonBanner = _mapper.Map<SalonBanner>(model);
                if (model.categoryType == 1)
                {
                    SalonBanner.Male = true;
                    SalonBanner.Female = false;
                }
                else if (model.categoryType == 2)
                {
                    SalonBanner.Female = true;
                    SalonBanner.Male = false;
                }
                else
                {
                    SalonBanner.Male = true;
                    SalonBanner.Female = true;
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
                        _response.Messages = ResponseMessages.msgNotFound + "Category";
                        return Ok(_response);
                    }
                    else
                    {
                        var categoryType = 0;
                        if (getCategoryDetail.Male == true && getCategoryDetail.Female == false)
                        {
                            categoryType = 1;
                        }
                        else if (getCategoryDetail.Male == false && getCategoryDetail.Female == true)
                        {
                            categoryType = 2;
                        }
                        else
                        {
                            categoryType = 3;
                        }
                        if (categoryType != 3)
                        {
                            if (model.categoryType != categoryType)
                            {
                                _response.StatusCode = HttpStatusCode.OK;
                                _response.IsSuccess = false;
                                _response.Messages = "Please enter valid category type.";
                                return Ok(_response);
                            }
                        }

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
                        _response.Messages = ResponseMessages.msgNotFound + "Category";
                        return Ok(_response);
                    }
                    else
                    {
                        var categoryType = 0;
                        if (getCategoryDetail.Male == true && getCategoryDetail.Female == false)
                        {
                            categoryType = 1;
                        }
                        else if (getCategoryDetail.Male == false && getCategoryDetail.Female == true)
                        {
                            categoryType = 2;
                        }
                        else
                        {
                            categoryType = 3;
                        }
                        if (categoryType != 3)
                        {
                            if (model.categoryType != categoryType)
                            {
                                _response.StatusCode = HttpStatusCode.OK;
                                _response.IsSuccess = false;
                                _response.Messages = "Please enter valid category type.";
                                return Ok(_response);
                            }
                        }
                    }
                }
                else
                {
                    model.mainCategoryId = null;
                }

                var updteSalonBanner = await _context.SalonBanner.FirstOrDefaultAsync(u => u.SalonBannerId == model.salonBannerId);
                var oldBannerImage = updteSalonBanner.BannerImage;
                _mapper.Map(model, updteSalonBanner);

                if (model.categoryType == 1)
                {
                    updteSalonBanner.Male = true;
                    updteSalonBanner.Female = false;
                }
                else if (model.categoryType == 2)
                {
                    updteSalonBanner.Female = true;
                    updteSalonBanner.Male = false;
                }
                else
                {
                    updteSalonBanner.Male = true;
                    updteSalonBanner.Female = true;
                }

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
                    if (SalonBannerDetail.male == true && SalonBannerDetail.female == false)
                    {
                        SalonBannerDetail.categoryType = 1;
                    }
                    else if (SalonBannerDetail.male == false && SalonBannerDetail.female == true)
                    {
                        SalonBannerDetail.categoryType = 2;
                    }
                    else
                    {
                        SalonBannerDetail.categoryType = 3;
                    }
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
                    if (SalonBannerDetail.male == true && SalonBannerDetail.female == false)
                    {
                        SalonBannerDetail.categoryType = 1;
                    }
                    else if (SalonBannerDetail.male == false && SalonBannerDetail.female == true)
                    {
                        SalonBannerDetail.categoryType = 2;
                    }
                    else
                    {
                        SalonBannerDetail.categoryType = 3;
                    }
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
                    if (model.mainCategoryId > 0)
                    {
                        salonBanners = await _context.SalonBanner.Where(u => (u.SalonId == model.salonId) && (u.MainCategoryId == model.mainCategoryId)).ToListAsync();
                    }
                    else if (model.subCategoryId > 0)
                    {
                        salonBanners = await _context.SalonBanner.Where(u => (u.SalonId == model.salonId) && (u.SubCategoryId == model.subCategoryId)).ToListAsync();
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
                    else if (model.salonBannerType == BannerType.SalonCategoryBanner.ToString())
                    {
                        salonBanners = await _context.SalonBanner.Where(u => (u.SalonId == model.salonId) && (u.BannerType == BannerType.SalonCategoryBanner.ToString())).ToListAsync();
                        if (model.subCategoryId > 0)
                        {
                            salonBanners = salonBanners.Where(u => (u.SalonId == model.salonId) && (u.SubCategoryId == model.subCategoryId)).ToList();
                        }
                        else if (model.mainCategoryId > 0)
                        {
                            salonBanners = salonBanners.Where(u => (u.SalonId == model.salonId) && (u.MainCategoryId == model.mainCategoryId)).ToList();
                        }
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
                    _response.Messages = ResponseMessages.msgNotFound + "record.";
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

                if (model.categoryType > 0)
                {
                    if (model.categoryType == 1)
                    {
                        SalonBannerList = SalonBannerList.Where(u => u.male == true && u.female == false).ToList();
                    }
                    if (model.categoryType == 2)
                    {
                        SalonBannerList = SalonBannerList.Where(u => u.male == false && u.female == true).ToList();
                    }
                }

                foreach (var item in SalonBannerList)
                {
                    if (item.male == true && item.female == false)
                    {
                        item.categoryType = 1;
                    }
                    else if (item.male == false && item.female == true)
                    {
                        item.categoryType = 2;
                    }
                    else
                    {
                        item.categoryType = 3;
                    }
                }

                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Data = SalonBannerList;
                _response.Messages = "SalonBanner" + ResponseMessages.msgListFoundSuccess;
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

        #region GetVendorAppointmentList
        /// <summary>
        ///  Get appointment list for vendor {date format : dd-MM-yyyy}.
        /// </summary>
        [HttpGet("GetVendorAppointmentList")]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Authorize]
        public async Task<IActionResult> GetVendorAppointmentList([FromQuery] OrderFilterationListDTO model)
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

                List<BookedService>? bookedService;
                double? finalPrice = 0;
                var orderList = new List<AppointmentedListDTO>();
                DateTime fromDate = DateTime.Now;
                DateTime toDate = DateTime.Now;

                if (model.salonId > 0)
                {
                    bookedService = await _context.BookedService.Where(u => u.SalonId == model.salonId).ToListAsync();

                    if (bookedService == null)
                    {
                        _response.StatusCode = HttpStatusCode.OK;
                        _response.IsSuccess = false;
                        _response.Messages = ResponseMessages.msgNotFound + "record.";
                        return Ok(_response);
                    }
                }
                else if (!string.IsNullOrEmpty(model.vendorId))
                {
                    bookedService = await _context.BookedService.Where(u => u.VendorId == model.vendorId).ToListAsync();
                    if (bookedService == null)
                    {
                        _response.StatusCode = HttpStatusCode.OK;
                        _response.IsSuccess = false;
                        _response.Messages = ResponseMessages.msgNotFound + "record.";
                        return Ok(_response);
                    }
                }
                else
                {
                    bookedService = await _context.BookedService.ToListAsync();
                }

                if (model.fromDate != null && model.toDate != null)
                {
                    if (!CommonMethod.IsValidDateFormat_ddmmyyyy(model.fromDate) || !CommonMethod.IsValidDateFormat_ddmmyyyy(model.toDate))
                    {
                        _response.StatusCode = HttpStatusCode.OK;
                        _response.IsSuccess = false;
                        _response.Messages = "Please enter date in dd-MM-yyyy format.";
                        return Ok(_response);
                    }

                    fromDate = DateTime.ParseExact(model.fromDate, "dd-MM-yyyy", null);
                    toDate = DateTime.ParseExact(model.toDate, "dd-MM-yyyy", null);
                }

                var distinctAppointments = bookedService.DistinctBy(u => u.AppointmentId).OrderByDescending(u => u.CreateDate).ToList();

                foreach (var item in distinctAppointments)
                {
                    var appointmentDetail = await _context.Appointment
                        .Where(u => u.AppointmentId == item.AppointmentId)
                        .FirstOrDefaultAsync();

                    var mappedData = _mapper.Map<AppointmentedListDTO>(appointmentDetail);

                    var bookedServices = await _context.BookedService
                        .Where(u => u.AppointmentId == item.AppointmentId)
                        .ToListAsync();

                    string appointmentStatus = string.Empty;

                    var scheduledServices = bookedServices
                        .Where(a => a.AppointmentStatus == AppointmentStatus.Scheduled.ToString())
                        .ToList();

                    if (scheduledServices.Any())
                    {
                        bookedServices = scheduledServices;
                        appointmentStatus = AppointmentStatus.Scheduled.ToString();
                    }
                    else
                    {
                        var completedServices = bookedServices
                            .Where(a => a.AppointmentStatus == AppointmentStatus.Completed.ToString())
                            .ToList();

                        if (completedServices.Any())
                        {
                            bookedServices = completedServices;
                            appointmentStatus = AppointmentStatus.Completed.ToString();
                        }
                        else
                        {
                            bookedServices = bookedServices
                                .Where(a => a.AppointmentStatus == AppointmentStatus.Cancelled.ToString())
                                .ToList();

                            appointmentStatus = AppointmentStatus.Cancelled.ToString();
                        }
                    }

                    mappedData.appointmentStatus = appointmentStatus;
                    mappedData.appointmentDate = bookedServices.FirstOrDefault()?.AppointmentDate.ToString(@"dd-MM-yyyy");
                    mappedData.createDate = appointmentDetail.CreateDate.ToString(@"dd-MM-yyyy");
                    orderList.Add(mappedData);
                }

                if (!string.IsNullOrEmpty(model.paymentStatus))
                {
                    orderList = orderList.Where(x => (x.paymentStatus == model.paymentStatus)).ToList();
                }
                if (!string.IsNullOrEmpty(model.appointmentStatus))
                {
                    orderList = orderList.Where(x => (x.appointmentStatus?.IndexOf(model.appointmentStatus, StringComparison.OrdinalIgnoreCase) >= 0)
                    ).ToList();
                }

                if (!string.IsNullOrEmpty(model.searchQuery))
                {
                    orderList = orderList.Where(x => (x.customerFirstName?.IndexOf(model.searchQuery, StringComparison.OrdinalIgnoreCase) >= 0)
                    ).ToList();
                }
                if (model.sortDateBy == 2)
                {
                    orderList = orderList.OrderByDescending(x => CommonMethod.ddMMyyyToDateTime(x.appointmentDate)).ToList();
                    if (model.fromDate != null && model.toDate != null)
                    {
                        orderList = orderList.Where(x => (CommonMethod.ddMMyyyToDateTime(x.appointmentDate).Date >= fromDate.Date) && (CommonMethod.ddMMyyyToDateTime(x.appointmentDate).Date <= toDate.Date)).OrderByDescending(x => CommonMethod.ddMMyyyToDateTime(x.appointmentDate)).ToList();
                    }
                }
                else
                {
                    orderList = orderList.OrderByDescending(x => CommonMethod.ddMMyyyToDateTime(x.createDate)).ToList();
                    if (model.fromDate != null && model.toDate != null)
                    {
                        orderList = orderList.Where(x => (CommonMethod.ddMMyyyToDateTime(x.createDate).Date >= fromDate.Date) && (CommonMethod.ddMMyyyToDateTime(x.createDate).Date <= toDate.Date)).OrderByDescending(x => CommonMethod.ddMMyyyToDateTime(x.createDate)).ToList();
                    }
                }

                foreach (var item in orderList.Where(x => x.appointmentStatus == "Cancelled" && x.cancelledPrice == x.basePrice))
                {
                    item.finalPrice = item.totalPrice;
                }
                // Get's No of Rows Count   
                int count = orderList.Count();

                // Parameter is passed from Query string if it is null then it default Value will be pageNumber:1  
                int CurrentPage = model.pageNumber;

                // Parameter is passed from Query string if it is null then it default Value will be pageSize:20  
                int PageSize = model.pageSize;

                // Display TotalCount to Records to User  
                int TotalCount = count;

                // Calculating Totalpage by Dividing (No of Records / Pagesize)  
                int TotalPages = (int)Math.Ceiling(count / (double)PageSize);

                // Returns List of Customer after applying Paging   
                var items = orderList.Skip((CurrentPage - 1) * PageSize).Take(PageSize).ToList();

                // if CurrentPage is greater than 1 means it has previousPage  
                var previousPage = CurrentPage > 1 ? "Yes" : "No";

                // if TotalPages is greater than CurrentPage means it has nextPage  
                var nextPage = CurrentPage < TotalPages ? "Yes" : "No";

                // Returing List of Customers Collections  
                FilterationResponseModel<AppointmentedListDTO> obj = new FilterationResponseModel<AppointmentedListDTO>();
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
                    _response.Messages = ResponseMessages.msgSomethingWentWrong;
                    return Ok(_response);
                }

                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Data = orderList;
                _response.Messages = ResponseMessages.msgListFoundSuccess + "Appointment";
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

        #region GetVendorAppointmentDetail
        /// <summary>
        ///  Get Vendor Appointment Detail
        /// </summary>
        [HttpGet("GetVendorAppointmentDetail")]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Authorize(Roles = "Vendor")]
        public async Task<IActionResult> GetVendorAppointmentDetail(int appointmentId)
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

                var appointmentDetail = await _context.Appointment.FirstOrDefaultAsync(u => (u.AppointmentId == appointmentId));

                var response = _mapper.Map<VendorAppointmentDetailDTO>(appointmentDetail);
                // convert datetime into india time zone
                var ctz = TZConvert.GetTimeZoneInfo("India Standard Time");
                var convrtedZoneDate = TimeZoneInfo.ConvertTimeFromUtc(Convert.ToDateTime(appointmentDetail.CreateDate), ctz);
                response.createDate = Convert.ToDateTime(convrtedZoneDate).ToString(@"hh:mm tt");
                response.createDate = Convert.ToDateTime(convrtedZoneDate).ToString(@"dd-MM-yyyy");

                var appointmentList = await _context.BookedService.Where(u => u.AppointmentId == response.appointmentId && u.VendorId == currentUserId).ToListAsync();
                appointmentList = appointmentList.OrderByDescending(u => u.CreateDate).ToList();
                var bookedServices = _mapper.Map<List<BookedServicesDTO>>(appointmentList);
                response.basePrice = 0;
                response.finalPrice = 0;
                response.totalPrice = 0;
                response.discount = 0;
                response.totalDiscount = 0;
                response.totalServices = 0;
                foreach (var item in bookedServices)
                {
                    response.basePrice = response.basePrice + item.basePrice;
                    if (response.appointmentStatus == "Cancelled" && item.cancelledPrice == item.basePrice)
                    {
                        response.finalPrice = response.finalPrice + item.listingPrice;
                        response.cancelledPrice = 0;
                        response.totalPrice = response.totalPrice + item.totalPrice;
                        response.discount = response.discount + item.totalDiscount;
                        item.discount = item.totalDiscount;
                        item.finalPrice = item.totalPrice;
                        item.cancelledPrice = 0;
                    }
                    else
                    {
                        response.finalPrice = response.finalPrice + item.finalPrice;
                        response.totalPrice = response.totalPrice + item.totalPrice;
                        response.discount = response.discount + item.discount;
                        response.totalDiscount = response.totalDiscount + item.totalDiscount;
                    }
                    response.totalServices = response.totalServices + 1;

                    item.appointmentDate = Convert.ToDateTime(item.appointmentDate).ToString(@"dd-MM-yyyy");
                }
                response.bookedServices = bookedServices;

                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Data = response;
                _response.Messages = "Vendor appointment detail shown successfully.";
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

        #region SetAppointmentStatus
        /// <summary>
        /// Set appointment status {Pending, Completed, Scheduled, Cancelled}.
        /// </summary>
        [HttpPost]
        [Route("SetAppointmentStatus")]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Authorize(Roles = "Vendor")]
        public async Task<IActionResult> SetAppointmentStatus(SetAppointmentStatusDTO model)
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

                var appointmentDetail = await _context.Appointment.Where(u => u.AppointmentId == model.appointmentId).FirstOrDefaultAsync();
                if (appointmentDetail == null)
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Data = new Object { };
                    _response.Messages = ResponseMessages.msgNotFound + "appointment.";
                    return Ok(_response);
                }

                List<string> slotIds = new List<string>();
                if (!string.IsNullOrEmpty(model.slotIds))
                {
                    string[] splitSlotIds = model.slotIds.Split(",");
                    foreach (var item in splitSlotIds)
                    {
                        slotIds.Add(item);
                    }
                }
                if (appointmentDetail.AppointmentStatus == AppointmentStatus.Scheduled.ToString())
                {
                    if (model.appointmentStatus == AppointmentStatus.Pending.ToString())
                    {
                        _response.StatusCode = HttpStatusCode.OK;
                        _response.IsSuccess = false;
                        _response.Data = new Object { };
                        _response.Messages = "Please enter valid appointment status.";
                        return Ok(_response);
                    }
                }

                if (appointmentDetail.AppointmentStatus == AppointmentStatus.Cancelled.ToString())
                {

                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Data = new Object { };
                    _response.Messages = "Can't be change after cancelled";
                    return Ok(_response);
                }
                if (appointmentDetail.AppointmentStatus == AppointmentStatus.Completed.ToString())
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Data = new Object { };
                    _response.Messages = "Can't be change after completed";
                    return Ok(_response);
                }
                if (model.appointmentStatus == AppointmentStatus.Completed.ToString())
                {
                    if (appointmentDetail.AppointmentStatus == AppointmentStatus.Cancelled.ToString())
                    {
                        _response.StatusCode = HttpStatusCode.OK;
                        _response.IsSuccess = false;
                        _response.Data = new Object { };
                        _response.Messages = "Can't be change after completed";
                        return Ok(_response);
                    }

                    var bookedServices = await _context.BookedService.Where(u => u.AppointmentId == model.appointmentId).ToListAsync();//&& u.AppointmentStatus != AppointmentStatus.Cancelled.ToString()
                    if (bookedServices.Count > 0)
                    {
                        if (slotIds.Count > 0 || bookedServices.Count == 1 && model.setToAll == false)
                        {
                            foreach (var item in slotIds)
                            {
                                var slotIdInt = Convert.ToInt32(item);
                                BookedService? bookedService;
                                if (slotIdInt > 0)
                                {
                                    bookedService = bookedServices.FirstOrDefault(x => x.AppointmentId == model.appointmentId && x.SlotId == slotIdInt);
                                }
                                else
                                {
                                    bookedService = bookedServices.FirstOrDefault();
                                }

                                if (bookedService != null)
                                {
                                    if (bookedService.AppointmentStatus == AppointmentStatus.Completed.ToString())
                                    {
                                        _response.StatusCode = HttpStatusCode.OK;
                                        _response.IsSuccess = false;
                                        _response.Messages = "Appointment already completed.";
                                        return Ok(_response);
                                    }
                                }

                                bookedService.AppointmentStatus = AppointmentStatus.Completed.ToString();
                                _context.Update(bookedService);
                                await _context.SaveChangesAsync();
                            }
                            var bookingServiceStatus = bookedServices.Where(u => u.AppointmentId == model.appointmentId &&
                            (u.AppointmentStatus == AppointmentStatus.Scheduled.ToString() || u.AppointmentStatus == AppointmentStatus.Cancelled.ToString()));
                            if (bookingServiceStatus.Count() < 1)
                            {
                                appointmentDetail.AppointmentStatus = AppointmentStatus.Completed.ToString();
                                _context.Update(appointmentDetail);
                                await _context.SaveChangesAsync();
                            }
                        }
                        else
                        {
                            foreach (var booked in bookedServices)
                            {
                                var timeSlot = await _context.TimeSlot.Where(u => u.SlotId == booked.SlotId).FirstOrDefaultAsync();
                                if (timeSlot.SlotCount == 0 && timeSlot.Status == false)
                                {
                                    timeSlot.Status = true;
                                }
                                timeSlot.SlotCount = (int)(timeSlot.SlotCount + booked.ServiceCountInCart);
                                _context.Update(timeSlot);
                                await _context.SaveChangesAsync();

                                booked.AppointmentStatus = AppointmentStatus.Completed.ToString();
                                _context.Update(booked);
                                await _context.SaveChangesAsync();
                            }

                            appointmentDetail.AppointmentStatus = AppointmentStatus.Completed.ToString();
                            _context.Update(appointmentDetail);
                            await _context.SaveChangesAsync();
                        }
                        _response.StatusCode = HttpStatusCode.OK;
                        _response.IsSuccess = true;
                        _response.Messages = "Appointment status set to completed successfully.";
                        return Ok(_response);
                    }
                }
                if (model.appointmentStatus == AppointmentStatus.Cancelled.ToString())
                {
                    if (appointmentDetail.AppointmentStatus == AppointmentStatus.Completed.ToString())
                    {
                        _response.StatusCode = HttpStatusCode.OK;
                        _response.IsSuccess = false;
                        _response.Data = new Object { };
                        _response.Messages = "Can't be change after cancelled.";
                        return Ok(_response);
                    }
                    var bookedServices = await _context.BookedService.Where(u => u.AppointmentId == model.appointmentId).ToListAsync();//&& u.AppointmentStatus != AppointmentStatus.Cancelled.ToString()
                    if (bookedServices.Count > 0)
                    {
                        if (slotIds.Count > 0 || bookedServices.Count == 1 && model.setToAll == false)
                        {
                            foreach (var item in slotIds)
                            {
                                var slotIdInt = Convert.ToInt32(item);
                                BookedService? bookedService;
                                if (slotIdInt > 0)
                                {
                                    bookedService = bookedServices.FirstOrDefault(x => x.AppointmentId == model.appointmentId && x.SlotId == slotIdInt);
                                }
                                else
                                {
                                    bookedService = bookedServices.FirstOrDefault();
                                }

                                if (bookedService != null)
                                {
                                    if (bookedService.AppointmentStatus == AppointmentStatus.Cancelled.ToString())
                                    {
                                        _response.StatusCode = HttpStatusCode.OK;
                                        _response.IsSuccess = false;
                                        _response.Messages = "Appointment already cancelled.";
                                        return Ok(_response);
                                    }

                                }
                                appointmentDetail.FinalPrice = appointmentDetail.FinalPrice - bookedService.ListingPrice;
                                appointmentDetail.Discount = appointmentDetail.Discount - bookedService.Discount;

                                bookedService.AppointmentStatus = AppointmentStatus.Cancelled.ToString();
                                bookedService.FinalPrice = bookedService.FinalPrice - bookedService.ListingPrice;
                                bookedService.CancelledPrice = bookedService.ListingPrice + bookedService.Discount;
                                bookedService.Discount = bookedService.Discount - bookedService.Discount;

                                appointmentDetail.CancelledPrice = appointmentDetail.CancelledPrice + bookedService.CancelledPrice;
                                _context.Update(bookedService);
                                await _context.SaveChangesAsync();

                                var bookingServiceStatus = bookedServices.Where(u => u.AppointmentId == model.appointmentId && (u.AppointmentStatus == AppointmentStatus.Scheduled.ToString() || u.AppointmentStatus == AppointmentStatus.Completed.ToString()));
                                if (bookingServiceStatus.Count() < 1)
                                {
                                    appointmentDetail.AppointmentStatus = AppointmentStatus.Cancelled.ToString();
                                    _context.Update(appointmentDetail);
                                    await _context.SaveChangesAsync();
                                }
                                else
                                {
                                    _context.Update(appointmentDetail);
                                    await _context.SaveChangesAsync();
                                }
                            }
                        }
                        else
                        {
                            double? finalPrice = 0;
                            double? discount = 0;
                            double? cancelledPrice = 0;
                            foreach (var booked in bookedServices)
                            {
                                var timeSlot = await _context.TimeSlot.Where(u => u.SlotId == booked.SlotId).FirstOrDefaultAsync();
                                if (timeSlot.SlotCount == 0 && timeSlot.Status == false)
                                {
                                    timeSlot.Status = true;
                                }
                                timeSlot.SlotCount = (int)(timeSlot.SlotCount + booked.ServiceCountInCart);
                                _context.Update(timeSlot);
                                await _context.SaveChangesAsync();

                                finalPrice = finalPrice + booked.ListingPrice;
                                discount = discount + booked.Discount;
                                cancelledPrice = booked.ListingPrice + booked.Discount;
                                booked.AppointmentStatus = AppointmentStatus.Cancelled.ToString();
                                booked.FinalPrice = booked.FinalPrice - booked.ListingPrice;
                                booked.CancelledPrice = booked.ListingPrice + booked.Discount;
                                booked.Discount = booked.Discount - booked.Discount;

                                _context.Update(booked);
                                await _context.SaveChangesAsync();
                            }

                            appointmentDetail.AppointmentStatus = AppointmentStatus.Cancelled.ToString();
                            appointmentDetail.FinalPrice = appointmentDetail.FinalPrice - finalPrice;
                            appointmentDetail.Discount = appointmentDetail.Discount - discount;
                            appointmentDetail.CancelledPrice = appointmentDetail.CancelledPrice + cancelledPrice;

                            _context.Update(appointmentDetail);
                            await _context.SaveChangesAsync();
                        }

                        _response.StatusCode = HttpStatusCode.OK;
                        _response.IsSuccess = true;
                        _response.Messages = "Appointment cancelled successfully.";
                        return Ok(_response);
                    }
                }
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Messages = "appointment status" + ResponseMessages.msgUpdationSuccess;
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

        #region SetPaymentStatus
        /// <summary>
        /// Set payment status {Paid, Unpaid, Refunded}.
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

                if (model.paymentStatus != PaymentStatus.Paid.ToString()
                && model.paymentStatus != PaymentStatus.Unpaid.ToString()
                && model.paymentStatus != PaymentStatus.Refunded.ToString())
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = "Please select a valid status.";
                    return Ok(_response);
                }

                var appointmentDetails = await _context.Appointment.Where(u => u.AppointmentId == model.appointmentId).FirstOrDefaultAsync();
                if (appointmentDetails == null)
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Data = new Object { };
                    _response.Messages = ResponseMessages.msgNotFound + "appointment.";
                    return Ok(_response);
                }

                if (appointmentDetails.PaymentStatus == model.paymentStatus)
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Data = new Object { };
                    _response.Messages = "Appointment status already set to " + appointmentDetails.PaymentStatus.ToString() + ".";
                    return Ok(_response);
                }

                if (appointmentDetails.PaymentStatus == PaymentStatus.Paid.ToString())
                {
                    if (model.paymentStatus == PaymentStatus.Unpaid.ToString())
                    {
                        _response.StatusCode = HttpStatusCode.OK;
                        _response.IsSuccess = false;
                        _response.Data = new Object { };
                        _response.Messages = "Please enter valid payment status.";
                        return Ok(_response);
                    }
                }

                if (appointmentDetails.PaymentStatus == PaymentStatus.Unpaid.ToString())
                {
                    if (model.paymentStatus == PaymentStatus.Refunded.ToString())
                    {
                        _response.StatusCode = HttpStatusCode.OK;
                        _response.IsSuccess = false;
                        _response.Data = new Object { };
                        _response.Messages = "Status can't be change from unpaid to refunded.";
                        return Ok(_response);
                    }
                }

                if (appointmentDetails.PaymentStatus == PaymentStatus.Refunded.ToString())
                {
                    if (model.paymentStatus == PaymentStatus.Paid.ToString()
                    || model.paymentStatus == PaymentStatus.Unpaid.ToString())
                    {
                        _response.StatusCode = HttpStatusCode.OK;
                        _response.IsSuccess = false;
                        _response.Data = new Object { };
                        _response.Messages = "Please enter valid payment status.";
                        return Ok(_response);
                    }
                }

                appointmentDetails.PaymentStatus = model.paymentStatus;
                _context.Appointment.Update(appointmentDetails);
                await _context.SaveChangesAsync();
                // send Notification

                string notificationMessage = "";

                if (appointmentDetails.PaymentStatus == PaymentStatus.Unpaid.ToString())
                {
                    notificationMessage = "Payment remain unpaid.";
                }
                else if (appointmentDetails.PaymentStatus == PaymentStatus.Paid.ToString())
                {
                    notificationMessage = "Paid successfully.";
                }
                else
                {
                    notificationMessage = "Your payment has been refunded.";
                }

                var user = await _context.UserDetail.Where(a => (a.UserId == appointmentDetails.CustomerUserId) && (a.IsDeleted != true)).FirstOrDefaultAsync();
                var userprofileDetail = _userManager.FindByIdAsync(user.UserId).GetAwaiter().GetResult();
                var token = user.Fcmtoken;
                var title = "Payment Status";
                var description = String.Format("Hi {0},\n{1}", userprofileDetail.FirstName, notificationMessage);
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
                    notificationSent.NotificationType = NotificationType.Appointment.ToString();
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

        #region MarkAppointmentAsRead
        /// <summary>
        /// </summary>
        [HttpPost]
        [Route("MarkAppointmentAsRead")]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Authorize(Roles = "Vendor")]
        public async Task<IActionResult> MarkAppointmentAsRead(ReadStatusDTO model)
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

                var appointmentDetail = await _context.Appointment.Where(u => u.AppointmentId == model.appointmentId).FirstOrDefaultAsync();
                if (appointmentDetail == null)
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Data = new Object { };
                    _response.Messages = "Not found any appointment.";
                    return Ok(_response);
                }

                appointmentDetail.IsUpdated = false;
                _context.Appointment.Update(appointmentDetail);
                await _context.SaveChangesAsync();

                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                // _response.Data = response;
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

        #region UpComingSchedule
        /// <summary>
        /// Get UpComing Schedule .
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Authorize(Roles = "Vendor")]
        [Route("UpComingSchedule")]
        public async Task<IActionResult> UpComingSchedule(int salonId)
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
                var salon = await _context.SalonDetail.FirstOrDefaultAsync(a => a.SalonId == salonId);
                if (salon == null)
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = ResponseMessages.msgNotFound + "Salon.";
                    return Ok(_response);
                }

                List<upcomingScheduleDTO> bookServices = await _context.BookedService.Where(a => a.SalonId == salon.SalonId && a.AppointmentStatus == "Scheduled")
                                                           .GroupBy(a => new { a.AppointmentDate })
                                                           .Select(y => new upcomingScheduleDTO
                                                           {
                                                               date = y.Key.AppointmentDate.Date.ToString(@"dd-MM-yyyy"),
                                                               // serviceName = y.Key.ServiceName,
                                                               slotCount = y.Count(),
                                                               day = y.Key.AppointmentDate.DayOfWeek.ToString(),
                                                           }).ToListAsync();

                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = bookServices.Count == 0 ? false : true;
                _response.Messages = bookServices.Count == 0 ? ResponseMessages.msgNotFound + "record." : "Upcoming slot found successfully";
                _response.Data = bookServices;
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

        #region UpComingScheduleDetail
        /// <summary>
        /// Get UpComing Schedule .
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Authorize(Roles = "Vendor")]
        [Route("UpComingScheduleDetail")]
        public async Task<IActionResult> UpComingScheduleDetail(string queryDate)
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

                string format = "dd-MM-yyyy";
                DateTime searchDate = new DateTime();

                try
                {
                    // Parse the string into a DateTime object using the specified format
                    searchDate = DateTime.ParseExact(queryDate, format, null);
                }
                catch (FormatException)
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = "Invalid date format.";
                    return Ok(_response);
                }

                var bookServices = await _context.BookedService
                .Where(a => a.AppointmentStatus == "Scheduled" && a.AppointmentDate.Date == searchDate.Date).ToListAsync();
                List<UpcomingScheduleDetailDTO> upcomingScheduleDetailDTO = new List<UpcomingScheduleDetailDTO>();
                foreach (var item in bookServices)
                {
                    UpcomingScheduleDetailDTO upcomingScheduleDetail = new UpcomingScheduleDetailDTO();
                    upcomingScheduleDetail.serviceId = item.ServiceId;
                    upcomingScheduleDetail.serviceName = item.ServiceName;
                    upcomingScheduleDetail.serviceCountInCart = (int)item.ServiceCountInCart;
                    // var slotDetail = await _context.TimeSlot.Where(u => u.SlotId == item.SlotId).FirstOrDefaultAsync();
                    upcomingScheduleDetail.fromTime = item.FromTime;
                    upcomingScheduleDetail.toTime = item.ToTime;
                    upcomingScheduleDetail.listingPrice = (double)item.ListingPrice;
                    upcomingScheduleDetail.serviceImage = item.ServiceImage;
                    upcomingScheduleDetail.slotId = (int)item.SlotId;
                    upcomingScheduleDetail.appointmentDate = item.AppointmentDate.ToString(@"dd-MM-yyyy");

                    upcomingScheduleDetailDTO.Add(upcomingScheduleDetail);
                }

                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = upcomingScheduleDetailDTO.Count == 0 ? false : true;
                _response.Messages = upcomingScheduleDetailDTO.Count == 0 ?
                ResponseMessages.msgNotFound + "record." : "Upcoming schedule details found successfully";
                _response.Data = upcomingScheduleDetailDTO;
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

