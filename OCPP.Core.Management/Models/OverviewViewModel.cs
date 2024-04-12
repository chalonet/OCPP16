
using OCPP.Core.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OCPP.Core.Management.Models
{
    public class OverviewViewModel
    {

        /// <summary>
        /// List of chargepoints with status information
        /// </summary>
        public List<ChargePointsOverviewViewModel> ChargePoints { get; set; }

        public List<Company> Companies { get; set; }

        /// <summary>
        /// Does the status contain live information from the OCPP.Server?
        /// </summary>
        public bool ServerConnection { get; set; }
    }
}
