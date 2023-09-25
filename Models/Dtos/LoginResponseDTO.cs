namespace BeautyHubAPI.Models.Dtos
{
    public class LoginResponseDTO
    {
        public string id { get; set; }
        public string vendorId { get; set; }
        public string salonName { get; set; }
        public int? salonId { get; set; }
        public string salonImage { get; set; }
        public string email { get; set; }
        public string firstName { get; set; }
        public string lastName { get; set; }
        public string profilepic { get; set; }
        public string? gender { get; set; }
        public string? dialCode { get; set; }
        public string phoneNumber { get; set; }
        public string role { get; set; }
        public string token { get; set; }
        public string phoneOTP { get; set; }
        // public bool dairyWalletStatusForSalonping { get; set; }
    }
}
