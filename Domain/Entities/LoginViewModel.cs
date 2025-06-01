using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HabitTrackerApp.Models
{
    public class LoginViewModel
    {
        [Required]
        public string Role { get; set; } // "Admin" or "User"

        [Required]
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }
    
        public LoginViewModel () {}
    public LoginViewModel (string role, string email ,string  password)
        {
            Role = role;
            Email = email;
            Password = password;
        }
}
}
