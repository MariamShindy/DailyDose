namespace News.Service.Services
{
    public class AccountService(ILoggingService _logger, IMemoryCache _memoryCache,
        ImageUploader _imageUploader, UserManager<ApplicationUser> _userManager,
        IConfiguration _configuration, IMailSettings _mailSettings) : IAccountService
    {
        public async Task<(bool isSuccess, string message, string? token)> RegisterUserAsync(RegisterModel model)
        {
            _logger.LogInfo(nameof(AccountService), "RegisterUser called");

            if (!ArePasswordsMatching(model))
            {
                _logger.LogWarning(nameof(AccountService), "RegisterUser, passwords do not match!");
                return (false, "Password and Confirm Password do not match!", null);
            }

            if (await IsUserExistsAsync(model.UserName))
            {
                _logger.LogWarning(nameof(AccountService), "RegisterUser, user already exists!");
                return (false, "User already exists!", null);
            }

            string? profilePicUrl = await UploadProfilePictureAsync(model.ProfilePicUrl);
            var user = CreateUserFromModel(model, profilePicUrl);

            var role = IsFirstUser() ? "Admin" : "User";
            var result = await _userManager.CreateAsync(user, model.Password);

            if (!result.Succeeded)
            {
                var error = result.Errors.FirstOrDefault()?.Description ?? "User creation failed";
                _logger.LogWarning(nameof(AccountService), $"RegisterUser failed: {error}");
                return (false, error, null);
            }

            await _userManager.AddToRoleAsync(user, role);
            _logger.LogInfo(nameof(AccountService), "RegisterUser succeeded");

            var token = GenerateJwtToken(user);
            return (true, "User created successfully!", token);
        }

        public async Task<(bool isSuccess, string token, string message, bool isDeletionCancelled)> LoginUserAsync(LoginModel model)
        {
            _logger.LogInfo(nameof(AccountService), "LoginUser called");

            var user = await _userManager.FindByNameAsync(model.UserName);
            if (user == null)
            {
                _logger.LogWarning(nameof(AccountService), "LoginUser, account does not exist");
                return (false, null, "Account does not exist.", false);
            }

            if (await _userManager.IsLockedOutAsync(user))
            {
                _logger.LogWarning(nameof(AccountService), "LoginUser, account locked");
                return (false, null, "Account locked.", false);
            }

            var (canLogin, _, deletionMsg, isDeletionCancelled) = await HandlePendingDeletion(user);
            if (!canLogin)
                return (false, null, deletionMsg, isDeletionCancelled);

            if (!await _userManager.CheckPasswordAsync(user, model.Password))
            {
                _logger.LogWarning(nameof(AccountService), "LoginUser, invalid credentials");
                return (false, null, "Invalid credentials.", false);
            }

            var token = GenerateJwtToken(user);
            var message = isDeletionCancelled ? "Login successful. Deletion request canceled." : "Login successful.";

            _logger.LogInfo(nameof(AccountService), "LoginUser succeeded");
            return (true, token, message, isDeletionCancelled);
        }

        public async Task<(bool Success, string Message)> ForgotPasswordAsync(string email)
        {
            _logger.LogInfo(nameof(AccountService), "ForgotPassword called");

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                _logger.LogInfo(nameof(AccountService), "ForgotPassword, user not found");
                return (false, "User not found.");
            }

            var code = GenerateVerificationCode();
            _memoryCache.Set(email, code, TimeSpan.FromMinutes(15));

            await _mailSettings.SendEmail(CreateVerificationEmail(email, code));
            _logger.LogInfo(nameof(AccountService), "ForgotPassword, verification code sent");

            return (true, "Verification code sent to your email.");
        }

        public async Task<(bool Success, string Message)> ValidateVerificationCodeAsync(string email, string code)
        {
            _logger.LogInfo(nameof(AccountService), "ValidateVerificationCode called");

            if (!_memoryCache.TryGetValue(email, out string storedCode) || storedCode != code)
            {
                _logger.LogWarning(nameof(AccountService), "ValidateVerificationCode, invalid or expired code");
                return (false, "Invalid or expired verification code.");
            }

            _logger.LogInfo(nameof(AccountService), "ValidateVerificationCode, code is valid");
            return (true, "Verification code is valid.");
        }

        public async Task<(bool Success, string Message, string? Token)> ResetPasswordAsync(string email, string code, string newPassword)
        {
            _logger.LogInfo(nameof(AccountService), "ResetPassword called");

            var validate = await ValidateVerificationCodeAsync(email, code);
            if (!validate.Success)
                return (false, validate.Message, null);

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                _logger.LogInfo(nameof(AccountService), "ResetPassword, user not found");
                return (false, "User not found.", null);
            }

            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, resetToken, newPassword);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogWarning(nameof(AccountService), $"ResetPassword failed: {errors}");
                return (false, $"Password reset failed: {errors}", null);
            }

            var token = GenerateJwtToken(user);
            _logger.LogInfo(nameof(AccountService), "ResetPassword succeeded");

            return (true, "Password reset successful.", token);
        }

        private bool ArePasswordsMatching(RegisterModel model) =>
            model.Password == model.ConfirmPassword;

        private async Task<bool> IsUserExistsAsync(string username) =>
            await _userManager.FindByNameAsync(username) is not null;

        private async Task<string?> UploadProfilePictureAsync(IFormFile? image) =>
            image is not null ? await _imageUploader.UploadProfileImageAsync(image) : null;

        private ApplicationUser CreateUserFromModel(RegisterModel model, string? picUrl) => new()
        {
            Email = model.Email,
            UserName = model.UserName,
            FirstName = model.FirstName,
            LastName = model.LastName,
            ProfilePicUrl = picUrl
        };

        private bool IsFirstUser() => !_userManager.Users.Any();
        private string GenerateVerificationCode() =>
            new Random().Next(100000, 999999).ToString();
        private Email CreateVerificationEmail(string email, string code) => new()
        {
            To = email,
            Subject = "Password Reset Verification Code",
            Body = $"Your password reset verification code is: {code}"
        };

        private async Task<(bool isSuccess, string token, string message, bool isDeletionCancelled)> HandlePendingDeletion(ApplicationUser user)
        {
            if (!user.IsPendingDeletion || user.DeletionRequestedAt is null)
                return (true, null, "", false);

            var deletionDays = (DateTime.UtcNow - user.DeletionRequestedAt.Value).TotalDays;
            if (deletionDays < 14)
            {
                user.IsPendingDeletion = false;
                user.DeletionRequestedAt = null;
                await _userManager.UpdateAsync(user);

                _logger.LogInfo(nameof(AccountService), "LoginUser, Deletion request canceled");
                return (true, null, "Deletion request canceled.", true);
            }

            _logger.LogWarning(nameof(AccountService), "LoginUser, account pending deletion period expired");
            return (false, null, "Account has been deleted.", false);
        }

        private string GenerateJwtToken(ApplicationUser user)
        {
            _logger.LogInfo(nameof(AccountService), "GenerateJwtToken called");

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, user.UserName ?? ""),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var roles = _userManager.GetRolesAsync(user).Result;
            claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                expires: DateTime.Now.AddHours(3),
                claims: claims,
                signingCredentials: new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256)
            );

            _logger.LogInfo(nameof(AccountService), "GenerateJwtToken succeeded");
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
