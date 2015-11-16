using Nancy;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpandoDB.Server.Web
{
    public class DbService : NancyModule
    {
        private readonly Database _db;

        public DbService(Database db) : base("/db")
        {
            _db = db;

            Get["/{collection}"] = req =>
            {
                var collectionName = (string)req["collection"];
                var collection = _db[collectionName];
                var schema = collection.GetSchema();
                return schema;
            };
        }
    }    
}
