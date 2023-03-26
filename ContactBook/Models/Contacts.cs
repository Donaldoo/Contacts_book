using ContactBook.Models;
using MessagePack;
using System.ComponentModel.DataAnnotations.Schema;
using ContactBook.Api;

namespace ContactBook.Models
{
    public class Contacts
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string Name { get; set; }
        public string Lastname { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
        public ICollection<Email> Email { get; set; } = new List<Email>();

        public static Contacts From(int id, EditContactsRequest request)
        {
            return new Contacts
            {
                Address = request.Address,
                Email = request.Emails,
                Id = id,
                Lastname = request.Lastname,
                Name = request.Name,
                Phone = request.Phone
            };
        }
    }

}


