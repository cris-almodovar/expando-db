using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpandoDB.Rest.DTO
{
    public class UpdateResponseDto : ResponseDto
    {        
        public string from { get; set; }
        public int affectedCount { get; set; }
    }
}
