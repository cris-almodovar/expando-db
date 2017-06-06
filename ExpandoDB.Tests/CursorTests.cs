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
        const string DATA_PATH = @"D:\Users\Crispin\Github\expando-db\ExpandoDB.Service\bin\Debug\data";
        private Database _db;
        private Collection _docs;

        [TestInitialize]
        public void Initialize()
        {
            //_db = new Database(DATA_PATH);
            //_db.Dispose();

            _db = new Database(DATA_PATH);
            _docs = _db["documents"];
        }

        //[TestMethod]
        //public void Can_create_cursor()
        //{
        //    try
        //    {
        //        using (var cursor = _docs.OpenCursor(new Search.CursorSearchCriteria { Query = "singapore", SelectFields = "Author,DocumentSentiment,PublishDate", TopN = 100000}))
        //        {
        //            var groups = cursor.AsParallel().GroupBy(doc => doc["Author"] as string).ToList();
                    
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        var error = ex.ToString();
        //    }
        //}

        [TestCleanup]
        public void Cleanup()
        {
            _db.Dispose();
        }
    }
}
