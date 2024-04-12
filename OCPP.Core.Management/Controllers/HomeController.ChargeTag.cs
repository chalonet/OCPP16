using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OCPP.Core.Database;
using OCPP.Core.Management.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using System.Net.Mail;


namespace OCPP.Core.Management.Controllers
{
    
    public partial class HomeController : BaseController
    {

        
        [Authorize]
        public IActionResult ChargeTag(string Id, ChargeTagViewModel ctvm)
        {
            try
            {
                if (User != null && !User.IsInRole(Constants.AdminRoleName) && !User.IsInRole(Constants.SuperAdminRoleName))
                {
                    Logger.LogWarning("ChargeTag: Request by non-administrator: {0}", User?.Identity?.Name);
                    TempData["ErrMsgKey"] = "AccessDenied";
                    return RedirectToAction("Error", new { Id = "" });
                }

                ViewBag.DatePattern = CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern;
                ViewBag.Language = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
                ctvm.CurrentTagId = Id;

                var optionsBuilder = new DbContextOptionsBuilder<OCPPCoreContext>();
                optionsBuilder.UseSqlServer(_configuration.GetConnectionString("SqlServer"));

                using (var dbContext = new OCPPCoreContext(optionsBuilder.Options))
                {
                    var authenticatedAdministratorID = "";
                    if (User.Identity.IsAuthenticated)
                    {
                        var authenticatedUserName = User.Identity.Name;

                        var authenticatedUser = dbContext.Users.FirstOrDefault(u => u.Username == authenticatedUserName);

                        if (authenticatedUser != null)
                        {
                            if (authenticatedUser.Role == Constants.AdminRoleName)
                            {
                                authenticatedAdministratorID = authenticatedUser.UserId.ToString();
                            }
                        }
                    }

                    Logger.LogTrace("ChargePoint: Loading charge points...");

                    List<ChargeTag> dbChargeTags;
                    if (!User.IsInRole(Constants.SuperAdminRoleName))
                    {
                        dbChargeTags = dbContext.ChargeTags
                            .Where(cp => cp.Company.AdministratorId.ToString() == authenticatedAdministratorID)
                            .ToList();
                    }
                    else
                    {
                        dbChargeTags = dbContext.ChargeTags.ToList();
                    }

                    List<Company> companies = dbContext.Companies.ToList();

                    ChargeTag currentChargeTag = null;
                    if (!string.IsNullOrEmpty(Id))
                    {
                        foreach (ChargeTag tag in dbChargeTags)
                        {
                            if (tag.TagId.Equals(Id, StringComparison.InvariantCultureIgnoreCase))
                            {
                                currentChargeTag = tag;
                                Logger.LogTrace("ChargeTag: Current charge tag: {0} / {1}", tag.TagId, tag.TagName);
                                break;
                            }
                        }
                    }


                    if (Request.Method == "POST")
                    {
                        return ProcessChargeTagPostRequest(Id, ctvm, dbChargeTags, currentChargeTag, dbContext, companies);
                    }
                    else
                    {

                        ViewBag.CompanyId = currentChargeTag?.CompanyId;
                        ViewBag.CompanyName = currentChargeTag?.CompanyId != null ? dbContext.Companies.FirstOrDefault(c => c.CompanyId == currentChargeTag.CompanyId)?.Name : "";

                        return DisplayChargeTagForm(Id, ctvm, dbChargeTags, currentChargeTag, companies);
                    }
                }
            }
            catch (Exception exp)
            {
                Logger.LogError(exp, "ChargeTag: Error loading charge tags from database");
                TempData["ErrMessage"] = exp.Message;
                return RedirectToAction("Error", new { Id = "" });
            }
        }

