using IdentityService.Authorization.Authorization;
using IdentityService.Authorization.Models.Authentication;
using IdentityService.Authorization.Services;
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
        private readonly RefreshTokenService refreshTokenService;

        public AuthenticationController(UserManager<LwIdentityUser> userManager,
            JwtHandler jwtHandler, RefreshTokenService refreshTokenService) 
        {
            this.userManager = userManager;
            this.jwtHandler = jwtHandler;
            this.refreshTokenService = refreshTokenService;
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

            var ipAddress = HttpContext.Connection.LocalIpAddress?.ToString() ?? string.Empty;
            var signingCredentials = jwtHandler.GetSigningCredentials();
            var claims = jwtHandler.GetClaims(user);
            var tokenOptions = jwtHandler.GenerateTokenOptions(signingCredentials, claims);
            var token = new JwtSecurityTokenHandler().WriteToken(tokenOptions);
            var refreshToken = await refreshTokenService.WriteToken(user, ipAddress);

            return Ok(new LoginResponse()
            {
                Email = user.Email,
                Token = token,
                RefreshToken = refreshToken.Token
            });
        }

        [HttpPost("refresh-token")]
        public async Task<ActionResult> RefreshToken(RefreshTokenRequest refreshToken)
        {
            if (string.IsNullOrEmpty(refreshToken.RefreshToken))
            {
                return BadRequest("Invalid token");
            }

            var user = await refreshTokenService.GetUserByRefreshToken(refreshToken.RefreshToken);

            if (user == null)
            {
                return BadRequest("Invalid token");
            }

            var ipAddress = HttpContext.Connection.LocalIpAddress?.ToString() ?? string.Empty;
            var signingCredentials = jwtHandler.GetSigningCredentials();
            var claims = jwtHandler.GetClaims(user);
            var tokenOptions = jwtHandler.GenerateTokenOptions(signingCredentials, claims);
            var token = new JwtSecurityTokenHandler().WriteToken(tokenOptions);
            var newRefreshToken = await refreshTokenService.RefreshToken(user, refreshToken.RefreshToken, ipAddress);

            return Ok(new LoginResponse()
            {
                Email = user.Email,
                Token = token,
                RefreshToken = newRefreshToken.Token
            });
        }

        [HttpPost("revoke-token")]
        public async Task<ActionResult> RevokeToken(RevokeTokenRequest refreshToken)
        {
            if (string.IsNullOrEmpty(refreshToken.RefreshToken))
                return BadRequest(new { message = "Token is required" });

            var user = await refreshTokenService.GetUserByRefreshToken(refreshToken.RefreshToken);

            if (user == null)
            {
                return BadRequest("Invalid token");
            }

            var ipAddress = HttpContext.Connection.LocalIpAddress?.ToString() ?? string.Empty;

            refreshTokenService.RevokeToken(user, refreshToken.RefreshToken, ipAddress);

            return Ok("Token revoked");
        }
    }
}
