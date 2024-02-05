using BeautyHubAPI.Models.Dtos;
using BeautyHubAPI.Models.Helper;
using BeautyHubAPI.Repository.IRepository;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using static BeautyHubAPI.Common.GlobalVariables;
using BeautyHubAPI.Helpers;
using Microsoft.AspNetCore.Authorization;
using BeautyHubAPI.Models;
using BeautyHubAPI.Repository;
using BeautyHubAPI.Data;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;
using AutoMapper;
using Amazon.S3.Model;
using Amazon.S3;
using BeautyHubAPI.Common;

namespace BeautyHubAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UploadController : ControllerBase
    {
        protected APIResponse _response;
        private readonly IEmailManager _emailSender;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUploadRepository _uploadRepository;
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        public UploadController(
            UserManager<ApplicationUser> userManager,
            IEmailManager emailSender,
            IMapper mapper,
            IUploadRepository uploadRepository,
            ApplicationDbContext context

        )
        {
            _response = new();
            _emailSender = emailSender;
            _userManager = userManager;
            _uploadRepository = uploadRepository;
            _context = context;
            _mapper = mapper;

        }

        #region UploadProfilePic
        /// <summary>
        ///  Upload profile picture.
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Authorize]
        [Route("UploadProfilePic")]
        public async Task<IActionResult> Login([FromForm] UploadProfilePicDto model)
        {
            var currentUserId = HttpContext.User.Claims.First().Value;
            var currentUser = _userManager.FindByIdAsync(currentUserId).GetAwaiter().GetResult();
            if (currentUser == null)
            {
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = false;
                _response.Messages = ResponseMessages.msgUserNotFound;
                return Ok(_response);
            }

            if (!string.IsNullOrEmpty(model.id))
            {
                currentUserId = model.id;
            }

            var userDetail = await _context.UserDetail.FirstOrDefaultAsync(u => u.UserId == currentUserId);

            // Delete previous file
            if (!string.IsNullOrEmpty(userDetail.ProfilePic))
            {
                var chk = await _uploadRepository.DeleteFilesFromServer("FileToSave/" + userDetail.ProfilePic);
            }
            var documentFile = ContentDispositionHeaderValue.Parse(model.profilePic.ContentDisposition).FileName.Trim('"');
            documentFile = CommonMethod.EnsureCorrectFilename(documentFile);
            documentFile = CommonMethod.RenameFileName(documentFile);

            var documentPath = profilePicContainer + documentFile;
            userDetail.ProfilePic = documentPath;
            _context.UserDetail.Update(userDetail);
            await _context.SaveChangesAsync();
            bool uploadStatus = await _uploadRepository.UploadFilesToServer(
                    model.profilePic,
                    profilePicContainer,
                    documentFile
                );


            _response.StatusCode = HttpStatusCode.OK;
            _response.IsSuccess = true;
            _response.Messages = ResponseMessages.msgUpdationSuccess;
            _response.Data = documentPath;
            return Ok(_response);
        }
        #endregion

        #region UploadPaymentReceipt
        /// <summary>
        ///  Upload payment receipt.
        /// </summary>
        [HttpPost("UploadPaymentReceipt")]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Authorize(Roles = "Customer,Vendor,Admin,SuperAdmin")]
        public async Task<IActionResult> UploadPaymentReceipt([FromForm] UploadPaymentReceipt model)
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

                var addPaymentReceipt = new PaymentReceipt();
                var documentFile = ContentDispositionHeaderValue.Parse(model.paymentReceipt.ContentDisposition).FileName.Trim('"');
                documentFile = CommonMethod.EnsureCorrectFilename(documentFile);
                documentFile = CommonMethod.RenameFileName(documentFile);

                var documentPath = paymentReceipt + documentFile;
                //bool uploadStatus = await _uploadRepository.UploadFilesToServer(
                //        model.paymentReceipt,
                //        paymentReceipt,
                //        documentFile
                //    );
                addPaymentReceipt.PaymentReceiptImage = documentPath;
                addPaymentReceipt.UserId = currentUserId;

                await _context.PaymentReceipt.AddAsync(addPaymentReceipt);
                await _context.SaveChangesAsync();

                var response = _mapper.Map<PaymentReceiptDTO>(addPaymentReceipt);

                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Data = response;
                _response.Messages = "Payment receipt uploaded successfully.";
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

        #region UploadSalonImage
        /// <summary>
        ///  Upload Slon image.
        /// </summary>
        [HttpPost("UploadaSlonImage")]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Authorize]
        public async Task<IActionResult> UploadSlonImage([FromForm] UploadSalonImageDTO model)
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

                if (model.salonImage == null)
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = "Please select a image.";
                    return Ok(_response);
                }

                var SalonDetail = await _context.SalonDetail.Where(u => (u.SalonId == model.salonId)).FirstOrDefaultAsync();

                if (SalonDetail == null)
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = ResponseMessages.msgNotFound + "Record";
                    return Ok(_response);
                }

                // Delete previous file
                if (!string.IsNullOrEmpty(SalonDetail.SalonImage))
                {
                    var chk = await _uploadRepository.DeleteFilesFromServer("FileToSave/" + SalonDetail.SalonImage);
                }
                var documentFile = ContentDispositionHeaderValue.Parse(model.salonImage.ContentDisposition).FileName.Trim('"');
                documentFile = CommonMethod.EnsureCorrectFilename(documentFile);
                documentFile = CommonMethod.RenameFileName(documentFile);

                var documentPath = SalonImageContainer + documentFile;
                bool uploadStatus = await _uploadRepository.UploadFilesToServer(
                        model.salonImage,
                        SalonImageContainer,
                        documentFile
                    );
                SalonDetail.SalonImage = documentPath;

                _context.Update(SalonDetail);
                await _context.SaveChangesAsync();
                var SalonResponse = _mapper.Map<SalonResponseDTO>(SalonDetail);

                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Data = SalonResponse;
                _response.Messages = "Salon image" + ResponseMessages.msgUpdationSuccess;
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

        #region UploadQRCode
        /// <summary>
        ///  Upload QR code.
        /// </summary>
        [HttpPost("UploadQRCode")]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Authorize]
        public async Task<IActionResult> UploadQRCode([FromForm] UploadQRImage model)
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

                string[] Ids = model.upidetailIds.Split(",");
                if (Ids.Count() != model.qrcode.Count)
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = "The QR code must match the UPI ID.";
                    return Ok(_response);
                }
                for (int i = 0; i < Ids.Length; i++)
                {
                    int id = Convert.ToInt32(Ids[i]);
                    if (id > 0)
                    {
                        var upiDetail = await _context.Upidetail.Where(u => (u.UpidetailId == id)).FirstOrDefaultAsync();
                        if (upiDetail == null)
                        {
                            _response.StatusCode = HttpStatusCode.OK;
                            _response.IsSuccess = false;
                            _response.Messages = ResponseMessages.msgNotFound + "record";
                            return Ok(_response);
                        }

                        // Delete previous file
                        if (!string.IsNullOrEmpty(upiDetail.Qrcode))
                        {
                            var chk = await _uploadRepository.DeleteFilesFromServer("FileToSave/" + upiDetail.Qrcode);
                        }
                        if (model.qrcode[i] != null)
                        {
                            var documentFile = ContentDispositionHeaderValue.Parse(model.qrcode[i].ContentDisposition).FileName.Trim('"');
                            documentFile = CommonMethod.EnsureCorrectFilename(documentFile);
                            documentFile = CommonMethod.RenameFileName(documentFile);

                            var documentPath = qrImageContainer + documentFile;
                            bool uploadStatus = await _uploadRepository.UploadFilesToServer(
                                    model.qrcode[i],
                                    qrImageContainer,
                                    documentFile
                                );
                            upiDetail.Qrcode = documentPath;
                            _context.Update(upiDetail);
                            await _context.SaveChangesAsync();
                        }
                    }
                }

                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                // _response.Data = SalonResponse;
                _response.Messages = "QR image" + ResponseMessages.msgUpdationSuccess;
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

        #region UploadCategoryImage
        /// <summary>
        ///  Updatecategory image.
        /// </summary>
        [HttpPost("UploadCategoryImage")]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Authorize]
        public async Task<IActionResult> UploadCategoryImage([FromForm] UploadCategoryImageDTO model)
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
                if (model.CategoryImageMale == null && model.CategoryImageFemale == null)
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = "Please upload image.";
                    return Ok(_response);
                }
                var categoryDetails = new VendorCategoryDTO();

                if (model.subCategoryId > 0)
                {
                    var categoryDetail = await _context.SubCategory.FirstOrDefaultAsync(u => (u.SubCategoryId == model.subCategoryId));
                    if (categoryDetail == null)
                    {
                        _response.StatusCode = HttpStatusCode.OK;
                        _response.IsSuccess = false;
                        _response.Messages = ResponseMessages.msgNotFound + "record";
                        return Ok(_response);
                    }

                    if (model.CategoryImageMale != null)
                    {
                        // Delete previous file
                        if (!string.IsNullOrEmpty(categoryDetail.CategoryImageMale))
                        {
                            var chk = await _uploadRepository.DeleteFilesFromServer("FileToSave/" + categoryDetail.CategoryImageMale);
                        }
                        var documentFile = ContentDispositionHeaderValue.Parse(model.CategoryImageMale.ContentDisposition).FileName.Trim('"');
                        documentFile = CommonMethod.EnsureCorrectFilename(documentFile);
                        documentFile = CommonMethod.RenameFileName(documentFile);

                        var documentPath = categoryImageContainer + documentFile;

                        bool uploadStatus = await _uploadRepository.UploadFilesToServer(
                                model.CategoryImageMale,
                                categoryImageContainer,
                                documentFile
                            );

                        categoryDetail.ModifiedBy = currentUserId;
                        categoryDetail.CategoryImageMale = documentPath;
                    }
                    if (model.CategoryImageFemale != null)
                    {
                        if (!string.IsNullOrEmpty(categoryDetail.CategoryImageFemale))
                        {
                            var chk = await _uploadRepository.DeleteFilesFromServer("FileToSave/" + categoryDetail.CategoryImageFemale);
                        }
                        var documentFile = ContentDispositionHeaderValue.Parse(model.CategoryImageFemale.ContentDisposition).FileName.Trim('"');
                        documentFile = CommonMethod.EnsureCorrectFilename(documentFile);
                        documentFile = CommonMethod.RenameFileName(documentFile);

                        var documentPath = categoryImageContainer + documentFile;

                        bool uploadStatus = await _uploadRepository.UploadFilesToServer(
                                model.CategoryImageFemale,
                                categoryImageContainer,
                                documentFile
                            );

                        categoryDetail.ModifiedBy = currentUserId;
                        categoryDetail.CategoryImageFemale = documentPath;
                    }
                    _context.Update(categoryDetail);
                    _context.SaveChanges();
                    categoryDetails = _mapper.Map<VendorCategoryDTO>(categoryDetail);

                }
                else
                {
                    var categoryDetail = await _context.MainCategory.FirstOrDefaultAsync(u => (u.MainCategoryId == model.mainCategoryId));
                    if (categoryDetail == null)
                    {
                        _response.StatusCode = HttpStatusCode.OK;
                        _response.IsSuccess = false;
                        _response.Messages = ResponseMessages.msgNotFound + "record";
                        return Ok(_response);
                    }   

                    if (model.CategoryImageMale != null)
                    {
                        // Delete previous file
                        if (!string.IsNullOrEmpty(categoryDetail.CategoryImageMale))
                        {
                            var chk = await _uploadRepository.DeleteFilesFromServer("FileToSave/" + categoryDetail.CategoryImageMale);
                        }
                        var documentFile = ContentDispositionHeaderValue.Parse(model.CategoryImageMale.ContentDisposition).FileName.Trim('"');
                        documentFile = CommonMethod.EnsureCorrectFilename(documentFile);
                        documentFile = CommonMethod.RenameFileName(documentFile);

                        var documentPath = categoryImageContainer + documentFile;

                        bool uploadStatus = await _uploadRepository.UploadFilesToServer(
                                model.CategoryImageMale,
                                categoryImageContainer,
                                documentFile
                            );

                        categoryDetail.ModifiedBy = currentUserId;
                        categoryDetail.CategoryImageMale = documentPath;
                    }
                    if (model.CategoryImageFemale != null)
                    {
                        if (!string.IsNullOrEmpty(categoryDetail.CategoryImageFemale))
                        {
                            var chk = await _uploadRepository.DeleteFilesFromServer("FileToSave/" + categoryDetail.CategoryImageFemale);
                        }
                        var documentFile = ContentDispositionHeaderValue.Parse(model.CategoryImageFemale.ContentDisposition).FileName.Trim('"');
                        documentFile = CommonMethod.EnsureCorrectFilename(documentFile);
                        documentFile = CommonMethod.RenameFileName(documentFile);

                        var documentPath = categoryImageContainer + documentFile;

                        bool uploadStatus = await _uploadRepository.UploadFilesToServer(
                                model.CategoryImageFemale,
                                categoryImageContainer,
                                documentFile
                            );

                        categoryDetail.ModifiedBy = currentUserId;
                        categoryDetail.CategoryImageFemale = documentPath;
                    }
                    _context.Update(categoryDetail);
                    _context.SaveChanges();
                    categoryDetails = _mapper.Map<VendorCategoryDTO>(categoryDetail);
                }

                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Data = categoryDetails;
                _response.Messages = "Category image uploaded successfully.";
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

        #region UploadServiceImage
        /// <summary>
        ///  Upload Service Image
        /// </summary>
        [HttpPost("UploadServiceImage")]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Authorize]
        public async Task<IActionResult> UploadServiceImage([FromForm] UploadServiceImageDTO model)
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

                if (model.salonServiceImage == null)
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = "Please select at least one image.";
                    return Ok(_response);
                }

                if (model.salonServiceImage.Count > 5)
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = "Can't upload more than five images.";
                    return Ok(_response);
                }

                var serviceDetail = await _context.SalonService.Where(u => (u.ServiceId == model.serviceId)).FirstOrDefaultAsync();
                if (serviceDetail == null)
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = ResponseMessages.msgNotFound + "record";
                    return Ok(_response);
                }

                // Delete previous file
                if (!string.IsNullOrEmpty(serviceDetail.ServiceImage1))
                {
                    var chk = await _uploadRepository.DeleteFilesFromServer("FileToSave/" + serviceDetail.ServiceImage1);
                }
                if (!string.IsNullOrEmpty(serviceDetail.ServiceImage2))
                {
                    var chk = await _uploadRepository.DeleteFilesFromServer("FileToSave/" + serviceDetail.ServiceImage2);
                }
                if (!string.IsNullOrEmpty(serviceDetail.ServiceImage3))
                {
                    var chk = await _uploadRepository.DeleteFilesFromServer("FileToSave/" + serviceDetail.ServiceImage3);
                }
                if (!string.IsNullOrEmpty(serviceDetail.ServiceImage4))
                {
                    var chk = await _uploadRepository.DeleteFilesFromServer("FileToSave/" + serviceDetail.ServiceImage4);
                }
                if (!string.IsNullOrEmpty(serviceDetail.ServiceImage5))
                {
                    var chk = await _uploadRepository.DeleteFilesFromServer("FileToSave/" + serviceDetail.ServiceImage5);
                }

                // Delete document path
                serviceDetail.ServiceImage1 = string.Empty;
                serviceDetail.ServiceImage2 = string.Empty;
                serviceDetail.ServiceImage3 = string.Empty;
                serviceDetail.ServiceImage4 = string.Empty;
                serviceDetail.ServiceImage5 = string.Empty;

                _context.Update(serviceDetail);
                await _context.SaveChangesAsync();

                int imageNo = 1;

                foreach (var item in model.salonServiceImage)
                {
                    var documentFile = ContentDispositionHeaderValue.Parse(item.ContentDisposition).FileName.Trim('"');
                    documentFile = CommonMethod.EnsureCorrectFilename(documentFile);
                    documentFile = CommonMethod.RenameFileName(documentFile);

                    var documentPath = serviceImageContainer + documentFile;
                    bool uploadStatus = await _uploadRepository.UploadFilesToServer(
                            item,
                            serviceImageContainer,
                            documentFile
                        );
                    if (imageNo == 1)
                    {
                        serviceDetail.ServiceImage1 = documentPath;
                        imageNo++;
                    }
                    else if (imageNo == 2)
                    {
                        serviceDetail.ServiceImage2 = documentPath;
                        imageNo++;
                    }
                    else if (imageNo == 3)
                    {
                        serviceDetail.ServiceImage3 = documentPath;
                        imageNo++;
                    }
                    else if (imageNo == 4)
                    {
                        serviceDetail.ServiceImage4 = documentPath;
                        imageNo++;
                    }
                    else
                    {
                        serviceDetail.ServiceImage5 = documentPath;
                        imageNo++;
                    }
                }

                _context.Update(serviceDetail);
                await _context.SaveChangesAsync();

                var getService = await _context.SalonService.FirstOrDefaultAsync(u => u.ServiceId == serviceDetail.ServiceId);
                var response = _mapper.Map<serviceDetailDTO>(serviceDetail);

                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Data = response;
                _response.Messages = "Service image uploaded successfully.";
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

        #region UploadServiceIconImage
        /// <summary>
        ///  Upload service icon image
        /// </summary>
        [HttpPost("UploadServiceIconImage")]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Authorize]
        public async Task<IActionResult> UploadServiceIconImage([FromForm] UploadServiceIconImageDTO model)
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

                if (model.salonServiceIconImage == null)
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = "Please select image.";
                    return Ok(_response);
                }

                var serviceDetail = await _context.SalonService.Where(u => (u.ServiceId == model.serviceId)).FirstOrDefaultAsync();
                if (serviceDetail == null)
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = ResponseMessages.msgNotFound + "record";
                    return Ok(_response);
                }

                if (model.salonServiceIconImage.Length < 10)
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = false;
                    _response.Messages = "Image quality is very poor.";
                    return Ok(_response);
                }

                // Delete previous file
                if (!string.IsNullOrEmpty(serviceDetail.ServiceIconImage))
                {
                    var chk = await _uploadRepository.DeleteFilesFromServer("FileToSave/" + serviceDetail.ServiceImage1);
                }

                // Delete document path
                serviceDetail.ServiceIconImage = string.Empty;

                _context.Update(serviceDetail);
                await _context.SaveChangesAsync();

                var documentFile = ContentDispositionHeaderValue.Parse(model.salonServiceIconImage.ContentDisposition).FileName.Trim('"');
                documentFile = CommonMethod.EnsureCorrectFilename(documentFile);
                documentFile = CommonMethod.RenameFileName(documentFile);

                var documentPath = serviceImageContainer + documentFile;
                bool uploadStatus = await _uploadRepository.UploadFilesToServer(
                        model.salonServiceIconImage,
                        serviceImageContainer,
                        documentFile
                    );

                serviceDetail.ServiceIconImage = documentPath;

                _context.Update(serviceDetail);
                await _context.SaveChangesAsync();

                var getService = await _context.SalonService.FirstOrDefaultAsync(u => u.ServiceId == serviceDetail.ServiceId);
                var response = _mapper.Map<serviceDetailDTO>(serviceDetail);

                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Data = response;
                _response.Messages = "Service image uploaded successfully.";
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

    }
}
