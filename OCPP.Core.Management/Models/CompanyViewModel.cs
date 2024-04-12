using OCPP.Core.Database;
using System.Collections.Generic;

namespace OCPP.Core.Management.Models
{
    public class CompanyViewModel
    {
        public List<Company> Companies { get; set; }

        public List<User> Administrators { get; set; }
        public string CurrentCompanyId { get; set; }
        public int CompanyId { get; set; }

        
        public string Name { get; set; }
        
        public string Address { get; set; }
        
        public string Phone { get; set; }
        
        public int AdministratorId { get; set; }
        public virtual ICollection<ChargePoint> ChargePoints { get; set; }
        
        public virtual ICollection<ChargeTag> ChargeTags { get; set; }
        
        public virtual ICollection<ConnectorStatus> ConnectorStatuses { get; set; }
    }
}
