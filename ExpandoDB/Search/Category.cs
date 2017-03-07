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
    /// Represents a category that a Document is assigned to.
    /// </summary>
    public class Category  // TODO: Rename to CategoryValue
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
        /// Gets or sets the category values (names and counts).
        /// </summary>
        /// <value>
        /// The sub category names and counts.
        /// </value>                
        public IList<Category> Values { get; set; }
        
    }
}
