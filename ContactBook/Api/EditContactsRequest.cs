using ContactBook.Models;
using MessagePack;
using System.ComponentModel.DataAnnotations.Schema;

namespace ContactBook.Api
{
    public class EditContactsRequest
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Lastname { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
        public List<Email> Emails { get; set; }
    }

}


