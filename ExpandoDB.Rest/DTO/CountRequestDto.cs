using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpandoDB.Rest.DTO
{
    public class CountRequestDto
    {
        /// <summary>
        /// Gets or sets the where clause for the ExpandoDB count query
        /// </summary>
        /// <value>
        /// A Lucene query expression
        /// </value>
        public string where { get; set; }
    }
}
