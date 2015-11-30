using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpandoDB.Service.DTO
{
    public class SchemaResponseDto : IResponseDto
    {
        public DateTime timestamp { get; set; }
        public string elapsed { get; set; }        
        public ContentCollectionSchema schema { get; set; }
    }
}
