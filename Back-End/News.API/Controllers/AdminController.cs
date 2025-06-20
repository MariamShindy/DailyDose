namespace News.API.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController(UserManager<ApplicationUser> _userManager,
        IUserService _userService) : ApiController
    {
        // POST : api/admin/lock-user/{id}
        [HttpPost("lock-user/{id}")]
        public async Task<IActionResult> LockUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NoContent();
            user.LockoutEnd = DateTimeOffset.MaxValue;
            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
                return Ok(new { result = "User locked" });
            else
                return BadRequest();
        }
        // POST : api/admin/unlock-user/{id}
        [HttpPost("unlock-user/{id}")]
        public async Task<IActionResult> UnlockUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NoContent();
            user.LockoutEnd = null;
            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
                return Ok(new { result = "User unlocked" });
            else
                return BadRequest();
        }
        // GET : api/admin/all-users
        [HttpGet("all-users")]
        public async Task<IActionResult> GetAllUsers()
        {
            try
            {
                var userDtos = await _userService.GetAllUsersAsync();
                    return Ok(userDtos);
            }
            catch (Exception)
            {
                return StatusCode(500, new { Status = "Error", Message = "An error occurred while fetching users." });
            }
        }
        //GET : api/admin/all-surveys
        [HttpGet("all-surveys")]
        public async Task<ActionResult> GetSurveys()
        {
            var surveys = await _userService.GetAllSurvyesAsync();
            return Ok(surveys); 
        }
    }
}
