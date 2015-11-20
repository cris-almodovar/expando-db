using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpandoDB.Server.Web.DTO
{
    public class UpdateResponseDto : IResponseDto
    {
        public string Elapsed { get; set; }
        public string FromCollection { get; set; }
        public int AffectedCount { get; set; }
    }
}
