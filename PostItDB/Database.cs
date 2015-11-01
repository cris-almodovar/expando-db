using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PostItDB
{
    public class Database
    {
        public IList<Collection> Collections { get; set; }
    }
}
