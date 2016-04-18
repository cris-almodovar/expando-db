using ExpandoDB.Search;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace ExpandoDB.Tests
{
    [TestClass]
    public class DatabaseTests
    {
        private string _dbPath;
        private Database _db;

        [TestInitialize]
        public void Initialize()
        {
            var appPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            _dbPath = Path.Combine(appPath, $"db-{Guid.NewGuid()}");
            if (Directory.Exists(_dbPath))
                Directory.Delete(_dbPath, true);

            _db = new Database(_dbPath);

            var book1 = TestUtils.CreateBook("The Hitchhiker's Guide to the Galaxy", "Douglas Adams", new DateTime(1979, 10, 12, 12, 0, 0, DateTimeKind.Utc), 10, "The Hitchhiker's Guide to the Galaxy is a comedy science fiction series created by Douglas Adams. Originally a radio comedy broadcast on BBC Radio 4 in 1978, it was later adapted to other formats, and over several years it gradually became an international multi-media phenomenon.");
            var book2 = TestUtils.CreateBook("The Restaurant at the End of the Universe", "Douglas Adams", new DateTime(1980, 10, 12, 12, 0, 0, DateTimeKind.Utc), 9, "The Restaurant at the End of the Universe (1980, ISBN 0-345-39181-0) is the second book in the Hitchhiker's Guide to the Galaxy comedy science fiction 'trilogy' by Douglas Adams, and is a sequel.");
            var book3 = TestUtils.CreateBook("Life, the Universe and Everything", "Douglas Adams", new DateTime(1982, 10, 12, 12, 0, 0, DateTimeKind.Utc), 9, "Life, the Universe and Everything (1982, ISBN 0-345-39182-9) is the third book in the five-volume Hitchhiker's Guide to the Galaxy science fiction trilogy by British writer Douglas Adams. The title refers to the Answer to Life, the Universe, and Everything.");
            var book4 = TestUtils.CreateBook("So Long, and Thanks for All the Fish", "Douglas Adams", new DateTime(1984, 10, 12, 12, 0, 0, DateTimeKind.Utc), 9, "So Long, and Thanks for All the Fish is the fourth book of the Hitchhiker's Guide to the Galaxy trilogy written by Douglas Adams. Its title is the message left by the dolphins when they departed Planet Earth just before it was demolished to make way for a hyperspace bypass, as described in The Hitchhiker's Guide to the Galaxy.");
            var book5 = TestUtils.CreateBook("Mostly Harmless", "Douglas Adams", new DateTime(1992, 10, 12, 12, 0, 0, DateTimeKind.Utc), 9, "Mostly Harmless is a novel by Douglas Adams and the fifth book in the Hitchhiker's Guide to the Galaxy series. It was the last Hitchhiker's book written by Adams.");

            var booksCollection = _db["books"];
            foreach (var book in new[] { book1, book2, book3, book4, book5 })
                booksCollection.InsertAsync(book).Wait();

            // Give the collection some time to refresh the Lucene index
            Thread.Sleep(1000);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _db.Dispose();
            Thread.Sleep(TimeSpan.FromSeconds(15));
            Directory.Delete(_dbPath, true);
        }

        [TestMethod]
        [TestCategory("Database tests")]
        public void Can_count_document()
        {
            var criteria = new SearchCriteria { Query = "*:*" };
            var count = _db["books"].Count(criteria);

            Assert.AreEqual<int>(5, count);
        }

        [TestMethod]
        [TestCategory("Database tests")]
        public void Can_search_for_documents()
        {
            var criteria = new SearchCriteria { Query = "Author:Douglas", SortByField = "-Title", TopN = 1 };
            var result = _db["books"].SearchAsync(criteria).Result;

            Assert.AreEqual<int?>(1, result.ItemCount);
            Assert.AreEqual<int?>(5, result.TotalHits);

            dynamic firstItem = result.Items.First();
            Assert.AreEqual<string>("The Restaurant at the End of the Universe", firstItem.Title);
        }

        [TestMethod]
        [TestCategory("Database tests")]
        public void Can_dispose_and_reload()
        {
            _db.Dispose();
            _db = new Database(_dbPath);

            var criteria = new SearchCriteria { Query = "Author:Douglas", SortByField = "-Title", TopN = 1 };
            var result = _db["books"].SearchAsync(criteria).Result;

            Assert.AreEqual<int?>(1, result.ItemCount);
            Assert.AreEqual<int?>(5, result.TotalHits);

            dynamic firstItem = result.Items.First();
            Assert.AreEqual<string>("The Restaurant at the End of the Universe", firstItem.Title);
        }
    }
}
