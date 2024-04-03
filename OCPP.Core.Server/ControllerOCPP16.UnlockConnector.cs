

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OCPP.Core.Database;
using OCPP.Core.Server.Messages_OCPP16;

namespace OCPP.Core.Server
{
    public partial class ControllerOCPP16
    {
        public void HandleUnlockConnector(OCPPMessage msgIn, OCPPMessage msgOut)
        {
            Logger.LogInformation("UnlockConnector answer: ChargePointId={0} / MsgType={1} / ErrCode={2}", ChargePointStatus.Id, msgIn.MessageType, msgIn.ErrorCode);

            try
            {
                UnlockConnectorResponse unlockConnectorResponse = DeserializeMessage<UnlockConnectorResponse>(msgIn);
                Logger.LogInformation("HandleUnlockConnector => Answer status: {0}", unlockConnectorResponse?.Status);
                WriteMessageLog(ChargePointStatus?.Id, null, msgOut.Action, unlockConnectorResponse?.Status.ToString(), msgIn.ErrorCode);

                if (msgOut.TaskCompletionSource != null)
                {
                    // Set API response as TaskCompletion-result
                    string apiResult = "{\"status\": " + JsonConvert.ToString(unlockConnectorResponse.Status.ToString()) + "}";
                    Logger.LogTrace("HandleUnlockConnector => API response: {0}", apiResult);

                    msgOut.TaskCompletionSource.SetResult(apiResult);
                }
            }
            catch (Exception exp)
            {
                Logger.LogError(exp, "HandleUnlockConnector => Exception: {0}", exp.Message);
            }
        }
    }
}
