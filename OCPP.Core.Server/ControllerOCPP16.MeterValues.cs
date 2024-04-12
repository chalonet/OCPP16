

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OCPP.Core.Database;
using OCPP.Core.Server.Messages_OCPP16;

namespace OCPP.Core.Server
{
    public partial class ControllerOCPP16
    {
        public string HandleMeterValues(OCPPMessage msgIn, OCPPMessage msgOut)
        {
            string errorCode = null;
            MeterValuesResponse meterValuesResponse = new MeterValuesResponse();

            int connectorId = -1;
            string msgMeterValue = string.Empty;

            try
            {
                Logger.LogTrace("Processing meter values...");
                MeterValuesRequest meterValueRequest = DeserializeMessage<MeterValuesRequest>(msgIn);
                Logger.LogTrace("MeterValues => Message deserialized");

                connectorId = meterValueRequest.ConnectorId;

                if (ChargePointStatus != null)
                {
                    // Known charge station => process meter values
                    double currentChargeKW = -1;
                    double meterKWH = -1;
                    DateTimeOffset? meterTime = null;
                    double stateOfCharge = -1;
                    foreach (MeterValue meterValue in meterValueRequest.MeterValue)
                    {
                        foreach (SampledValue sampleValue in meterValue.SampledValue)
                        {
                            Logger.LogTrace("MeterValues => Context={0} / Format={1} / Value={2} / Unit={3} / Location={4} / Measurand={5} / Phase={6}",
                                sampleValue.Context, sampleValue.Format, sampleValue.Value, sampleValue.Unit, sampleValue.Location, sampleValue.Measurand, sampleValue.Phase);

                            if (sampleValue.Measurand == SampledValueMeasurand.Power_Active_Import)
                            {
                                // current charging power
                                if (double.TryParse(sampleValue.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out currentChargeKW))
                                {
                                    if (sampleValue.Unit == SampledValueUnit.W ||
                                        sampleValue.Unit == SampledValueUnit.VA ||
                                        sampleValue.Unit == SampledValueUnit.Var ||
                                        sampleValue.Unit == null)
                                    {
                                        Logger.LogTrace("MeterValues => Charging '{0:0.0}' W", currentChargeKW);
                                        // convert W => kW
                                        currentChargeKW = currentChargeKW / 1000;
                                    }
                                    else if (sampleValue.Unit == SampledValueUnit.KW ||
                                            sampleValue.Unit == SampledValueUnit.KVA ||
                                            sampleValue.Unit == SampledValueUnit.Kvar)
                                    {
                                        // already kW => OK
                                        Logger.LogTrace("MeterValues => Charging '{0:0.0}' kW", currentChargeKW);
                                    }
                                    else
                                    {
                                        Logger.LogWarning("MeterValues => Charging: unexpected unit: '{0}' (Value={1})", sampleValue.Unit, sampleValue.Value);
                                    }
                                }
                                else
                                {
                                    Logger.LogError("MeterValues => Charging: invalid value '{0}' (Unit={1})", sampleValue.Value, sampleValue.Unit);
                                }
                            }
                            else if (sampleValue.Measurand == SampledValueMeasurand.Energy_Active_Import_Register ||
                                    sampleValue.Measurand == null)
                            {
                                // charged amount of energy
                                if (double.TryParse(sampleValue.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out meterKWH))
                                {
                                    if (sampleValue.Unit == SampledValueUnit.Wh ||
                                        sampleValue.Unit == SampledValueUnit.Varh ||
                                        sampleValue.Unit == null)
                                    {
                                        Logger.LogTrace("MeterValues => Value: '{0:0.0}' Wh", meterKWH);
                                        // convert Wh => kWh
                                        meterKWH = meterKWH / 1000;
                                    }
                                    else if (sampleValue.Unit == SampledValueUnit.KWh ||
                                            sampleValue.Unit == SampledValueUnit.Kvarh)
                                    {
                                        // already kWh => OK
                                        Logger.LogTrace("MeterValues => Value: '{0:0.0}' kWh", meterKWH);
                                    }
                                    else
                                    {
                                        Logger.LogWarning("MeterValues => Value: unexpected unit: '{0}' (Value={1})", sampleValue.Unit, sampleValue.Value);
                                    }
                                    meterTime = meterValue.Timestamp;
                                }
                                else
                                {
                                    Logger.LogError("MeterValues => Value: invalid value '{0}' (Unit={1})", sampleValue.Value, sampleValue.Unit);
                                }
                            }
                            else if (sampleValue.Measurand == SampledValueMeasurand.SoC)
                            {
                                // state of charge (battery status)
                                if (double.TryParse(sampleValue.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out stateOfCharge))
                                {
                                    Logger.LogTrace("MeterValues => SoC: '{0:0.0}'%", stateOfCharge);
                                }
                                else
                                {
                                    Logger.LogError("MeterValues => invalid value '{0}' (SoC)", sampleValue.Value);
                                }
                            }
                        }
                    }

                    // write charging/meter data in chargepoint status
                    if (connectorId > 0)
                    {
                        msgMeterValue = $"Meter (kWh): {meterKWH} | Charge (kW): {currentChargeKW} | SoC (%): {stateOfCharge}";

                        if (meterKWH >= 0)
                        {
                            UpdateConnectorStatus(connectorId, null, null, meterKWH, meterTime);
                        }

                        if (currentChargeKW >= 0 || meterKWH >= 0 || stateOfCharge >= 0)
                        {
                            if (ChargePointStatus.OnlineConnectors.ContainsKey(connectorId))
                            {
                                OnlineConnectorStatus ocs = ChargePointStatus.OnlineConnectors[connectorId];
                                if (currentChargeKW >= 0) ocs.ChargeRateKW = currentChargeKW;
                                if (meterKWH >= 0) ocs.MeterKWH = meterKWH;
                                if (stateOfCharge >= 0) ocs.SoC = stateOfCharge;
                            }
                            else
                            {
                                OnlineConnectorStatus ocs = new OnlineConnectorStatus();
                                if (currentChargeKW >= 0) ocs.ChargeRateKW = currentChargeKW;
                                if (meterKWH >= 0) ocs.MeterKWH = meterKWH;
                                if (stateOfCharge >= 0) ocs.SoC = stateOfCharge;
                                if (ChargePointStatus.OnlineConnectors.TryAdd(connectorId, ocs))
                                {
                                    Logger.LogTrace("MeterValues => Set OnlineConnectorStatus for ChargePoint={0} / Connector={1} / Values: {2}", ChargePointStatus?.Id, connectorId, msgMeterValue);
                                }
                                else
                                {
                                    Logger.LogError("MeterValues => Error adding new OnlineConnectorStatus for ChargePoint={0} / Connector={1} / Values: {2}", ChargePointStatus?.Id, connectorId, msgMeterValue);
                                }
                            }
                        }
                    }
                }
                else
                {
                    // Unknown charge station
                    errorCode = ErrorCodes.GenericError;
                }

                msgOut.JsonPayload = JsonConvert.SerializeObject(meterValuesResponse);
                Logger.LogTrace("MeterValues => Response serialized");
            }
            catch (Exception exp)
            {
                Logger.LogError(exp, "MeterValues => Exception: {0}", exp.Message);
                errorCode = ErrorCodes.InternalError;
            }

            WriteMessageLog(ChargePointStatus.Id, connectorId, msgIn.Action, msgMeterValue, errorCode);
            return errorCode;
        }
    }
}
