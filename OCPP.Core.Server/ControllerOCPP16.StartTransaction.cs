

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OCPP.Core.Database;
using OCPP.Core.Server.Messages_OCPP16;
using Microsoft.EntityFrameworkCore;

namespace OCPP.Core.Server
{
    public partial class ControllerOCPP16
    {
        public string HandleStartTransaction(OCPPMessage msgIn, OCPPMessage msgOut)
        {
            string errorCode = null;
            StartTransactionResponse startTransactionResponse = new StartTransactionResponse();

            int connectorId = -1;
            bool denyConcurrentTx = Configuration.GetValue<bool>("DenyConcurrentTx", false);

            try
            {
                Logger.LogTrace("Processing startTransaction request...");
                StartTransactionRequest startTransactionRequest = DeserializeMessage<StartTransactionRequest>(msgIn);
                Logger.LogTrace("StartTransaction => Message deserialized");

                string idTag = CleanChargeTagId(startTransactionRequest.IdTag, Logger);
                connectorId = startTransactionRequest.ConnectorId;

                startTransactionResponse.IdTagInfo.ParentIdTag = string.Empty;
                startTransactionResponse.IdTagInfo.ExpiryDate = MaxExpiryDate;

                if (string.IsNullOrWhiteSpace(idTag))
                {
                    // no RFID-Tag => accept request
                    startTransactionResponse.IdTagInfo.Status = IdTagInfoStatus.Accepted;
                    Logger.LogInformation("StartTransaction => no charge tag => Status: {0}", startTransactionResponse.IdTagInfo.Status);
                }
                else
                {
                    try
                    {
                        // Construir DbContextOptions usando IConfiguration
                    var optionsBuilder = new DbContextOptionsBuilder<OCPPCoreContext>();
                    optionsBuilder.UseSqlServer(Configuration.GetConnectionString("SqlServer"));

                    // Crear una instancia de OCPPCoreContext usando DbContextOptions
                    using (var dbContext = new OCPPCoreContext(optionsBuilder.Options))
                        {
                            ChargeTag ct = dbContext.Find<ChargeTag>(idTag);
                            if (ct != null)
                            {
                                if (ct.ExpiryDate.HasValue) startTransactionResponse.IdTagInfo.ExpiryDate = ct.ExpiryDate.Value;
                                startTransactionResponse.IdTagInfo.ParentIdTag = ct.ParentTagId;
                                if (ct.Blocked.HasValue && ct.Blocked.Value)
                                {
                                    startTransactionResponse.IdTagInfo.Status = IdTagInfoStatus.Blocked;
                                }
                                else if (ct.ExpiryDate.HasValue && ct.ExpiryDate.Value < DateTime.Now)
                                {
                                    startTransactionResponse.IdTagInfo.Status = IdTagInfoStatus.Expired;
                                }
                                else
                                {
                                    startTransactionResponse.IdTagInfo.Status = IdTagInfoStatus.Accepted;

                                    if (denyConcurrentTx)
                                    {
                                        // Check that no open transaction with this idTag exists
                                        Transaction tx = dbContext.Transactions
                                            .Where(t => !t.StopTime.HasValue && t.StartTagId == idTag)
                                            .OrderByDescending(t => t.TransactionId)
                                            .FirstOrDefault();

                                        if (tx != null)
                                        {
                                            startTransactionResponse.IdTagInfo.Status = IdTagInfoStatus.ConcurrentTx;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                startTransactionResponse.IdTagInfo.Status = IdTagInfoStatus.Invalid;
                            }

                            Logger.LogInformation("StartTransaction => Charge tag='{0}' => Status: {1}", idTag, startTransactionResponse.IdTagInfo.Status);
                        }
                    }
                    catch (Exception exp)
                    {
                        Logger.LogError(exp, "StartTransaction => Exception reading charge tag ({0}): {1}", idTag, exp.Message);
                        startTransactionResponse.IdTagInfo.Status = IdTagInfoStatus.Invalid;
                    }
                }

                if (connectorId > 0)
                {
                    // Update meter value in db connector status 
                    UpdateConnectorStatus(connectorId, ConnectorStatusEnum.Occupied.ToString(), startTransactionRequest.Timestamp, (double)startTransactionRequest.MeterStart / 1000, startTransactionRequest.Timestamp);
                }

                if (startTransactionResponse.IdTagInfo.Status == IdTagInfoStatus.Accepted)
                {
                    try
                    {
                        // Construir DbContextOptions usando IConfiguration
                    var optionsBuilder = new DbContextOptionsBuilder<OCPPCoreContext>();
                    optionsBuilder.UseSqlServer(Configuration.GetConnectionString("SqlServer"));

                    // Crear una instancia de OCPPCoreContext usando DbContextOptions
                    using (var dbContext = new OCPPCoreContext(optionsBuilder.Options))
                        {
                            Transaction transaction = new Transaction();
                            transaction.ChargePointId = ChargePointStatus?.Id;
                            transaction.ConnectorId = startTransactionRequest.ConnectorId;
                            transaction.StartTagId = idTag;
                            transaction.StartTime = startTransactionRequest.Timestamp.UtcDateTime;
                            transaction.MeterStart = (double)startTransactionRequest.MeterStart / 1000; // Meter value here is always Wh
                            transaction.StartResult = startTransactionResponse.IdTagInfo.Status.ToString();
                            dbContext.Add<Transaction>(transaction);
                            dbContext.SaveChanges();

                            // Return DB-ID as transaction ID
                            startTransactionResponse.TransactionId = transaction.TransactionId;
                        }
                    }
                    catch (Exception exp)
                    {
                        Logger.LogError(exp, "StartTransaction => Exception writing transaction: chargepoint={0} / tag={1}", ChargePointStatus?.Id, idTag);
                        errorCode = ErrorCodes.InternalError;
                    }
                }

                msgOut.JsonPayload = JsonConvert.SerializeObject(startTransactionResponse);
                Logger.LogTrace("StartTransaction => Response serialized");
            }
            catch (Exception exp)
            {
                Logger.LogError(exp, "StartTransaction => Exception: {0}", exp.Message);
                errorCode = ErrorCodes.FormationViolation;
            }

            WriteMessageLog(ChargePointStatus?.Id, connectorId, msgIn.Action, startTransactionResponse.IdTagInfo?.Status.ToString(), errorCode);
            return errorCode;
        }
    }
}
