using ExpandoDB.Search;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Reflection;
using System.Linq;
using System.Text;
using System.Threading;
using System.Collections.Generic;

namespace ExpandoDB.Tests
{
    [TestClass]    
    public class LuceneIndexTests
    {
        private LuceneIndex _luceneIndex;
        private string _appPath;
        private string _indexPath;

        [TestInitialize]
        public void Initialize()
        {
            _appPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            _indexPath = Path.Combine(_appPath, "index");

            if (Directory.Exists(_indexPath))
                Directory.Delete(_indexPath, true);            
            
            _luceneIndex = new LuceneIndex(_indexPath);
        }

        [TestCleanup]
        public void Cleanup()
        {   
            // Release the resources used by the index
            _luceneIndex.Dispose();

            // Delete the index directory and all index files
            if (Directory.Exists(_indexPath))    
                Directory.Delete(_indexPath, true);
        }

        [TestMethod]
        [TestCategory("Lucene Index test")]
        public void Can_convert_content_to_Lucene_document()
        {
            var content = CreateContent();
            var luceneDocument = content.ToLuceneDocument();
            var idField = luceneDocument.GetField(LuceneField.ID_FIELD_NAME);
            var bytes = idField.binaryValue().Bytes.Where(b => b > 0).ToArray();
            var id = Encoding.UTF8.GetString(bytes);

            Assert.AreEqual<string>(content._id.ToString(), id);
        }

        private static Content CreateContent()
        {
            var content = new Content();
            content._id = Guid.NewGuid();
            content._createdTimestamp = DateTime.UtcNow;

            dynamic book = content.AsExpando();
            book.Title = "The Hitchhiker's Guide to the Galaxy";
            book.Author = "Douglas Adams";
            book.PublishDate = new DateTime(1979, 10, 12, 12, 0, 0, DateTimeKind.Utc);
            book.Rating = 10;
            book.Description = "The Hitchhiker's Guide to the Galaxy is a comedy science fiction series created by Douglas Adams. Originally a radio comedy broadcast on BBC Radio 4 in 1978, it was later adapted to other formats, and over several years it gradually became an international multi-media phenomenon.";
            return content;
        }

        [TestMethod]
        [TestCategory("Lucene Index test")]
        public void Can_add_contents_and_search()
        {
            var contents = new List<Content>();
            for(var i = 0; i < 5; i++)
            {
                contents.Add(CreateContent());
                Thread.Sleep(1500);
            }

            var contentGuidsInReverseOrder = contents.OrderByDescending(c => c._createdTimestamp).Select(c => c._id.Value).ToList();

            for (var i = contents.Count - 1; i >= 0; i--)
                _luceneIndex.Insert(contents[i]);

            _luceneIndex.Refresh();

            int hitCount;
            int pageCount;
            bool hasMoreHits;
            int topN = 5;

            var result = _luceneIndex.Search("hitchhiker AND galaxy", out hitCount, out pageCount, out hasMoreHits, sortByFields: new [] { '-'+LuceneField.CREATED_TIMESTAMP_FIELD_NAME }, topN: topN);

            Assert.AreEqual<int>(hitCount, 5);
            Assert.IsFalse(hasMoreHits);
            Assert.IsTrue(result.SequenceEqual(contentGuidsInReverseOrder));

        }
    }
}
