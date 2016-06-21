using ExpandoDB.Storage;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;


namespace ExpandoDB.Tests
{
    [TestClass]    
    public class ContentStorageTests
    {
        private string _appPath;
        private string _dataPath;                  
        private IDocumentStorage _documentStorage;
        const string COLLECTION_NAME = "books";

        [TestInitialize]
        public void Initialize()
        {            
            _appPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            
            _dataPath = Path.Combine(_appPath, "data");
            if (!Directory.Exists(_dataPath))
                Directory.CreateDirectory(_dataPath);
            
            _documentStorage = new LightningDocumentStorage(_dataPath);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _documentStorage.Dispose();

            Thread.Sleep(TimeSpan.FromSeconds(2));            
            Directory.Delete(_dataPath, true);
        }

        [TestMethod]
        [TestCategory("Document Storage tests")]
        public void Database_file_is_auto_created()        
        {            
            var dbFilePath = Path.Combine(_dataPath, "data.mdb");  
            Assert.IsTrue(File.Exists(dbFilePath));
        }

        [TestMethod]
        [TestCategory("Document Storage tests")]
        public void Can_insert_dynamic_document()
        {
            dynamic inserted = new Document();
            inserted.Title = "The Hitchhiker's Guide to the Galaxy";
            inserted.Author = "Douglas Adams";
            inserted.PublishDate = DateTime.Now;
            inserted.Rating = 10;
            inserted.Description = "The Hitchhiker's Guide to the Galaxy is a comedy science fiction series created by Douglas Adams. Originally a radio comedy broadcast on BBC Radio 4 in 1978, it was later adapted to other formats, and over several years it gradually became an international multi-media phenomenon.";
            inserted.RelatedTitles = new List<string> { "The Restaurant at the End of the Universe", "Life, the Universe and Everything" };
            inserted.Characters = new Dictionary<string, object> { { "Simon Jones", "Arthur Dent" }, { "Geoffrey McGivern", "Ford Prefect" } };
            inserted.X = null;

            var guid = _documentStorage.InsertAsync(COLLECTION_NAME, inserted).Result;
            Assert.AreNotEqual<Guid>(guid, Guid.Empty);
            
            dynamic retrieved = _documentStorage.GetAsync(COLLECTION_NAME, guid).Result;

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
        [TestCategory("Document Storage tests")]
        public void Can_delete_dynamic_document()
        {
            dynamic inserted = new Document();
            inserted.Title = "The Hitchhiker's Guide to the Galaxy";
            inserted.Author = "Douglas Adams";
            inserted.PublishDate = DateTime.Now;
            inserted.Rating = 10;
            inserted.Description = "The Hitchhiker's Guide to the Galaxy is a comedy science fiction series created by Douglas Adams. Originally a radio comedy broadcast on BBC Radio 4 in 1978, it was later adapted to other formats, and over several years it gradually became an international multi-media phenomenon.";
            inserted.RelatedTitles = new List<string> { "The Restaurant at the End of the Universe", "Life, the Universe and Everything" };
            inserted.Characters = new Dictionary<string, object> { { "Simon Jones", "Arthur Dent" }, { "Geoffrey McGivern", "Ford Prefect" } };
                       
            var guid = _documentStorage.InsertAsync(COLLECTION_NAME, inserted).Result;
            Assert.AreNotEqual<Guid>(guid, Guid.Empty);

            _documentStorage.DeleteAsync(COLLECTION_NAME, guid).Wait();

            dynamic retrieved = _documentStorage.GetAsync(COLLECTION_NAME, guid).Result;
            Assert.IsNull(retrieved);            
        }

        [TestMethod]
        [TestCategory("Document Storage tests")]
        public void Can_update_dynamic_document()
        {
            dynamic inserted = new Document();
            inserted.Title = "The Hitchhiker's Guide to the Galaxy";
            inserted.Author = "Douglas Adams";
            inserted.PublishDate = DateTime.Now;
            inserted.Rating = 10;
            inserted.Description = "The Hitchhiker's Guide to the Galaxy is a comedy science fiction series created by Douglas Adams. Originally a radio comedy broadcast on BBC Radio 4 in 1978, it was later adapted to other formats, and over several years it gradually became an international multi-media phenomenon.";
            
            var guid = _documentStorage.InsertAsync(COLLECTION_NAME, inserted).Result;
            Assert.AreNotEqual<Guid>(guid, Guid.Empty);       

            dynamic retrieved = _documentStorage.GetAsync(COLLECTION_NAME, guid).Result;
            retrieved.Rating = 12;
            _documentStorage.UpdateAsync(COLLECTION_NAME, retrieved).Wait();

            retrieved = _documentStorage.GetAsync(COLLECTION_NAME, retrieved._id).Result;

            Assert.AreEqual<int>(retrieved.Rating, 12);            
        }
    }
        
}
