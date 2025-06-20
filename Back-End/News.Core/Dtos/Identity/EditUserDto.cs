namespace News.Core.Dtos.Identity
{
    public class EditUserDto
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public IFormFile? ProfilePicUrl { get; set; }
        public string? Username { get; set; }
        public string? OldPassword { get; set; }
        public string? NewPassword { get; set; }
        public string? ConfirmNewPassword { get; set; }
        public string? Email { get; set; }
    }
}
