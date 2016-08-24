using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace ExpandoDB.Rest.DTO
{  
    [Serializable] 
    [DataContract]
    public class CategoryDto 
    {
        /// <summary>
        /// Gets or sets the name of the category.
        /// </summary>
        /// <value>
        /// The name.
        /// </value> 
        [DataMember(Order=0)]
        public string name { get; set; }        

        /// <summary>
        /// Gets or sets the count of Documents under the category.
        /// </summary>
        /// <value>
        /// The count.
        /// </value>                   
        public int? count { get; set; }

        /// <summary>
        /// Gets or sets the category values (names and counts).
        /// </summary>
        /// <value>
        /// The sub category names and counts.
        /// </value>                   
        public IList<CategoryDto> values { get; set; }        
    }
}