        [Authorize]
        private IActionResult ProcessChargeTagPostRequest(string Id, ChargeTagViewModel ctvm, List<ChargeTag> dbChargeTags, ChargeTag currentChargeTag, OCPPCoreContext dbContext, List<Company> companies)
        {
            try
            {
                string errorMsg = null;

                if (Id == "@")
                {
                    Logger.LogTrace("ChargeTag: Creating new charge tag...");

                    if (string.IsNullOrWhiteSpace(ctvm.TagId))
                    {
                        errorMsg = _localizer["ChargeTagIdRequired"].Value;
                        Logger.LogInformation("ChargeTag: New => no charge tag ID entered");
                    }
                    else
                    {
                        var authenticatedAdministratorID = "";
                        int companyId = -1;
                        if (User.Identity.IsAuthenticated)
                        {
                            var authenticatedUserName = User.Identity.Name;

                            var authenticatedUser = dbContext.Users.FirstOrDefault(u => u.Username == authenticatedUserName);

                            if (authenticatedUser != null)
                            {
                                if (authenticatedUser.Role == Constants.AdminRoleName)
                                {
                                    authenticatedAdministratorID = authenticatedUser.UserId.ToString();

                                    var associatedCompany = dbContext.Companies.FirstOrDefault(c => c.AdministratorId == authenticatedUser.UserId);

                                    if (associatedCompany != null)
                                    {
                                        companyId = associatedCompany.CompanyId;
                                    }
                                }
                            }
                        }

                        if (string.IsNullOrEmpty(errorMsg))
                        {
                            {
                                ChargeTag newTag = new ChargeTag();
                                newTag.TagId = ctvm.TagId;
                                newTag.TagName = ctvm.TagName;
                                newTag.Email = ctvm.Email;
                                newTag.ParentTagId = ctvm.ParentTagId;
                                newTag.ExpiryDate = ctvm.ExpiryDate;
                                newTag.Blocked = ctvm.Blocked;
                                newTag.ChargingTime = ctvm.ChargingTime ?? 0;
                                if (!User.IsInRole(Constants.SuperAdminRoleName))
                                {
                                    newTag.CompanyId = companyId;
                                }
                                else
                                {
                                    newTag.CompanyId = ctvm.CompanyId;
                                }
                                dbContext.ChargeTags.Add(newTag);
                                dbContext.SaveChanges();
                                Logger.LogInformation("ChargeTag: New => charge tag saved: {0} / {1}", ctvm.TagId, ctvm.TagName);
                            }
                        }
                        else
                        {
                            ViewBag.ErrorMsg = errorMsg;
                            return View("ChargeTagDetail", ctvm);
                        }
                    }
                }
                else if (currentChargeTag.TagId == Id)
                {
                    currentChargeTag.TagName = ctvm.TagName;
                    currentChargeTag.Email = ctvm.Email;
                    currentChargeTag.ParentTagId = ctvm.ParentTagId;
                    currentChargeTag.ExpiryDate = ctvm.ExpiryDate;
                    currentChargeTag.Blocked = ctvm.Blocked;
                    currentChargeTag.ChargingTime = ctvm.ChargingTime;
                    currentChargeTag.CompanyId = ctvm.CompanyId;
                    dbContext.SaveChanges();
                    Logger.LogInformation("ChargeTag: Edit => charge tag saved: {0} / {1}", ctvm.TagId, ctvm.TagName);
                }

                return RedirectToAction("ChargeTag", new { Id = "" });
            }
            catch (Exception exp)
            {
                Logger.LogError(exp, "ChargeTag: Error processing charge tag POST request");
                TempData["ErrMessage"] = exp.Message;
                return RedirectToAction("Error", new { Id = "" });
            }
        }

        [Authorize]
        private IActionResult DisplayChargeTagForm(string Id, ChargeTagViewModel ctvm, List<ChargeTag> dbChargeTags, ChargeTag currentChargeTag, List<Company> companies)
        {
            ctvm = new ChargeTagViewModel();
            ctvm.ChargeTags = dbChargeTags;
            ctvm.CurrentTagId = Id;
            ctvm.Companies = companies;

            if (currentChargeTag != null)
            {
                ctvm.TagId = currentChargeTag.TagId;
                ctvm.TagName = currentChargeTag.TagName;
                ctvm.Email = currentChargeTag.Email;
                ctvm.ParentTagId = currentChargeTag.ParentTagId;
                ctvm.ExpiryDate = currentChargeTag.ExpiryDate;
                ctvm.Blocked = (currentChargeTag.Blocked != null) && currentChargeTag.Blocked.Value;
                ctvm.ChargingTime = currentChargeTag.ChargingTime;
                ctvm.CompanyId = currentChargeTag.CompanyId;
            }

            string viewName = (!string.IsNullOrEmpty(ctvm.TagId) || Id == "@") ? "ChargeTagDetail" : "ChargeTagList";
            return View(viewName, ctvm);
        }

