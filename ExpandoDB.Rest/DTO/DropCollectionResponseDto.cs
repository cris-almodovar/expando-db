using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpandoDB.Rest.DTO
{
    public class DropCollectionResponseDto : ResponseDto
    {
        public bool isDropped { get; set; }
    }
}
