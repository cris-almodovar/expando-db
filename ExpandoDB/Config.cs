﻿using ExpandoDB.Search;
using System.IO;
using System.Reflection;

namespace ExpandoDB
{
    public static class Config
    {
        public static string DbPath { get; set; }
        public static string LuceneNullToken { get; set; }

        static Config()
        {
            var appPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);            
            DbPath = Path.Combine(appPath, Database.DB_DIRECTORY_NAME);

            LuceneNullToken = LuceneExtensions.DEFAULT_NULL_TOKEN;
        }
    }
}