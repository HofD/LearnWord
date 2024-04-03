using IdentityService.Authorization.Models.Authentication;
using IdentityService.Authorization.Models.Users;
using IdentityService.Authorization.Services;
using IdentityService.DAL.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace IdentityService.WebApi.Controllers
{
    [ApiController]
    [Route("account")]
    public class AccountController : ControllerBase
    {
        private readonly UserManager<LwIdentityUser> userManager;
        private readonly EmailService emailService;
        private readonly ILogger<AccountController> logger;

        public AccountController(UserManager<LwIdentityUser> userManager, EmailService emailService, ILogger<AccountController> logger) 
        {
            this.userManager = userManager;
            this.emailService = emailService;
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
                return Created();
            }

            var user = new LwIdentityUser(request.Email);
            user.Email = request.Email;

            var result = await userManager.CreateAsync(user, request.Password);

            if (result.Succeeded)
            {
                try
                {
                    var code = await userManager.GenerateEmailConfirmationTokenAsync(user);
                    var param = new Dictionary<string, string?>()
                    {
                        { "userId", user.Id },
                        { "code", code }
                    };
                    var callbackUrl = QueryHelpers.AddQueryString("https://learnword.online/confirm", param);

                    await emailService.SendRegistrationEmailAsync(request.Email, "Confirm your account",
                        $"Please confirm your email by following this <a href='{callbackUrl}'>link</a>");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex.ToString(), user.Email);
                    return Ok();
                }

                return Ok();
            }

            logger.LogError(result.Errors.ToString(), user.Email);
            return BadRequest(result.Errors);
        }

        [HttpPost("conformation/send")]
        public async Task<IActionResult> SendEmailConformation(SendConfirmationRequest sendConfirmationRequest)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState.ToString());
            }

            var user = await userManager.FindByEmailAsync(sendConfirmationRequest.Email);

            if (user == null)
            {
                return NotFound("User not exists.");
            }

            if (await userManager.IsEmailConfirmedAsync(user))
            {
                return BadRequest("Email already confirmed.");
            }

            var code = await userManager.GenerateEmailConfirmationTokenAsync(user);
            var param = new Dictionary<string, string?>()
            {
                { "userId", user.Id },
                { "code", code }
            };
            var callbackUrl = QueryHelpers.AddQueryString("https://learnword.online/confirm", param);

            await emailService.SendRegistrationEmailAsync(sendConfirmationRequest.Email, "Confirm your account",
                $"Please confirm your email by following this <a href='{callbackUrl}'>link</a>");
            
            return Ok();
        }

        [HttpGet("confirm")]
        public async Task<IActionResult> ConfirmEmail(string userId, string code)
        {
            if (userId == null || code == null)
            {
                return BadRequest();
            }

            var user = await userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            var result = await userManager.ConfirmEmailAsync(user, code);

            if (result.Succeeded)
                return Ok();
            else
                return StatusCode(StatusCodes.Status500InternalServerError, result.Errors);
        }
    }
}
