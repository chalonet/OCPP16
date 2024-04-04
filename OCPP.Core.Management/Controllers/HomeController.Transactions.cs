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

                using (OCPPCoreContext dbContext = new OCPPCoreContext(this.Config))
                {
                    Logger.LogTrace("Transactions: Loading charge points...");
                    tlvm.ChargePoints = dbContext.ChargePoints.ToList<ChargePoint>();

                    Logger.LogTrace("Transactions: Loading charge points connectors...");
                    tlvm.ConnectorStatuses = dbContext.ConnectorStatuses.ToList<ConnectorStatus>();

                    // Count connectors for every charge point (=> naming scheme)
                    Dictionary<string, int> dictConnectorCount = new Dictionary<string, int>();
                    foreach (ConnectorStatus cs in tlvm.ConnectorStatuses)
                    {
                        if (dictConnectorCount.ContainsKey(cs.ChargePointId))
                        {
                            // > 1 connector
                            dictConnectorCount[cs.ChargePointId] = dictConnectorCount[cs.ChargePointId] + 1;
                        }
                        else
                        {
                            // first connector
                            dictConnectorCount.Add(cs.ChargePointId, 1);
                        }
                    }


                    // load charge tags for name resolution
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
                        
                        // Obtener la cantidad de transacciones antes de cargar las nuevas
                        int initialTransactionCount = tlvm.Transactions.Count;
                        
                        // Cargar las nuevas transacciones desde la base de datos
                        tlvm.Transactions = dbContext.Transactions
                                            .Where(t => t.ChargePointId == tlvm.CurrentChargePointId &&
                                                        t.ConnectorId == tlvm.CurrentConnectorId &&
                                                        t.StartTime >= DateTime.UtcNow.AddDays(-1 * days))
                                            .OrderByDescending(t => t.TransactionId)
                                            .ToList<Transaction>();

                        // Obtener la cantidad de transacciones después de cargar las nuevas
                        int finalTransactionCount = tlvm.Transactions.Count;

                        // Calcular la cantidad de nuevas transacciones añadidas
                        int newTransactionsCount = finalTransactionCount - initialTransactionCount;

                        // Verificar si se han añadido nuevas transacciones
                        if (newTransactionsCount == previousNewTransactionsCount)
                        {
                            Logger.LogInformation($"No se han añadido nuevas transacciones a la tabla de transacciones.");
                        }
                        else
                        {
                            
                                // Calcular y asignar TimeConnect para cada transacción
                                foreach (var transaction in tlvm.Transactions)
                                {
                                    if (transaction.StopTime != null)
                                    {
                                        // Calcular la duración de la conexión
                                        TimeSpan connectionDuration = transaction.StopTime.Value - transaction.StartTime;

                                        // Redondear hacia arriba al minuto más cercano
                                        int timeConnectMinutes = (int)Math.Ceiling(connectionDuration.TotalMinutes);

                                        // Asignar el tiempo de conexión en minutos
                                        transaction.TimeConnect = timeConnectMinutes;
                                    }
                                }

                                // Obtener la última transacción de la tabla Transactions
                                var lastTransaction = dbContext.Transactions
                                    .OrderByDescending(t => t.TransactionId)
                                    .FirstOrDefault();
                                if (lastTransaction.StopTime != null)
                                {

                                    if (lastTransaction != null)
                                    {
                                        // Obtener el tag asociado con la última transacción
                                        if (lastTransaction.StartTagId != null && tlvm.ChargeTags.ContainsKey(lastTransaction.StartTagId))
                                        {
                                            ChargeTag tag = tlvm.ChargeTags[lastTransaction.StartTagId];

                                            // Restar el tiempo de conexión al tiempo de carga del tag
                                            tag.ChargingTime -= lastTransaction.TimeConnect; // Utilizando el TimeConnect de la última transacción

                                            // Guardar los cambios en la base de datos
                                            dbContext.ChargeTags.Update(tag);
                                            await dbContext.SaveChangesAsync();
                                            
                                            // Mensaje de depuración para verificar el cambio en el tiempo de carga del tag
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
