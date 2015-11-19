using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpandoDB.Server.Web.DTO
{
    public class InsertResponseDto
    {
        public string Elapsed { get; set; }
        public string FromCollection { get; set; }
        public Guid _id { get; set; }        
    }
}
