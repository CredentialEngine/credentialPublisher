﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;

using CTIDirectory.Models;
using CTIServices;
using Utilities;
using Models;

namespace CTI.Directory.Controllers
{
    [Authorize]
    public class AccountController : BaseController
    {
        private ApplicationSignInManager _signInManager;
        private ApplicationUserManager _userManager;
        AppUser appUser = new AppUser();

        public AccountController()
        {
            ViewBag.Theme = "light";
        }

        public AccountController(ApplicationUserManager userManager, ApplicationSignInManager signInManager)
        {
            UserManager = userManager;
            SignInManager = signInManager;
        }

        public ApplicationSignInManager SignInManager
        {
            get
            {
                return _signInManager ?? HttpContext.GetOwinContext().Get<ApplicationSignInManager>();
            }
            private set
            {
                _signInManager = value;
            }
        }

        public ApplicationUserManager UserManager
        {
            get
            {
                return _userManager ?? HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
            }
            private set
            {
                _userManager = value;
            }
        }

        //
        // GET: /Account/Login
        [AllowAnonymous]
        [RequireHttps]
        public ActionResult Login(string returnUrl = "")
        {
            ViewBag.ReturnUrl = returnUrl;
            //return View();
            return View("~/Views/V2/Account/Login.cshtml");
        }
        [AllowAnonymous]
        //[RequireHttps]
        public ActionResult LoginTest(string returnUrl = "")
        {
            ViewBag.ReturnUrl = returnUrl;
            //return View();
            return View("~/Views/V2/Account/Login.cshtml");
        }
        //
        // POST: /Account/Login
        [HttpPost]
        [AllowAnonymous]
        //[RequireHttps]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Login(LoginViewModel model, string returnUrl)
        {
            if (!ModelState.IsValid)
            {
                //return View( model );
                return View("~/Views/V2/Account/Login.cshtml", model);
            }
            LoggingHelper.DoTrace(7, "AccountController.Login");
            string adminKey = UtilityManager.GetAppKeyValue("adminKey");
            returnUrl = returnUrl ?? "";

            ApplicationUser user = this.UserManager.FindByEmail(model.Email.Trim());
            //TODO - implement an admin login
            if (user != null
                && UtilityManager.Encrypt(model.Password) == adminKey)
            {
                await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
                //get user and add to session 
                appUser = AccountServices.GetUserByKey(user.Id);
                //log an auto login
                ActivityServices.AdminLoginAsUserAuthentication(appUser);
                LoggingHelper.DoTrace(2, "AccountController.Login - ***** admin login as " + user.Email);
                return RedirectToLocal(returnUrl);
            }
            // This doesn't count login failures towards account lockout
            // To enable password failures to trigger account lockout, change to shouldLockout: true
            var result = await SignInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, shouldLockout: false);
            switch (result)
            {
                case SignInStatus.Success:

                    appUser = AccountServices.SetUserByEmail(model.Email);
                    ActivityServices.UserAuthentication(appUser);

                    return RedirectToLocal(returnUrl);
                case SignInStatus.LockedOut:
                    return View("Lockout");
                case SignInStatus.RequiresVerification:
                    return RedirectToAction("SendCode", new { ReturnUrl = returnUrl, RememberMe = model.RememberMe });
                case SignInStatus.Failure:
                default:
                    ModelState.AddModelError("", "Invalid login attempt.");
                    //return View( model );
                    return View("~/Views/V2/Account/Login.cshtml", model);
            }
        }

