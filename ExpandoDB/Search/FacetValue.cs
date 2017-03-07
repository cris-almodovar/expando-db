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
    /// Represents the value of a Document category, including all its sub-categories.
    /// </summary>
    public class FacetValue
    {
        /// <summary>
        /// Gets or sets the name of the category.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>        
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the count of Documents under the category.
        /// </summary>
        /// <value>
        /// The count.
        /// </value>                
        public int? Count { get; set; }

        /// <summary>
        /// Gets or sets the values of sub-categories.
        /// </summary>
        /// <value>
        /// The sub category names and counts.
        /// </value>                
        public IList<FacetValue> Values { get; set; }

    }
}
