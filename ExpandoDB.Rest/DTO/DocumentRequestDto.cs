using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpandoDB.Rest.DTO
{
    /// <summary>
    /// Represents the REST request accepted by the GET /db/{collection}/{id} API
    /// </summary>
    public class DocumentRequestDto
    {
        public string select { get; set; }
    }
}
