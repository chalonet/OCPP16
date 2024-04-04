
using System;
using System.Collections.Generic;

#nullable disable

namespace OCPP.Core.Database
{
    public partial class ConnectorStatus
    {
        public string ChargePointId { get; set; }
        public int ConnectorId { get; set; }

        public string ConnectorName { get; set; }

        public string LastStatus { get; set; }
        public DateTime? LastStatusTime { get; set; }

        public double? LastMeter { get; set; }
        public DateTime? LastMeterTime { get; set; }

        
    }
}
