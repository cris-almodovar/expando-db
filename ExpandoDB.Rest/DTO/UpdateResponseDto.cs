using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpandoDB.Service.DTO
{
    public class UpdateResponseDto : IResponseDto
    {
        public string elapsed { get; set; }
        public string fromCollection { get; set; }
        public int affectedCount { get; set; }
    }
}
