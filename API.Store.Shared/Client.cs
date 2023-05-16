using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace API.Store.Shared
{
    public class Client
    {
        public int Id { get; set; }
        public string FirstaName { get; set; }
        public string LastName { get; set; }
        public DateTime BirthDate { get; set; }
        public string Phone { get; set; }
        public string Addres { get; set; }
    }
}
