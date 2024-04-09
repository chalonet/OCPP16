using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using OCPP.Core.Management.Models;
using OCPP.Core.Database;

namespace OCPP.Core.Management
{
    public class UserManager
    {
        private readonly OCPPCoreContext _dbContext;

        public UserManager(IConfiguration configuration, OCPPCoreContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task SignIn(HttpContext httpContext, UserViewModel user, bool isPersistent = false)
        {
            var usuario = await _dbContext.Usuarios.FirstOrDefaultAsync(u => u.Username == user.Username && u.Password == user.Password);
            if (usuario != null)
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, usuario.Username),
                    new Claim(ClaimTypes.Name, usuario.Username),
                    new Claim(ClaimTypes.Role, usuario.Role) 
                };
                
                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = isPersistent
                };
                
                await httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);
                
                user.Username = null;
                user.Password = null;
            }
        }


        public async Task SignOut(HttpContext httpContext)
        {
            await httpContext.SignOutAsync();
        }

        public async Task<Usuario> GetUser(string username)
        {
            return await _dbContext.Usuarios.FirstOrDefaultAsync(u => u.Username == username);
        }
    }
}
