namespace News.API.Controllers
{
	public class AccountController(IMapper _mapper,
        SignInManager<ApplicationUser> _signInManager, 
        IAccountService _accountService) : ApiController
    {
        // POST : api/account/register
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromForm] RegisterDto model)
        {
            var registerModel = _mapper.Map<RegisterModel>(model);
            var (isSuccess, message,token) = await _accountService.RegisterUserAsync(registerModel);
            if (!isSuccess)
                return StatusCode(StatusCodes.Status500InternalServerError, new { Status = "Error", Message = message });
            return Ok(new { Status = "Success", Message = message, Token = token });
        }

        // POST : api/account/login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto model)
        {
            var loginModel = _mapper.Map<LoginModel>(model);
            var (isSuccess, token, message, isDeletionCancelled) = await _accountService.LoginUserAsync(loginModel);

            if (!isSuccess)
                return Unauthorized(new { Message = message });

            return Ok(new
            {
                Token = token,
                Message = isDeletionCancelled ? "Login successful. Deletion request canceled." : message
            });
        }

        // POST : api/account/logout
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return Ok(new { result = "Logged out" });
        }

        // POST : api/account/forgot-password
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto model)
        {
            var (success, message) = await _accountService.ForgotPasswordAsync(model.Email);
            if (!success)
                return BadRequest(new { Status = "Error", Message = message });

            return Ok(new{ Status = "Success", Message = message,});
        }

        //POST : api/account/validate-verification-code
        [HttpPost("validate-verification-code")]
        public async Task<IActionResult> ValidateVerificationCode([FromBody] ValidateVerificationCodeDto model)
        {
            var result = await _accountService.ValidateVerificationCodeAsync(model.Email, model.VerificationCode);
            if (!result.Success)
                return BadRequest(new { Status = "Error", Message = result.Message });

            return Ok(new { Status = "Success", Message = result.Message });
        }

        // POST : api/account/reset-password
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto model)
        {
            var result = await _accountService.ResetPasswordAsync(model.Email, model.VerificationCode, model.NewPassword);
            if (!result.Success)
                return BadRequest(new { Status = "Error", Message = result.Message });

            return Ok(new { Status = "Success", Message = result.Message , Token = result.Token});
        }
    }

}
