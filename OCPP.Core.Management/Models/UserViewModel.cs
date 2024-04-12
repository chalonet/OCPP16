using OCPP.Core.Database;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace OCPP.Core.Management.Models
{
    public class UserViewModel
    {
        public List<User> Users { get; set; }

        public string CurrentUserId { get; set; }
        
        public int UserId { get; set; }

        public string Username { get; set; }

        public string Email { get; set; }

        public string Password { get; set; }

        public string Role { get; set; }
    }
}
