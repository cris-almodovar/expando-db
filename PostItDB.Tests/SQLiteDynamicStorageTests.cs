using System;
using System.Data.SQLite;
using System.Dynamic;
using System.IO;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PostItDB.Storage;
using System.Collections.Generic;

namespace PostItDB.Tests
{
    [TestClass]
    public class SQLiteDynamicStorageTests
    {
        private string _appPath;

        [TestInitialize]
        public void Initialize()
        {
            _appPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        }

        [TestMethod]
        public void Database_file_is_automatically_created()
        {            
            var dbFilePath = Path.Combine(_appPath, Guid.NewGuid().ToString() + ".db");
            if (File.Exists(dbFilePath))
                File.Delete(dbFilePath);

            var storage = new SQLiteDynamicStorage(dbFilePath,"test");

            Assert.AreEqual<bool>(File.Exists(dbFilePath), true);

            SQLiteConnection.ClearAllPools();            
            File.Delete(dbFilePath);
        }

        [TestMethod]
        public void Can_insert_dynamic_content()
        {
            dynamic insertedContent = new ExpandoObject();
            insertedContent.Title = "The Hitchhiker's Guide to the Galaxy";
            insertedContent.Author = "Douglas Adams";
            insertedContent.PublishDate = DateTime.Now;
            insertedContent.Rating = 10;
            insertedContent.Text = "The Hitchhiker's Guide to the Galaxy is a comedy science fiction series created by Douglas Adams. Originally a radio comedy broadcast on BBC Radio 4 in 1978, it was later adapted to other formats, and over several years it gradually became an international multi-media phenomenon.";
            
            var dbFilePath = Path.Combine(_appPath, Guid.NewGuid().ToString() + ".db");
            var storage = new SQLiteDynamicStorage(dbFilePath, "content");
            
            var guid = storage.InsertAsync(insertedContent).Result;
            Assert.IsNotNull(guid);

            dynamic retrievedContent = storage.GetAsync(guid).Result;

            Assert.AreEqual<Guid>(insertedContent._id, retrievedContent._id);
            Assert.AreEqual<string>(insertedContent.Title, retrievedContent.Title);
            Assert.AreEqual<string>(insertedContent.Author, retrievedContent.Author);
            Assert.AreEqual<DateTime>(insertedContent.PublishDate, retrievedContent.PublishDate);
            Assert.AreEqual<int>(insertedContent.Rating, retrievedContent.Rating);
            Assert.AreEqual<string>(insertedContent.Text, retrievedContent.Text);            

            SQLiteConnection.ClearAllPools();
            File.Delete(dbFilePath);
        }
    }
}
