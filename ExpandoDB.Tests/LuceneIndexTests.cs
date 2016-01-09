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
        [TestCategory("Lucene Index tests")]
        public void Can_convert_content_to_Lucene_document()
        {
            var content = CreateContent();
            var luceneDocument = content.ToLuceneDocument();
            var idField = luceneDocument.GetField(LuceneField.ID_FIELD_NAME);            
            var id = idField.StringValue();

            Assert.AreEqual<string>(content._id.ToString(), id);
        }

        private static Content CreateContent()
        {
            var content = new Content();
            content._id = Guid.NewGuid();
            content._createdTimestamp = DateTime.UtcNow;

            dynamic book = content;
            book.Title = "The Hitchhiker's Guide to the Galaxy";
            book.Author = "Douglas Adams";
            book.PublishDate = new DateTime(1979, 10, 12, 12, 0, 0, DateTimeKind.Utc);
            book.Rating = 10;
            book.Description = "The Hitchhiker's Guide to the Galaxy is a comedy science fiction series created by Douglas Adams. Originally a radio comedy broadcast on BBC Radio 4 in 1978, it was later adapted to other formats, and over several years it gradually became an international multi-media phenomenon.";
            book.RelatedTitles = new List<string> { "The Restaurant at the End of the Universe", "Life, the Universe and Everything" };
            book.Characters = new Dictionary<string, object> { { "Name", "Arthur Dent" }, { "Character", "Ford Prefect" } };
            book.X = null;

            return content;
        }

        [TestMethod]
        [TestCategory("Lucene Index tests")]
        public void Can_add_contents_and_search()
        {
            var contents = new List<Content>();
            for(var i = 0; i < 10; i++)
            {
                dynamic newContent = CreateContent();                
                newContent.BookId = 9-i;

                contents.Add(newContent);
                _luceneIndex.Insert(newContent);
                Thread.Sleep(1200);
                
            }

            var contentIds = contents.Select(c => c._id.Value).ToList();
            contentIds.Reverse();

            _luceneIndex.Refresh();

            var criteria = new SearchCriteria
            {
                Query = "hitchhiker AND galaxy",
                SortByField = "BookId",
                TopN = 10
            };           

            var result = _luceneIndex.Search(criteria);

            Assert.AreEqual<int?>(result.ItemCount, 10);
            Assert.AreEqual<int?>(result.ItemCount, result.TotalHits);
            Assert.IsTrue(result.Items.SequenceEqual(contentIds));
        }       
    }
}
