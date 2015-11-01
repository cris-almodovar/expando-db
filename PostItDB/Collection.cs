using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PostItDB.Storage;

namespace PostItDB
{
    public class Collection
    {
        public IDynamicStorage Storage { get; set; }

    }
}
