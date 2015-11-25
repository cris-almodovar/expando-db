using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpandoDB.Service.DTO
{
    public class ContentResposeDto : IResponseDto
    {
        public string elapsed { get; set; }
        public string fromCollection { get; set; }
        public ExpandoObject content { get; set; }
    }
}
