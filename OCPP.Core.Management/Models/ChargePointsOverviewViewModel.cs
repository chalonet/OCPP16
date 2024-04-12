
using OCPP.Core.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OCPP.Core.Management.Models
{
    public class ChargePointsOverviewViewModel
    {
        public List<Company> Companies { get; set; }
        public string ChargePointId { get; set; }

        public int ConnectorId { get; set; }

        public string Name { get; set; }

        public string Comment { get; set; }

        public double MeterStart { get; set; }

        public double? MeterStop { get; set; }

        public DateTime? StartTime { get; set; }

        public DateTime? StopTime { get; set; }

        public ConnectorStatusEnum ConnectorStatus { get; set; }

        public bool Online { get; set; }

        public string CurrentChargeData { get; set; }

        public int CompanyId { get; set; }
        public string CompanyName { get; set; }
    }
    
}
