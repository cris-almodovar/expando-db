using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpandoDB.Rest.DTO
{
    /// <summary>
    /// Represents the REST request accepted by the DELETE /db/{collection} API
    /// </summary>
    public class DeleteCollectionDto
    {
        /// <summary>
        /// Indicates whether to completely drop all the data and index allocated for the Collection.
        /// </summary>
        /// <value>
        /// A boolean value indicating whether to drop the Collection.
        /// </value>
        public bool? drop { get; set; }
    }
}
