using Jil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace ExpandoDB.Search
{
    /// <summary>
    /// Represents the value of a Document Facet (a.k.a. category), including all its sub-facets.
    /// </summary>
    public class FacetValue
    {
        /// <summary>
        /// Gets or sets the name of the Facet.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>        
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the count of Documents under the Facet.
        /// </summary>
        /// <value>
        /// The count.
        /// </value>                
        public int? Count { get; set; }

        /// <summary>
        /// Gets or sets the values of the Facet's sub-Facets.
        /// </summary>
        /// <value>
        /// The sub Facet names and counts.
        /// </value>                
        public IList<FacetValue> Values { get; set; }

    }
}
