using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OCPP.Core.Database
{
    [Table("Users")]
    public partial class User
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int UserId { get; set; }

        [Required]
        [StringLength(50)]
        public string Username { get; set; }
        
        [Required]
        [StringLength(50)]
        public string Email { get; set; } 

        [Required]
        [StringLength(100)]
        public string Password { get; set; } 

        [Required]
        [StringLength(50)]
        public string Role { get; set; } 
    }
}
