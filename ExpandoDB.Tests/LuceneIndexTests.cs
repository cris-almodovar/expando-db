using ExpandoDB.Search;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Reflection;
using System.Linq;

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
            if (!Directory.Exists(_indexPath))
                Directory.CreateDirectory(_indexPath);

            var schema = SearchSchema.Default;
            _luceneIndex = new LuceneIndex(_indexPath, () => schema);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _luceneIndex.Dispose();

            if (Directory.Exists(_indexPath))    
                Directory.Delete(_indexPath, true);
        }

        [TestMethod]
        public void Can_convert_Content_to_Lucene_document()
        {
            var content = CreateContent();
            var luceneDocument = content.ToLuceneDocument();

            Assert.AreEqual<string>(content._id.ToString(), luceneDocument.Get("_id"));
        }

        private static Content CreateContent()
        {
            var content = new Content();
            content._id = Guid.NewGuid();
            content._createdTimestamp = DateTime.UtcNow;

            dynamic book = content.AsExpando();
            book.Title = "The Hitchhiker's Guide to the Galaxy";
            book.Author = "Douglas Adams";
            book.PublishDate = DateTime.Now;
            book.Rating = 10;
            book.Description = "The Hitchhiker's Guide to the Galaxy is a comedy science fiction series created by Douglas Adams. Originally a radio comedy broadcast on BBC Radio 4 in 1978, it was later adapted to other formats, and over several years it gradually became an international multi-media phenomenon.";
            return content;
        }

        [TestMethod]
        public void Can_add_Content_and_search()
        {
            var content = CreateContent();
            _luceneIndex.Insert(content);

            var guid = _luceneIndex.Search("hitchhiker AND galaxy").FirstOrDefault();

            Assert.AreEqual<Guid?>(content._id, guid);


        }
    }
}
