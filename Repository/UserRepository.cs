using AutoMapper;
using BeautyHubAPI.Data;
using BeautyHubAPI.Models.Dtos;
using BeautyHubAPI.Models;
using BeautyHubAPI.Repository.IRepository;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BeautyHubAPI.Models;
using BeautyHubAPI.Models.Helper;
using Microsoft.EntityFrameworkCore;
using System.Web.Helpers;
using BeautyHubAPI.Common;
using BeautyHubAPI.IRepository;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace BeautyHubAPI.Repository
{
    public class UserRepository : IUserRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IContentRepository _contentRepository;
        private readonly RoleManager<IdentityRole> _roleManager;
        private string secretKey;
        private readonly IMapper _mapper;
        private ITwilioManager _twilioManager;
        protected APIResponse _response;

        public UserRepository(ApplicationDbContext context, ITwilioManager twilioManager, IConfiguration configuration,
            UserManager<ApplicationUser> userManager, IMapper mapper, IContentRepository contentRepository, RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _mapper = mapper;
            _userManager = userManager;
            secretKey = configuration.GetValue<string>("ApiSettings:Secret");
            _roleManager = roleManager;
            _contentRepository = contentRepository;
            _twilioManager = twilioManager;
            _response = new();
        }

        public bool IsUniqueUser(string email, string phoneNumber)
        {
            var user = _context.ApplicationUsers.FirstOrDefault(x => (x.Email.ToLower() == email.ToLower()) || (x.PhoneNumber == phoneNumber));
            if (user == null)
            {
                return true;
            }
            return false;
        }
        public async Task<ApplicationUser> IsValidUser(string EmailOrPhone)
        {
            var user = _context.ApplicationUsers.FirstOrDefault(x => (x.Email.ToLower() == EmailOrPhone.ToLower()) || (x.PhoneNumber == EmailOrPhone));
            if (user == null)
            {
                return user;
            }
            return new ApplicationUser();
        }
        public bool IsUniqueEmail(string email)
        {
            var user = _context.ApplicationUsers.FirstOrDefault(x => (x.Email.ToLower() == email.ToLower()));
            if (user == null)
            {
                return true;
            }
            return false;
        }
        public bool IsUniquePhone(string phoneNumber)
        {
            var user = _context.ApplicationUsers.FirstOrDefault(x => (x.PhoneNumber == phoneNumber));
            if (user == null)
            {
                return true;
            }
            return false;
        }
        public async Task<LoginResponseDTO> Login(LoginRequestDTO loginRequestDTO)
        {
            var user = _context.ApplicationUsers
                .FirstOrDefault(u => (u.Email.ToLower() == loginRequestDTO.emailOrPhone.ToLower()) || u.PhoneNumber.ToLower() == loginRequestDTO.emailOrPhone.ToLower());

            bool isValid = await _userManager.CheckPasswordAsync(user, loginRequestDTO.password);


            if (user == null || isValid == false)
            {
                return new LoginResponseDTO();
            }

            user.SecurityStamp = CommonMethod.RandomString(20);

            _context.Update(user);
            await _context.SaveChangesAsync();

            // var userDetail = await _context.UserDetail.Where(u => u.UserId == user.Id).FirstOrDefaultAsync();

            //if user was found generate JWT Token
            var roles = await _userManager.GetRolesAsync(user);
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(secretKey);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimTypes.Name, user.Id.ToString()),
                    new Claim(ClaimTypes.Role, roles.FirstOrDefault()),
                    new Claim("SecurityStamp", user.SecurityStamp),
                    // new Claim(ClaimTypes.Anonymous,user.SecurityStamp)
                    // new Claim(ClaimTypes.Anonymous,user.SecurityStamp)
                }),
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = new(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            LoginResponseDTO loginResponseDTO = new LoginResponseDTO()
            {
                token = tokenHandler.WriteToken(token),
            };
            _mapper.Map(user, loginResponseDTO);

            var userdetail = await _context.UserDetail.Where(u => (u.UserId == user.Id) && (u.IsDeleted == false)).FirstOrDefaultAsync();
            if (userdetail == null)
            {
                return new LoginResponseDTO();
            }
            loginResponseDTO.role = roles[0];
            if (roles[0] == "Vendor")
            {
                loginResponseDTO.vendorId = userdetail.UserId;
                loginResponseDTO.salonId = null;
                var shop = await _context.SalonDetail.Where(u => u.VendorId == userdetail.UserId).FirstOrDefaultAsync();
                if (shop != null)
                {
                    loginResponseDTO.salonName = shop.SalonName;
                    loginResponseDTO.salonImage = shop.SalonImage;
                    loginResponseDTO.salonId = shop.SalonId;
                }
            }

            loginResponseDTO.gender = userdetail.Gender;
            loginResponseDTO.dialCode = userdetail.DialCode;
            loginResponseDTO.profilepic = userdetail.ProfilePic;

            return loginResponseDTO;
        }

        public async Task<LoginResponseDTO> Register(RegisterationRequestDTO registerationRequestDTO)
        {
            ApplicationUser user = new()
            {
                Email = registerationRequestDTO.email,
                UserName = registerationRequestDTO.email,
                NormalizedEmail = registerationRequestDTO.email.ToUpper(),
                FirstName = registerationRequestDTO.firstName,
                LastName = registerationRequestDTO.lastName,
                PhoneNumber = registerationRequestDTO.phoneNumber
            };

            try
            {
                var result = await _userManager.CreateAsync(user, registerationRequestDTO.password);
                if (result.Succeeded)
                {
                    if (!await _roleManager.RoleExistsAsync(registerationRequestDTO.role))
                    {
                        await _roleManager.CreateAsync(new IdentityRole(registerationRequestDTO.role));
                    }

                    await _userManager.AddToRoleAsync(user, registerationRequestDTO.role);

                    // Add user detail
                    UserDetail userdetail = new UserDetail();
                    userdetail.UserId = user.Id;
                    userdetail.CountryId = registerationRequestDTO.countryId;
                    userdetail.StateId = registerationRequestDTO.stateId;
                    userdetail.Gender = registerationRequestDTO.gender;
                    userdetail.DialCode = registerationRequestDTO.dialCode;
                    userdetail.DeviceType = registerationRequestDTO.deviceType;
                    await _context.UserDetail.AddAsync(userdetail);
                    await _context.SaveChangesAsync();

                    var userToReturn = await _context.ApplicationUsers
                        .Where(u => u.Email.ToLower() == registerationRequestDTO.email.ToLower()).FirstOrDefaultAsync();
                    LoginRequestDTO loginRequestDTO = new LoginRequestDTO();
                    loginRequestDTO.emailOrPhone = registerationRequestDTO.email;
                    loginRequestDTO.password = registerationRequestDTO.password;
                    LoginResponseDTO loginResponseDTO = await Login(loginRequestDTO);

                    return loginResponseDTO;
                }
            }
            catch (Exception e)
            {

            }

            return new LoginResponseDTO();
        }
        public async Task<LoginResponseDTO> PhoneLogin(PhoneLoginRequestDTO model)
        {
            var user = await _context.ApplicationUsers.FirstOrDefaultAsync(u => (u.PhoneNumber == model.PhoneNumber));

            if (user == null)
            {
                // register user
                user = new ApplicationUser();
                user.FirstName = "";
                user.LastName = "";
                user.PhoneNumber = model.PhoneNumber;
                user.UserName = model.PhoneNumber;
                user.PasswordHash = Crypto.HashPassword("123456");

                await _context.AddAsync(user);
                await _context.SaveChangesAsync();

                if (!await _roleManager.RoleExistsAsync(model.role))
                {
                    await _roleManager.CreateAsync(new IdentityRole(model.role));
                }
                var dd = await _userManager.AddToRoleAsync(user, model.role);

                var userDetail = new UserDetail();
                userDetail.UserId = user.Id;
                userDetail.DialCode = model.dialCode;
                userDetail.DeviceType = model.deviceType;
                userDetail.Fcmtoken = model.fcmtoken;
                userDetail.AddressLongitude = model.addressLongitude;
                userDetail.AddressLatitude = model.addressLatitude;

                await _context.AddAsync(userDetail);
                await _context.SaveChangesAsync();

                // send OTP on phone number

                // string userphoneNumber = model.DialCode + model.PhoneNumber;
                // var verificationResult = await _twilioManager.StartVerificationAsync(
                //     userphoneNumber,
                //     GlobalVariables.TwilioChannelTypes.Sms.ToString().ToLower());
            }

            //generate JWT Token
            var roles = await _userManager.GetRolesAsync(user);
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(secretKey);
            user.SecurityStamp = user.SecurityStamp == null ? secretKey : user.SecurityStamp;
            // roles.FirstOrDefault() = user.SecurityStamp == null ? secretKey : user.SecurityStamp;
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimTypes.Name, user.Id.ToString()),
                    new Claim(ClaimTypes.Role, roles.FirstOrDefault()),
                    new Claim("SecurityStamp", user.SecurityStamp),

                    // new Claim(ClaimTypes.Anonymous,user.SecurityStamp)
                }),
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = new(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            LoginResponseDTO loginResponseDTO = new LoginResponseDTO()
            {
                token = tokenHandler.WriteToken(token),
            };
            _mapper.Map(user, loginResponseDTO);

            var userdetail = await _context.UserDetail.Where(u => (u.UserId == user.Id) && (u.IsDeleted == false)).FirstOrDefaultAsync();
            if (userdetail == null)
            {
                return new LoginResponseDTO();
            }
            loginResponseDTO.role = roles[0];

            loginResponseDTO.gender = userdetail.Gender;
            loginResponseDTO.dialCode = userdetail.DialCode;
            loginResponseDTO.profilepic = userdetail.ProfilePic;
            loginResponseDTO.phoneOTP = "1234";

            return loginResponseDTO;
        }
        public async Task<IActionResult> ResetPassword(ResetPasswordDTO model)
        {
            if (!string.IsNullOrEmpty(model.email))
            {
                var applicationUser = _userManager.FindByEmailAsync(model.email).GetAwaiter().GetResult();
                if (applicationUser != null)
                {
                    var password = Crypto.HashPassword(model.newPassword);
                    applicationUser.PasswordHash = password;

                    await _userManager.UpdateAsync(applicationUser);

                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = true;
                    _response.Data = new { NewPassword = model.newPassword };
                    _response.Messages = "Password reset successfully.";
                    return (IActionResult)_response;
                }
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = false;
                _response.Messages = "User not found.";
                return (IActionResult)_response;
            }
            else
            {
                var applicationUser = _context.ApplicationUsers.FirstOrDefault(x => (x.PhoneNumber == model.phoneNumber));
                if (applicationUser != null)
                {
                    var password = Crypto.HashPassword(model.newPassword);
                    applicationUser.PasswordHash = password;

                    await _userManager.UpdateAsync(applicationUser);

                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = true;
                    _response.Data = new { NewPassword = model.newPassword };
                    _response.Messages = "Password reset successfully.";
                    return (IActionResult)_response;
                }
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = false;
                _response.Messages = "User not found.";
                return (IActionResult)_response;
            }
        }
        public async Task<VendorSalonResponseDTO> VendorRegistration(AddVendorSalonDTO addVendorDTO, string currentUserId)
        {
            //ApplicationUser user = new()
            //{
            //    Email = addVendorDTO.Email,
            //    UserName = addVendorDTO.Email,
            //    NormalizedEmail = addVendorDTO.Email.ToUpper(),
            //    FirstName = addVendorDTO.FirstName,
            //    LastName = addVendorDTO.LastName,
            //    PhoneNumber = addVendorDTO.PhoneNumber
            //};
            var user = _mapper.Map<ApplicationUser>(addVendorDTO);
            user.UserName = addVendorDTO.email;

            try
            {
                var password = CommonMethod.GeneratePassword();
                var result = await _userManager.CreateAsync(user, password);
                if (result.Succeeded)
                {
                    if (!await _roleManager.RoleExistsAsync("Vendor"))
                    {
                        await _roleManager.CreateAsync(new IdentityRole("Vendor"));
                    }

                    await _userManager.AddToRoleAsync(user, "Vendor");
                    // Add user detail
                    // var userdetail = _mapper.Map<UserDetail>(addVendorDTO);
                    var userdetail = new UserDetail();
                    userdetail.UserId = user.Id;
                    userdetail.DialCode = addVendorDTO.dialCode;
                    userdetail.CountryId = addVendorDTO.countryId;
                    userdetail.StateId = addVendorDTO.stateId;
                    userdetail.Gender = addVendorDTO.gender;
                    userdetail.DeviceType = addVendorDTO.deviceType;
                    userdetail.UserId = user.Id;
                    foreach (var item in userdetail.Upidetail)
                    {
                        item.UserId = user.Id;
                        // item.IsActive = false;
                    }
                    userdetail.CreatedBy = currentUserId;
                    // userdetail.ModifyDate = DateTime.UtcNow;

                    await _context.AddAsync(userdetail);
                    await _context.SaveChangesAsync();

                    var userToReturn = _context.ApplicationUsers
                        .FirstOrDefault(u => u.Email.ToLower() == addVendorDTO.email.ToLower());
                    var mappedData = _mapper.Map<VendorSalonResponseDTO>(userToReturn);
                    mappedData.vendorId = user.Id;
                    mappedData.createdBy = currentUserId;
                    mappedData.gender = userdetail.Gender;
                    var roles = await _userManager.GetRolesAsync(user);
                    mappedData.password = password;
                    mappedData.countryId = userdetail.CountryId;
                    mappedData.stateId = userdetail.StateId;
                    mappedData.gender = userdetail.Gender;
                    mappedData.dialCode = userdetail.DialCode;
                    if (userdetail.ProfilePic != null)
                    {
                        mappedData.profilePic = userdetail.ProfilePic;
                    }

                    var createdByDetail = _context.ApplicationUsers.FirstOrDefault(u => u.Id == currentUserId);
                    mappedData.createdByName = createdByDetail.FirstName + " " + createdByDetail.LastName;

                    return mappedData;
                }
            }
            catch (Exception e)
            {

            }
            return new VendorSalonResponseDTO();
        }
        public async Task<AdminUserDTO> AdminUserRegistration(AdminUserRegisterationRequestDTO registerationRequestDTO)
        {
            ApplicationUser user = new()
            {
                Email = registerationRequestDTO.email,
                UserName = registerationRequestDTO.email,
                NormalizedEmail = registerationRequestDTO.email.ToUpper(),
                FirstName = registerationRequestDTO.firstName,
                LastName = registerationRequestDTO.lastName,
                PhoneNumber = registerationRequestDTO.phoneNumber
            };

            try
            {
                var password = CommonMethod.GeneratePassword();
                var result = await _userManager.CreateAsync(user, password);
                if (result.Succeeded)
                {
                    if (!await _roleManager.RoleExistsAsync("Admin"))
                    {
                        await _roleManager.CreateAsync(new IdentityRole("Admin"));
                    }

                    await _userManager.AddToRoleAsync(user, "Admin");

                    // Add user detail
                    UserDetail userdetail = new UserDetail();
                    userdetail.UserId = user.Id;
                    userdetail.CountryId = registerationRequestDTO.countryId;
                    userdetail.StateId = registerationRequestDTO.stateId;
                    userdetail.Gender = registerationRequestDTO.gender;
                    userdetail.ModifyDate = DateTime.UtcNow;
                    userdetail.DeviceType = registerationRequestDTO.deviceType;
                    userdetail.DialCode = registerationRequestDTO.dialCode;
                    _context.UserDetail.Add(userdetail);
                    _context.SaveChanges();

                    var userToReturn = _context.ApplicationUsers
                        .FirstOrDefault(u => u.Email.ToLower() == registerationRequestDTO.email.ToLower());
                    var mappedData = _mapper.Map<AdminUserDTO>(userToReturn);
                    mappedData.gender = userdetail.Gender;
                    if (userdetail.ProfilePic != null)
                    {
                        mappedData.profilepic = userdetail.ProfilePic;
                    }
                    var roles = await _userManager.GetRolesAsync(user);
                    mappedData.role = roles[0];
                    mappedData.password = password;

                    return mappedData;
                }
            }
            catch (Exception e)
            {

            }
            return new AdminUserDTO();
        }
    }
}
