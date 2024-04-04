

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

                using (OCPPCoreContext dbContext = new OCPPCoreContext(this.Config))
                {
                    Logger.LogTrace("ChargeTag: Loading charge tags...");
                    List<ChargeTag> dbChargeTags = dbContext.ChargeTags.ToList<ChargeTag>();
                    Logger.LogInformation("ChargeTag: Found {0} charge tags", dbChargeTags.Count);

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
                        return ProcessChargeTagPostRequest(Id, ctvm, dbChargeTags, currentChargeTag,dbContext);
                    }
                    else
                    {
                        return DisplayChargeTagForm(Id, ctvm, dbChargeTags, currentChargeTag);
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
        private IActionResult ProcessChargeTagPostRequest(string Id, ChargeTagViewModel ctvm, List<ChargeTag> dbChargeTags, ChargeTag currentChargeTag, OCPPCoreContext dbContext)
        {
            try
            {
                string errorMsg = null;

                if (Id == "@")
                {
                    Logger.LogTrace("ChargeTag: Creating new charge tag...");

                    // Crear nueva etiqueta
                    if (string.IsNullOrWhiteSpace(ctvm.TagId))
                    {
                        errorMsg = _localizer["ChargeTagIdRequired"].Value;
                        Logger.LogInformation("ChargeTag: New => no charge tag ID entered");
                    }

                    if (string.IsNullOrEmpty(errorMsg))
                    {
                        // Guardar etiqueta en la BD
                        {
                            ChargeTag newTag = new ChargeTag();
                            newTag.TagId = ctvm.TagId;
                            newTag.TagName = ctvm.TagName;
                            newTag.ParentTagId = ctvm.ParentTagId;
                            newTag.ExpiryDate = ctvm.ExpiryDate;
                            newTag.Blocked = ctvm.Blocked;
                            newTag.ChargingTime = ctvm.ChargingTime; 
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
                else if (currentChargeTag.TagId == Id)
                {
                    // Editar etiqueta existente
                    currentChargeTag.TagName = ctvm.TagName;
                    currentChargeTag.ParentTagId = ctvm.ParentTagId;
                    currentChargeTag.ExpiryDate = ctvm.ExpiryDate;
                    currentChargeTag.Blocked = ctvm.Blocked;
                    currentChargeTag.ChargingTime = ctvm.ChargingTime; 
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
        private IActionResult DisplayChargeTagForm(string Id, ChargeTagViewModel ctvm, List<ChargeTag> dbChargeTags, ChargeTag currentChargeTag)
        {
            // Listar todas las etiquetas de carga
            ctvm = new ChargeTagViewModel();
            ctvm.ChargeTags = dbChargeTags;
            ctvm.CurrentTagId = Id;

            if (currentChargeTag != null)
            {
                ctvm.TagId = currentChargeTag.TagId;
                ctvm.TagName = currentChargeTag.TagName;
                ctvm.ParentTagId = currentChargeTag.ParentTagId;
                ctvm.ExpiryDate = currentChargeTag.ExpiryDate;
                ctvm.Blocked = (currentChargeTag.Blocked != null) && currentChargeTag.Blocked.Value;
                ctvm.ChargingTime = currentChargeTag.ChargingTime;
            }

            string viewName = (!string.IsNullOrEmpty(ctvm.TagId) || Id=="@") ? "ChargeTagDetail" : "ChargeTagList";
            return View(viewName, ctvm);
        }


        [Authorize]
        public IActionResult EditTag(string id, ChargeTagViewModel ctvm)
        {
            try
            {
                if (User != null && !User.IsInRole(Constants.AdminRoleName) && !User.IsInRole(Constants.SuperAdminRoleName))
                {
                    Logger.LogWarning("EditTag: Request by non-administrator: {0}", User?.Identity?.Name);
                    TempData["ErrMsgKey"] = "AccessDenied";
                    return RedirectToAction("Error", new { Id = "" });
                }

                ViewBag.DatePattern = CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern;
                ViewBag.Language = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
                ctvm.CurrentTagId = id;

                using (OCPPCoreContext dbContext = new OCPPCoreContext(this.Config))
                {
                    ChargeTag currentChargeTag = null;
                    if (!string.IsNullOrEmpty(id))
                    {
                        // Convertir id a mayúsculas
                        string idUpper = id.ToUpper();
                        currentChargeTag = dbContext.ChargeTags.FirstOrDefault(tag => tag.TagId.ToUpper() == idUpper);
                    }

                    if (currentChargeTag != null)
                    {
                        if (Request.Method == "POST")
                        {
                            // Editar la etiqueta existente
                            currentChargeTag.TagName = ctvm.TagName;
                            currentChargeTag.ParentTagId = ctvm.ParentTagId;
                            currentChargeTag.ExpiryDate = ctvm.ExpiryDate;
                            currentChargeTag.Blocked = ctvm.Blocked;
                            currentChargeTag.ChargingTime = ctvm.ChargingTime;
                            dbContext.SaveChanges();
                            Logger.LogInformation("EditTag: charge tag edited: {0} / {1}", ctvm.TagId, ctvm.TagName);

                            return RedirectToAction("ChargeTag", new { Id = "" });
                        }
                        else
                        {
                            // Rellenar el modelo de vista con los datos actuales de la etiqueta
                            ctvm.TagId = currentChargeTag.TagId;
                            ctvm.TagName = currentChargeTag.TagName;
                            ctvm.ParentTagId = currentChargeTag.ParentTagId;
                            ctvm.ExpiryDate = currentChargeTag.ExpiryDate;
                            ctvm.Blocked = (currentChargeTag.Blocked != null) && currentChargeTag.Blocked.Value;
                            ctvm.ChargingTime = currentChargeTag.ChargingTime;

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

            using (OCPPCoreContext dbContext = new OCPPCoreContext(this.Config))
            {
                // Obtener todas las etiquetas de carga y filtrar en memoria
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