        [Authorize]
        public IActionResult EditTag(string id, ChargeTagViewModel ctvm)
        {
            try
            {
                if (!User.IsInRole(Constants.AdminRoleName) && !User.IsInRole(Constants.SuperAdminRoleName))
                {
                    Logger.LogWarning("EditTag: Request by non-administrator: {0}", User?.Identity?.Name);
                    TempData["ErrMsgKey"] = "AccessDenied";
                    return RedirectToAction("Error", new { Id = "" });
                }

                ViewBag.DatePattern = CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern;
                ViewBag.Language = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
                ctvm.CurrentTagId = id;

                var optionsBuilder = new DbContextOptionsBuilder<OCPPCoreContext>();
                optionsBuilder.UseSqlServer(_configuration.GetConnectionString("SqlServer"));

                using (var dbContext = new OCPPCoreContext(optionsBuilder.Options))
                {
                    ChargeTag currentChargeTag = dbContext.ChargeTags.FirstOrDefault(tag => tag.TagId == id);

                    if (currentChargeTag != null)
                    {
                        var companies = dbContext.Companies.ToList();

                        if (Request.Method == "POST")
                        {
                            currentChargeTag.TagName = ctvm.TagName;
                            currentChargeTag.Email = ctvm.Email;
                            currentChargeTag.ParentTagId = ctvm.ParentTagId;
                            currentChargeTag.ExpiryDate = ctvm.ExpiryDate;
                            currentChargeTag.Blocked = ctvm.Blocked;

                            if (User.IsInRole(Constants.SuperAdminRoleName))
                            {
                                currentChargeTag.CompanyId = ctvm.CompanyId;
                            }

                            dbContext.SaveChanges();
                            Logger.LogInformation("EditTag: charge tag edited: {0} / {1}", ctvm.TagId, ctvm.TagName);

                            return RedirectToAction("ChargeTag", new { Id = "" });
                        }
                        else
                        {
                            ctvm.TagId = currentChargeTag.TagId;
                            ctvm.TagName = currentChargeTag.TagName;
                            ctvm.Email = currentChargeTag.Email;
                            ctvm.ParentTagId = currentChargeTag.ParentTagId;
                            ctvm.ExpiryDate = currentChargeTag.ExpiryDate;
                            ctvm.Blocked = (currentChargeTag.Blocked != null) && currentChargeTag.Blocked.Value;

                            ctvm.Companies = companies;

                            return View("ChargeTagDetail", ctvm);
                        }
                    }
                    else
                    {
                        TempData["ErrMsgKey"] = "TagNotFound";
                        return RedirectToAction("Error", new { Id = "" });
                    }
                }
            }
            catch (Exception exp)
            {
                Logger.LogError(exp, "EditTag: Error editing charge tag");
                TempData["ErrMessage"] = exp.Message;
                return RedirectToAction("Error", new { Id = "" });
            }
        }


        [Authorize]
        public IActionResult EditChargingTime(string id)
        {
            try
            {
                var optionsBuilder = new DbContextOptionsBuilder<OCPPCoreContext>();
                optionsBuilder.UseSqlServer(_configuration.GetConnectionString("SqlServer"));

                using (var dbContext = new OCPPCoreContext(optionsBuilder.Options))
                {
                    var currentChargeTag = dbContext.ChargeTags.FirstOrDefault(tag => tag.TagId == id);
                    if (currentChargeTag == null)
                    {
                        TempData["ErrMsgKey"] = "TagNotFound";
                        return RedirectToAction("Error", new { Id = "" });
                    }

                    string tagName = currentChargeTag.TagName;
                    var currentTime = currentChargeTag.ChargingTime;
                    string emailTag = currentChargeTag.Email;

                    ViewData["TagId"] = id;
                    ViewData["CurrentTime"] = currentTime;
                    ViewData["TagName"] = tagName;

                    return View();
                }
            }
            catch (Exception exp)
            {
                Logger.LogError(exp, "EditChargingTime: Error loading data for charging time editing view");
                TempData["ErrMessage"] = exp.Message;
                return RedirectToAction("Error", new { Id = "" });
            }
        }



       [HttpPost]
        public IActionResult EditChargingTime(string id,ChargingTimeViewModel ctvm,string command)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var optionsBuilder = new DbContextOptionsBuilder<OCPPCoreContext>();
                    optionsBuilder.UseSqlServer(_configuration.GetConnectionString("SqlServer"));

