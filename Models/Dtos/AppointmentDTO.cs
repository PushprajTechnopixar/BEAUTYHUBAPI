using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BeautyHubAPI.Models.Dtos
{
    public class PlaceAppointmentRequestDTO
    {
        public int? couponId { get; set; }
        public int? paymentReceiptId { get; set; }
        public string? paymentMethod { get; set; }
    }
    public class SetAppointmentStatusDTO
    {
        public int appointmentId { get; set; }
        public string? slotIds { get; set; }
        public string appointmentStatus { get; set; }
        public bool setToAll { get; set; }
    }
    public class ReadStatusDTO
    {
        public int appointmentId { get; set; }
        // public string? appointmentStatus { get; set; }
        // public bool? isUpdated { get; set; }
    }
    public class SetPaymentStatusDTO
    {
        public int appointmentId { get; set; }
        public string? paymentStatus { get; set; }
    }

    public class CancelAppointmentDTO
    {
        public int appointmentId { get; set; }
        public string? slotIds { get; set; }
        public bool cancelAllAppointments { get; set; }
    }
    public class AppointmentDetailDTO
    {
        public int appointmentId { get; set; }
        public string customerUserId { get; set; }
        public string? transactionId { get; set; }
        public int? couponId { get; set; }
        public string customerFirstName { get; set; }
        public string? customerLastName { get; set; }
        public string? appointmentStatus { get; set; }
        public string? paymentMethod { get; set; }
        public string? paymentStatus { get; set; }
        public double? cgst { get; set; }
        public double? igst { get; set; }
        public double? sgst { get; set; }
        public double? basePrice { get; set; }
        public double? finalPrice { get; set; }
        public double? cancelledPrice { get; set; }
        public double? totalPrice { get; set; }
        public double? discount { get; set; }
        public double? totalDiscount { get; set; }
        public string createDate { get; set; }
        public int? totalServices { get; set; }
        public string? PaymentReceipt { get; set; }
        public List<BookedServicesPerSalonDTO> appointmentFromSalon { get; set; }

    }
    public class VendorAppointmentDetailDTO
    {
        public int appointmentId { get; set; }
        public string customerUserId { get; set; }
        public int? serviceInvoiceId { get; set; }
        public string? transactionId { get; set; }
        public int? couponId { get; set; }
        public string customerFirstName { get; set; }
        public string? customerLastName { get; set; }
        // public string? deliveryType { get; set; }
        public string? appointmentStatus { get; set; }
        public string? paymentMethod { get; set; }
        public string? paymentStatus { get; set; }
        public string? salonName { get; set; }
        public double? cgst { get; set; }
        public double? igst { get; set; }
        public double? sgst { get; set; }
        public double? basePrice { get; set; }
        public double? finalPrice { get; set; }
        public double? totalPrice { get; set; }
        public double? discount { get; set; }
        public double? totalDiscount { get; set; }
        public double? cancelledPrice { get; set; }
        public string createDate { get; set; }
        public int? totalServices { get; set; }
        public string? customerAddress { get; set; }
        public string? PaymentReceipt { get; set; }
        public List<BookedServicesDTO> bookedServices { get; set; }
    }
    public class BookedServicesPerSalonDTO
    {
        public int salonId { get; set; }
        public string salonName { get; set; }
        public string salonPhoneNumber { get; set; }
        public string? salonImage { get; set; }
        public string salonAddress { get; set; }
        public string salonLatitude { get; set; }
        public string salonLongitude { get; set; }
        public string distance { get; set; }
        public string duration { get; set; }
        public double? cgst { get; set; }
        public double? igst { get; set; }
        public double? sgst { get; set; }
        public double? basePrice { get; set; }
        public double? finalPrice { get; set; }
        public double? totalPrice { get; set; }
        public double? totalDiscount { get; set; }
        public double? cancelledPrice { get; set; }
        public double? discount { get; set; }
        public int serviceCountInCart { get; set; }
        public List<BookedServicesDTO> AppointmentedServices { get; set; }
    }
    public class BookedServicesDTO
    {
        public int bookedServiceId { get; set; }
        public int? couponId { get; set; }
        public int? slotId { get; set; }
        public int appointmentId { get; set; }
        public int? serviceId { get; set; }
        public string? serviceName { get; set; }
        public string? serviceImage { get; set; }
        public double? basePrice { get; set; }
        public double? listingPrice { get; set; }
        public double? finalPrice { get; set; }
        public double? totalPrice { get; set; }
        public double? totalDiscount { get; set; }
        public double? cancelledPrice { get; set; }
        public double? discount { get; set; }
        public string? vendorId { get; set; }
        public int? salonId { get; set; }
        public string? vendorName { get; set; }
        public string? salonName { get; set; }
        public string appointmentDate { get; set; }
        public string fromTime { get; set; } = null!;
        public string toTime { get; set; } = null!;
        public int? durationInMinutes { get; set; }
        public bool favoritesStatus { get; set; }
        public string? AppointmentStatus { get; set; }
        public string createDate { get; set; }
        public DateTime appointmentDateTime { get; set; }
        public int serviceCountInCart { get; set; }

    }
    public class AppointmentedListDTO
    {
        public int appointmentId { get; set; }
        public string customerUserId { get; set; }
        public string customerFirstName { get; set; }
        public string? customerLastName { get; set; }
        public string? phoneNumber { get; set; }
        public string? appointmentStatus { get; set; }
        public string? paymentMethod { get; set; }
        public string? paymentStatus { get; set; }
        public double? finalPrice { get; set; }
        public double? basePrice { get; set; }
        public double? totalPrice { get; set; }
        public double? cancelledPrice { get; set; }
        public double? totalDiscount { get; set; }
        public string appointmentDate { get; set; }
        public string createDate { get; set; }
        public string? paymentReceipt { get; set; }
        public string? totalServices { get; set; }
        public bool? IsUpdated { get; set; }
    }
    public class CustomerAppointmentedListDTO
    {
        public int appointmentId { get; set; }
        public string customerUserId { get; set; }
        public string appointmentTitle { get; set; }
        public string? appointmentDescription { get; set; }
        public string? serviceImage { get; set; }
        public string? appointmentStatus { get; set; }
        public string? paymentMethod { get; set; }
        public string? paymentStatus { get; set; }
        public double? totalPrice { get; set; }
        public double? finalPrice { get; set; }
        public double? basePrice { get; set; }
        public double? cancelledPrice { get; set; }
        public double? totalDiscount { get; set; }
        public string appointmentDate { get; set; }
        public string appointmentFromTime { get; set; }
        public string appointmentToTime { get; set; }
        public int totalServices { get; set; }
        public bool favoritesStatus { get; set; }
        public string? paymentReceipt { get; set; }
        public string CreateDate { get; set; }
        public string salonLatitude { get; set; }
        public string salonLongitude { get; set; }
        public string salonPhoneNumber { get; set; }
        public string salonAddress { get; set; }
        public string salonName { get; set; }
        public string? distance { get; set; }
        public string? duration { get; set; }
        public int scheduleCount { get; set; }
        public int cancelledCount { get; set; }
        public int completedCount { get; set; }
        public DateTime appointmentDateTime { get; set; }
    }

}
