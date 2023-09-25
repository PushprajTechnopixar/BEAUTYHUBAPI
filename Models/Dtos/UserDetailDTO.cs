namespace BeautyHubAPI.Models.Dtos
{
    public class UserDetailDTO
    {
        public string id { get; set; }
        public string email { get; set; }
        public string firstName { get; set; }
        public string lastName { get; set; }
        public string? gender { get; set; }
        public string? dialCode { get; set; }
        public string phoneNumber { get; set; }
        public string? profilePic { get; set; }
        public string? pan { get; set; }
        public int? countryId { get; set; }
        public int? stateId { get; set; }
        public string? countryName { get; set; }
        public string? stateName { get; set; }
    }
}
