using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OCPP.Core.Database;
using OCPP.Core.Management.Models;
using System.Linq;
using Microsoft.EntityFrameworkCore;


namespace OCPP.Core.Management.Controllers
{
    [Authorize]
    public class AccountController : BaseController
    {
        private readonly OCPPCoreContext _dbContext;

        public AccountController(
            UserManager userManager,
            ILoggerFactory loggerFactory,
            IConfiguration config,
            OCPPCoreContext dbContext) : base(userManager, loggerFactory, config)
        {
            Logger = loggerFactory.CreateLogger<AccountController>();
            _dbContext = dbContext;
        }

        // GET: /Account/Login
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(UserViewModel uvm, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (ModelState.IsValid)
            {
                var user = await _dbContext.Usuarios
                    .Where(u => u.Username == uvm.Username)
                    .FirstOrDefaultAsync();

                if (user != null && user.Password == uvm.Password)
                {
                    await UserManager.SignIn(HttpContext, uvm, false);
                    Logger.LogInformation("User '{0}' logged in", uvm.Username);
                    
                    // Limpiar los datos del modelo de usuario (opcional)
                    uvm = new UserViewModel();

                    // Redirigir al usuario a la página deseada después del inicio de sesión exitoso
                    return RedirectToLocal(returnUrl);
                }
                else
                {
                    Logger.LogInformation("Invalid login attempt: User '{0}'", uvm.Username);
                    ModelState.AddModelError(string.Empty, "Invalid login attempt");
                }
            }

            // Si llegamos aquí, algo falló o la autenticación no fue exitosa, redirigir al usuario a la página de inicio de sesión
            return View(uvm);
        }



        [AllowAnonymous]
        public async Task<IActionResult> Logout(UserViewModel userModel)
        {
            Logger.LogInformation("Signing out user '{0}'", userModel.Username);
            await UserManager.SignOut(HttpContext);
            return RedirectToAction(nameof(Login));
        }

        private IActionResult RedirectToLocal(string returnUrl)
        {
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction(nameof(HomeController.Index), "Home");
            }
        }
    }
}
