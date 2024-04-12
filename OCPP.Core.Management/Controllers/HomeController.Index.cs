using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OCPP.Core.Database;
using OCPP.Core.Management.Models;
using Microsoft.EntityFrameworkCore;

namespace OCPP.Core.Management.Controllers
{
    public partial class HomeController : BaseController
    {
        [Authorize]
        public async Task<IActionResult> Index()
        {
            Logger.LogTrace("Index: Loading charge points with latest transactions...");

            OverviewViewModel overviewModel = new OverviewViewModel();
            overviewModel.ChargePoints = new List<ChargePointsOverviewViewModel>();
            try
            {
                Dictionary<string, ChargePointStatus> dictOnlineStatus = new Dictionary<string, ChargePointStatus>();
                #region Load online status from OCPP server
                string serverApiUrl = base.Config.GetValue<string>("ServerApiUrl");
                string apiKeyConfig = base.Config.GetValue<string>("ApiKey");
                if (!string.IsNullOrEmpty(serverApiUrl))
                {
                    bool serverError = false;
                    try
                    {
                        ChargePointStatus[] onlineStatusList = null;

                        using (var httpClient = new HttpClient())
                        {
                            if (!serverApiUrl.EndsWith('/'))
                            {
                                serverApiUrl += "/";
                            }
                            Uri uri = new Uri(serverApiUrl);
                            uri = new Uri(uri, "Status");
                            httpClient.Timeout = new TimeSpan(0, 0, 4); // use short timeout

                            // API-Key authentication?
                            if (!string.IsNullOrWhiteSpace(apiKeyConfig))
                            {
                                httpClient.DefaultRequestHeaders.Add("X-API-Key", apiKeyConfig);
                            }
                            else
                            {
                                Logger.LogWarning("Index: No API-Key configured!");
                            }

                            HttpResponseMessage response = await httpClient.GetAsync(uri);
                            if (response.StatusCode == System.Net.HttpStatusCode.OK)
                            {
                                string jsonData = await response.Content.ReadAsStringAsync();
                                if (!string.IsNullOrEmpty(jsonData))
                                {
                                    onlineStatusList = JsonConvert.DeserializeObject<ChargePointStatus[]>(jsonData);
                                    overviewModel.ServerConnection = true;

                                    if (onlineStatusList != null)
                                    {
                                        foreach(ChargePointStatus cps in onlineStatusList)
                                        {
                                            if (!dictOnlineStatus.TryAdd(cps.Id, cps))
                                            {
                                                Logger.LogError("Index: Online charge point status (ID={0}) could not be added to dictionary", cps.Id);
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    Logger.LogError("Index: Result of status web request is empty");
                                    serverError = true;
                                }
                            }
                            else
                            {
                                Logger.LogError("Index: Result of status web request => httpStatus={0}", response.StatusCode);
                                serverError = true;
                            }
                        }

                        Logger.LogInformation("Index: Result of status web request => Length={0}", onlineStatusList?.Length);
                    }
                    catch (Exception exp)
                    {
                        Logger.LogError(exp, "Index: Error in status web request => {0}", exp.Message);
                        serverError = true;
                    }

                    if (serverError)
                    {
                        ViewBag.ErrorMsg = _localizer["ErrorOCPPServer"];
                    }
                }
                #endregion

                var optionsBuilder = new DbContextOptionsBuilder<OCPPCoreContext>();
                optionsBuilder.UseSqlServer(_configuration.GetConnectionString("SqlServer"));

                using (var dbContext = new OCPPCoreContext(optionsBuilder.Options))
                {
                    List<Company> companies = dbContext.Companies.ToList();
                    overviewModel.Companies = companies;

                    List<ConnectorStatusView> connectorStatusViewList = dbContext.ConnectorStatusViews.ToList<ConnectorStatusView>();

                    Dictionary<string, int> dictConnectorCount = new Dictionary<string, int>();
                    foreach(ConnectorStatusView csv in connectorStatusViewList)
                    {
                        if (dictConnectorCount.ContainsKey(csv.ChargePointId))
                        {
                            dictConnectorCount[csv.ChargePointId] = dictConnectorCount[csv.ChargePointId] + 1;
                        }
                        else
                        {
                            dictConnectorCount.Add(csv.ChargePointId, 1);
                        }
                    }
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

                    List<ChargePoint> dbChargePoints;
                    if (!User.IsInRole(Constants.SuperAdminRoleName))
                    {
                        dbChargePoints = dbContext.ChargePoints
                            .Where(cp => cp.Company.AdministratorId.ToString() == authenticatedAdministratorID)
                            .ToList();
                    }
                    else
                    {
                        dbChargePoints = dbContext.ChargePoints.ToList();
                    }
                    if (dbChargePoints != null)
                    {
                        foreach(ChargePoint cp in dbChargePoints)
                        {
                            
                            ChargePointStatus cpOnlineStatus = null;
                            dictOnlineStatus.TryGetValue(cp.ChargePointId, out cpOnlineStatus);

                            bool foundConnectorStatus = false;
                            if (connectorStatusViewList != null)
                            {
                                foreach (ConnectorStatusView connStatus in connectorStatusViewList)
                                {
                                    if (string.Equals(cp.ChargePointId, connStatus.ChargePointId, StringComparison.InvariantCultureIgnoreCase))
                                    {
                                        foundConnectorStatus = true;

                                        ChargePointsOverviewViewModel cpovm = new ChargePointsOverviewViewModel();
                                        cpovm.ChargePointId = cp.ChargePointId;
                                        cpovm.CompanyId = cp.CompanyId;
                                        cpovm.ConnectorId = connStatus.ConnectorId;
                                        if (string.IsNullOrWhiteSpace(connStatus.ConnectorName))
                                        {
                                            if (dictConnectorCount.ContainsKey(cp.ChargePointId) &&
                                                dictConnectorCount[cp.ChargePointId] > 1)
                                            {
                                                cpovm.Name = $"{cp.Name}:{connStatus.ConnectorId}";
                                            }
                                            else
                                            {
                                                cpovm.Name = cp.Name;
                                            }
                                        }
                                        else
                                        {
                                            cpovm.Name = connStatus.ConnectorName;
                                        }
                                        cpovm.Online = cpOnlineStatus != null;
                                        cpovm.ConnectorStatus = ConnectorStatusEnum.Undefined;
                                        OnlineConnectorStatus onlineConnectorStatus = null;
                                        if (cpOnlineStatus != null &&
                                            cpOnlineStatus.OnlineConnectors != null &&
                                            cpOnlineStatus.OnlineConnectors.ContainsKey(connStatus.ConnectorId))
                                        {
                                            onlineConnectorStatus = cpOnlineStatus.OnlineConnectors[connStatus.ConnectorId];
                                            cpovm.ConnectorStatus = onlineConnectorStatus.Status;
                                            Logger.LogTrace("Index: Found online status for CP='{0}' / Connector='{1}' / Status='{2}'", cpovm.ChargePointId, cpovm.ConnectorId, cpovm.ConnectorStatus);
                                        }

                                        if (connStatus.TransactionId.HasValue)
                                        {
                                            cpovm.MeterStart = connStatus.MeterStart.Value;
                                            cpovm.MeterStop = connStatus.MeterStop;
                                            cpovm.StartTime = connStatus.StartTime;
                                            cpovm.StopTime = connStatus.StopTime;

                                            cpovm.ConnectorStatus = (cpovm.StopTime.HasValue) ? ConnectorStatusEnum.Available : ConnectorStatusEnum.Occupied;
                                        }
                                        else
                                        {
                                            cpovm.MeterStart = -1;
                                            cpovm.MeterStop = -1;
                                            cpovm.StartTime = null;
                                            cpovm.StopTime = null;

                                            cpovm.ConnectorStatus = ConnectorStatusEnum.Available;
                                        }

                                        if (cpovm.ConnectorStatus == ConnectorStatusEnum.Occupied &&
                                            onlineConnectorStatus != null)
                                        {
                                            string currentCharge = string.Empty;
                                            if (onlineConnectorStatus.ChargeRateKW != null)
                                            {
                                                currentCharge = string.Format("{0:0.0}kW", onlineConnectorStatus.ChargeRateKW.Value);
                                            }
                                            if (onlineConnectorStatus.SoC != null)
                                            {
                                                if (!string.IsNullOrWhiteSpace(currentCharge)) currentCharge += " | ";
                                                currentCharge += string.Format("{0:0}%", onlineConnectorStatus.SoC.Value);
                                            }
                                            if (!string.IsNullOrWhiteSpace(currentCharge))
                                            {
                                                cpovm.CurrentChargeData = currentCharge;
                                            }
                                        }

                                        overviewModel.ChargePoints.Add(cpovm);
                                    }
                                }
                            }
                            if (foundConnectorStatus == false)
                            {
                                ChargePointsOverviewViewModel cpovm = new ChargePointsOverviewViewModel();
                                cpovm.ChargePointId = cp.ChargePointId;
                                cpovm.ConnectorId = 0;
                                cpovm.Name = cp.Name;
                                cpovm.Comment = cp.Comment;
                                cpovm.Online = cpOnlineStatus != null;
                                cpovm.ConnectorStatus = ConnectorStatusEnum.Undefined;
                                overviewModel.ChargePoints.Add(cpovm);
                            }
                        }
                    }

                    Logger.LogInformation("Index: Found {0} charge points / connectors", overviewModel.ChargePoints?.Count);
                }
            }
            catch (Exception exp)
            {
                Logger.LogError(exp, "Index: Error loading charge points from the database");
                TempData["ErrMessage"] = exp.Message;
                return RedirectToAction("Error", new { Id = "" });
            }

            return View(overviewModel);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View();
        }
    }
}
