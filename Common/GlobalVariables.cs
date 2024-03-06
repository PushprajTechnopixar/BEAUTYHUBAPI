namespace BeautyHubAPI.Common
{
    public class GlobalVariables
    {
        public static readonly string profilePicContainer = "ProfileImages/";
        public static readonly string categoryImageContainer = "CategoryImages/";
        public static readonly string brandImageContainer = "BrandImages/";
        public static readonly string bannerImageContainer = "BannerImages/";
        public static readonly string SalonBannerImageContainer = "SalonBannerImages/";
        public static readonly string SalonImageContainer = "SalonImages/";
        public static readonly string paymentReceipt = "PaymentReceipt/";
        public static readonly string qrImageContainer = "QRImages/";
        public static readonly string serviceImageContainer = "ServiceImages/";
        public static readonly string collectionImageContainer = "CollectionImages/";
        public static readonly string passwordValidationMessage = "Your password must be at least 8 characters long, contain at least one number and have a mixture of uppercase and lowercase letters";

        #region "Twillio Validate Credentials"
        public const string twilio_accountSid = "AC827b59b1129fbac11722ad4a561856b3";
        public const string twilio_authToken = "548b36114477ff0f6fa4c3697ee84d08";
        public const string twilio_verificationSid = "VA6321353a3daef66b28810e7a1673ab96";
        public const string twilio_phoneNumber = "+12295525406";
        #endregion

        // #region "Twillio Validate Credentials"
        // public const string twilio_accountSid = "AC29c4a7421fc7562c7a608b498739e48d";
        // public const string twilio_authToken = "af5b64a8f714ef6512b764901a7dc383";
        // public const string twilio_verificationSid = "VA0e41787d8f0df2e73535c7ac87b68621";
        // public const string twilio_phoneNumber = "+16206709247";
        // #endregion

        // public const string bucketURL = "https://beautyhubtest-file.s3.ap-south-1.amazonaws.com/FileToSave/";
        // public const string imgURL = "https://beautyhubtest-file.s3.ap-south-1.amazonaws.com/FileToSave/";

        // public const string imgURL = "https://beautyhubtest-file.s3.ap-south-1.amazonaws.com/FileToSave/";
        // public const string bucketURL = "https://beautyhubtest-file.s3.ap-south-1.amazonaws.com/FileToSave/";


        public const string imgURL = "https://salon-near-me-file.s3.ap-south-1.amazonaws.com/FileToSave/";
        public const string bucketURL = "https://salon-near-me-file.s3.ap-south-1.amazonaws.com/FileToSave/";
        public const string imgData = $"data:image/jpeg;base64,";
        public static readonly string vendor_registration = "Vendor_Register.html";
        public static readonly string distributor_registration = "Distributor_Register.html";
        public static readonly string admin_user_registration = "Admin_User_Register.html";
        public static readonly string customer_registration = "Customer_Register.html";
        public static readonly string mainTemplatesContainer = "Templates";
        public static readonly string ExcelfileContainer = "Excelfiles";
        public static readonly string emailTemplatesContainer = "EmailTemplates";
        public static readonly string tempImageContainer = "TempImages/";
        public static readonly string ExcelFileContainer = "ExcelFiles/";

        public static readonly char stringSplitterPipe = '|';
        public static string distancematrixAPIKey = "AIzaSyBgLMQ8wvy5yda0qP1_8y1e_aJJ_HrTdZw";
        public enum TwilioChannelTypes
        {
            Sms = 1,
            Call,
            Email
        }
        public enum GSTTypes
        {
            Inclusive = 1,
            Exclusive,
        }
        public enum Gender
        {
            Male = 1,
            Female,
            Others,
            NA
        }
        public enum bannerCategoryType
        {
            Male = 1,
            Female,
            Male_and_Female,
            NA
        }
        public enum ProductFilter
        {
            Grocery = 1,
            Dairy,
        }

        public enum CategoryName
        {
            DairyandEggs = 3 // 3 is dairy category id
        }
        public enum Role
        {
            Admin = 1,
            SuperAdmin,
            Vendor,
            Customer
        }
        public enum Status
        {
            Pending = 0,
            Approved = 1,
            Rejected = 2,
            Expired
        }
        public enum ServiceStatus
        {
            Pending = 0,
            Active = 1,
            InActive = 2,
            Unavailable = 3
        }

        public enum DiscountType
        {
            Percentage = 0,
            Flat = 1,
        }
        // public enum BannerType
        // {
        //     Home = 0,
        //     SalonBanner = 1,
        //     SalonCategoryBanner = 2
        // }
        public enum AddressType
        {
            Home = 0,
            Work = 1,
            Other = 2
        }
        public enum AppointmentStatus
        {
            Pending = 0,
            Scheduled = 1,
            Cancelled,
            Completed
        }

        public enum CancelledBy
        {
            Customer = 0,
            Vendor = 1
        }
        public enum NotificationType
        {
            Appointment = 0,
            Broadcast,
            Subscription
        }
        public enum PaymentStatus
        {
            OnHold = 0,
            Paid = 1,
            Unpaid = 2,
            Refunded = 3,

        }
        public enum PaymentMethod
        {
            PayByUPI = 0,
            InCash,
            Acc_Ifsc,
        }

        public enum SalonType
        {
            Male = 1,
            Female = 2,
            Unisex
        }
        public enum TimePeriod
        {
            Monthly = 1,
            Quarterly,
            Semi_Annually,
            Annually
        }

        public enum DairySchedule
        {
            Daily = 1,
            Alternate_Day = 2,
            Every_3_Day = 3,
            Weekly = 4,
            Monthly = 5
        }

        public enum tempOrPermanent
        {
            Temporary = 1,
            Permanent = 2
        }
        public enum BannerType
        {
            Home = 0,
            SalonBanner = 1,
            SalonCategoryBanner = 2
        }
    }
}
