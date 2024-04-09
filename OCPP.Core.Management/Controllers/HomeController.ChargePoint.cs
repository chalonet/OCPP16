

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OCPP.Core.Database;
using OCPP.Core.Management.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace OCPP.Core.Management.Controllers
{
    public partial class HomeController : BaseController
    {
        private readonly IConfiguration _configuration;
        private readonly IStringLocalizer<HomeController> _localizer;

        public HomeController(
            UserManager userManager,
            IStringLocalizer<HomeController> localizer,
            ILoggerFactory loggerFactory,
            IConfiguration configuration) : base(userManager, loggerFactory, configuration)
        {
            _localizer = localizer;
            _configuration = configuration;
            Logger = loggerFactory.CreateLogger<HomeController>();
        }
        [Authorize]
        public IActionResult ChargePoint(string Id, ChargePointViewModel cpvm)
        {
            try
            {
                if (User != null && !User.IsInRole(Constants.AdminRoleName)&& !User.IsInRole(Constants.SuperAdminRoleName))
                {
                    Logger.LogWarning("ChargePoint: Request by non-administrator: {0}", User?.Identity?.Name);
                    TempData["ErrMsgKey"] = "AccessDenied";
                    return RedirectToAction("Error", new { Id = "" });
                }

                cpvm.CurrentId = Id;

                 // Construir DbContextOptions usando IConfiguration
                    var optionsBuilder = new DbContextOptionsBuilder<OCPPCoreContext>();
                    optionsBuilder.UseSqlServer(_configuration.GetConnectionString("SqlServer"));

                    // Crear una instancia de OCPPCoreContext usando DbContextOptions
                    using (var dbContext = new OCPPCoreContext(optionsBuilder.Options))
                {
                    Logger.LogTrace("ChargePoint: Loading charge points...");
                    List<ChargePoint> dbChargePoints = dbContext.ChargePoints.ToList<ChargePoint>();
                    Logger.LogInformation("ChargePoint: Found {0} charge points", dbChargePoints.Count);

                    ChargePoint currentChargePoint = null;
                    if (!string.IsNullOrEmpty(Id))
                    {
                        foreach (ChargePoint cp in dbChargePoints)
                        {
                            if (cp.ChargePointId.Equals(Id, StringComparison.InvariantCultureIgnoreCase))
                            {
                                currentChargePoint = cp;
                                Logger.LogTrace("ChargePoint: Current charge point: {0} / {1}", cp.ChargePointId, cp.Name);
                                break;
                            }
                        }
                    }

                    if (Request.Method == "POST")
                    {
                        if (Id == "@")
                        {
                            return CreateChargePoint(cpvm, dbChargePoints);
                        }
                        else if (currentChargePoint.ChargePointId == Id)
                        {
                            Logger.LogTrace("ChargePoint: Saving charge point '{0}'", Id);
                            currentChargePoint.Name = cpvm.Name;
                            currentChargePoint.Comment = cpvm.Comment;
                            currentChargePoint.Username = cpvm.Username;
                            currentChargePoint.Password = cpvm.Password;
                            currentChargePoint.ClientCertThumb = cpvm.ClientCertThumb;

                            dbContext.SaveChanges();
                            Logger.LogInformation("ChargePoint: Edit => charge point saved: {0} / {1}", cpvm.ChargePointId, cpvm.Name);
                        }

                        return RedirectToAction("ChargePoint", new { Id = "" });
                    }
                    else
                    {
                        cpvm = new ChargePointViewModel();
                        cpvm.ChargePoints = dbChargePoints;
                        cpvm.CurrentId = Id;

                        if (currentChargePoint != null)
                        {
                            cpvm = new ChargePointViewModel();
                            cpvm.ChargePointId = currentChargePoint.ChargePointId;
                            cpvm.Name = currentChargePoint.Name;
                            cpvm.Comment = currentChargePoint.Comment;
                            cpvm.Username = currentChargePoint.Username;
                            cpvm.Password = currentChargePoint.Password;
                            cpvm.ClientCertThumb = currentChargePoint.ClientCertThumb;
                        }

                        string viewName = (!string.IsNullOrEmpty(cpvm.ChargePointId) || Id == "@") ? "ChargePointDetail" : "ChargePointList";
                        return View(viewName, cpvm);
                    }
                }
            }
            catch (Exception exp)
            {
                Logger.LogError(exp, "ChargePoint: Error loading charge points from database");
                TempData["ErrMessage"] = exp.Message;
                return RedirectToAction("Error", new { Id = "" });
            }
        }


        [Authorize]
        public IActionResult CreateChargePoint(ChargePointViewModel cpvm, List<ChargePoint> dbChargePoints)
        {
            try
            {
                string errorMsg = null;

                Logger.LogTrace("ChargePoint: Creating new charge point...");

                // Validaciones para la creación del charge point
                if (string.IsNullOrWhiteSpace(cpvm.ChargePointId))
                {
                    errorMsg = _localizer["ChargePointIdRequired"].Value;
                    Logger.LogInformation("ChargePoint: New => no charge point ID entered");
                }

                if (string.IsNullOrEmpty(errorMsg))
                {
                    // Comprobar si ya existe un charge point con el mismo ID
                    foreach (ChargePoint cp in dbChargePoints)
                    {
                        if (cp.ChargePointId.Equals(cpvm.ChargePointId, StringComparison.InvariantCultureIgnoreCase))
                        {
                            // El ID ya existe
                            errorMsg = _localizer["ChargePointIdExists"].Value;
                            Logger.LogInformation("ChargePoint: New => charge point ID already exists: {0}", cpvm.ChargePointId);
                            break;
                        }
                    }
                }

                if (string.IsNullOrEmpty(errorMsg))
                {
                    // Construir DbContextOptions usando IConfiguration
                    var optionsBuilder = new DbContextOptionsBuilder<OCPPCoreContext>();
                    optionsBuilder.UseSqlServer(_configuration.GetConnectionString("SqlServer"));

                    // Crear una instancia de OCPPCoreContext usando DbContextOptions
                    using (var dbContext = new OCPPCoreContext(optionsBuilder.Options))
                    {
                        ChargePoint newChargePoint = new ChargePoint();
                        newChargePoint.ChargePointId = cpvm.ChargePointId;
                        newChargePoint.Name = cpvm.Name;
                        newChargePoint.Comment = cpvm.Comment;
                        newChargePoint.Username = cpvm.Username;
                        newChargePoint.Password = cpvm.Password;
                        newChargePoint.ClientCertThumb = cpvm.ClientCertThumb;
                        dbContext.ChargePoints.Add(newChargePoint);
                        dbContext.SaveChanges();
                        Logger.LogInformation("ChargePoint: New => charge point saved: {0} / {1}", cpvm.ChargePointId, cpvm.Name);
                    }

                    return RedirectToAction("ChargePoint", new { Id = "" });
                }
                else
                {
                    ViewBag.ErrorMsg = errorMsg;
                    return View("ChargePointDetail", cpvm);
                }
            }
            catch (Exception exp)
            {
                Logger.LogError(exp, "CreateChargePoint: Error creating charge point");
                TempData["ErrMessage"] = exp.Message;
                return RedirectToAction("Error", new { Id = "" });
            }
        }

        
        [Authorize]
        public IActionResult DeleteChargePoint(string id)
        {
            try
            {
                if (User != null && !User.IsInRole(Constants.AdminRoleName) && !User.IsInRole(Constants.SuperAdminRoleName))
                {
                    Logger.LogWarning("DeleteChargePoint: Request by non-administrator: {0}", User?.Identity?.Name);
                    TempData["ErrMsgKey"] = "AccessDenied";
                    return RedirectToAction("Error", new { Id = "" });
                }

                // Construir DbContextOptions usando IConfiguration
                    var optionsBuilder = new DbContextOptionsBuilder<OCPPCoreContext>();
                    optionsBuilder.UseSqlServer(_configuration.GetConnectionString("SqlServer"));

                    // Crear una instancia de OCPPCoreContext usando DbContextOptions
                    using (var dbContext = new OCPPCoreContext(optionsBuilder.Options))
                {
                    var chargePointToDelete = dbContext.ChargePoints
                        .AsEnumerable()
                        .FirstOrDefault(cp => cp.ChargePointId.Equals(id, StringComparison.InvariantCultureIgnoreCase));

                    if (chargePointToDelete == null)
                    {
                        TempData["ErrMsgKey"] = "ChargePointNotFound";
                        return RedirectToAction("Error", new { Id = "" });
                    }

                    dbContext.ChargePoints.Remove(chargePointToDelete);
                    dbContext.SaveChanges();
                    Logger.LogInformation("DeleteChargePoint: Charge point deleted: {0} / {1}", chargePointToDelete.ChargePointId, chargePointToDelete.Name);

                    return RedirectToAction("ChargePoint", new { Id = "" });
                }
            }
            catch (Exception exp)
            {
                Logger.LogError(exp, "DeleteChargePoint: Error deleting charge point");
                TempData["ErrMessage"] = exp.Message;
                return RedirectToAction("Error", new { Id = "" });
            }
        }

        [Authorize]
        public IActionResult EditChargePoint(string id, ChargePointViewModel cpvm)
        {
            try
            {
                if (User != null && !User.IsInRole(Constants.AdminRoleName) && !User.IsInRole(Constants.SuperAdminRoleName))
                {
                    Logger.LogWarning("EditChargePoint: Request by non-administrator: {0}", User?.Identity?.Name);
                    TempData["ErrMsgKey"] = "AccessDenied";
                    return RedirectToAction("Error", new { Id = "" });
                }

                // Construir DbContextOptions usando IConfiguration
                    var optionsBuilder = new DbContextOptionsBuilder<OCPPCoreContext>();
                    optionsBuilder.UseSqlServer(_configuration.GetConnectionString("SqlServer"));

                    // Crear una instancia de OCPPCoreContext usando DbContextOptions
                    using (var dbContext = new OCPPCoreContext(optionsBuilder.Options))
                {
                    var chargePointToEdit = dbContext.ChargePoints
                        .FirstOrDefault(cp => cp.ChargePointId.ToUpper() == id.ToUpper());

                    if (chargePointToEdit == null)
                    {
                        TempData["ErrMsgKey"] = "ChargePointNotFound";
                        return RedirectToAction("Error", new { Id = "" });
                    }

                    if (Request.Method == "POST")
                    {
                        // Validación de datos
                        string errorMsg = ValidateChargePointData(cpvm);
                        if (!string.IsNullOrEmpty(errorMsg))
                        {
                            ViewBag.ErrorMsg = errorMsg;
                            return View("ChargePointDetail", cpvm);
                        }

                        // Actualización de datos del ChargePoint
                        chargePointToEdit.Name = cpvm.Name;
                        chargePointToEdit.Comment = cpvm.Comment;
                        chargePointToEdit.Username = cpvm.Username;
                        chargePointToEdit.Password = cpvm.Password;
                        chargePointToEdit.ClientCertThumb = cpvm.ClientCertThumb;

                        dbContext.SaveChanges();
                        Logger.LogInformation("EditChargePoint: Charge point edited: {0} / {1}", chargePointToEdit.ChargePointId, chargePointToEdit.Name);

                        return RedirectToAction("ChargePoint", new { Id = "" });
                    }
                    else
                    {
                        // Cargar datos del ChargePoint para mostrar en la vista
                        cpvm = new ChargePointViewModel();
                        cpvm.ChargePointId = chargePointToEdit.ChargePointId;
                        cpvm.Name = chargePointToEdit.Name;
                        cpvm.Comment = chargePointToEdit.Comment;
                        cpvm.Username = chargePointToEdit.Username;
                        cpvm.Password = chargePointToEdit.Password;
                        cpvm.ClientCertThumb = chargePointToEdit.ClientCertThumb;

                        return View("ChargePointDetail", cpvm);
                    }
                }
            }
            catch (Exception exp)
            {
                Logger.LogError(exp, "EditChargePoint: Error editing charge point");
                TempData["ErrMessage"] = exp.Message;
                return RedirectToAction("Error", new { Id = "" });
            }
        }

        public string ValidateChargePointData(ChargePointViewModel cpvm)
        {
            if (string.IsNullOrWhiteSpace(cpvm.ChargePointId))
            {
                return _localizer["ChargePointIdRequired"].Value;
            }

            if (cpvm.ChargePointId.Length > 100)
            {
                return _localizer["ChargePointIdTooLong"].Value;
            }

            if (string.IsNullOrWhiteSpace(cpvm.Name))
            {
                return _localizer["NameRequired"].Value;
            }

            if (cpvm.Name.Length > 100)
            {
                return _localizer["NameTooLong"].Value;
            }

            if (!string.IsNullOrWhiteSpace(cpvm.Username) && cpvm.Username.Length > 50)
            {
                return _localizer["UsernameTooLong"].Value;
            }

            if (!string.IsNullOrWhiteSpace(cpvm.Password) && cpvm.Password.Length > 50)
            {
                return _localizer["PasswordTooLong"].Value;
            }

            if (!string.IsNullOrWhiteSpace(cpvm.ClientCertThumb) && cpvm.ClientCertThumb.Length > 100)
            {
                return _localizer["ClientCertThumbTooLong"].Value;
            }

            return null;
        }


    }
}
