

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using OCPP.Core.Database;
using OCPP.Core.Management.Models;

namespace OCPP.Core.Management.Controllers
{
    public partial class HomeController : BaseController
    {
        [Authorize]
        public IActionResult Connector(string Id, string ConnectorId, ConnectorStatusViewModel csvm)
        {
            try
            {
                if (User != null && !User.IsInRole(Constants.AdminRoleName)&& !User.IsInRole(Constants.SuperAdminRoleName))
                {
                    Logger.LogWarning("Connector: Request by non-administrator: {0}", User?.Identity?.Name);
                    TempData["ErrMsgKey"] = "AccessDenied";
                    return RedirectToAction("Error", new { Id = "" });
                }

                ViewBag.DatePattern = CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern;
                ViewBag.Language = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;

                using (OCPPCoreContext dbContext = new OCPPCoreContext(this.Config))
                {
                    Logger.LogTrace("Connector: Loading connectors...");
                    List<ConnectorStatus> dbConnectorStatuses = dbContext.ConnectorStatuses.ToList<ConnectorStatus>();
                    Logger.LogInformation("Connector: Found {0} connectors", dbConnectorStatuses.Count);

                    ConnectorStatus currentConnectorStatus = null;
                    if (!string.IsNullOrEmpty(Id) && !string.IsNullOrEmpty(ConnectorId))
                    {
                        foreach (ConnectorStatus cs in dbConnectorStatuses)
                        {
                            if (cs.ChargePointId.Equals(Id, StringComparison.InvariantCultureIgnoreCase) &&
                                cs.ConnectorId.ToString().Equals(ConnectorId, StringComparison.InvariantCultureIgnoreCase))
                            {
                                currentConnectorStatus = cs;
                                Logger.LogTrace("Connector: Current connector: {0} / {1}", cs.ChargePointId, cs.ConnectorId);
                                break;
                            }
                        }
                    }

                    if (Request.Method == "POST")
                    {
                        if (currentConnectorStatus.ChargePointId == Id)
                        {
                            // Save connector
                            currentConnectorStatus.ConnectorName = csvm.ConnectorName;
                            dbContext.SaveChanges();
                            Logger.LogInformation("Connector: Edit => Connector saved: {0} / {1} => '{2}'", csvm.ChargePointId, csvm.ConnectorId, csvm.ConnectorName);
                        }

                        return RedirectToAction("Connector", new { Id = "" });
                    }
                    else
                    {
                        // List all charge tags
                        csvm = new ConnectorStatusViewModel();
                        csvm.ConnectorStatuses = dbConnectorStatuses;

                        if (currentConnectorStatus != null)
                        {
                            csvm.ChargePointId = currentConnectorStatus.ChargePointId;
                            csvm.ConnectorId = currentConnectorStatus.ConnectorId;
                            csvm.ConnectorName = currentConnectorStatus.ConnectorName;
                            csvm.LastStatus = currentConnectorStatus.LastStatus;
                            csvm.LastStatusTime = currentConnectorStatus.LastStatusTime;
                            csvm.LastMeter = currentConnectorStatus.LastMeter;
                            csvm.LastMeterTime = currentConnectorStatus.LastMeterTime;
                        }

                        string viewName = "ConnectorList";
                        return View(viewName, csvm);

                    }
                }
            }
            catch (Exception exp)
            {
                Logger.LogError(exp, "Connector: Error loading connectors from database");
                TempData["ErrMessage"] = exp.Message;
                return RedirectToAction("Error", new { Id = "" });
            }
        }
    }
}
