
using ExpandoDB.Search;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ExpandoDB.Tests
{
    [TestClass]
    public class CollectionTests
    {
        private string _appPath;
        private string _dbPath;       
        private ContentCollection _collection;

        [TestInitialize]
        public void Initialize()
        {
            _appPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            _dbPath = Path.Combine(_appPath, "db");
            if (Directory.Exists(_dbPath))
                Directory.Delete(_dbPath, true); 

            _collection = new ContentCollection("books", _dbPath);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _collection.DropAsync().Wait();
            SQLiteConnection.ClearAllPools();            
            Directory.Delete(_dbPath, true);            
        }

        public static Content CreateBook(string title, string author, DateTime publishDate, int rating, string description)
        {
            var content = new Content();
            content._id = Guid.NewGuid();
            content._createdTimestamp = DateTime.UtcNow;

            dynamic book = content;
            book.Title = title; 
            book.Author = author; 
            book.PublishDate = publishDate; 
            book.Rating = rating; 
            book.Description = description; 

            return content;
        }

        [TestMethod]
        [TestCategory("Content Collection tests")]
        public void Can_insert_content()
        {
            var book = CreateBook("The Hitchhiker's Guide to the Galaxy", "Douglas Adams", new DateTime(1979, 10, 12, 12, 0, 0, DateTimeKind.Utc), 10, "The Hitchhiker's Guide to the Galaxy is a comedy science fiction series created by Douglas Adams. Originally a radio comedy broadcast on BBC Radio 4 in 1978, it was later adapted to other formats, and over several years it gradually became an international multi-media phenomenon.");
            var guid = _collection.InsertAsync(book).Result;

            Assert.AreEqual<Guid>(book._id.Value, guid);
        }

        [TestMethod]
        [TestCategory("Content Collection tests")]
        public void Can_search_for_content()
        {
            var book1 = CreateBook("The Hitchhiker's Guide to the Galaxy", "Douglas Adams", new DateTime(1979, 10, 12, 12, 0, 0, DateTimeKind.Utc), 10, "The Hitchhiker's Guide to the Galaxy is a comedy science fiction series created by Douglas Adams. Originally a radio comedy broadcast on BBC Radio 4 in 1978, it was later adapted to other formats, and over several years it gradually became an international multi-media phenomenon.");
            var book2 = CreateBook("The Restaurant at the End of the Universe", "Douglas Adams", new DateTime(1980, 10, 12, 12, 0, 0, DateTimeKind.Utc), 9, "The Restaurant at the End of the Universe (1980, ISBN 0-345-39181-0) is the second book in the Hitchhiker's Guide to the Galaxy comedy science fiction 'trilogy' by Douglas Adams, and is a sequel.");
            var book3 = CreateBook("Life, the Universe and Everything", "Douglas Adams", new DateTime(1982, 10, 12, 12, 0, 0, DateTimeKind.Utc), 9, "Life, the Universe and Everything (1982, ISBN 0-345-39182-9) is the third book in the five-volume Hitchhiker's Guide to the Galaxy science fiction trilogy by British writer Douglas Adams. The title refers to the Answer to Life, the Universe, and Everything.");
            var book4 = CreateBook("So Long, and Thanks for All the Fish", "Douglas Adams", new DateTime(1984, 10, 12, 12, 0, 0, DateTimeKind.Utc), 9, "So Long, and Thanks for All the Fish is the fourth book of the Hitchhiker's Guide to the Galaxy trilogy written by Douglas Adams. Its title is the message left by the dolphins when they departed Planet Earth just before it was demolished to make way for a hyperspace bypass, as described in The Hitchhiker's Guide to the Galaxy.");

            foreach (var book in new[] {  book1, book2, book3, book4 })
                _collection.InsertAsync(book).Wait();

            Thread.Sleep(1000);

            var criteria = new SearchCriteria
            {
                Query = @"Rating:9",
                SortByField = "Title"
            };

            var result = _collection.SearchAsync(criteria).Result;
            dynamic firstItem = result.Items.First(); 
                   
            var expected = "Life, the Universe and Everything";
            var actual = firstItem.Title as string;

            Assert.AreEqual<string>(expected, actual);


        }
    }
}
