﻿using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.Owin.Security;
using MultiShop.Models;
using System.Data.Entity;
using Microsoft.Extensions.Logging;
using Microsoft.Owin.Logging;

namespace MultiShop.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        MultiShopDbContext db = new MultiShopDbContext();

        private readonly ILogger<AccountController> _logger;

        public AccountController(ILogger<AccountController> logger)
        {
            _logger = logger;
        }
        public AccountController()
            : this(new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(new ApplicationDbContext())))
        {
        }

        public AccountController(UserManager<ApplicationUser> userManager)
        {
            UserManager = userManager;
        }

        public UserManager<ApplicationUser> UserManager { get; private set; }

        public ActionResult Edit()
        {
            var model = db.Customers.Find(User.Identity.Name);
            return View(model);
        }
        [HttpPost]
        public ActionResult Edit(Customer model)
        {
            var f = Request.Files["upPhoto"];
            if (f != null && f.ContentLength > 0)
            {
                var ext = f.FileName.Substring(f.FileName.LastIndexOf("."));
                var path = Server.MapPath("/Content/img/customers/" + model.Id + ext);
                f.SaveAs(path);
                model.Photo = model.Id + ext;
            }
            db.Entry(model).State = EntityState.Modified;
            db.SaveChanges();

            return View(model);
        }
        //Download source code tại Sharecode.vn
        [AllowAnonymous]
        public ActionResult Activate(String UserName)
        {
            var user = db.Customers.Find(UserName);
            user.Activated = true;
            db.SaveChanges();
            return RedirectToAction("Login");
        }

        [AllowAnonymous]
        public ActionResult Forgot()
        {
            return View();
        }

        [AllowAnonymous, HttpPost]
        public async Task<ActionResult> Forgot(String Id, String Email)
        {
            try
            {
                if (!string.IsNullOrEmpty(Id) && !string.IsNullOrEmpty(Email))
                {
                    var cust = db.Customers.SingleOrDefault(c => c.Id == Id && c.Email == Email);
                    if (cust != null)
                    {
                        // Xác minh thành công, thực hiện các hành động khác ở đây
                        var userManager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(new ApplicationDbContext()));
                        if (userManager != null)
                        {
                            var user = UserManager.FindByName(Id);
                            //var userId = cust.Id;
                            var resultRemovePassword = await userManager.RemovePasswordAsync(user.Id);
                            if (resultRemovePassword.Succeeded)
                            {
                                //using (var dbContext = new ApplicationDbContext())
                                //{
                                    try
                                    {
                                        //_logger.LogInformation("Mật khẩu đã được xóa thành công cho userId: {resultRemovePassword}", resultRemovePassword);
                                        String TokenCode = Guid.NewGuid().ToString();
                                        var resultAddPassword = await userManager.AddPasswordAsync(user.Id, TokenCode);
                                        if (resultAddPassword.Succeeded)
                                        {
                                            // Lưu thay đổi vào cơ sở dữ liệu
                                            await db.SaveChangesAsync();
                                            XMail.Send(Email, "Token Code", TokenCode);
                                            return View("Reset");
                                        }
                                        else
                                        {
                                            ModelState.AddModelError("", "Không thể thêm mật khẩu mới.");
                                            return View();
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine("Đã xảy ra lỗi: " + ex.Message);
                                        return View();
                                    }
                                //}
                            }
                            else
                            {
                                 ModelState.AddModelError("", "Không thể xóa mật khẩu.");
                                 return View();
                            }
                        }
                        else
                        {
                            ModelState.AddModelError("", "Sai thông tin user !");
                            return View();
                        }
                    }
                    else
                    {
                        // Xử lý trường hợp không tìm thấy hoặc thông tin không phù hợp
                        ModelState.AddModelError("", "Không tìm thấy thông tin người dùng !");
                        return View();
                    }
                }
                else
                {
                    // Xử lý trường hợp dữ liệu không hợp lệ
                    ModelState.AddModelError("", "Dữ liệu nhập vào không hợp lệ !");
                    return View();
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Có lỗi xảy ra khi gửi email: " + ex.Message);
            }
        }

        [AllowAnonymous, HttpPost]
        public async Task<ActionResult> Reset(String id, String tokenCode, String newPassword)
        {
            try
            {
                ApplicationUser user = UserManager.FindByName(id);

                if (user != null)
                {
                    var result = await UserManager.ChangePasswordAsync(user.Id, tokenCode, newPassword);
                    if (result.Succeeded)
                    {
                        ModelState.AddModelError("", "Thay đổi mật khẩu thành công!");
                        return View();
                    }
                    else
                    {
                        // Thay đổi mật khẩu không thành công, hiển thị thông báo lỗi cho người dùng
                       ModelState.AddModelError("", "Thay đổi mật khẩu không thành công!");
                       return View();
                    }
                    //return RedirectToAction("Login");
                }
                else
                {
                    // Xử lý trường hợp dữ liệu không hợp lệ
                    ModelState.AddModelError("", "Dữ liệu nhập vào không hợp lệ !");
                    return View();
                }
            }
            catch (Exception ex)
            {
                // Xử lý lỗi khi thay đổi mật khẩu
                ModelState.AddModelError("", "Đã xảy ra lỗi khi thay đổi mật khẩu: " + ex.Message);
                return View();
            }
        }

        //
        // GET: /Account/Login
        [AllowAnonymous]
        //public ActionResult Login(string returnUrl)
        public ActionResult Login()
        {
           //if(returnUrl.Contains("/Admin/"))
           //{
           //    Response.Redirect("/Admin/Account/Login?returnUrl="+returnUrl);
           //}
           //ViewBag.ReturnUrl = returnUrl;
            return View(new LoginViewModel());
        }

        //
        // POST: /Account/Login
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Login(LoginViewModel model, string returnUrl)
        {
            if (ModelState.IsValid)
            {
                var user = await UserManager.FindAsync(model.UserName, model.Password);
                if (user != null)
                {
                    await SignInAsync(user, model.RememberMe);
                    return RedirectToLocal(returnUrl);
                }
                else
                {
                    ModelState.AddModelError("", "Invalid username or password.");
                    return View();
                }
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // GET: /Account/Register
        [AllowAnonymous]
        [HttpGet]
        public ActionResult Register()
        {
            return View(new Customer());
        }

        //
        // POST: /Account/Register
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Register(Customer model)
        {
            //if (!XCaptcha.IsValid)
            //{
            //    ModelState.AddModelError("", "Invalid security code !");
            //    return View(model);
            //}
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser() { UserName = model.Id };
                var result = await UserManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    db.Customers.Add(model);
                    db.SaveChanges();

                    await SignInAsync(user, isPersistent: false);
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    AddErrors(result);
                }
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // POST: /Account/Disassociate
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Disassociate(string loginProvider, string providerKey)
        {
            ManageMessageId? message = null;
            IdentityResult result = await UserManager.RemoveLoginAsync(User.Identity.GetUserId(), new UserLoginInfo(loginProvider, providerKey));
            if (result.Succeeded)
            {
                message = ManageMessageId.RemoveLoginSuccess;
            }
            else
            {
                message = ManageMessageId.Error;
            }
            return RedirectToAction("Manage", new { Message = message });
        }

        //
        // GET: /Account/Manage
        public ActionResult Manage(ManageMessageId? message)
        {
            ViewBag.StatusMessage =
                message == ManageMessageId.ChangePasswordSuccess ? "Your password has been changed."
                : message == ManageMessageId.SetPasswordSuccess ? "Your password has been set."
                : message == ManageMessageId.RemoveLoginSuccess ? "The external login was removed."
                : message == ManageMessageId.Error ? "An error has occurred."
                : "";
            ViewBag.HasLocalPassword = HasPassword();
            ViewBag.ReturnUrl = Url.Action("Manage");
            return View();
        }

        //
        // POST: /Account/Manage
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Manage(ManageUserViewModel model)
        {
            bool hasPassword = HasPassword();
            ViewBag.HasLocalPassword = hasPassword;
            ViewBag.ReturnUrl = Url.Action("Manage");
            if (hasPassword)
            {
                if (ModelState.IsValid)
                {
                    IdentityResult result = await UserManager.ChangePasswordAsync(User.Identity.GetUserId(), model.OldPassword, model.NewPassword);
                    if (result.Succeeded)
                    {
                        return RedirectToAction("Manage", new { Message = ManageMessageId.ChangePasswordSuccess });
                    }
                    else
                    {
                        AddErrors(result);
                    }
                }
            }
            else
            {
                // User does not have a password so remove any validation errors caused by a missing OldPassword field
                ModelState state = ModelState["OldPassword"];
                if (state != null)
                {
                    state.Errors.Clear();
                }

                if (ModelState.IsValid)
                {
                    IdentityResult result = await UserManager.AddPasswordAsync(User.Identity.GetUserId(), model.NewPassword);
                    if (result.Succeeded)
                    {
                        return RedirectToAction("Manage", new { Message = ManageMessageId.SetPasswordSuccess });
                    }
                    else
                    {
                        AddErrors(result);
                    }
                }
            }

            // If we got this far, something failed, redisplay form
            return View(model);
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

        //
        // GET: /Account/ExternalLoginCallback
        [AllowAnonymous]
        public async Task<ActionResult> ExternalLoginCallback(string returnUrl)
        {
            var loginInfo = await AuthenticationManager.GetExternalLoginInfoAsync();
            if (loginInfo == null)
            {
                return RedirectToAction("Login");
            }

            // Sign in the user with this external login provider if the user already has a login
            var user = await UserManager.FindAsync(loginInfo.Login);
            if (user != null)
            {
                await SignInAsync(user, isPersistent: false);
                return RedirectToLocal(returnUrl);
            }
            else
            {
                // If the user does not have an account, then prompt the user to create an account
                ViewBag.ReturnUrl = returnUrl;
                ViewBag.LoginProvider = loginInfo.Login.LoginProvider;
                return View("ExternalLoginConfirmation", new ExternalLoginConfirmationViewModel { UserName = loginInfo.DefaultUserName });
            }
        }

        //
        // POST: /Account/LinkLogin
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult LinkLogin(string provider)
        {
            // Request a redirect to the external login provider to link a login for the current user
            return new ChallengeResult(provider, Url.Action("LinkLoginCallback", "Account"), User.Identity.GetUserId());
        }

        //
        // GET: /Account/LinkLoginCallback
        public async Task<ActionResult> LinkLoginCallback()
        {
            var loginInfo = await AuthenticationManager.GetExternalLoginInfoAsync(XsrfKey, User.Identity.GetUserId());
            if (loginInfo == null)
            {
                return RedirectToAction("Manage", new { Message = ManageMessageId.Error });
            }
            var result = await UserManager.AddLoginAsync(User.Identity.GetUserId(), loginInfo.Login);
            if (result.Succeeded)
            {
                return RedirectToAction("Manage");
            }
            return RedirectToAction("Manage", new { Message = ManageMessageId.Error });
        }

        //
        // POST: /Account/ExternalLoginConfirmation
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ExternalLoginConfirmation(ExternalLoginConfirmationViewModel model, string returnUrl)
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Manage");
            }

            if (ModelState.IsValid)
            {
                // Get the information about the user from the external login provider
                var info = await AuthenticationManager.GetExternalLoginInfoAsync();
                if (info == null)
                {
                    return View("ExternalLoginFailure");
                }
                var user = new ApplicationUser() { UserName = model.UserName };
                var result = await UserManager.CreateAsync(user);
                if (result.Succeeded)
                {
                    result = await UserManager.AddLoginAsync(user.Id, info.Login);
                    if (result.Succeeded)
                    {
                        await SignInAsync(user, isPersistent: false);
                        return RedirectToLocal(returnUrl);
                    }
                }
                AddErrors(result);
            }

            ViewBag.ReturnUrl = returnUrl;
            return View(model);
        }

        //
        // GET: /Account/LogOff
        public ActionResult LogOff()
        {
            AuthenticationManager.SignOut();
            return RedirectToAction("Index", "Home");
        }

        //
        // GET: /Account/ExternalLoginFailure
        [AllowAnonymous]
        public ActionResult ExternalLoginFailure()
        {
            return View();
        }

        [ChildActionOnly]
        public ActionResult RemoveAccountList()
        {
            var linkedAccounts = UserManager.GetLogins(User.Identity.GetUserId());
            ViewBag.ShowRemoveButton = HasPassword() || linkedAccounts.Count > 1;
            return (ActionResult)PartialView("_RemoveAccountPartial", linkedAccounts);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && UserManager != null)
            {
                UserManager.Dispose();
                UserManager = null;
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

        private async Task SignInAsync(ApplicationUser user, bool isPersistent)
        {
            AuthenticationManager.SignOut(DefaultAuthenticationTypes.ExternalCookie);
            var identity = await UserManager.CreateIdentityAsync(user, DefaultAuthenticationTypes.ApplicationCookie);
            AuthenticationManager.SignIn(new AuthenticationProperties() { IsPersistent = isPersistent }, identity);
        }

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error);
            }
        }

        private bool HasPassword()
        {
            var user = UserManager.FindById(User.Identity.GetUserId());
            if (user != null)
            {
                return user.PasswordHash != null;
            }
            return false;
        }

        public enum ManageMessageId
        {
            ChangePasswordSuccess,
            SetPasswordSuccess,
            RemoveLoginSuccess,
            Error
        }

        private ActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction("Index", "Home");
            }
        }

        private class ChallengeResult : HttpUnauthorizedResult
        {
            public ChallengeResult(string provider, string redirectUri) : this(provider, redirectUri, null)
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
                var properties = new AuthenticationProperties() { RedirectUri = RedirectUri };
                if (UserId != null)
                {
                    properties.Dictionary[XsrfKey] = UserId;
                }
                context.HttpContext.GetOwinContext().Authentication.Challenge(properties, LoginProvider);
            }
        }
        #endregion
    }
}