using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace ContactBook.Models
{
    public class AppUser: IdentityUser
    {
        [NotMapped]
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }
}
