

using System;
using System.Collections.Generic;

#nullable disable

namespace OCPP.Core.Database
{
    public partial class ChargePoint
    {
        public ChargePoint()
        {
            Transactions = new HashSet<Transaction>();
        }

        public string ChargePointId { get; set; }
        public string Name { get; set; }
        public string Comment { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string ClientCertThumb { get; set; }
        public int CompanyId { get; set; }
        public virtual Company Company { get; set; }
        public virtual ICollection<Transaction> Transactions { get; set; }
    }
}
