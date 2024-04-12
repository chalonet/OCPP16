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
        public IActionResult Companies(string Id, CompanyViewModel cvm)
        {
            try
            {
                if (User != null && !User.IsInRole(Constants.AdminRoleName) && !User.IsInRole(Constants.SuperAdminRoleName))
                {
                    Logger.LogWarning("Company: Request by non-administrator: {0}", User?.Identity?.Name);
                    TempData["ErrMsgKey"] = "AccessDenied";
                    return RedirectToAction("Error", new { Id = "" });
                }

                ViewBag.DatePattern = CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern;
                ViewBag.Language = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
                cvm.CurrentCompanyId = Id;

                    var optionsBuilder = new DbContextOptionsBuilder<OCPPCoreContext>();
                    optionsBuilder.UseSqlServer(_configuration.GetConnectionString("SqlServer"));

                    using (var dbContext = new OCPPCoreContext(optionsBuilder.Options))
                {
                    Logger.LogTrace("Company: Loading charge companies...");
                    List<Company> dbCompanies = dbContext.Companies.ToList<Company>();
                    Logger.LogInformation("Company: Found {0} charge tags", dbCompanies.Count);

                    List<User>  admins = dbContext.Users
                        .Where(u => u.Role == "Administrator")
                        .ToList();

                    Company currentCompany = null;
                    if (!string.IsNullOrEmpty(Id) && int.TryParse(Id, out int idValue))
                    {
                        foreach (Company company in dbCompanies)
                        {
                            if (company.CompanyId == idValue)
                            {
                                currentCompany = company;
                                Logger.LogTrace("Company: Current charge tag: {0} / {1}", company.CompanyId, company.Name);
                                break;
                            }
                        }
                    }

                    if (Request.Method == "POST")
                    {
                        return ProcessCompanyPostRequest(Id, cvm, dbCompanies, currentCompany,dbContext,admins);
                    }
                    else
                    {
                        return DisplayCompanyForm(Id, cvm, dbCompanies, currentCompany,admins);
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
        private IActionResult ProcessCompanyPostRequest(string id, CompanyViewModel cvm, List<Company> dbCompanies, Company currentCompany, OCPPCoreContext dbContext,List<User> admins)
        {
            try
            {
                string errorMsg = null;

                if (id == "@")
                {
                    Logger.LogTrace("Company: Creating new company...");

                    if (string.IsNullOrWhiteSpace(cvm.Name))
                    {
                        errorMsg = _localizer["CompanyNameRequired"].Value;
                        Logger.LogInformation("Company: New => no company name entered");
                    }

                    if (string.IsNullOrEmpty(errorMsg))
                    {
                        {
                            Company newCompany = new Company();
                            newCompany.Name = cvm.Name;
                            newCompany.Address = cvm.Address;
                            newCompany.Phone = cvm.Phone;
                            newCompany.AdministratorId = cvm.AdministratorId;
                            dbContext.Companies.Add(newCompany);
                            dbContext.SaveChanges();
                            Logger.LogInformation("Company: New => company saved: {0}", cvm.Name);
                        }
                    }
                    else
                    {
                        ViewBag.ErrorMsg = errorMsg;
                        return View("CompanyDetail", cvm);
                    }
                }
                else if (currentCompany != null && currentCompany.CompanyId.ToString() == id)
                {
                    currentCompany.Name = cvm.Name;
                    currentCompany.Address = cvm.Address;
                    currentCompany.Phone = cvm.Phone;
                    currentCompany.AdministratorId = cvm.AdministratorId;
                    dbContext.SaveChanges();
                    Logger.LogInformation("Company: Edit => company saved: {0} / {1}", cvm.CompanyId, cvm.Name);
                }

                return RedirectToAction("Companies", new { id = "" });
            }
            catch (Exception exp)
            {
                Logger.LogError(exp, "Company: Error processing company POST request");
                TempData["ErrMessage"] = exp.Message;
                return RedirectToAction("Error", new { Id = "" });
            }
        }

        [Authorize]
        private IActionResult DisplayCompanyForm(string Id, CompanyViewModel cvm, List<Company> dbCompanies, Company currentCompany,List<User> admins)
        {
            cvm = new CompanyViewModel();
            cvm.Companies = dbCompanies;
            cvm.CurrentCompanyId = Id;
            cvm.Administrators = admins;


            if (currentCompany != null)
            {
                cvm.CompanyId = currentCompany.CompanyId;
                cvm.Name = currentCompany.Name;
                cvm.Address = currentCompany.Address;
                cvm.Phone = currentCompany.Phone;
                cvm.AdministratorId = currentCompany.AdministratorId;
            }

            string viewName = ( Id == "@") ? "CompanyDetail" : "CompanyList";

            return View(viewName, cvm);
        }


        [Authorize]
        public IActionResult EditCompany(string id, CompanyViewModel cvm)
        {
            try
            {
                if (User != null && !User.IsInRole(Constants.AdminRoleName) && !User.IsInRole(Constants.SuperAdminRoleName))
                {
                    Logger.LogWarning("EditCompany: Request by non-administrator: {0}", User?.Identity?.Name);
                    TempData["ErrMsgKey"] = "AccessDenied";
                    return RedirectToAction("Error", new { Id = "" });
                }

                ViewBag.DatePattern = CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern;
                ViewBag.Language = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
                cvm.CurrentCompanyId = id;

                var optionsBuilder = new DbContextOptionsBuilder<OCPPCoreContext>();
                optionsBuilder.UseSqlServer(_configuration.GetConnectionString("SqlServer"));

                using (var dbContext = new OCPPCoreContext(optionsBuilder.Options))
                {
                    Company currentCompany = null;
                    if (!string.IsNullOrEmpty(id))
                    {
                        string idUpper = id.ToUpper();
                        currentCompany = dbContext.Companies.FirstOrDefault(c => c.CompanyId.ToString().ToUpper() == idUpper);
                    }

                    if (currentCompany != null)
                    {
                        var administrators = dbContext.Users
                            .Where(u => u.Role == "Administrator")
                            .ToList();

                        if (Request.Method == "POST")
                        {
                            currentCompany.Name = cvm.Name;
                            currentCompany.Address = cvm.Address;
                            currentCompany.Phone = cvm.Phone;
                            currentCompany.AdministratorId = cvm.AdministratorId;
                            dbContext.SaveChanges();
                            Logger.LogInformation("EditCompany: company edited: {0} / {1}", cvm.CompanyId, cvm.Name);

                            return RedirectToAction("Companies", new { id = "" });
                        }
                        else
                        {
                            cvm.CompanyId = currentCompany.CompanyId;
                            cvm.Name = currentCompany.Name;
                            cvm.Address = currentCompany.Address;
                            cvm.Phone = currentCompany.Phone;
                            cvm.AdministratorId = currentCompany.AdministratorId;

                            cvm.Administrators = administrators;
                            return View("CompanyDetail", cvm);
                        }
                    }
                    else
                    {
                        TempData["ErrMsgKey"] = "CompanyNotFound";
                        return RedirectToAction("Error", new { Id = "" });
                    }
                }
            }
            catch (Exception exp)
            {
                Logger.LogError(exp, "EditCompany: Error editing company");
                TempData["ErrMessage"] = exp.Message;
                return RedirectToAction("Error", new { Id = "" });
            }
        }

        [Authorize]
        [HttpPost]
        public IActionResult DeleteCompany(string id)
        {
            try
            {
                if (User != null && !User.IsInRole(Constants.AdminRoleName) && !User.IsInRole(Constants.SuperAdminRoleName))
                {
                    Logger.LogWarning("DeleteCompany: Request by non-administrator: {0}", User?.Identity?.Name);
                    TempData["ErrMsgKey"] = "AccessDenied";
                    return RedirectToAction("Error", new { Id = "" });
                }

                var optionsBuilder = new DbContextOptionsBuilder<OCPPCoreContext>();
                optionsBuilder.UseSqlServer(_configuration.GetConnectionString("SqlServer"));

                using (var dbContext = new OCPPCoreContext(optionsBuilder.Options))
                {
                    List<Company> allCompanies = dbContext.Companies.ToList();
                    Company currentCompany = allCompanies.FirstOrDefault(c => c.CompanyId.ToString().Equals(id, StringComparison.InvariantCultureIgnoreCase));

                    if (currentCompany != null)
                    {
                        dbContext.Companies.Remove(currentCompany);
                        dbContext.SaveChanges();
                        Logger.LogInformation("DeleteCompany: company deleted: {0} / {1}", currentCompany.CompanyId, currentCompany.Name);

                        return RedirectToAction("Companies", new { id = "" });
                    }
                    else
                    {
                        TempData["ErrMsgKey"] = "CompanyNotFound";
                        return RedirectToAction("Error", new { Id = "" });
                    }
                }
            }
            catch (Exception exp)
            {
                Logger.LogError(exp, "DeleteCompany: Error deleting company");
                TempData["ErrMessage"] = exp.Message;
                return RedirectToAction("Error", new { Id = "" });
            }
        }
    }
}
