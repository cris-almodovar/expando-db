using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpandoDB.Rest.DTO.DTO
{
    public class SchemaResponseDto : IResponseDto
    {
        public string elapsed { get; set; }
        public ContentCollectionSchema schema { get; set; }
    }
}
