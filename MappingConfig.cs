﻿using AutoMapper;
using BeautyHubAPI.Dtos;
using BeautyHubAPI.Models;
using BeautyHubAPI.Models.Dtos;
using BeautyHubAPI.Repository;
using BeautyHubAPI.Repository.IRepository;

namespace BeautyHubAPI
{
    public class MappingConfig : Profile
    {
        public MappingConfig()
        {
            CreateMap<IUserRepository, UserRepository>().ReverseMap();
            CreateMap<UserDTO, ApplicationUser>().ReverseMap();
            CreateMap<LoginResponseDTO, ApplicationUser>().ReverseMap();
            CreateMap<UserDetailDTO, ApplicationUser>().ReverseMap();
            CreateMap<UserDetailDTO, UserDetail>().ReverseMap();
            CreateMap<ApplicationUser, UserRequestDTO>().ReverseMap();
            CreateMap<UserDetail, UserRequestDTO>().ReverseMap();
            CreateMap<Notification, NotificationDTO>().ReverseMap();
            CreateMap<NotificationDTO, Notification>().ReverseMap();
            CreateMap<NotificationSent, NotificationSentDTO>().ReverseMap();
            CreateMap<NotificationSentDTO, NotificationSent>().ReverseMap();
            CreateMap<BannerDTO, Banner>().ReverseMap();
            CreateMap<AddBannerDTO, Banner>().ReverseMap();
            CreateMap<UpdateBannerDTO, Banner>().ReverseMap();
            CreateMap<AddVendorSalonDTO, UserDetail>().ReverseMap();
            CreateMap<AddVendorSalonDTO, ApplicationUser>().ReverseMap();
            CreateMap<AddSalonDTO, SalonDetail>().ReverseMap();
            CreateMap<SalonResponseDTO, SalonDetail>().ReverseMap();
            CreateMap<VendorSalonResponseDTO, ApplicationUser>().ReverseMap();
            CreateMap<PaymentReceiptDTO, PaymentReceipt>().ReverseMap();
            CreateMap<AddVendorSalonDTO, BankDetail>().ReverseMap();
            CreateMap<UpdateVendorSalonDTO, ApplicationUser>().ReverseMap();
            CreateMap<UpdateVendorSalonDTO, UserDetail>().ReverseMap();
            CreateMap<UpdateVendorSalonDTO, BankDetail>().ReverseMap();
            CreateMap<UpdateBankDTO, BankDetail>().ReverseMap();
            CreateMap<UpdateSalonDTO, BankDetail>().ReverseMap();
            CreateMap<UpdateVendorSalonDTO, SalonDetail>().ReverseMap();
            CreateMap<VendorSalonResponseDTO, ApplicationUser>().ReverseMap();
            CreateMap<AddVendorSalonDTO, UserDetail>().ReverseMap();
            CreateMap<AddVendorSalonDTO, ApplicationUser>().ReverseMap();
            CreateMap<VendorSalonResponseDTO, UserDetail>().ReverseMap();
            CreateMap<SalonResponseDTO, SalonDetail>().ReverseMap();
            CreateMap<AddSalonDTO, SalonDetail>().ReverseMap();
            CreateMap<BankResponseDTO, BankDetail>().ReverseMap();
            CreateMap<AddBankDTO, BankDetail>().ReverseMap();
            CreateMap<AddUPIDTO, Upidetail>().ReverseMap();
            CreateMap<GetMembershipPlanListDTO, MembershipPlan>().ReverseMap();
            CreateMap<GetMembershipPlanDTO, MembershipPlan>().ReverseMap();
            CreateMap<MembershipPlanDTO, MembershipPlan>().ReverseMap();
            CreateMap<UPIResponseDTO, Upidetail>().ReverseMap();
            CreateMap<SuperAdminResponseDTO, ApplicationUser>().ReverseMap();
            CreateMap<UserDetail, ApplicationUser>().ReverseMap();
            CreateMap<UpdateSuperAdminDTO, ApplicationUser>().ReverseMap();
            CreateMap<UserDetail, SuperAdminResponseDTO>().ReverseMap();
            CreateMap<UpdateUPIDTO, Upidetail>().ReverseMap();
            CreateMap<AccountDetailDTO, BankDetail>().ReverseMap();
            CreateMap<GetMembershipRecordDTO, MembershipRecord>().ReverseMap();
            CreateMap<UpdateSalonDTO, SalonDetail>().ReverseMap();
            CreateMap<CustomerSalonListDTO, SalonDetail>().ReverseMap();
            CreateMap<AddCategoryDTO, MainCategory>().ReverseMap();
            CreateMap<AddCategoryDTO, SubCategory>().ReverseMap();
            CreateMap<CategoryDTO, MainCategory>().ReverseMap();
            CreateMap<CategoryDTO, SubCategory>().ReverseMap();
            CreateMap<UpdateCategoryDTO, MainCategory>().ReverseMap();
            CreateMap<UpdateCategoryDTO, SubCategory>().ReverseMap();
            CreateMap<VendorCategoryRequestDTO, VendorCategory>().ReverseMap();
            CreateMap<AddSalonBannerDTO, SalonBanner>().ReverseMap();
            CreateMap<UpdateSalonBannerDTO, SalonBanner>().ReverseMap();
            CreateMap<GetSalonBannerDTO, SalonBanner>().ReverseMap();
            CreateMap<UpdateCustomerAddressRequestDTO, CustomerAddress>().ReverseMap();
            CreateMap<AddCustomerAddressRequestDTO, CustomerAddress>().ReverseMap();
            CreateMap<UpdateLiveLocationDTO, UserDetail>().ReverseMap();
            CreateMap<AddUpdateSalonServiceDTO, SalonService>().ReverseMap();
            CreateMap<GetSalonServiceDTO, SalonService>().ReverseMap();
            CreateMap<AdminUserListDTO, ApplicationUser>().ReverseMap();
            CreateMap<UpdateAdminUserDTO, ApplicationUser>().ReverseMap();
            CreateMap<UpdateAdminUserDTO, UserDetail>().ReverseMap();
            CreateMap<AdminUserDTO, ApplicationUser>().ReverseMap();
            CreateMap<CustomerAddressDTO, CustomerAddress>().ReverseMap();
            CreateMap<GetSalonServiceDTO, SalonDetail>().ReverseMap();
            CreateMap<timeSlotsDTO, TimeSlot>().ReverseMap();
            CreateMap<serviceDetailDTO, SalonService>().ReverseMap();
            CreateMap<SalonServiceListDTO, SalonService>().ReverseMap();
            CreateMap<AddServiceToCartDTO, Cart>().ReverseMap();
            CreateMap<CartServicesDTO, Cart>().ReverseMap();
            CreateMap<CartServicesDTO, SalonService>().ReverseMap();
            CreateMap<CartServicesDTO, TimeSlot>().ReverseMap();
            CreateMap<AppointmentDetailDTO, Appointment>().ReverseMap();
            CreateMap<CustomerAppointmentedListDTO, Appointment>().ReverseMap();
            CreateMap<AppointmentedListDTO, Appointment>().ReverseMap();
            CreateMap<AppointmentedListDTO, BookedService>().ReverseMap();
            CreateMap<BookedServicesDTO, BookedService>().ReverseMap();
            CreateMap<BookedServicesDTO, Appointment>().ReverseMap();
            CreateMap<VendorAppointmentDetailDTO, Appointment>().ReverseMap();
            CreateMap<VendorAppointmentDetailDTO, BookedService>().ReverseMap();
            CreateMap<SalonService, IncludeServiceDTO>().ReverseMap();
            // CreateMap<UploadCategoryImageDTO, MainCategory>().ReverseMap();
            // CreateMap<UploadCategoryImageDTO, SubCategory>().ReverseMap();
        }
    }
}