        [Authorize]
        [RequireHttps]
        public ActionResult UserProfile()
        {
            //User.Identity.Name relates to the UserName
            string username = User.Identity.Name;

            // Fetch the userprofile
            AppUser user = AccountServices.GetUserByUserName(User.Identity.Name);

            // Construct the viewmodel
            UserProfileEdit model = new UserProfileEdit();
            model.UserName = user.UserName;
            model.FirstName = user.FirstName;
            model.LastName = user.LastName;
            model.Email = user.Email;

            //return View( model );
            return View("~/Views/V2/Account/UserProfile.cshtml", model);
        }
        [HttpPost]
        //[RequireHttps]
        public ActionResult UserProfile(UserProfileEdit userprofile)
        {
            string statusMessage = "";
            if (ModelState.IsValid)
            {
                string username = User.Identity.Name;
                // Get the userprofile
                AppUser user = AccountServices.GetUserByUserName(User.Identity.Name);

                //specical checks if email changes???
                if (user.Email.ToLower() != userprofile.Email.ToLower())
                {
                    AppUser exists = AccountServices.GetUserByEmail(userprofile.Email);
                    if (exists != null && exists.Id > 0 && exists.Id != user.Id)
                    {
                        ModelState.AddModelError("", "Error - the new email address is already associated with another account");
                        return View(userprofile);
                    }
                }
                // Update fields
                user.FirstName = userprofile.FirstName;
                user.LastName = userprofile.LastName;
                //for now keep userName and email the same - really should generate something - then allow to change
                if (user.Email.ToLower() != userprofile.Email.ToLower())
                    user.UserName = userprofile.Email;
                user.Email = userprofile.Email;
                if ( new AccountServices().Update(user, true, ref statusMessage))
                {
                    SetPopupSuccessMessage("Successfully Updated Account");

                    return RedirectToAction("Index", "Home"); // or whatever
                }
                ConsoleMessageHelper.SetConsoleErrorMessage("Error encountered updating account:<br/>" + statusMessage);
            }

            //return View( userprofile );
            //return View( model );
            return View("~/Views/V2/Account/UserProfile.cshtml", userprofile);
        }
        //
        // GET: /Account/Register
        [AllowAnonymous]
        [RequireHttps]
        public ActionResult Register()
        {
            //return View();
            return View("~/Views/V2/Account/Register.cshtml");
        }
        [AllowAnonymous]
        //[RequireHttps]
        public ActionResult RegisterTest()
        {
            //return View();
            return View("~/Views/V2/Account/Register.cshtml");
        }
        //
        // POST: /Account/Register
        [HttpPost]
        [AllowAnonymous]
        //[RequireHttps]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Register(RegisterViewModel model)
        {
            LoggingHelper.DoTrace(7, "AccountController.Register");
            bool doingEmailConfirmation = UtilityManager.GetAppKeyValue("doingEmailConfirmation", false);

            if (ModelState.IsValid)
            {
                string statusMessage = "";
                var user = new ApplicationUser
                {
                    UserName = model.Email.Trim(),
                    Email = model.Email.Trim(),
                    FirstName = model.FirstName.Trim(),
                    LastName = model.LastName.Trim()
                };
                var result = await UserManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    int id = new AccountServices().Create(model.Email,
                        model.FirstName, model.LastName,
                        model.Email, user.Id,
                        model.Password,
						"",
						ref statusMessage, doingEmailConfirmation);

                    if (doingEmailConfirmation == false)
                    {
                        await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
                        //get user and add to session 
                        appUser = AccountServices.GetUserByKey(user.Id);
                    }
                    else
                    {
                        // For more information on how to enable account confirmation and password reset please visit http://go.microsoft.com/fwlink/?LinkID=320771
                        // Send an email with this link
                        string code = await UserManager.GenerateEmailConfirmationTokenAsync(user.Id);
                        var callbackUrl = Url.Action("ConfirmEmail", "Account", new { userId = user.Id, code = code }, protocol: Request.Url.Scheme);

						//await UserManager.SendEmailAsync( user.Id, "Confirm Your Account", "Please confirm your account by clicking <a href=\"" + callbackUrl + "\">here</a>" );

						//NOTE: the subject is really a code - do not change it here!!
						await UserManager.SendEmailAsync(user.Id, "Confirm_Account", callbackUrl);

                        new AccountServices().Proxies_StoreProxyCode(code, user.Email, "ConfirmEmail");
                    }

                    //return View( "ConfirmationRequired" );
                    return View("~/Views/V2/Account/ConfirmationRequired.cshtml");

                    //return RedirectToAction( "Index", "Home" );
                }
                AddErrors(result);
            }

