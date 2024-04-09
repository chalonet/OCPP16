using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OCPP.Core.Database
{
    [Table("Usuarios")]
    public partial class Usuario
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int UserId { get; set; }

        [Required]
        [StringLength(50)]
        public string Username { get; set; } 

        [Required]
        [StringLength(100)]
        public string Password { get; set; } 

        [Required]
        [StringLength(50)]
        public string Role { get; set; } 
    }
}
