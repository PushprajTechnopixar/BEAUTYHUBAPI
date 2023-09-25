namespace BeautyHubAPI.Models.Dtos
{
    public class UserDTO
    {
        public string id { get; set; }
        public string email { get; set; }
        public string firstName { get; set; }
        public string lastName { get; set; }
        public string profilepic { get; set; }
        public string? gender { get; set; }
        public string? dialCode { get; set; }
        public string phoneNumber { get; set; }
        public string role { get; set; }
    }

    public class AdminUserListDTO
    {
        public string id { get; set; }
        public string email { get; set; }
        public string firstName { get; set; }
        public string lastName { get; set; }
        public string profilepic { get; set; }
        public string? gender { get; set; }
        public DateTime? modifyDate { get; set; }
    }
}
