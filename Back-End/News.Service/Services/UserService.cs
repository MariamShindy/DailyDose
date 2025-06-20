namespace News.Service.Services
{
    public class UserService(IHttpContextAccessor _httpContextAccessor,
        IMapper _mapper, ImageUploader _imageUploader,
        IUnitOfWork _unitOfWork, ILoggingService _logger,
        IMailSettings _mailSettings, UserManager<ApplicationUser> _userManager) : IUserService
    {
        public async Task<List<UserPreferencesDto>> GetUsersPreferencesAsync()
        {
            var users = await _unitOfWork.Repository<ApplicationUser>().GetAllAsync();
            return users.Select(u => new UserPreferencesDto
            {
                UserId = u.Id,
                PreferredCategories = u.Categories.Select(c => c.Name).ToList()
            }).ToList();
        }

        public async Task<bool> SendFeedbackAsync(FeedbackDto feedbackDto)
        {
            _logger.LogInfo(nameof(UserService), "SendFeedback called");
            var email = CreateEmail("NewsAggregator Contact Us Form", BuildFeedbackEmailBody(feedbackDto));
            return await TrySendEmailAsync(email, "SendFeedback");
        }

        public async Task<bool> SendSurveyAsync(SurveyDto surveyDto)
        {
            _logger.LogInfo(nameof(UserService), "SendSurveyAsync called");
            var email = CreateEmail("NewsAggregator Survey Form", BuildSurveyEmailBody(surveyDto));

            if (!await TrySendEmailAsync(email, "SendSurveyAsync"))
                return false;

            try
            {
                var survey = _mapper.Map<Survey>(surveyDto);
                survey.ApplicationUser = await GetCurrentUserAsync();
                await _unitOfWork.Repository<Survey>().AddAsync(survey);
                await _unitOfWork.CompleteAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(UserService), "SendSurveyAsync failed", ex);
                return false;
            }
        }

        public async Task<IEnumerable<SurveyResponseDto>> GetAllSurvyesAsync()
        {
            var surveys = await _unitOfWork.Repository<Survey>()
                .GetAllAsync(include: q => q.Include(s => s.ApplicationUser));

            return surveys.Select(MapSurveyToDto);
        }

        public async Task<ApplicationUser> GetCurrentUserAsync()
        {
            _logger.LogInfo(nameof(UserService), "GetCurrentUser called");

            var userId = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                throw new UnauthorizedAccessException("No user is logged in.");

            var user = await _userManager.Users.Include(u => u.Categories)
                .FirstOrDefaultAsync(u => u.UserName == userId);

            if (user == null)
                throw new InvalidOperationException("The user was not found.");

            _logger.LogInfo(nameof(UserService), "GetCurrentUser succeeded");
            return user;
        }

        public async Task<IdentityResult> UpdateUserAsync(EditUserDto dto)
        {
            _logger.LogInfo(nameof(UserService), "UpdateUser called");
            var user = await GetCurrentUserAsync();

            var validationResult = await ValidateAndUpdateUserFields(dto, user);
            if (!validationResult.Succeeded)
                return validationResult;

            await UpdateUserImageAsync(dto, user);

            user.FirstName = dto.FirstName ?? user.FirstName;
            user.LastName = dto.LastName ?? user.LastName;

            var result = await _userManager.UpdateAsync(user);
            _logger.LogInfo(nameof(UserService), result.Succeeded ? "UpdateUser succeeded" : "UpdateUser failed");

            return result;
        }

        public async Task SetUserPreferredCategoriesAsync(ApplicationUser user, ICollection<string> categoryNames)
        {
            _logger.LogInfo(nameof(UserService), "SetUserPreferredCategoriesAsync called");

            var categories = await _unitOfWork.Repository<Category>()
                .FindAsync(c => categoryNames.Contains(c.Name));

            if (categories.Count() != categoryNames.Count)
                throw new ArgumentException("One or more category names are invalid.");

            user.Categories.Clear();
            foreach (var category in categories)
                user.Categories.Add(category);

            await _unitOfWork.CompleteAsync();
        }

        public async Task<IEnumerable<CategoryDto>> GetUserPreferredCategoriesAsync()
        {
            _logger.LogInfo(nameof(UserService), "GetUserPreferredCategoriesAsync called");

            var user = await GetCurrentUserAsync();
            if (user == null)
                throw new ArgumentException("User not found.");

            return user.Categories.Select(c => new CategoryDto
            {
                Id = c.Id,
                Name = c.Name
            });
        }

        public async Task<IEnumerable<CategoryDto>> GetUserPreferredCategoriesAsync(string userId)
        {
            _logger.LogInfo(nameof(UserService), $"GetUserPreferredCategoriesAsync for User with id {userId} called");

            var user = await _userManager.Users.Include(u => u.Categories).FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
                throw new ArgumentException("User not found.");

            return user.Categories.Select(c => new CategoryDto
            {
                Id = c.Id,
                Name = c.Name
            });
        }

        public async Task<List<UserDto>> GetAllUsersAsync()
        {
            _logger.LogInfo(nameof(UserService), "GetAllUsersAsync called");

            try
            {
                var users = await _userManager.GetUsersInRoleAsync("User");
                return users?.Select(MapUserToDto).ToList() ?? new List<UserDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(UserService), "Error while getting users", ex);
                return new List<UserDto>();
            }
        }

        public async Task<IEnumerable<NotificationDto>> GetUserNotificationsAsync(string userId)
        {
            var notifications = await _unitOfWork.Repository<Notification>()
                .FindAsync(n => n.ApplicationUserId == userId);

            return _mapper.Map<IEnumerable<NotificationDto>>(notifications);
        }

        public async Task<IdentityResult> RequestAccountDeletionAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return IdentityResult.Failed(new IdentityError { Description = "User not found" });

            user.IsPendingDeletion = true;
            user.DeletionRequestedAt = DateTime.UtcNow;

            return await _userManager.UpdateAsync(user);
        }

        private Email CreateEmail(string subject, string body) => new()
        {
            To = "mariamshindyroute@gmail.com",
            Subject = subject,
            Body = body
        };

        private async Task<bool> TrySendEmailAsync(Email email, string context)
        {
            try
            {
                await _mailSettings.SendEmail(email);
                _logger.LogInfo(nameof(UserService), $"{context} succeeded");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(UserService), $"{context} failed", ex);
                return false;
            }
        }

        private SurveyResponseDto MapSurveyToDto(Survey s) => new()
        {
            SourceDiscovery = s.SourceDiscovery,
            VisitFrequency = s.VisitFrequency,
            IsLoadingSpeedSatisfactory = s.IsLoadingSpeedSatisfactory,
            NavigationEaseRating = s.NavigationEaseRating,
            ApplicationUserId = s.ApplicationUser?.Id ?? "0",
            ApplicationUserName = s.ApplicationUser?.UserName ?? "N/A",
            ApplicationUserEmail = s.ApplicationUser?.Email ?? "N/A"
        };

        private UserDto MapUserToDto(ApplicationUser user) => new()
        {
            Id = user.Id,
            UserName = user.UserName,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            PasswordHash = user.PasswordHash,
            ProfilePicUrl = user.ProfilePicUrl,
            IsLockedOut = user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.UtcNow
        };

        private async Task<IdentityResult> ValidateAndUpdateUserFields(EditUserDto dto, ApplicationUser user)
        {
            if (!string.IsNullOrWhiteSpace(dto.Username) && dto.Username != user.UserName)
            {
                if (await _userManager.FindByNameAsync(dto.Username) != null)
                    return IdentityResult.Failed(new IdentityError { Description = "Username already exists." });

                user.UserName = dto.Username;
            }

            if (!string.IsNullOrWhiteSpace(dto.Email) && dto.Email != user.Email)
            {
                if (await _userManager.FindByEmailAsync(dto.Email) != null)
                    return IdentityResult.Failed(new IdentityError { Description = "Email already exists." });

                user.Email = dto.Email;
            }

            if (!string.IsNullOrWhiteSpace(dto.NewPassword))
            {
                if (string.IsNullOrWhiteSpace(dto.OldPassword))
                    return IdentityResult.Failed(new IdentityError { Description = "Old password is required." });

                if (!await _userManager.CheckPasswordAsync(user, dto.OldPassword))
                    return IdentityResult.Failed(new IdentityError { Description = "Old password is incorrect." });

                if (dto.NewPassword != dto.ConfirmNewPassword)
                    return IdentityResult.Failed(new IdentityError { Description = "Password confirmation does not match." });

                user.PasswordHash = _userManager.PasswordHasher.HashPassword(user, dto.NewPassword);
            }

            return IdentityResult.Success;
        }

        private async Task UpdateUserImageAsync(EditUserDto dto, ApplicationUser user)
        {
            if (dto.ProfilePicUrl != null && dto.ProfilePicUrl.Length > 0)
            {
                var imagePath = await _imageUploader.UploadProfileImageAsync(dto.ProfilePicUrl);
                user.ProfilePicUrl = imagePath;
            }
        }

        private string BuildFeedbackEmailBody(FeedbackDto feedbackDto)
        {
            var Body = $@"
        <html>
        <head>
            <style>
                body {{
                    font-family: Arial, sans-serif;
                    line-height: 1.6;
                    color: #333333;
                }}
                h2 {{
                    color: #0066cc;
                }}
                .container {{
                    border: 1px solid #dddddd;
                    padding: 20px;
                    border-radius: 10px;
                    background-color: #f9f9f9;
                    max-width: 600px;
                    margin: 20px auto;
                }}
                .content {{
                    margin-bottom: 15px;
                }}
                .footer {{
                    font-size: 0.9em;
                    color: #555555;
                    margin-top: 20px;
                    border-top: 1px solid #dddddd;
                    padding-top: 10px;
                    text-align: center;
                }}
            </style>
        </head>
        <body>
            <div class='container'>
                <h2>📰 Contact Us Form</h2>
                <div class='content'>
                    <p><strong>Subject:</strong> {feedbackDto.Subject}</p>
                    <p><strong>Name:</strong> {feedbackDto.FullName}</p>
                    <p><strong>Email:</strong> {feedbackDto.Email}</p>
                    <p><strong>Message:</strong></p>
                    <p>{feedbackDto.Message}</p>
                </div>
                <div class='footer'>
                    <p>This email was sent via the NewsAggregator contact us form.</p>
                </div>
            </div>
        </body>
        </html>";
            return Body;
        }

        private string BuildSurveyEmailBody(SurveyDto surveyDto)
        {
            var Body = $@"
        <html>
        <head>
            <style>
                body {{
                    font-family: Arial, sans-serif;
                    line-height: 1.6;
                    color: #333333;
                }}
                h2 {{
                    color: #0066cc;
                }}
                .container {{
                    border: 1px solid #dddddd;
                    padding: 20px;
                    border-radius: 10px;
                    background-color: #f9f9f9;
                    max-width: 600px;
                    margin: 20px auto;
                }}
                .content {{
                    margin-bottom: 15px;
                }}
                .footer {{
                    font-size: 0.9em;
                    color: #555555;
                    margin-top: 20px;
                    border-top: 1px solid #dddddd;
                    padding-top: 10px;
                    text-align: center;
                }}
            </style>
        </head>
        <body>
            <div class='container'>
                <h2>📰 Survey Form</h2>
                <div class='content'>
                    <p><strong>How did you find out about our news website?</strong><br>{surveyDto.SourceDiscovery}</p>
                    <p><strong>How often do you visit news websites?</strong><br>{surveyDto.VisitFrequency}</p>
                    <p><strong>Is the website loading speed satisfactory?</strong><br>{surveyDto.IsLoadingSpeedSatisfactory}</p>
                    <p><strong>How easy is it to navigate our website?</strong><br>{surveyDto.NavigationEaseRating}</p>
                </div>
                <div class='footer'>
                    <p>This email was sent via the NewsAggregator survey form.</p>
                </div>
            </div>
        </body>
        </html>";
            return Body;
        }
    }
}

