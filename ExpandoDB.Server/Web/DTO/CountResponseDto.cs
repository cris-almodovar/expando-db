using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpandoDB.Server.Web.DTO
{
    public class CountResponseDto : IResponseDto
    {
        public string Elapsed { get; set; }
        public string FromCollection { get; set; }
        public string Where { get; set; }
        public int Count { get; set; }        
    }
}
