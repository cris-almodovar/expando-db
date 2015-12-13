using ExpandoDB.Search;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ExpandoDB
{
    public static class Config
    {
        public static string DbPath { get; set; }
        public static string LuceneNullToken { get; set; }

        static Config()
        {
            var appPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);            
            DbPath = Path.Combine(appPath, Database.DB_DIR_NAME);

            LuceneNullToken = LuceneField.DEFAULT_NULL_VALUE_TOKEN;
        }
    }
}
