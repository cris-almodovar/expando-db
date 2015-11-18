using ExpandoDB.Storage;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Reflection;


namespace ExpandoDB.Tests
{
    [TestClass]    
    public class SQLiteDynamicStorageTests
    {
        private string _appPath;
        private string _dbPath;
        private string _dbFilePath;        
        private SQLiteContentStorage _storage;

        [TestInitialize]
        public void Initialize()
        {            
            _appPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            
            _dbPath = Path.Combine(_appPath, "db");
            if (!Directory.Exists(_dbPath))
                Directory.CreateDirectory(_dbPath);

            _dbFilePath = Path.Combine(_dbPath, Guid.NewGuid().ToString() + ".db");
            if (File.Exists(_dbFilePath))
                File.Delete(_dbFilePath);

            _storage = new SQLiteContentStorage(_dbFilePath, "books");
        }

        [TestCleanup]
        public void Cleanup()
        {
            SQLiteConnection.ClearAllPools();
            Thread.Sleep(1000);
            File.Delete(_dbFilePath);
            Directory.Delete(_dbPath, true);
        }

        [TestMethod]
        [TestCategory("Content Storage tests")]
        public void Database_file_is_auto_created()        
        {            
            var dbFilePath = Path.Combine(_dbPath, Guid.NewGuid().ToString() + ".db");
            if (File.Exists(dbFilePath))
                File.Delete(dbFilePath);

            var storage = new SQLiteContentStorage(dbFilePath, "test");

            Assert.IsTrue(File.Exists(dbFilePath));

            SQLiteConnection.ClearAllPools();
            File.Delete(dbFilePath);
        }

        [TestMethod]
        [TestCategory("Content Storage tests")]
        public void Can_insert_dynamic_content()
        {
            dynamic inserted = new Content();
            inserted.Title = "The Hitchhiker's Guide to the Galaxy";
            inserted.Author = "Douglas Adams";
            inserted.PublishDate = DateTime.Now;
            inserted.Rating = 10;
            inserted.Description = "The Hitchhiker's Guide to the Galaxy is a comedy science fiction series created by Douglas Adams. Originally a radio comedy broadcast on BBC Radio 4 in 1978, it was later adapted to other formats, and over several years it gradually became an international multi-media phenomenon.";
            inserted.RelatedTitles = new List<string> { "The Restaurant at the End of the Universe", "Life, the Universe and Everything" };
            inserted.Characters = new Dictionary<string, object> { { "Simon Jones", "Arthur Dent" }, { "Geoffrey McGivern", "Ford Prefect" } };
            inserted.X = null;

            var guid = _storage.InsertAsync(inserted).Result;
            Assert.AreNotEqual<Guid>(guid, Guid.Empty);
            
            dynamic retrieved = _storage.GetAsync(guid).Result;

            Assert.AreEqual<Guid>(inserted._id, retrieved._id);
            Assert.AreEqual<string>(inserted.Title, retrieved.Title);
            Assert.AreEqual<string>(inserted.Author, retrieved.Author);
            Assert.AreEqual<DateTime>(inserted.PublishDate, retrieved.PublishDate);
            Assert.AreEqual<int>(inserted.Rating, retrieved.Rating);
            Assert.AreEqual<string>(inserted.Description, retrieved.Description);

            Assert.AreEqual<string>(inserted.RelatedTitles[0], retrieved.RelatedTitles[0]);
            Assert.AreEqual<string>(inserted.RelatedTitles[1], retrieved.RelatedTitles[1]);

            var insertedCharacters = inserted.Characters as IDictionary<string, object>;
            var retrievedCharacters = retrieved.Characters as IDictionary<string, object>; 

            Assert.AreEqual<string>(insertedCharacters["Simon Jones"].ToString(), retrievedCharacters["Simon Jones"].ToString());
            Assert.AreEqual<string>(insertedCharacters["Geoffrey McGivern"].ToString(), retrievedCharacters["Geoffrey McGivern"].ToString());
        }

        [TestMethod]
        [TestCategory("Content Storage tests")]
        public void Can_delete_dynamic_content()
        {
            dynamic inserted = new Content();
            inserted.Title = "The Hitchhiker's Guide to the Galaxy";
            inserted.Author = "Douglas Adams";
            inserted.PublishDate = DateTime.Now;
            inserted.Rating = 10;
            inserted.Description = "The Hitchhiker's Guide to the Galaxy is a comedy science fiction series created by Douglas Adams. Originally a radio comedy broadcast on BBC Radio 4 in 1978, it was later adapted to other formats, and over several years it gradually became an international multi-media phenomenon.";
            inserted.RelatedTitles = new List<string> { "The Restaurant at the End of the Universe", "Life, the Universe and Everything" };
            inserted.Characters = new Dictionary<string, object> { { "Simon Jones", "Arthur Dent" }, { "Geoffrey McGivern", "Ford Prefect" } };
                       
            var guid = _storage.InsertAsync(inserted).Result;
            Assert.AreNotEqual<Guid>(guid, Guid.Empty);

            _storage.DeleteAsync(guid).Wait();

            dynamic retrieved = _storage.GetAsync(guid).Result;
            Assert.IsNull(retrieved);            
        }

        [TestMethod]
        [TestCategory("Content Storage tests")]
        public void Can_update_dynamic_content()
        {
            dynamic inserted = new Content();
            inserted.Title = "The Hitchhiker's Guide to the Galaxy";
            inserted.Author = "Douglas Adams";
            inserted.PublishDate = DateTime.Now;
            inserted.Rating = 10;
            inserted.Description = "The Hitchhiker's Guide to the Galaxy is a comedy science fiction series created by Douglas Adams. Originally a radio comedy broadcast on BBC Radio 4 in 1978, it was later adapted to other formats, and over several years it gradually became an international multi-media phenomenon.";
            
            var guid = _storage.InsertAsync(inserted).Result;
            Assert.AreNotEqual<Guid>(guid, Guid.Empty);       

            dynamic retrieved = _storage.GetAsync(guid).Result;
            retrieved.Rating = 12;
            _storage.UpdateAsync(retrieved).Wait();

            retrieved = _storage.GetAsync(retrieved._id).Result;

            Assert.AreEqual<int>(retrieved.Rating, 12);            
        }
    }
        
}
