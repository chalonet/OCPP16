using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using OCPP.Core.Database;
using OCPP.Core.Management.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;

namespace OCPP.Core.Management.Controllers
{
    public partial class HomeController : BaseController
    {
        private static int previousNewTransactionsCount = 0;

        [Authorize]
        public async Task<IActionResult> Transactions(string Id, string ConnectorId)
        {
            Logger.LogTrace("Transactions: Loading charge point transactions...");

            int currentConnectorId = -1;
            int.TryParse(ConnectorId, out currentConnectorId);

            TransactionListViewModel tlvm = new TransactionListViewModel();
            tlvm.CurrentChargePointId = Id;
            tlvm.CurrentConnectorId = currentConnectorId;
            tlvm.ConnectorStatuses = new List<ConnectorStatus>();
            tlvm.Transactions = new List<Transaction>();

            try
            {
                string ts = Request.Query["t"];
                int days = 30;
                if (ts == "2")
                {
                    // 90 days
                    days = 90;
                    tlvm.Timespan = 2;
                }
                else if (ts == "3")
                {
                    // 365 days
                    days = 365;
                    tlvm.Timespan = 3;
                }
                else
                {
                    // 30 days
                    days = 30;
                    tlvm.Timespan = 1;
                }

                    var optionsBuilder = new DbContextOptionsBuilder<OCPPCoreContext>();
                    optionsBuilder.UseSqlServer(_configuration.GetConnectionString("SqlServer"));

                    using (var dbContext = new OCPPCoreContext(optionsBuilder.Options))
                {
                    Logger.LogTrace("Transactions: Loading charge points...");
                    tlvm.ChargePoints = dbContext.ChargePoints.ToList<ChargePoint>();

                    Logger.LogTrace("Transactions: Loading charge points connectors...");
                    tlvm.ConnectorStatuses = dbContext.ConnectorStatuses.ToList<ConnectorStatus>();

                    Dictionary<string, int> dictConnectorCount = new Dictionary<string, int>();
                    foreach (ConnectorStatus cs in tlvm.ConnectorStatuses)
                    {
                        if (dictConnectorCount.ContainsKey(cs.ChargePointId))
                        {
                            dictConnectorCount[cs.ChargePointId] = dictConnectorCount[cs.ChargePointId] + 1;
                        }
                        else
                        {
                            dictConnectorCount.Add(cs.ChargePointId, 1);
                        }
                    }

                    Logger.LogTrace("Transactions: Loading charge tags...");
                    List<ChargeTag> chargeTags = dbContext.ChargeTags.ToList<ChargeTag>();
                    tlvm.ChargeTags = new Dictionary<string, ChargeTag>();
                    if (chargeTags != null)
                    {
                        foreach(ChargeTag tag in chargeTags)
                        {
                            tlvm.ChargeTags.Add(tag.TagId, tag);
                        }
                    }

                    if (!string.IsNullOrEmpty(tlvm.CurrentChargePointId))
                    {
                        Logger.LogTrace("Transactions: Loading charge point transactions...");
                        
                        int initialTransactionCount = tlvm.Transactions.Count;
                        
                        tlvm.Transactions = dbContext.Transactions
                                            .Where(t => t.ChargePointId == tlvm.CurrentChargePointId &&
                                                        t.ConnectorId == tlvm.CurrentConnectorId &&
                                                        t.StartTime >= DateTime.UtcNow.AddDays(-1 * days))
                                            .OrderByDescending(t => t.TransactionId)
                                            .ToList<Transaction>();

                        int finalTransactionCount = tlvm.Transactions.Count;

                        int newTransactionsCount = finalTransactionCount - initialTransactionCount;

                        if (newTransactionsCount == previousNewTransactionsCount)
                        {
                            Logger.LogInformation($"No se han añadido nuevas transacciones a la tabla de transacciones.");
                        }
                        else
                        {
                                foreach (var transaction in tlvm.Transactions)
                                {
                                    if (transaction.StopTime != null)
                                    {
                                        TimeSpan connectionDuration = transaction.StopTime.Value - transaction.StartTime;

                                        int timeConnectMinutes = (int)Math.Ceiling(connectionDuration.TotalMinutes);

                                        transaction.TimeConnect = timeConnectMinutes;
                                    }
                                }

                                var lastTransaction = dbContext.Transactions
                                    .OrderByDescending(t => t.TransactionId)
                                    .FirstOrDefault();
                                if (lastTransaction.StopTime != null)
                                {
                                    if (lastTransaction != null)
                                    {
                                        if (lastTransaction.StartTagId != null && tlvm.ChargeTags.ContainsKey(lastTransaction.StartTagId))
                                        {
                                            ChargeTag tag = tlvm.ChargeTags[lastTransaction.StartTagId];

                                            tag.ChargingTime -= lastTransaction.TimeConnect; 

                                            dbContext.ChargeTags.Update(tag);
                                            await dbContext.SaveChangesAsync();
                                            
                                            Logger.LogInformation($"Tiempo de carga restado para el tag {tag.TagId}: {lastTransaction.TimeConnect} minutos");
                                            Logger.LogInformation($"Nuevo tiempo de carga del tag {tag.TagId}: {tag.ChargingTime} minutos");
                                        }
                                    }

                                    Logger.LogInformation($"Se han añadido {newTransactionsCount} nuevas transacciones a la tabla de transacciones.");
                                    previousNewTransactionsCount = newTransactionsCount;
                            }
                        }
                    }



                }
            }
            catch (Exception exp)
            {
                Logger.LogError(exp, "Transactions: Error loading charge points from database");
            }

            return View(tlvm);
        }
    }
}
