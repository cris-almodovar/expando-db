using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlexLucene.Document;

namespace ExpandoDB.Search
{
    public static class Extensions
    {
        public static Document ToLuceneDocument(this ExpandoObject content, IndexSchema indexSchema)
        {
            return null;

            // Check that the content has an _id field
        }
    }
}
