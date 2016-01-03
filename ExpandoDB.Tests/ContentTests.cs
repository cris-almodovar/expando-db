using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Collections;
using Jil;

namespace ExpandoDB.Tests
{
    [TestClass]
    public class ContentTests
    {
        [TestMethod]
        [TestCategory("Content tests")]
        public void Create_content_auto_generates_id()
        {
            var content = new Content();

            Assert.IsTrue(content._id.HasValue);            
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        [TestCategory("Content tests")]
        public void id_accepts_guid_only()
        {
            dynamic content = new Content();
            content._id = Guid.NewGuid();

            // This will raise an InvalidOperationException
            content._id = 100;
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        [TestCategory("Content tests")]
        public void Content_does_not_allow_anon_type()
        {
            dynamic content = new Content();
            content.X = new { Name = "name", Value = "value" };
        }

        [TestMethod]
        [TestCategory("Content tests")]
        public void Content_allows_string_number_datetime_guid_list_and_dictionary()
        {
            dynamic content = new Content();
            content._id = Guid.NewGuid();
            content.IntegerValue = 12345;
            content.DecimalValue = 10.50m;
            content.FloatValue = 0.110f;
            content.TimestampValue = DateTime.Now;
            content.List = new List<string> { "A", "B", "C", "D", "E" };
            content.Lookup = new Dictionary<string, object> { { "name", "abcde" }, { "value", 12345 } };

            Assert.AreEqual(typeof(Guid), content._id.GetType());
            Assert.AreEqual(typeof(int), content.IntegerValue.GetType());
            Assert.AreEqual(typeof(decimal), content.DecimalValue.GetType());
            Assert.AreEqual(typeof(float), content.FloatValue.GetType());
            Assert.AreEqual(typeof(DateTime), content.TimestampValue.GetType());
            Assert.IsInstanceOfType(content.List, typeof(IList));
            Assert.IsInstanceOfType(content.Lookup, typeof(IDictionary));

        }
    }
}
