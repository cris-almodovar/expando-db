using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpandoDB.Service.DTO
{
    public class InsertResponseDto : IResponseDto
    {
        public string elapsed { get; set; }
        public string fromCollection { get; set; }
        public Guid _id { get; set; }        
    }
}
