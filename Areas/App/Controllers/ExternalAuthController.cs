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
            var redirectUrl = Url.Action("ExternalAuthCallback", "ExternalAuth", new {
                ReturnUrl = returnUrl,
            });
             var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
             return new ChallengeResult(provider, properties);
            }
            catch(Exception ex){
                _logger.LogError(ex.Message);
                return RedirectToAction("Index", "App/Login");
            }
           
        }
        [Route("app/external-auth-callback")]
        public async Task<IActionResult> ExternalAuthCallback(string returnUrl = "", string remoteError = ""){
            try{
                if(!string.IsNullOrEmpty(remoteError)){
                    _logger.LogError("Remote Error : {RemoteError}", remoteError);
                    return RedirectToAction("Index", "Login", new {Area = "App"}); 
                } 
              
                var info = await _signInManager.GetExternalLoginInfoAsync();
                if(info is null) {
                    _logger.LogError("External login info is null");
                    return RedirectToAction("Index", "Login", new{Area = "App"}); 
                }
                var signInResult = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, 
                info.ProviderKey, isPersistent: false, bypassTwoFactor: true);
            
                if(signInResult.Succeeded) return RedirectToAction("Index", "Dashboard", new{ Area = "App"});
                var email = info.Principal.FindFirstValue(ClaimTypes.Email);
                var givenName = info.Principal.FindFirstValue(ClaimTypes.GivenName);
                var surname = info.Principal.FindFirstValue(ClaimTypes.Surname);
                if(string.IsNullOrEmpty(email)) return RedirectToAction("Index", "Login", new{Area = "App"}); 

                var user = await _userManager.FindByEmailAsync(email);

                if(user is null){
                    user = new(){
                        Email = email,
                        GivenName = givenName ?? "",
                        Surname = surname ?? "",
                        EmailConfirmed = true,
                        UserName = email
                    };
                   var createResult =  await _userManager.CreateAsync(user);
                   if(!createResult.Succeeded){
                    _logger.LogError("User creation failed for user with email '{Email}' and named '{GivenName} {Surname}' ", email, givenName, surname);
                     return RedirectToAction("Index", "Login", new { Area = "App" });
                   }
                }
            await _signInManager.SignInAsync(user, isPersistent:true);
            return RedirectToAction("Index", "Dashboard", new {Area = "App"});
         }catch(Exception ex){
             _logger.LogError(ex.Message);
             return RedirectToAction("Index", "Login", new {Area = "App"});
         }
    }
    }
}