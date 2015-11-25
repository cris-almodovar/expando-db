using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpandoDB.Rest.DTO.DTO
{
    public class CountResponseDto : IResponseDto
    {
        public string elapsed { get; set; }
        public string fromCollection { get; set; }
        public string where { get; set; }
        public int count { get; set; }        
    }
}
