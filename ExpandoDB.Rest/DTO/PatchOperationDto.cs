using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpandoDB.Rest.DTO
{

    /// <summary>
    /// Represents data for REST PATCH operation. See https://tools.ietf.org/html/rfc5789. 
    /// </summary>
    public class PatchOperationDto
    {
        public string op { get; set; }
        public string path { get; set; }
        public object value { get; set; }
    }   
}
