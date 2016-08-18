using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpandoDB.Search
{
    /// <summary>
    /// Represents a category that a Document is assigned to.
    /// </summary>
    public class Category
    {
        /// <summary>
        /// Gets or sets the name of the category.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public string Name { get; set; }
       

        /// <summary>
        /// Gets or sets the category names and counts.
        /// </summary>
        /// <value>
        /// The category names and counts.
        /// </value>
        public IList<CategoryCount> Values { get; private set; } = new List<CategoryCount>();


        /// <summary>
        /// Represents the name and count of a category
        /// </summary>
        public class CategoryCount
        {
            /// <summary>
            /// Gets or sets the name of the category.
            /// </summary>
            /// <value>
            /// The name.
            /// </value>
            public string Name { get; set; }
            /// <summary>
            /// Gets or sets the count of the category.
            /// </summary>
            /// <value>
            /// The count.
            /// </value>
            public int Count { get; set; }
        }        
    }
}
