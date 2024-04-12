

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OCPP.Core.Database;
using OCPP.Core.Server.Messages_OCPP16;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace OCPP.Core.Server
{
    public partial class ControllerOCPP16
    {
        public string HandleStopTransaction(OCPPMessage msgIn, OCPPMessage msgOut)
        {
            string errorCode = null;
            StopTransactionResponse stopTransactionResponse = new StopTransactionResponse();
            stopTransactionResponse.IdTagInfo = new IdTagInfo();

            try
            {
                Logger.LogTrace("Processing stopTransaction request...");
                StopTransactionRequest stopTransactionRequest = DeserializeMessage<StopTransactionRequest>(msgIn);
                Logger.LogTrace("StopTransaction => Message deserialized");

                string idTag = CleanChargeTagId(stopTransactionRequest.IdTag, Logger);

                if (string.IsNullOrWhiteSpace(idTag))
                {
                    // no RFID-Tag => accept request
                    stopTransactionResponse.IdTagInfo.Status = IdTagInfoStatus.Accepted;
                    Logger.LogInformation("StopTransaction => no charge tag => Status: {0}", stopTransactionResponse.IdTagInfo.Status);
                }
                else
                {
                    stopTransactionResponse.IdTagInfo.ExpiryDate = MaxExpiryDate;

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
                                if (ct.ExpiryDate.HasValue) stopTransactionResponse.IdTagInfo.ExpiryDate = ct.ExpiryDate.Value;
                                stopTransactionResponse.IdTagInfo.ParentIdTag = ct.ParentTagId;
                                if (ct.Blocked.HasValue && ct.Blocked.Value)
                                {
                                    stopTransactionResponse.IdTagInfo.Status = IdTagInfoStatus.Blocked;
                                }
                                else if (ct.ExpiryDate.HasValue && ct.ExpiryDate.Value < DateTime.Now)
                                {
                                    stopTransactionResponse.IdTagInfo.Status = IdTagInfoStatus.Expired;
                                }
                                else
                                {
                                    stopTransactionResponse.IdTagInfo.Status = IdTagInfoStatus.Accepted;
                                }
                            }
                            else
                            {
                                stopTransactionResponse.IdTagInfo.Status = IdTagInfoStatus.Invalid;
                            }

                            Logger.LogInformation("StopTransaction => RFID-tag='{0}' => Status: {1}", idTag, stopTransactionResponse.IdTagInfo.Status);
                        }

                    }
                    catch (Exception exp)
                    {
                        Logger.LogError(exp, "StopTransaction => Exception reading charge tag ({0}): {1}", idTag, exp.Message);
                        stopTransactionResponse.IdTagInfo.Status = IdTagInfoStatus.Invalid;
                    }
                }

                if (stopTransactionResponse.IdTagInfo.Status == IdTagInfoStatus.Accepted)
                {
                    try
                    {
                        // Construir DbContextOptions usando IConfiguration
                    var optionsBuilder = new DbContextOptionsBuilder<OCPPCoreContext>();
                    optionsBuilder.UseSqlServer(Configuration.GetConnectionString("SqlServer"));

                    // Crear una instancia de OCPPCoreContext usando DbContextOptions
                    using (var dbContext = new OCPPCoreContext(optionsBuilder.Options))
                        {
                            Transaction transaction = dbContext.Find<Transaction>(stopTransactionRequest.TransactionId);
                            if (transaction != null &&
                                transaction.ChargePointId == ChargePointStatus.Id &&
                                !transaction.StopTime.HasValue)
                            {
                                if (transaction.ConnectorId > 0)
                                {
                                    // Update meter value in db connector status 
                                    UpdateConnectorStatus(transaction.ConnectorId, null, null, (double)stopTransactionRequest.MeterStop / 1000, stopTransactionRequest.Timestamp);
                                }

                                // check current tag against start tag
                                bool valid = true;
                                if (!string.Equals(transaction.StartTagId, idTag, StringComparison.InvariantCultureIgnoreCase))
                                {
                                    // tags are different => same group?
                                    ChargeTag startTag = dbContext.Find<ChargeTag>(transaction.StartTagId);
                                    if (startTag != null)
                                    {
                                        if (!string.Equals(startTag.ParentTagId, stopTransactionResponse.IdTagInfo.ParentIdTag, StringComparison.InvariantCultureIgnoreCase))
                                        {
                                            Logger.LogInformation("StopTransaction => Start-Tag ('{0}') and End-Tag ('{1}') do not match: Invalid!", transaction.StartTagId, idTag);
                                            stopTransactionResponse.IdTagInfo.Status = IdTagInfoStatus.Invalid;
                                            valid = false;
                                        }
                                        else
                                        {
                                            Logger.LogInformation("StopTransaction => Different RFID-Tags but matching group ('{0}')", stopTransactionResponse.IdTagInfo.ParentIdTag);
                                        }
                                    }
                                    else
                                    {
                                        Logger.LogError("StopTransaction => Start-Tag not found: '{0}'", transaction.StartTagId);
                                        // assume "valid" and allow to end the transaction
                                    }
                                }

                                if (valid)
                                {
                                    transaction.StopTagId = idTag;
                                    transaction.MeterStop =  (double)stopTransactionRequest.MeterStop / 1000; // Meter value here is always Wh
                                    transaction.StopReason = stopTransactionRequest.Reason.ToString();
                                    transaction.StopTime = stopTransactionRequest.Timestamp.UtcDateTime;
                                    dbContext.SaveChanges();
                                }
                            }
                            else
                            {
                                Logger.LogError("StopTransaction => Unknown or not matching transaction: id={0} / chargepoint={1} / tag={2}", stopTransactionRequest.TransactionId, ChargePointStatus?.Id, idTag);
                                WriteMessageLog(ChargePointStatus?.Id, transaction?.ConnectorId, msgIn.Action, string.Format("UnknownTransaction:ID={0}/Meter={1}", stopTransactionRequest.TransactionId, stopTransactionRequest.MeterStop), errorCode);
                                errorCode = ErrorCodes.PropertyConstraintViolation;
                            }
                        }
                    }
                    catch (Exception exp)
                    {
                        Logger.LogError(exp, "StopTransaction => Exception writing transaction: chargepoint={0} / tag={1}", ChargePointStatus?.Id, idTag);
                        errorCode = ErrorCodes.InternalError;
                    }
                }

                msgOut.JsonPayload = JsonConvert.SerializeObject(stopTransactionResponse);
                Logger.LogTrace("StopTransaction => Response serialized");
            }
            catch (Exception exp)
            {
                Logger.LogError(exp, "StopTransaction => Exception: {0}", exp.Message);
                errorCode = ErrorCodes.FormationViolation;
            }

            WriteMessageLog(ChargePointStatus?.Id, null, msgIn.Action, stopTransactionResponse.IdTagInfo?.Status.ToString(), errorCode);
            return errorCode;
        }
    }
}