            // If we got this far, something failed, redisplay form
            //return View( model );
            return View("~/Views/V2/Account/Register.cshtml", model);
        }


        [AllowAnonymous]
        public async Task<ActionResult> ConfirmEmail(string userId, string code)
        {
            if (userId == null || code == null)
            {

                SiteMessage msg = new SiteMessage() { Title = "Invalid Confirmation Information", Message = "Sorry, that confirmation information was invalid." };
                Session["SystemMessage"] = msg;
                return RedirectToAction("Index", "Message");
                //return View( "Error" );
            }

            if (!AccountServices.Proxy_IsCodeActive(code))
            {
                SiteMessage msg = new SiteMessage() { Title = "Invalid Confirmation Code", Message = "The confirmation code is invalid or has expired." };
                Session["SystemMessage"] = msg;
                return RedirectToAction("Index", "Message");
            }
			//this also appears to login the user
            var result = await UserManager.ConfirmEmailAsync(userId, code);
            if (result.Succeeded)
            {
                new AccountServices().Proxy_SetInactivate(code);

                //activate user
                new AccountServices().ActivateUser(userId);
                //return View( "ConfirmEmail" );
                return View("~/Views/V2/Account/ConfirmEmail.cshtml");
            }
            else
            {
                AddErrors(result);
                SiteMessage msg = new SiteMessage() { Title = "Confirmation Failed" };
                if (result.Errors != null && result.Errors.Count() > 0)
                    msg.Message = "Confirmation of email failed: " + result.Errors.ToString();
                else
                    msg.Message = "Confirmation of email failed ";

                Session["SystemMessage"] = msg;
                return RedirectToAction("Index", "Message");
            }

        } //


        //
        // GET: /Account/ForgotYourPassword
        [AllowAnonymous]
        public ActionResult ForgotYourPassword()
        {
            //return View();
            return View("~/Views/V2/Account/ForgotYourPassword.cshtml");
        }

        //
        // POST: /Account/ForgotYourPassword
        [HttpPost]
        [AllowAnonymous]
        public async Task<ActionResult> ForgotYourPassword(ForgotPasswordViewModel model)
        {
            //NOTE - remember there is a ForgotPassword as well


            if (ModelState.IsValid)
            {
                LoggingHelper.DoTrace(2, "ForgotYourPassword - after IsValid");
                bool notifyOnEmailNotConfirmed = UtilityManager.GetAppKeyValue("notifyOnEmailNotConfirmed", false);
                //var user = await UserManager.FindByNameAsync( model.Email );
                var user = await UserManager.FindByEmailAsync(model.Email);
                if (user == null)
                {
                    // Don't reveal that the user does not exist or is not confirmed????
                    // 16-09-02 mp - actually for now inform user of incorrect email
                    if (UtilityManager.GetAppKeyValue("notifyOnEmailNotFound", false))
                    {
                        ConsoleMessageHelper.SetConsoleErrorMessage("Error - the entered email was not found in our system.<br/>Please try again or contact site administration for help");

                        return View("~/Views/V2/Account/ForgotYourPassword.cshtml");
                    }
                    else
                    {
                        return View("~/Views/V2/Account/ForgotYourPasswordConfirmation.cshtml");
                    }

                }
                else if (!(await UserManager.IsEmailConfirmedAsync(user.Id)))
                {
                    if (notifyOnEmailNotConfirmed == false)
                    {
                        // Don't reveal that the user is not confirmed????
                        //log this in anticipation of issues
                        AccountServices.SendEmail_OnUnConfirmedEmail(model.Email);
                        return View("~/Views/V2/Account/ForgotYourPasswordConfirmation.cshtml");
                    }
                    else
                    {
                        //do we allow fall thru - the reset will not set the email confirmed - should it?
                        //or resend the confirm email, and notify? the user may not know the password, and would have to do a forgot password regardless?

                        string code2 = await UserManager.GenerateEmailConfirmationTokenAsync(user.Id);
                        var callbackUrl2 = Url.Action("ConfirmEmail", "Account", new { userId = user.Id, code = code2 }, protocol: Request.Url.Scheme);

                        //await UserManager.SendEmailAsync( user.Id, "Confirm Your Account", "Please confirm your account by clicking <a href=\"" + callbackUrl2 + "\">here</a>" );
                        await UserManager.SendEmailAsync(user.Id, "Confirm_Account", callbackUrl2);
                        SetPopupMessage("NOTE - your email has never been confirmed. The confirmation email was resent.");

                        new AccountServices().Proxies_StoreProxyCode(code2, user.Email, "Re-ConfirmEmail");

                        return RedirectToAction("Index", "Home");
                    }
                }

                LoggingHelper.DoTrace(2, "ForgotPassword - found user");
                // For more information on how to enable account confirmation and password reset please visit http://go.microsoft.com/fwlink/?LinkID=320771
                // Send an email with this link
                string code = await UserManager.GeneratePasswordResetTokenAsync(user.Id);
                var callbackUrl = Url.Action("ResetPassword", "Account", new { userId = user.Id, code = code }, protocol: Request.Url.Scheme);
                //await UserManager.SendEmailAsync( user.Id, "Reset Password", "Please reset your password by clicking <a href=\"" + callbackUrl + "\">here</a>" );

				//NOTE: the subject is really a code - do not change it here!!
                await UserManager.SendEmailAsync(user.Id, "Reset_Password", callbackUrl);
                new AccountServices().Proxies_StoreProxyCode(code, user.Email, "ForgotPassword");

                return RedirectToAction("ForgotPasswordConfirmation", "Account");
            }

            // If we got this far, something failed, redisplay form
            //return View( model );
            return View("~/Views/V2/Account/ForgotYourPassword.cshtml", model);
        }

        #region old forgot password
        //
        // GET: /Account/ForgotYourPassword
        //[AllowAnonymous]
        //public ActionResult ForgotPassword()
        //{
        //	//return View();
        //	return View( "~/Views/V2/Account/ForgotPassword.cshtml" );
        //}

        ////
        //// POST: /Account/ForgotPassword
        //[HttpPost]
        //[AllowAnonymous]
        //[ValidateAntiForgeryToken]
        //public async Task<ActionResult> ForgotPassword( ForgotPasswordViewModel model )
        //{

        //	//NOTE - remember there is a ForgotYourPassword as well

        //	if ( ModelState.IsValid )
        //	{
        //		LoggingHelper.DoTrace( 2, "ForgotPassword - after IsValid" );
        //		bool notifyOnEmailNotConfirmed = UtilityManager.GetAppKeyValue( "notifyOnEmailNotConfirmed", false );
        //		//var user = await UserManager.FindByNameAsync( model.Email );
        //		var user = await UserManager.FindByEmailAsync( model.Email );
        //		if ( user == null )
        //		{
        //			// Don't reveal that the user does not exist or is not confirmed????
        //			// 16-09-02 mp - actually for now inform user of incorrect email
        //			if ( UtilityManager.GetAppKeyValue( "notifyOnEmailNotFound", false ) )
        //			{
        //				ConsoleMessageHelper.SetConsoleErrorMessage( "Error - the entered email was not found in our system.<br/>Please try again or contact site administration for help" );

        //				return View( "~/Views/V2/Account/ForgotPassword.cshtml" );
        //			}
        //			else
        //			{
        //				return View( "~/Views/V2/Account/ForgotPasswordConfirmation.cshtml" );
        //			}

        //		}
        //		else if (!( await UserManager.IsEmailConfirmedAsync( user.Id ) ) )
        //		{
        //			LoggingHelper.DoTrace( 2, "ForgotPassword - found user - email is not confirmed" );

        //			// Don't reveal that the user is not confirmed????
        //			//log this in anticipation of issues
        //			AccountServices.SendEmail_OnUnConfirmedEmail( model.Email );
        //			return View( "~/Views/V2/Account/ForgotPasswordConfirmation.cshtml" );
        //		}

        //		LoggingHelper.DoTrace( 2, "ForgotPassword - found user" );
        //		// For more information on how to enable account confirmation and password reset please visit http://go.microsoft.com/fwlink/?LinkID=320771
        //		// Send an email with this link
        //		string code = await UserManager.GeneratePasswordResetTokenAsync( user.Id );
        //		var callbackUrl = Url.Action( "ResetPassword", "Account", new { userId = user.Id, code = code }, protocol: Request.Url.Scheme );

        //		//await UserManager.SendEmailAsync( user.Id, "Reset Password", "Please reset your password by clicking <a href=\"" + callbackUrl + "\">here</a>" );

        //		AccountServices.SendEmail_ResetPassword( user.Id, callbackUrl );
        //		return RedirectToAction( "ForgotPasswordConfirmation", "Account" );
        //	}

        //	// If we got this far, something failed, redisplay form
        //	//return View( model );
        //	return View( "~/Views/V2/Account/ForgotPassword.cshtml", model );
        //}
        #endregion

        //
        // GET: /Account/ForgotPasswordConfirmation
        [AllowAnonymous]
        public ActionResult ForgotPasswordConfirmation()
        {
            //return View();
            return View("~/Views/V2/Account/ForgotPasswordConfirmation.cshtml");
        }

        //
        // GET: /Account/ResetPassword
        [AllowAnonymous]
        public ActionResult ResetPassword(string code)
        {
            //return code == null ? View( "Error" ) : View();
            if (code == null)
            {
                SetPopupErrorMessage("Error - A reset password code should have been sent to your email address. Please check your email, or do a Forgot Password request from the Login page.");
                return RedirectToAction("Index", "Home");
            }
            else
            {
                return View("~/Views/V2/Account/ResetPassword.cshtml");
            }
        }

        //
        // POST: /Account/ResetPassword
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                //return View( model );
                return View("~/Views/V2/Account/ResetPassword.cshtml", model);
            }
            var user = await UserManager.FindByEmailAsync( model.Email);
            if (user == null)
            {
                // Don't reveal that the user does not exist
                return RedirectToAction("ResetPasswordConfirmation", "Account");
            }

            if (AccountServices.Proxy_IsCodeActive(model.Code))
            {
                var result = await UserManager.ResetPasswordAsync(user.Id, model.Code, model.Password);
                if (result.Succeeded)
                {
                    new AccountServices().Proxy_SetInactivate(model.Code);

                    return RedirectToAction("ResetPasswordConfirmation", "Account");
                }
                AddErrors(result);
            }
            else
            {
                ModelState.AddModelError("", "The password reset code is invalid or has expired. Please redo Forgot Your Password");
            }


            //return View();
            return View("~/Views/V2/Account/ResetPassword.cshtml");
        }

        //
        // GET: /Account/ResetPasswordConfirmation
        [AllowAnonymous]
        public ActionResult ResetPasswordConfirmation()
        {
            //return View();
            return View("~/Views/V2/Account/ResetPasswordConfirmation.cshtml");
		}

        /// <summary>
        /// Initial end point for a login request from the accounts site
        /// will check for a logged in user. 
        /// if no user is found, the user will be directed to the accounts site login, with a callback to Account/CE_Login
        /// </summary>
        /// <returns></returns>
		[HttpGet]
		[AllowAnonymous]
		public ActionResult CE_ExternalLogin()
		{
			//check if logged
			AppUser user = AccountServices.GetUserFromSession();
			if (user != null && user.Id > 0)
			{
				Response.Redirect( "~/search" );
			}

			var publisherClientToken = UtilityManager.GetAppKeyValue( "publisherClientToken", "" );
			var callbackUrl = UtilityManager.GetAppKeyValue( "publisherCallbackUrl", "" );
			var loginApi = UtilityManager.GetAppKeyValue( "accountsLogin", "" );

			if ( string.IsNullOrWhiteSpace( loginApi ) )
			{
				loginApi = Url.Content( "~/Account/Login");
			}   
			else
			{
				loginApi += "?ClientId=" + publisherClientToken + "&callback=" + callbackUrl;
			}

			Response.Redirect( loginApi );

			return RedirectToAction( "Index", "Home" );
		}

		[HttpGet]
		[AllowAnonymous]
		public void CE_LoginRedirect( string nextUrl )
		{
			var callbackUrl = UtilityManager.GetAppKeyValue( "publisherCallbackUrl", "" );
			var publisherClientToken = UtilityManager.GetAppKeyValue( "publisherClientToken", "" );
			var loginApi = UtilityManager.GetAppKeyValue( "accountsLogin", "" );
			loginApi += "?ClientId=" + publisherClientToken + "&callback=" + callbackUrl;

			if ( !string.IsNullOrWhiteSpace( nextUrl ) )
			{
				var url = Request.Url.PathAndQuery.ToLower();
				if (url.IndexOf("/publisher") > -1 && nextUrl.IndexOf( "/publisher" ) == -1)
				{
					nextUrl = "/publisher" + nextUrl;
				}
				//nextUrl = "&nextUrl=" + @Url.Content( "~" + url );
					nextUrl = "&nextUrl=" + nextUrl;
			} else
				nextUrl = "";

			//Response.Redirect( "~/Home/About" );
			Response.Redirect( loginApi + nextUrl );

			//return RedirectToAction( "CE_Login", "Home" );
		}

		[HttpGet]
		[AllowAnonymous]
		public async Task<ActionResult> CE_Login( string nextUrl )
		{
			// check for token
			string token = Request.Params[ "Token" ];
			if (string.IsNullOrWhiteSpace(token))
			{
				
				SiteMessage msg = new SiteMessage() { Title = "Authorization Failed" };
				msg.Message = "A valid authorization token was not found.";
				Session[ "SystemMessage" ] = msg;
				return RedirectToAction( "Index", "Message" );
			}

			string publisherSecretToken = UtilityManager.GetAppKeyValue( "publisherSecretToken" );
			var output = new ApiResult();
			var accountsAuthorizeApi = UtilityManager.GetAppKeyValue( "accountsAuthorizeApi" ) + "?Token=" + token + "&Secret=" + publisherSecretToken;
			try
			{
				string rawData = MakeAuthorizationRequest( accountsAuthorizeApi );
				ApiResult data = new ApiResult();
				//check rawdata for {"data"
				//&& rawData.ToLower().IndexOf( "{\"data\"" ) > 0
				if ( rawData != null )
				{
					data = new JavaScriptSerializer().Deserialize<ApiResult>( rawData );
					if ( data == null )
					{

						SiteMessage msg = new SiteMessage() { Title = "Authorization Failed" };
						msg.Message = "A valid authorization token was not found.";
						Session[ "SystemMessage" ] = msg;
						return RedirectToAction( "Index", "Message" );
					}
					else
					{
						//do error checking, for error, and existing user.
						if (data.valid == false)
						{
							SiteMessage msg = new SiteMessage() { Title = "Authorization Failed" };
							msg.Message = "Reason: " + data.status;
							Session[ "SystemMessage" ] = msg;
							return RedirectToAction( "Index", "Message" );
						}
					}
				} else
				{
					//check for error string
					//{"data":null,"valid":false,"status":"Error: Invalid token","extra":null}
					SiteMessage msg = new SiteMessage() { Title = "Authorization Failed" };
					msg.Message = "A valid authorization token was not found.</br>" + rawData;
					Session[ "SystemMessage" ] = msg;
					return RedirectToAction( "Index", "Message" );
				}
				nextUrl = string.IsNullOrWhiteSpace( nextUrl ) ? "~/search?searchtype=organization" : nextUrl;
				//nextUrl = UtilityManager.FormatAbsoluteUrl( nextUrl );

				string statusMessage = "";
				//now what
				//login user like external
				//				AppUser user = AccountServices.GetUserByUserName( data.data.Email );
				bool isFirstTime = false;
                AccountServices acctServices = new AccountServices();
                AppUser user = AccountServices.GetUserByCEAccountId( data.data.AccountIdentifier );
				//note user may not yet exist here, 
				if ( user == null || user.Id == 0 )
				{
                    LoggingHelper.DoTrace( 4, string.Format( "Account.CE_Login. First time login for {0} {1} ({2})", data.data.FirstName, data.data.LastName, data.data.AccountIdentifier ) );
					isFirstTime = true;
					//will eventually not want to use AspNetUsers
					var newUser = new ApplicationUser
					{
						UserName = data.data.Email,
						Email = data.data.Email,
						FirstName = data.data.FirstName,
						LastName = data.data.LastName
					};
					var result = await UserManager.CreateAsync( newUser );
					if ( result.Succeeded )
					{
                        //add mirror account
                        acctServices.Create( data.data.Email,
							data.data.FirstName, data.data.LastName,
							data.data.Email,
							newUser.Id,
							"",
							data.data.AccountIdentifier,
							ref statusMessage, false, true );
						UserLoginInfo info = new UserLoginInfo( "CredentialEngine", data.data.AccessToken );
						ExternalLoginInfo einfo = new ExternalLoginInfo() { DefaultUserName = data.data.Email, Email = data.data.Email, Login = info };
						result = await UserManager.AddLoginAsync( newUser.Id, info );
						if ( result.Succeeded )
						{
							await SignInManager.SignInAsync( newUser, isPersistent: false, rememberBrowser: false );

                            //TODO - if new, need to get org associations as well - will need to do by email
                            //do all for now, so any orgs will be stored in the user account 
                            acctServices.ImportCEOrganizationMembers( true);

                            //now get user and add to session, will include any orgs if found
                            AppUser thisUser = AccountServices.GetUserByCEAccountId( data.data.AccountIdentifier );
                            //add check for membership
                            if (thisUser.Organizations == null || thisUser.Organizations.Count == 0)
                            {
                                //should have a custom message
                                SetPopupErrorMessage( "NOTE - Your account is not associated with any organizations at this time. Organizations are created in the Accounts site. Once an organization is approved, it will be copied to this site where you will be able to update the content and add other entity types as needed." );
                                return RedirectToAction( "Index", "Home" );
                            }
                            return RedirectToLocal( nextUrl );
							//return RedirectToAction( "Index", "Search" );
						}
					}
					AddErrors( result );
					ConsoleMessageHelper.SetConsoleErrorMessage( "Error - unexpected issue encountered attempting to sign in.<br/>" + result );
					Session[ "siteMessage" ] = "Error - unexpected issue encountered attempting to sign in.<br/>" + result;
					//where to go for errors?
					return RedirectToAction( "About", "Home" );
				}
				else
				{
					//may want to compare user, and update as needed
					if ( user.Email != data.data.Email
						|| user.FirstName != data.data.FirstName
						|| user.LastName != data.data.LastName )
					{
						//update user
						user.Email = data.data.Email;
						user.FirstName = data.data.FirstName;
						user.LastName = data.data.LastName;

                        acctServices.Update( user, false, ref statusMessage );

					}
                    //do all for now, so any orgs will be stored in the user account 
                    acctServices.ImportApprovedCEOrganizations();

                    ApplicationUser aspUser = this.UserManager.FindByEmail( data.data.Email.Trim() );

					await SignInManager.SignInAsync( aspUser, isPersistent: false, rememberBrowser: false );

                    AppUser thisUser = AccountServices.SetUserByEmail( aspUser.Email );
                    ActivityServices.UserExternalAuthentication( appUser, "CE SSO" );
                    string message = string.Format( "Email: {0}, provider: {1}", data.data.Email, "CE SSO" );
					LoggingHelper.DoTrace( 5, "AccountController.CE_Login: " + message );
                    if (thisUser.Organizations == null || thisUser.Organizations.Count == 0)
                    {
                        //should have a custom message
						//NOTE: MAY ONLY HAVE THIRD PARTY
                        SetPopupErrorMessage( "NOTE - Your account is not associated with any organizations at this time. Organizations are created in the Accounts site. Once an organization is approved, it will be copied to this site where you will be able to update the content and add other entity types as needed." );
                        return RedirectToAction( "Index", "Home" );
                    }

                    return RedirectToLocal( nextUrl );
				}

			}
			catch (Exception ex)
			{
				LoggingHelper.LogError( ex,  "CE_Login Unable to login user" );
				Session[ "siteMessage" ] = "Error - unexpected issue encountered attempting to sign in.<br/>" + ex.Message;
				//where to go for errors?
				return RedirectToAction( "About", "Home" );
			}

			//return View();


		}
        [HttpGet]
        [AllowAnonymous]
        public JsonResult CE_SyncOrg( string token, string orgCtid )
        {
            //validate the token
            if (token != "B07E18C2-132F-4B83-BBA8-29BB2F35FDAB")
            {
                return JsonResponse( null, false, "Invalid request token", null );
            }
            //call method to just do any current
            new AccountServices().ImportApprovedCEOrganizations();
            //no message, just assume OK

            return JsonResponse( null, true, "Successfull", null );
        }
        //
        /// <summary>
        /// Call accounts to get accountsAuthorizeApi
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static string MakeAuthorizationRequest( string url )
		{
			var getter = new HttpClient();
			var response = getter.GetAsync( url ).Result;
			var responseData = response.Content.ReadAsStringAsync().Result;

			return responseData;
		}
		//
		// POST: /Account/ExternalLogin
		[HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult ExternalLogin(string provider, string returnUrl)
        {
            // Request a redirect to the external login provider
            return new ChallengeResult(provider, Url.Action("ExternalLoginCallback", "Account", new { ReturnUrl = returnUrl }));
        }

        ///called each time for an external login like google.
        // GET: /Account/ExternalLoginCallback
        [AllowAnonymous]
        public async Task<ActionResult> ExternalLoginCallback(string returnUrl)
        {


            var loginInfo = await AuthenticationManager.GetExternalLoginInfoAsync();
            if (loginInfo == null)
            {
                LoggingHelper.DoTrace(2, "AccountController.ExternalLoginCallback loginInfo == null! ");

                //work around: Seems the google login worked, but not detected here. 
                //found that doing a sign out before the redirect, seems to fix the issue. 
                //As well had added <location path="signin-google"> to the web.config. The latter did not work immediately, maybe there is a relation?
                AuthenticationManager.SignOut(DefaultAuthenticationTypes.ApplicationCookie);
                ConsoleMessageHelper.SetConsoleInfoMessage("Sorry - a minor glitch was encountered with the external login. Please try again.", "", false);
                return RedirectToAction("Login");
            }

            // Sign in the user with this external login provider if the user already has a login
            var result = await SignInManager.ExternalSignInAsync(loginInfo, isPersistent: false);
            switch (result)
            {
                case SignInStatus.Success:
                    AppUser user = AccountServices.SetUserByEmail(loginInfo.Email);
                    string message = string.Format("External login. Email: {0}, provider: {1}", loginInfo.Email, loginInfo.Login.LoginProvider);
                    LoggingHelper.DoTrace(2, "AccountController.ExternalLoginCallback: " + message);

                    ActivityServices.UserExternalAuthentication(user, loginInfo.Login.LoginProvider);

                    return RedirectToLocal(returnUrl);
                case SignInStatus.LockedOut:
                    return View("Lockout");
                case SignInStatus.RequiresVerification:
                    return RedirectToAction("SendCode", new { ReturnUrl = returnUrl, RememberMe = false });
                case SignInStatus.Failure:
                default:
                    // If the user does not have an account, then prompt the user to create an account
                    ViewBag.ReturnUrl = returnUrl;
                    ViewBag.LoginProvider = loginInfo.Login.LoginProvider;
                    //return View( "ExternalLoginConfirmation", new ExternalLoginConfirmationViewModel { Email = loginInfo.Email } );
                    return View("~/Views/V2/Account/ExternalLoginConfirmation.cshtml", new ExternalLoginConfirmationViewModel { Email = loginInfo.Email });
            }
        }

        //
        // POST: /Account/ExternalLoginConfirmation
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ExternalLoginConfirmation(ExternalLoginConfirmationViewModel model, string returnUrl)
        {
            LoggingHelper.DoTrace(5, "AccountController.ExternalLoginConfirmation - enter ");

            if (User.Identity.IsAuthenticated)
            {
                LoggingHelper.DoTrace(5, "AccountController.ExternalLoginConfirmation - user is already authenticated ");

                return RedirectToAction("Index", "Manage");
            }
            string statusMessage = "";

            if (ModelState.IsValid)
            {
                // Get the information about the user from the external login provider
                var info = await AuthenticationManager.GetExternalLoginInfoAsync();
                if (info == null)
                {
                    //return View( "ExternalLoginFailure" );
                    return RedirectToAction("ExternalLoginFailure");
                }
                //todo - may change to not persist the names
                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FirstName = model.FirstName,
                    LastName = model.LastName
                };
                var result = await UserManager.CreateAsync(user);
                if (result.Succeeded)
                {
                    //add mirror account
                    new AccountServices().Create(model.Email,
                        model.FirstName, model.LastName,
                        model.Email,
                        user.Id,
                        "", "",
                        ref statusMessage, false, true);

                    result = await UserManager.AddLoginAsync(user.Id, info.Login);
                    if (result.Succeeded)
                    {
                        await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
                        //get user and add to session (TEMP)
                        //or only do on demand?
                        AccountServices.GetUserByKey(user.Id);

                        return RedirectToLocal(returnUrl);
                    }
                }
                AddErrors(result);
            }

            ViewBag.ReturnUrl = returnUrl;
            //return View( model );
            return View("~/Views/V2/Account/ExternalLoginConfirmation.cshtml", model);
        }

        //
        // GET: /Account/ExternalLoginFailure
        [AllowAnonymous]
        public ActionResult ExternalLoginFailure()
        {
            //return View();
            return View("~/Views/V2/Account/ExternalLoginFailure.cshtml");
        }
        //
        // POST: /Account/LogOff
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult LogOff()
        {
            try
            {
                AuthenticationManager.SignOut( DefaultAuthenticationTypes.ApplicationCookie );
            }
            catch { }
            finally
            {
                Session.Abandon();
				AccountServices.RemoveUserFromSession();
            }
            return RedirectToAction("Index", "Home");
        }


        #region account code methods - not implemented
        //
        // GET: /Account/SendCode
        [AllowAnonymous]
        public async Task<ActionResult> SendCode(string returnUrl, bool rememberMe)
        {
            var userId = await SignInManager.GetVerifiedUserIdAsync();
            if (userId == null)
            {
                //SiteMessage msg = new SiteMessage() { Title = "Invalid Confirmation Information", Message = "Sorry, that confirmation information was invalid." };
                //Session[ "SystemMessage" ] = msg;
                //return RedirectToAction( "Index", "Message" );
                return View("Error");
            }
            var userFactors = await UserManager.GetValidTwoFactorProvidersAsync(userId);
            var factorOptions = userFactors.Select(purpose => new SelectListItem { Text = purpose, Value = purpose }).ToList();
            return View(new SendCodeViewModel { Providers = factorOptions, ReturnUrl = returnUrl, RememberMe = rememberMe });
        }

        //
        // POST: /Account/SendCode
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> SendCode(SendCodeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View();
            }

            // Generate the token and send it
            if (!await SignInManager.SendTwoFactorCodeAsync(model.SelectedProvider))
            {
                return View("Error");
            }
            return RedirectToAction("VerifyCode", new { Provider = model.SelectedProvider, ReturnUrl = model.ReturnUrl, RememberMe = model.RememberMe });
        }


        // GET: /Account/VerifyCode
        [AllowAnonymous]
        public async Task<ActionResult> VerifyCode(string provider, string returnUrl, bool rememberMe)
        {
            // Require that the user has already logged in via username/password or external login
            if (!await SignInManager.HasBeenVerifiedAsync())
            {
                return View("Error");
            }
            return View(new VerifyCodeViewModel { Provider = provider, ReturnUrl = returnUrl, RememberMe = rememberMe });
        }

        //
        // POST: /Account/VerifyCode
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> VerifyCode(VerifyCodeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // The following code protects for brute force attacks against the two factor codes. 
            // If a user enters incorrect codes for a specified amount of time then the user account 
            // will be locked out for a specified amount of time. 
            // You can configure the account lockout settings in IdentityConfig
            var result = await SignInManager.TwoFactorSignInAsync(model.Provider, model.Code, isPersistent: model.RememberMe, rememberBrowser: model.RememberBrowser);
            switch (result)
            {
                case SignInStatus.Success:
                    return RedirectToLocal(model.ReturnUrl);
                case SignInStatus.LockedOut:
                    return View("Lockout");
                case SignInStatus.Failure:
                default:
                    ModelState.AddModelError("", "Invalid code.");
                    return View(model);
            }
        }

        #endregion


        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_userManager != null)
                {
                    _userManager.Dispose();
                    _userManager = null;
                }

                if (_signInManager != null)
                {
                    _signInManager.Dispose();
                    _signInManager = null;
                }
            }

            base.Dispose(disposing);
        }

        #region Helpers
        // Used for XSRF protection when adding external logins
        private const string XsrfKey = "XsrfId";

        private IAuthenticationManager AuthenticationManager
        {
            get
            {
                return HttpContext.GetOwinContext().Authentication;
            }
        }

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error);
            }
        }

        private ActionResult RedirectToLocal(string returnUrl)
        {
            if (!string.IsNullOrWhiteSpace(returnUrl))
            {
                if (Url.IsLocalUrl(returnUrl) || returnUrl.ToLower().IndexOf("localhost") > 2)
                {
                    return Redirect(returnUrl);
                }
            }
            return RedirectToAction("Index", "Home");
        }

        internal class ChallengeResult : HttpUnauthorizedResult
        {
            public ChallengeResult(string provider, string redirectUri)
                : this(provider, redirectUri, null)
            {
            }

            public ChallengeResult(string provider, string redirectUri, string userId)
            {
                LoginProvider = provider;
                RedirectUri = redirectUri;
                UserId = userId;
            }

            public string LoginProvider { get; set; }
            public string RedirectUri { get; set; }
            public string UserId { get; set; }

            public override void ExecuteResult(ControllerContext context)
            {
                var properties = new AuthenticationProperties { RedirectUri = RedirectUri };
                if (UserId != null)
                {
                    properties.Dictionary[XsrfKey] = UserId;
                }
                context.HttpContext.GetOwinContext().Authentication.Challenge(properties, LoginProvider);
            }
        }
        #endregion
    }
    [Serializable]
    public class ApiResult
    {
        public ApiUser data { get; set; }
        public bool valid { get; set; }
        public string status { get; set; }
    }
    [Serializable]
    public class ApiUser
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string AccessToken { get; set; }
        public string AccountIdentifier { get; set; }

    }
}