
using OCPP.Core.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OCPP.Core.Management.Models
{
    /// <summary>
    /// Encapsulates the data of a connected chargepoint in the server
    /// Attention: Identical class in OCPP.Server (shoud be external common...)
    /// </summary>

    public class ChargePointStatus
    {
        public ChargePointStatus()
        {
            OnlineConnectors = new Dictionary<int, OnlineConnectorStatus>();
        }

        [Newtonsoft.Json.JsonProperty("id")]
        public string Id { get; set; }

        [Newtonsoft.Json.JsonProperty("name")]
        public string Name { get; set; }

        [Newtonsoft.Json.JsonProperty("protocol")]
        public string Protocol { get; set; }


        public Dictionary<int, OnlineConnectorStatus> OnlineConnectors { get; set; }
    }
    public class OnlineConnectorStatus
    {

        public ConnectorStatusEnum Status { get; set; }

        public double? ChargeRateKW { get; set; }

        public double? MeterKWH { get; set; }

        public double? SoC { get; set; }
    }

    public enum ConnectorStatusEnum
    {
        [System.Runtime.Serialization.EnumMember(Value = @"")]
        Undefined = 0,

        [System.Runtime.Serialization.EnumMember(Value = @"Available")]
        Available = 1,

        [System.Runtime.Serialization.EnumMember(Value = @"Occupied")]
        Occupied = 2,

        [System.Runtime.Serialization.EnumMember(Value = @"Unavailable")]
        Unavailable = 3,

        [System.Runtime.Serialization.EnumMember(Value = @"Faulted")]
        Faulted = 4
    }
}
