using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpandoDB.Server.Web.DTO
{
    public class ContentResposeDto
    {
        public string Elapsed { get; set; }
        public string FromCollection { get; set; }
        public ExpandoObject Content { get; set; }
    }
}