                    using (var dbContext = new OCPPCoreContext(optionsBuilder.Options))
                    {
                        var currentChargeTag = dbContext.ChargeTags.FirstOrDefault(tag => tag.TagId == id);

                        string emailTag = currentChargeTag.Email;
                        string tagName = currentChargeTag.TagName;
                        string action = "";

                        if (currentChargeTag != null)
                        {
                            if (command == "add")
                            {
                                currentChargeTag.ChargingTime += ctvm.NewChargingTime;
                                action = "sumado";
                            }
                            else if (command == "subtract")
                            {
                                currentChargeTag.ChargingTime -= ctvm.NewChargingTime;
                                action = "restado";
                            }

                            dbContext.SaveChanges();

                            string Username = User.Identity.Name;
                            
                            var user = dbContext.Users.FirstOrDefault(u => u.Username == Username);

                            if (user != null)
                            {
                                string emailUser= user.Email;
                                string passwordUser = user.Password;
                                int currentTime = currentChargeTag.ChargingTime ?? 0;


                                SendEmail(emailUser,passwordUser, Username, emailTag, tagName, ctvm.NewChargingTime, currentTime,action);
                            }
                            else
                            {
                                TempData["ErrMsgKey"] = "UserNotFound";
                                return RedirectToAction("Error", new { Id = "" });
                            }
                        }
                        else
                        {
                            TempData["ErrMsgKey"] = "TagNotFound";
                            return RedirectToAction("Error", new { Id = "" });
                        }

                        return RedirectToAction("ChargeTag");
                    }
                }
                catch (Exception exp)
                {
                    Logger.LogError(exp, "EditChargingTime: Error processing charge tag POST request");
                    TempData["ErrMessage"] = exp.Message;
                    return RedirectToAction("Error", new { Id = "" });
                }
            }

            return View(ctvm);
        }


       // Método para enviar correo electrónico al usuario
        private void SendEmail(string emailUser, string passwordUser, string Username, string emailTag, string tagName, int NewChargingTime, int currentTime,string action)
        {
            try
            {
                SmtpClient clienteSmtp = new SmtpClient("smtp.outlook.com")
                {
                    Port = 587,
                    Credentials = new System.Net.NetworkCredential(emailUser, passwordUser),
                    EnableSsl = true
                };

                MailMessage mensaje = new MailMessage(emailUser, emailTag)
                {
                    Subject = "Nueva asignación realizada",
                    Body = $"Hola {tagName},\n\n¡Se te ha {action} tiempo!\n\nTotal de tiempo asignado hasta ahora: {currentTime} minutos.\nNuevo tiempo asignado: {NewChargingTime} minutos.\n\n¡Saludos cordiales!\n{Username}"
                };

                clienteSmtp.Send(mensaje);
            }
            catch (Exception ex)
            {
                Console.WriteLine("¡Ups! Hubo un error al enviar el correo electrónico: " + ex.ToString());
            }
        }



        [Authorize]
        [HttpPost]
        public IActionResult DeleteTag(string id)
        {
            try
            {
                if (User != null && !User.IsInRole(Constants.AdminRoleName) && !User.IsInRole(Constants.SuperAdminRoleName))
                {
                    Logger.LogWarning("DeleteTag: Request by non-administrator: {0}", User?.Identity?.Name);
                    TempData["ErrMsgKey"] = "AccessDenied";
                    return RedirectToAction("Error", new { Id = "" });
                }

                var optionsBuilder = new DbContextOptionsBuilder<OCPPCoreContext>();
                optionsBuilder.UseSqlServer(_configuration.GetConnectionString("SqlServer"));

                using (var dbContext = new OCPPCoreContext(optionsBuilder.Options))
                {
                    List<ChargeTag> allChargeTags = dbContext.ChargeTags.ToList();
                    ChargeTag currentChargeTag = allChargeTags.FirstOrDefault(tag => tag.TagId.Equals(id, StringComparison.InvariantCultureIgnoreCase));

                    if (currentChargeTag != null)
                    {
                        dbContext.ChargeTags.Remove(currentChargeTag);
                        dbContext.SaveChanges();
                        Logger.LogInformation("DeleteTag: charge tag deleted: {0} / {1}", currentChargeTag.TagId, currentChargeTag.TagName);

                        return RedirectToAction("ChargeTag", new { Id = "" });
                    }
                    else
                    {
                        TempData["ErrMsgKey"] = "TagNotFound";
                        return RedirectToAction("Error", new { Id = "" });
                    }
                }
            }
            catch (Exception exp)
            {
                Logger.LogError(exp, "DeleteTag: Error deleting charge tag");
                TempData["ErrMessage"] = exp.Message;
                return RedirectToAction("Error", new { Id = "" });
            }
        }
    }
}
