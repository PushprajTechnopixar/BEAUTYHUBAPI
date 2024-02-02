using BeautyHubAPI;
using BeautyHubAPI.Common;
using BeautyHubAPI.Helpers;

namespace BeautyHubAPI.Common
{
    public static class ResponseMessages
    {
        public static readonly string msgUserRegisterSuccess = "User registered successfully.";
        public static readonly string msgDoctorRegisterSuccess = "Doctor registered successfully.";

        public static readonly string msgUserRoleNotAuthorized =
            "Requested user type is not authorized.";
        public static readonly string msgSomethingWentWrong = "Something went wrong. ";
        public static readonly string msgOTPSentOnMobileSuccess = "OTP sent on your phone no";
        public static readonly string msgOTPSentOneMailuccess = "OTP sent on your email";
        public static readonly string msgParametersNotCorrect = "Parameters are not correct.";
        public static readonly string msgPasswordNotCorrect = "Password is not correct.";

        public static readonly string msgCouldNotFoundAssociatedUser =
            "Could not find any user associated with this email.";
        public static readonly string msgUserBlockedOrDeleted =
            "User blocked or deleted. Please contact the site administrator";
        public static readonly string msgEmailNotConfirmed = "Email not confirmed.";
        public static readonly string msgUserLoginSuccess = "Logged in successfully.";
        public static readonly string msgDoctorLoginSuccess = "Signed in successfully..";
        public static readonly string msgHospitalLoginSuccess = "Signed in successfully.";
        public static readonly string msgInvalidCredentials = "Username or password is incorrect.";
        public static readonly string msgEmailConfirmationSuccess = "Email confirmed successfully.";
        public static readonly string msgConfirmationCodeSentSuccess =
            "Confirmation code sent on your email.";
        public static readonly string msgInvalidOTP = "OTP is invalid.";
        public static readonly string msgOTPSentSuccess = "OTP sent on your email.";
        public static readonly string msgPasswordResetSuccess = "Password reset successfully.";
        public static readonly string msgPasswordChangeSuccess = "Password changed successfully.";
        public static readonly string msgEmailAlreadyConfirmed = "Email already confirmed.";
        public static readonly string msgResetEmailOtpSendSuccess =
            "Reset email OTP sent on your both emails.";
        public static readonly string msgNewOldOtpInvalid =
            "Either new or old email otp is invalid.";
        public static readonly string msgEmailAlreadyUsed =
            "The email provided already in use. Please try with other email.";
        public static readonly string msgEmailResetSuccess =
            "Email reset successfully. Please login with new email.";
        public static readonly string msgLogoutSuccess = "User logout successfully.";
        public static readonly string msgDbConnectionError = "Database connection error.";
        public static readonly string msgInvoiceStatusInvalid = "Requested status is not correct.";
        public static readonly string msgInvoiceTypeInvalid =
            "Requested type is not correct or Invoice number exists.";
        public static readonly string msgBlockOrInactiveUserNotPermitted =
            "Blocked or inactive user cannot update details.";
        public static readonly string msgSessionExpired =
            "Your current session has expired. Please login again.";
        public static readonly string msgInvalidAmount = "payment amount is invalid.";
        public static readonly string msgCreationSuccess = " created successfully.";
        public static readonly string msgUpdationSuccess = " updated successfully.";
        public static readonly string msgDataSavedSuccess = " saved successfully.";
        public static readonly string msgFoundSuccess = " found successfully.";
        public static readonly string msgShownSuccess = " shown successfully.";
        public static readonly string msgNotFound = "Could not find any ";
        public static readonly string msgListFoundSuccess = " list shown successfully.";
        public static readonly string msgDeletionSuccess = " deleted successfully.";
        public static readonly string msgAlreadyExists = " already exists.";
        public static readonly string msgResendInvoiceSuccess = " sent successfully.";
        public static readonly string msgExpenseStatus = "This expense status is not pending.";
        public static readonly string msgAlreadyDeleted =
            "This category already deleted so you cannot update this.";
        public static readonly string msgphoneNumberVerifiedSuccess =
            "Your phone number verified successfully";
        public static readonly string msgUserNotFound = "Could not found any user.";
        public static readonly string msgUserBlockedByAdmin =
            "You are blocked or rejected by admin. please contact to administrator";
        public static readonly string msgUserStatusPendingForApproval =
            "Your account is under review. Our team will contact you soon regarding approval.";
        public static readonly string msgTokenExpired = "Access token is expired";
        public static readonly string msgPlanExpired = "Plan is expired";
        public static readonly string msgSamePasswords =
            "Old password and New password cannot be same";
        public static readonly string msgAdditionSuccess = " added successfully.";
        public static readonly string msgGenerateSuccess = " generated successfully";
        public static readonly string msgEmailSentSuccess = "Email sent successfully";
        public static readonly string msgSentSuccess = " sent successfully";
        public static readonly string msgInvalidImageSize = "Image size is not valid";
        public static readonly string msgInvalidImage = "Only png images are valid";
        public static readonly string msgImageFieldRequired = "Image field must required.";
        public static readonly string msgInvalidTime = "Invalid date or time.";
        public static readonly string msgOTPNotGenerated =
            "You have not generated an OTP, Please generate OTP first";
        public static readonly string msgSixCharacter =
        "Please enter six character or more for password.";
    }
}
