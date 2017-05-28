using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpandoDB.Tests
{
    [TestClass]
    public class CursorTests
    {
        const string DATA_PATH = @"D:\Users\cris\GitHub\expando-db\ExpandoDB.Service\bin\Debug\data";
        private Database _db;
        private Collection _docs;

        [TestInitialize]
        public void Initialize()
        {
            _db = new Database(DATA_PATH);
            _docs = _db["documents"];
        }

        [TestMethod]
        public void Can_create_cursor()
        {
            using (var cursor = _docs.OpenCursor(new Search.CursorSearchCriteria { Query = "singapore", TopN = 1000 }))
            {
                var total = cursor.AsParallel().GroupBy(doc => doc["Author"]).LongCount();
            }
        }

        [TestCleanup]
        public void Cleanup()
        {
            _db.Dispose();
        }
    }
}
