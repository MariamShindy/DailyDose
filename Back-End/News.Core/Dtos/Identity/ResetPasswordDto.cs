namespace News.Core.Dtos.Identity
{
    public class ResetPasswordDto
    {
        public string Email { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
        public string VerificationCode { get; set; } = string.Empty;

    }
}
