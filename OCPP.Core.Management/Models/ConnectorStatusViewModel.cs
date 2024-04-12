

using OCPP.Core.Database;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace OCPP.Core.Management.Models
{
    public class ConnectorStatusViewModel
    {
        public List<ConnectorStatus> ConnectorStatuses { get; set; }

        public List<ChargePoint> ChargePoints { get; set; }

        public List<Company> Companies { get; set; }

        public string ChargePointId { get; set; }

        public int ConnectorId { get; set; }

        public string LastStatus { get; set; }

        public DateTime? LastStatusTime { get; set; }

        public double? LastMeter { get; set; }

        public DateTime? LastMeterTime { get; set; }

        [StringLength(100)]
        public string ConnectorName { get; set; }

        public int TimeConnect { get; set; }

        public int CompanyId { get; set; }
        public string CompanyName { get; set; }

    }
}
