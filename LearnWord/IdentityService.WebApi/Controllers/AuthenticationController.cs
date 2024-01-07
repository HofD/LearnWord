using IdentityService.Authorization.Authorization;
using IdentityService.Authorization.Models.Authentication;
using IdentityService.DAL.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;

namespace IdentityService.WebApi.Controllers
{
    [ApiController]
    [Route("auth")]
    public class AuthenticationController : ControllerBase
    {
        private readonly UserManager<LwIdentityUser> userManager;
        private readonly JwtHandler jwtHandler;

        public AuthenticationController(UserManager<LwIdentityUser> userManager,
            JwtHandler jwtHandler) 
        {
            this.userManager = userManager;
            this.jwtHandler = jwtHandler;
        }

        [HttpPost("login")]
        public async Task<ActionResult<LoginResponse>> Login(LoginRequest loginRequest)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState.ToString());
            }

            var user = await userManager.FindByEmailAsync(loginRequest.Email);

            if (user == null || !await userManager.CheckPasswordAsync(user, loginRequest.Password))
            {
                return Unauthorized();
            }

            if (!await userManager.IsEmailConfirmedAsync(user))
            {
                return Forbid("Email not confirmed.");
            }

            var signingCredentials = jwtHandler.GetSigningCredentials();
            var claims = jwtHandler.GetClaims(user);
            var tokenOptions = jwtHandler.GenerateTokenOptions(signingCredentials, claims);
            var token = new JwtSecurityTokenHandler().WriteToken(tokenOptions);

            return Ok(new LoginResponse()
            {
                Email = user.Email,
                Token = token
            });
        }
    }
}
