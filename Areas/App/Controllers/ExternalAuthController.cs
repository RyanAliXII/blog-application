using System.Net;
using System.Security.Claims;
using BlogApplication.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BlogApplication.Areas.App.Controllers{

    [Area("App")]
    public class ExternalAuthController: Controller{
        private readonly ILogger<ExternalAuthController> _logger;
        private readonly SignInManager<User> _signInManager;
        private readonly UserManager<User> _userManager;
        private readonly IAuthenticationSchemeProvider _authSchemeProvider;
        public ExternalAuthController(ILogger<ExternalAuthController> logger, SignInManager<User> signInManager, UserManager<User> userManager, IAuthenticationSchemeProvider authSchemeProvider){
            _logger = logger;
            _signInManager = signInManager;
            _userManager = userManager;
            _authSchemeProvider = authSchemeProvider; 
         }
        [Route("app/external-auth")]
        public async Task<IActionResult> ExternalAuth(string provider, string returnUrl = ""){
            try{
                /*
                    Initiates external authentication by redirecting the user to the chosen provider's login page (e.g., Google).
                    The 'redirectUrl' is crucial because, after a successful login, the provider will redirect the user to this URL.
                    This URL should point to a route in your application that handles the authentication logic after 
                    the external login (see the method in this class named 'ExternalAuthCallback'). 
                */

                //  If 'returnUrl' is not specified, the user will be redirected to the dashboard ('/app/dashboard') after login.
                returnUrl = string.IsNullOrEmpty(returnUrl) ? "/app/dashboard" : returnUrl;
                var authScheme = await _authSchemeProvider.GetSchemeAsync(provider);

                // Validate authentication scheme if supported and configured.
                if(authScheme is null){
                    _logger.LogError("Authentication schem is not supported {Provider}", provider);
                    return RedirectToAction("Index", "Login");  
                }

                var redirectUrl = Url.Action("ExternalAuthCallback", "ExternalAuth", new {
                    returnUrl,
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
        public async Task<IActionResult>ExternalAuthCallback(string? returnUrl, string remoteError = ""){
            try{
                //If returnUrl is empty then redirect user to Dashboard('/app/dashboard')
                returnUrl = string.IsNullOrEmpty(returnUrl) ? "/app/dashboard" : returnUrl;

                if(!string.IsNullOrEmpty(remoteError)){
                    _logger.LogError("Remote Error : {RemoteError}", remoteError);
                    return RedirectToAction("Index", "Login"); 
                } 
               
                // After successful external authentication, Get login information
                var info = await _signInManager.GetExternalLoginInfoAsync();
                if(info is null) {
                    _logger.LogError("External login info is null");
                    return RedirectToAction("Index", "Login"); 
                }
                /*  
                  Sign-in user using external login provider. This will fail if this is the first time that the user sign-in with the provider 
                  because the login method is still not yet added in the UserLogin table. (See https://stackoverflow.com/questions/35155447/the-aspnetuserlogins-table-identity)
                */
                var signInResult = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, 
                info.ProviderKey, isPersistent: true);
                
                if(signInResult.Succeeded) return Redirect(returnUrl);
                
                var email = info.Principal.FindFirstValue(ClaimTypes.Email);
                var givenName = info.Principal.FindFirstValue(ClaimTypes.GivenName);
                var surname = info.Principal.FindFirstValue(ClaimTypes.Surname);

                if(string.IsNullOrEmpty(email)) return RedirectToAction("Index", "Login"); 
                
                /* 
                    If external login is unsuccessful, attempt to find the user by email. 
                    If the user is not found, create a new one.
                */
                var user = await _userManager.FindByEmailAsync(email);
                if(user is null){
                    user = new(){
                        Email = email,
                        GivenName = givenName ?? "",
                        Surname = surname ?? "",
                        EmailConfirmed = true,
                        UserName = email,
                    };
                   var createResult =  await _userManager.CreateAsync(user);
                   if(!createResult.Succeeded){
                    _logger.LogError("User creation failed for user with email '{Email}' and named '{GivenName} {Surname}' ", email, givenName, surname);
                     return RedirectToAction("Index", "Login");
                   }
                 
                }
                //Bind the external login to the user.
                var createExternalLoginResult =  await _userManager.AddLoginAsync(user, info);
                if(!createExternalLoginResult.Succeeded){
                    _logger.LogError("External login creation failed for user with email '{Email}' and named '{GivenName} {Surname}' ", email, givenName, surname);
                    return RedirectToAction("Index", "Login");
                }
                // Sign in the user after successfully binding the external login.
                await _signInManager.SignInAsync(user, isPersistent:true);
                return Redirect(returnUrl);
         }catch(Exception ex){
             _logger.LogError(ex.Message);
             return RedirectToAction("Index", "Login");
         }
    }
    }
}