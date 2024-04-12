using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OCPP.Core.Database
{
    [Table("Companies")]
    public class Company
    {
        [Key]
        public int CompanyId { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public string Phone { get; set; }
        public int AdministratorId { get; set; }
        
        public virtual ICollection<ChargePoint> ChargePoints { get; set; }

        
        public virtual ICollection<ChargeTag> ChargeTags { get; set; }

       
        public virtual ICollection<ConnectorStatus> ConnectorStatus { get; set; }

        
        public Company()
        {
            ChargePoints = new HashSet<ChargePoint>();
            ChargeTags = new HashSet<ChargeTag>();
            ConnectorStatus = new HashSet<ConnectorStatus>();
        }
    }
}
