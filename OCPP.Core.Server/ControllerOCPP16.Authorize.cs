

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OCPP.Core.Database;
using OCPP.Core.Server.Messages_OCPP16;

namespace OCPP.Core.Server
{
    public partial class ControllerOCPP16
    {
        public string HandleAuthorize(OCPPMessage msgIn, OCPPMessage msgOut)
        {
            string errorCode = null;
            AuthorizeResponse authorizeResponse = new AuthorizeResponse();

            string idTag = null;
            try
            {
                Logger.LogTrace("Processing authorize request...");
                AuthorizeRequest authorizeRequest = DeserializeMessage<AuthorizeRequest>(msgIn);
                Logger.LogTrace("Authorize => Message deserialized");
                idTag = CleanChargeTagId(authorizeRequest.IdTag, Logger);

                authorizeResponse.IdTagInfo.ParentIdTag = string.Empty;
                authorizeResponse.IdTagInfo.ExpiryDate = DateTimeOffset.UtcNow.AddMinutes(5);   // default: 5 minutes
                try
                {
                    using (OCPPCoreContext dbContext = new OCPPCoreContext(Configuration))
                    {
                        ChargeTag ct = dbContext.Find<ChargeTag>(idTag);
                        if (ct != null)
                        {
                            if (ct.ExpiryDate.HasValue)
                            {
                                authorizeResponse.IdTagInfo.ExpiryDate = ct.ExpiryDate.Value;
                            }
                            authorizeResponse.IdTagInfo.ParentIdTag = ct.ParentTagId;
                            if (ct.Blocked.HasValue && ct.Blocked.Value)
                            {
                                authorizeResponse.IdTagInfo.Status = IdTagInfoStatus.Blocked;
                            }
                            else if (ct.ExpiryDate.HasValue && ct.ExpiryDate.Value < DateTime.Now)
                            {
                                authorizeResponse.IdTagInfo.Status = IdTagInfoStatus.Expired;
                            }
                            else
                            {
                                authorizeResponse.IdTagInfo.Status = IdTagInfoStatus.Accepted;
                            }
                        }
                        else
                        {
                            authorizeResponse.IdTagInfo.Status = IdTagInfoStatus.Invalid;
                        }

                        Logger.LogInformation("Authorize => Status: {0}", authorizeResponse.IdTagInfo.Status);
                    }
                }
                catch (Exception exp)
                {
                    Logger.LogError(exp, "Authorize => Exception reading charge tag ({0}): {1}", idTag, exp.Message);
                    authorizeResponse.IdTagInfo.Status = IdTagInfoStatus.Invalid;
                }

                msgOut.JsonPayload = JsonConvert.SerializeObject(authorizeResponse);
                Logger.LogTrace("Authorize => Response serialized");
            }
            catch (Exception exp)
            {
                Logger.LogError(exp, "Authorize => Exception: {0}", exp.Message);
                errorCode = ErrorCodes.FormationViolation;
            }

            WriteMessageLog(ChargePointStatus?.Id, null,msgIn.Action, $"'{idTag}'=>{authorizeResponse.IdTagInfo?.Status}", errorCode);
            return errorCode;
        }
    }
}
