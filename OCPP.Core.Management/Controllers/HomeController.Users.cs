using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using OCPP.Core.Database;
using OCPP.Core.Management.Models;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using System.Globalization;
namespace OCPP.Core.Management.Controllers
{
    public partial class HomeController : BaseController
    {
        

        [Authorize]
        public IActionResult Users(string Id, UserViewModel uvm)
        {
            try
            {
                if (User != null && !User.IsInRole(Constants.AdminRoleName) && !User.IsInRole(Constants.SuperAdminRoleName))
                {
                    Logger.LogWarning("User: Request by non-administrator: {0}", User?.Identity?.Name);
                    TempData["ErrMsgKey"] = "AccessDenied";
                    return RedirectToAction("Error", new { Id = "" });
                }

                ViewBag.DatePattern = CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern;
                ViewBag.Language = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
                uvm.CurrentUserId = Id;

                // Construir DbContextOptions usando IConfiguration
                    var optionsBuilder = new DbContextOptionsBuilder<OCPPCoreContext>();
                    optionsBuilder.UseSqlServer(_configuration.GetConnectionString("SqlServer"));

                    // Crear una instancia de OCPPCoreContext usando DbContextOptions
                    using (var dbContext = new OCPPCoreContext(optionsBuilder.Options))
                {
                    Logger.LogTrace("User: Loading Users...");
                    List<Usuario> dbUsers = dbContext.Usuarios.ToList<Usuario>();
                    Logger.LogInformation("User: Found {0} users", dbUsers.Count);

                    Usuario currentUser = null;
                  if (!string.IsNullOrEmpty(Id) && int.TryParse(Id, out int idValue))
                {
                    foreach (Usuario user in dbUsers)
                    {
                        if (user.UserId == idValue)
                        {
                            currentUser = user;
                            Logger.LogTrace("User: Current charge tag: {0} / {1}", user.UserId, user.Username);
                            break;
                        }
                    }
                }

                    if (Request.Method == "POST")
                    {
                        return ProcessUserPostRequest(Id, uvm, dbUsers, currentUser,dbContext);
                    }
                    else
                    {
                        return DisplayUserForm(Id, uvm, dbUsers, currentUser);
                    }
                }
            }
            catch (Exception exp)
            {
                Logger.LogError(exp, "User: Error loading charge tags from database");
                TempData["ErrMessage"] = exp.Message;
                return RedirectToAction("Error", new { Id = "" });
            }
        }

        [Authorize]
        private IActionResult ProcessUserPostRequest(string id, UserViewModel uvm, List<Usuario> dbUsers, Usuario currentUser, OCPPCoreContext dbContext)
        {
            try
            {
                string errorMsg = null;

                if (id == "@")
                {
                    Logger.LogTrace("User: Creating new user...");

                    // Crear nuevo usuario
                    if (string.IsNullOrWhiteSpace(uvm.Username))
                    {
                        errorMsg = _localizer["UsernameRequired"].Value;
                        Logger.LogInformation("User: New => no username entered");
                    }

                    if (string.IsNullOrEmpty(errorMsg))
                    {
                        // Guardar usuario en la BD
                        {
                            Usuario newUser = new Usuario();
                            newUser.Username = uvm.Username;
                            newUser.Password = uvm.Password;
                            newUser.Role = uvm.Role;
                            dbContext.Usuarios.Add(newUser);
                            dbContext.SaveChanges();
                            Logger.LogInformation("User: New => user saved: {0}", uvm.Username);
                        }
                    }
                    else
                    {
                        ViewBag.ErrorMsg = errorMsg;
                        return View("UserDetail", uvm);
                    }
                }
                else if (currentUser != null && currentUser.UserId.ToString() == id)
                {
                    // Editar usuario existente
                    currentUser.Username = uvm.Username;
                    currentUser.Password = uvm.Password;
                    currentUser.Role = uvm.Role;
                    dbContext.SaveChanges();
                    Logger.LogInformation("User: Edit => user saved: {0} / {1}", uvm.UserId, uvm.Username);
                }

                return RedirectToAction("Users", new { id = "" });
            }
            catch (Exception exp)
            {
                Logger.LogError(exp, "User: Error processing user POST request");
                TempData["ErrMessage"] = exp.Message;
                return RedirectToAction("Error", new { Id = "" });
            }
        }

         [Authorize]
        private IActionResult DisplayUserForm(string Id, UserViewModel uvm, List<Usuario> dbUsers, Usuario currentUser)
        {
            // Listar todas las etiquetas de carga
            uvm = new UserViewModel();
            uvm.Usuarios = dbUsers;
            uvm.CurrentUserId = Id;

            if (currentUser != null)
            {
                uvm.UserId = currentUser.UserId;
                uvm.Username = currentUser.Username;
                uvm.Password = currentUser.Password;
                uvm.Role = currentUser.Role;
            }

            string viewName = (Id=="@") ? "UserDetail" : "UserList";
            return View(viewName, uvm);
        }


