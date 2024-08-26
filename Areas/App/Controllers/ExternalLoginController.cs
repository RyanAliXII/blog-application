using System.Security.Claims;
using BlogApplication.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BlogApplication.Areas.App.Controllers{
    public class ExternalAuthController: Controller{
        private readonly ILogger<ExternalAuthController> _logger;
        private readonly SignInManager<User> _signInManager;
        private readonly UserManager<User> _userManager;
         public ExternalAuthController(ILogger<ExternalAuthController> logger, SignInManager<User> signInManager, UserManager<User> userManager){
            _logger = logger;
            _signInManager = signInManager;
            _userManager = userManager;
         }
         [Route("app/external-auth")]
          public IActionResult ExternalAuth(string provider, string returnUrl = ""){
            try{
            var redirectUrl = Url.Action("ExternalLoginCallback", "Login", new {
                            ReturnUrl = returnUrl,
            });
             var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
             return new ChallengeResult(provider, properties);
            }
            catch(Exception ex){
                _logger.LogError(ex.Message);
                return RedirectToAction("Index", "Login");
            }
           
        }
        [Route("app/external-auth-callback")]
        public async Task<IActionResult> ExternalAuthCallback(string returnUrl = "", string remoteError = ""){
            try{
                if(!string.IsNullOrEmpty(remoteError)) return RedirectToAction("Index", "Login"); 
                var info = await _signInManager.GetExternalLoginInfoAsync();
                if(info is null) return RedirectToAction("Login", "Login");
                var signInResult = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, 
                info.ProviderKey, isPersistent: false, bypassTwoFactor: true);
            
                if(signInResult.Succeeded) return RedirectToAction("Index", "Dashboard");
                var email = info.Principal.FindFirstValue(ClaimTypes.Email);
                
                if(string.IsNullOrEmpty(email)) return RedirectToAction("Index", "Login"); 

                var user = await _userManager.FindByEmailAsync(email);

                if(user is null){
                    user = new(){
                        Email = email,
                        EmailConfirmed = true,
                        UserName = email
                    };
                    await _userManager.CreateAsync(user);
                }
            await _signInManager.SignInAsync(user, isPersistent:true);
            return RedirectToAction("Index", "Dashboard");
         }catch(Exception ex){
             _logger.LogError(ex.Message);
             return RedirectToAction("Index", "Login");
         }
    }
    }
}