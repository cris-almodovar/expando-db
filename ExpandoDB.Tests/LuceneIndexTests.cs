using ExpandoDB.Search;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;

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
            
            _luceneIndex = new LuceneIndex(_indexPath, Schema.CreateDefault());
        }

        [TestCleanup]
        public void Cleanup()
        {   
            // Release the resources used by the index
            _luceneIndex.Dispose();

            Thread.Sleep(TimeSpan.FromSeconds(2));

            // Delete the index directory and all index files
            if (Directory.Exists(_indexPath))    
                Directory.Delete(_indexPath, true);
        }

        [TestMethod]
        [TestCategory("Lucene Index tests")]
        public void Can_convert_document_to_Lucene_document()
        {
            var document = CreateDocument();
            var luceneDocument = document.ToLuceneDocument();
            var idField = luceneDocument.GetField(Schema.MetadataField.ID).StringValue();            
            var id = Guid.Parse(idField);

            Assert.AreEqual<Guid?>(document._id, id);
        }

        private static Document CreateDocument()
        {
            var document = new Document();
            document._id = Guid.NewGuid();
            document._createdTimestamp = DateTime.UtcNow;

            dynamic book = document;
            book.Title = "The Hitchhiker's Guide to the Galaxy";
            book.Author = "Douglas Adams";
            book.PublishDate = new DateTime(1979, 10, 12, 12, 0, 0, DateTimeKind.Utc);
            book.Rating = 10;
            book.Description = "The Hitchhiker's Guide to the Galaxy is a comedy science fiction series created by Douglas Adams. Originally a radio comedy broadcast on BBC Radio 4 in 1978, it was later adapted to other formats, and over several years it gradually became an international multi-media phenomenon.";
            book.RelatedTitles = new List<string> { "The Restaurant at the End of the Universe", "Life, the Universe and Everything" };
            book.Characters = new Dictionary<string, object> { { "Name", "Arthur Dent" }, { "Character", "Ford Prefect" } };
            book.X = null;

            return document;
        }

        [TestMethod]
        [TestCategory("Lucene Index tests")]
        public void Can_add_documents_and_search()
        {
            var documents = new List<Document>();
            for(var i = 0; i < 10; i++)
            {
                dynamic newDocument = CreateDocument();                
                newDocument.BookId = 9-i;

                documents.Add(newDocument);
                _luceneIndex.Insert(newDocument);
                Thread.Sleep(1200);                
            }

            var documentIds = documents.Select(c => c._id.Value).ToList();
            documentIds.Reverse();

            _luceneIndex.Refresh();

            var criteria = new SearchCriteria
            {
                Query = "hitchhiker AND galaxy",
                SortByFields = "BookId",
                TopN = 10
            };           

            var result = _luceneIndex.Search(criteria);

            Assert.AreEqual<int?>(result.ItemCount, 10);
            Assert.AreEqual<int?>(result.ItemCount, result.TotalHits);
            Assert.IsTrue(result.Items.SequenceEqual(documentIds));
        }

        [TestMethod]
        [TestCategory("Lucene Index tests")]
        public void Can_tokenize_text()
        {
            var text = "The quick brown fox jumped over the lazy dog.";
            var expected = new[] { "quick", "brown", "fox", "jump", "over", "lazi", "dog" };
            var actual = FullTextAnalyzer.Tokenize(text).ToArray();

            Assert.IsTrue(expected.SequenceEqual(actual));           

        }
    }
}
