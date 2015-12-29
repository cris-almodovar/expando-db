using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpandoDB.Rest.DTO
{
    public abstract class ResponseDto
    {        
        public DateTime timestamp { get; set; }
        public TimeSpan elapsed { get; set; }
    }
}
