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
        private readonly IConfiguration configuration;

        public AccountController(
            UserManager<LwIdentityUser> userManager,
            EmailService emailService,
            ILogger<AccountController> logger,
            IConfiguration configuration)
        {
            this.userManager = userManager;
            this.emailService = emailService;
            this.logger = logger;
            this.configuration = configuration;
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
                var existingUser = await userManager.FindByEmailAsync(request.Email);
                if (existingUser != null && !await userManager.IsEmailConfirmedAsync(existingUser))
                {
                    await SendConfirmationEmailAsync(existingUser, request.Email);
                }

                return Ok();
            }

            var user = new LwIdentityUser(request.Email);
            user.Email = request.Email;

            var result = await userManager.CreateAsync(user, request.Password);

            if (result.Succeeded)
            {
                try
                {
                    await SendConfirmationEmailAsync(user, request.Email);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex.ToString(), user.Email);
                    return Ok();
                }

                return Ok();
            }

            logger.LogError("Failed to register {Email}: {Errors}", user.Email, string.Join("; ", result.Errors.Select(x => $"{x.Code}: {x.Description}")));
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
            var callbackUrl = QueryHelpers.AddQueryString(GetEmailConfirmationBaseUrl(), param);

            await emailService.SendRegistrationEmailAsync(sendConfirmationRequest.Email, "Confirm your account",
                $"Please confirm your email by following this <a href='{callbackUrl}'>link</a>");
            
            return Ok();
        }

        [HttpPost("password/forgot")]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState.ToString());
            }

            var user = await userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                return Ok();
            }

            try
            {
                await SendPasswordResetEmailAsync(user, request.Email);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to send password reset email to {Email}", request.Email);
            }

            return Ok();
        }

        [HttpPost("password/reset")]
        public async Task<IActionResult> ResetPassword(ResetPasswordRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState.ToString());
            }

            var user = await userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                return NotFound("User not exists.");
            }

            var result = await userManager.ResetPasswordAsync(user, request.Code, request.Password);
            if (result.Succeeded)
            {
                return Ok();
            }

            return BadRequest(result.Errors);
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

        private async Task SendConfirmationEmailAsync(LwIdentityUser user, string email)
        {
            var code = await userManager.GenerateEmailConfirmationTokenAsync(user);
            var param = new Dictionary<string, string?>()
            {
                { "userId", user.Id },
                { "code", code }
            };
            var callbackUrl = QueryHelpers.AddQueryString(GetEmailConfirmationBaseUrl(), param);

            await emailService.SendRegistrationEmailAsync(email, "Confirm your account",
                $"Please confirm your email by following this <a href='{callbackUrl}'>link</a>");
        }

        private async Task SendPasswordResetEmailAsync(LwIdentityUser user, string email)
        {
            var code = await userManager.GeneratePasswordResetTokenAsync(user);
            var param = new Dictionary<string, string?>()
            {
                { "email", email },
                { "code", code }
            };
            var callbackUrl = QueryHelpers.AddQueryString(GetPasswordResetBaseUrl(), param);

            await emailService.SendRegistrationEmailAsync(email, "Reset your password",
                $"Reset your password by following this <a href='{callbackUrl}'>link</a>");
        }

        private string GetEmailConfirmationBaseUrl()
        {
            return configuration["Registration:EmailConfirmationUrl"] ?? "https://learnword.online/confirm";
        }

        private string GetPasswordResetBaseUrl()
        {
            return configuration["PasswordRecovery:ResetPasswordUrl"] ?? "https://learnword.online/reset-password";
        }
    }
}
