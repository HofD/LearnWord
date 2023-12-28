using IdentityService.Authorization.Models.Users;
using IdentityService.DAL.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace IdentityService.WebApi.Controllers
{
    [ApiController]
    [Route("users")]
    public class UsersController : ControllerBase
    {
        private readonly UserManager<LwIdentityUser> userManager;
        private readonly ILogger<UsersController> logger;

        public UsersController(UserManager<LwIdentityUser> userManager, ILogger<UsersController> logger) 
        {
            this.userManager = userManager;
            this.logger = logger;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(UserRegisterRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState.ToString());
            }

            if (await userManager.FindByEmailAsync(request.Email) != null)
            {
                return BadRequest($"User with email '{request.Email}' is already exists.");
            }

            var user = new LwIdentityUser(request.UserName);
            user.Email = request.Email;

            var result = await userManager.CreateAsync(user, request.Password);

            if (result.Succeeded)
            {
                return Ok();
            }

            return BadRequest(result.Errors);
        }
    }
}
