

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OCPP.Core.Management.Models
{
    public class ChargePointsOverviewViewModel
    {
        /// <summary>
        /// ID of this chargepoint
        /// </summary>
        public string ChargePointId { get; set; }

        /// <summary>
        /// Connector-ID
        /// </summary>
        public int ConnectorId { get; set; }

        /// <summary>
        /// Name of this chargepoint
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Comment of this chargepoint
        /// </summary>
        public string Comment { get; set; }

        /// <summary>
        /// Meter start value of last transaction
        /// </summary>
        public double MeterStart { get; set; }

        /// <summary>
        /// Meter stop value of last transaction (or null if charging)
        /// </summary>
        public double? MeterStop { get; set; }

        /// <summary>
        /// Start time of last transaction
        /// </summary>
        public DateTime? StartTime { get; set; }

        /// <summary>
        /// Stop time of last transaction (or null if charging)
        /// </summary>
        public DateTime? StopTime { get; set; }

        /// <summary>
        /// Status of chargepoint
        /// </summary>
        public ConnectorStatusEnum ConnectorStatus { get; set; }

        /// <summary>
        /// Is this chargepoint currently connected to OCPP.Server?
        /// </summary>
        public bool Online { get; set; }

        /// <summary>
        /// Details about the current charge process
        /// </summary>
        public string CurrentChargeData { get; set; }
    }
}