        [Authorize]
        public IActionResult EditUser(string id, UserViewModel uvm)
        {
            try
            {
                if (User != null && !User.IsInRole(Constants.AdminRoleName) && !User.IsInRole(Constants.SuperAdminRoleName))
                {
                    Logger.LogWarning("EditUser: Request by non-administrator: {0}", User?.Identity?.Name);
                    TempData["ErrMsgKey"] = "AccessDenied";
                    return RedirectToAction("Error", new { Id = "" });
                }

                ViewBag.DatePattern = CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern;
                ViewBag.Language = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
                uvm.CurrentUserId = id;

                // Construir DbContextOptions usando IConfiguration
                var optionsBuilder = new DbContextOptionsBuilder<OCPPCoreContext>();
                optionsBuilder.UseSqlServer(_configuration.GetConnectionString("SqlServer"));

                // Crear una instancia de OCPPCoreContext usando DbContextOptions
                using (var dbContext = new OCPPCoreContext(optionsBuilder.Options))
                {
                    Usuario currentUser = null;
                    if (!string.IsNullOrEmpty(id))
                    {
                        // Convertir id a mayúsculas
                        string idUpper = id.ToUpper();
                        currentUser = dbContext.Usuarios.FirstOrDefault(u => u.UserId.ToString().ToUpper() == idUpper);
                    }

                    if (currentUser != null)
                    {
                        if (Request.Method == "POST")
                        {
                            // Editar el usuario existente
                            currentUser.Username = uvm.Username;
                            currentUser.Password = uvm.Password;
                            currentUser.Role = uvm.Role;
                            dbContext.SaveChanges();
                            Logger.LogInformation("EditUser: user edited: {0} / {1}", uvm.UserId, uvm.Username);

                            return RedirectToAction("Users", new { id = "" });
                        }
                        else
                        {
                            // Rellenar el modelo de vista con los datos actuales del usuario
                            uvm.UserId = currentUser.UserId;
                            uvm.Username = currentUser.Username;
                            uvm.Password = currentUser.Password;
                            uvm.Role = currentUser.Role;

                            return View("UserDetail", uvm);
                        }
                    }
                    else
                    {
                        TempData["ErrMsgKey"] = "UserNotFound";
                        return RedirectToAction("Error", new { Id = "" });
                    }
                }
            }
            catch (Exception exp)
            {
                Logger.LogError(exp, "EditUser: Error editing user");
                TempData["ErrMessage"] = exp.Message;
                return RedirectToAction("Error", new { Id = "" });
            }
        }

        [Authorize]
        [HttpPost]
        public IActionResult DeleteUser(string id)
        {
            try
            {
                if (User != null && !User.IsInRole(Constants.AdminRoleName) && !User.IsInRole(Constants.SuperAdminRoleName))
                {
                    Logger.LogWarning("DeleteUser: Request by non-administrator: {0}", User?.Identity?.Name);
                    TempData["ErrMsgKey"] = "AccessDenied";
                    return RedirectToAction("Error", new { Id = "" });
                }

                // Construir DbContextOptions usando IConfiguration
                var optionsBuilder = new DbContextOptionsBuilder<OCPPCoreContext>();
                optionsBuilder.UseSqlServer(_configuration.GetConnectionString("SqlServer"));

                // Crear una instancia de OCPPCoreContext usando DbContextOptions
                using (var dbContext = new OCPPCoreContext(optionsBuilder.Options))
                {
                    // Obtener todos los usuarios y filtrar en memoria
                    List<Usuario> allUsers = dbContext.Usuarios.ToList();
                    Usuario currentUser = allUsers.FirstOrDefault(u => u.UserId.ToString().Equals(id, StringComparison.InvariantCultureIgnoreCase));

                    if (currentUser != null)
                    {
                        dbContext.Usuarios.Remove(currentUser);
                        dbContext.SaveChanges();
                        Logger.LogInformation("DeleteUser: user deleted: {0} / {1}", currentUser.UserId, currentUser.Username);

                        return RedirectToAction("Users", new { id = "" });
                    }
                    else
                    {
                        TempData["ErrMsgKey"] = "UserNotFound";
                        return RedirectToAction("Error", new { Id = "" });
                    }
                }
            }
            catch (Exception exp)
            {
                Logger.LogError(exp, "DeleteUser: Error deleting user");
                TempData["ErrMessage"] = exp.Message;
                return RedirectToAction("Error", new { Id = "" });
            }
        }
    }
}
