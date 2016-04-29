using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections;
using System.Collections.Generic;
using ExpandoDB.Storage;

namespace ExpandoDB.Tests
{
    [TestClass]
    public class DocumentTests
    {
        [TestMethod]
        [TestCategory("Document tests")]
        public void Create_document_auto_generates_id()
        {
            var document = new Document();

            Assert.IsTrue(document._id.HasValue);            
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        [TestCategory("Document tests")]
        public void id_accepts_guid_only()
        {
            dynamic document = new Document();
            document._id = Guid.NewGuid();

            // This will raise an InvalidOperationException
            document._id = 100;
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        [TestCategory("Document tests")]
        public void Document_does_not_allow_anon_type()
        {
            dynamic document = new Document();
            document.X = new { Name = "name", Value = "value" };
        }

        [TestMethod]
        [TestCategory("Document tests")]
        public void Document_allows_string_number_datetime_guid_list_and_dictionary()
        {
            dynamic document = new Document();
            document._id = Guid.NewGuid();
            document.IntegerValue = 12345;
            document.DecimalValue = 10.50m;
            document.FloatValue = 0.110f;
            document.TimestampValue = DateTime.Now;
            document.List = new List<string> { "A", "B", "C", "D", "E" };
            document.Lookup = new Dictionary<string, object> { { "name", "abcde" }, { "value", 12345 } };

            Assert.AreEqual(typeof(Guid), document._id.GetType());
            Assert.AreEqual(typeof(int), document.IntegerValue.GetType());
            Assert.AreEqual(typeof(decimal), document.DecimalValue.GetType());
            Assert.AreEqual(typeof(float), document.FloatValue.GetType());
            Assert.AreEqual(typeof(DateTime), document.TimestampValue.GetType());
            Assert.IsInstanceOfType(document.List, typeof(IList));
            Assert.IsInstanceOfType(document.Lookup, typeof(IDictionary));

            

        }


    }
}
