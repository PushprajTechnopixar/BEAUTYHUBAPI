using BeautyHubAPI.Models;
using BeautyHubAPI.Models.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace BeautyHubAPI.Repository.IRepository
{
    public interface ICustomerRepository
    {
        Task<Object> AddSalon(AddCustomerSalonDTO model, string currentUserId);
        Task<Object> GetSalonList(string? salonQuery, string? salonType, string? searchBy, int? liveLocation, string currentUserId);
        Task<Object> GetFavouriteSalonList(string? salonType, string? searchBy, int? liveLocation, string currentUserId);
        Task<Object> AddCustomerAddress(AddCustomerAddressRequestDTO model, string currentUserId);
        Task<Object> UpdateCustomerAddress(UpdateCustomerAddressRequestDTO model, string currentUserId);
        Task<Object> DeleteCustomerAddress(int customerAddressId, string currentUserId);
        Task<Object> GetCustomerAddressDetail(int customerAddressId, string currentUserId);
        Task<Object> SetCustomerAddressStatus(SerCustomerAddressRequestStatusDTO model, string currentUserId);
        Task<Object> GetCustomerAddressList(string currentUserId);
        Task<Object> DeleteCustomerAccount(string? customerUserId, string currentUserId);
        Task<Object> AddServiceToCart(AddServiceToCartDTO model, string currentUserId);
        Task<Object> GetServiceListFromCart(string? availableService, int? liveLocation, string currentUserId);
        Task<Object> RemoveServiceFromCart(int serviceId, int? slotId, string currentUserId);
        Task<Object> CancelAppointment(CancelAppointmentDTO model, string currentUserId);
        Task<Object> GetServiceCountInCart(string currentUserId);
        Task<Object> GetUnavailableServices(int? liveLocation, string currentUserId);
        Task<Object> setFavouriteSalonStatus(SetFavouriteSalon model, string currentUserId);
        Task<Object> BookAppointment(PlaceAppointmentRequestDTO model, string currentUserId);
        Task<Object> GetCustomerAppointmentList(CustomerAppointmentFilterationListDTO model, string currentUserId);
        Task<Object> GetCustomerAppointmentDetail(int appointmentId, int? liveLocation, string currentUserId);
        Task<Object> SetFavouriteServiceStatus(SetFavouriteService model, string currentUserId);
        Task<Object> GetFavouriteServiceList(int salonId, string currentUserId);
        Task<Object> GetCustomerDashboardData(DashboardServiceFilterationListDTO model, string currentUserId);
    }
}
