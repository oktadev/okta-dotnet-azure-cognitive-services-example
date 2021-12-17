using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;
using Okta.AspNetCore;
using Okta.Sdk;

namespace OktaProfilePicture.Controllers
{
    public class AccountController : Controller
    {
        private readonly OktaClient _oktaClient;

        public AccountController(OktaClient oktaClient)
        {
            _oktaClient = oktaClient;
        }
        
        public IActionResult LogIn()
        {
            if (!HttpContext.User.Identity.IsAuthenticated)
            {
                return Challenge(OktaDefaults.MvcAuthenticationScheme);
            }

            return RedirectToAction("Index", "Home");
        }
        
        public IActionResult LogOut()
        {
            return new SignOutResult(
                new[]
                {
                    OktaDefaults.MvcAuthenticationScheme,
                    CookieAuthenticationDefaults.AuthenticationScheme,
                },
                new AuthenticationProperties { RedirectUri = "/Home/" });
        }
        
        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var user = await GetOktaUser();
            return View(user);
        }
        
        private async Task<IUser> GetOktaUser()
        {
            var subject = HttpContext.User.Claims.First(claim => claim.Type == JwtRegisteredClaimNames.Sub).Value;
            return await _oktaClient.Users.GetUserAsync(subject);
        }
    }
}