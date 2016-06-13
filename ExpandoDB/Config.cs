using ExpandoDB.Search;
using System.IO;
using System.Reflection;

namespace ExpandoDB
{
    public static class Config
    {
        public static string DataPath { get; set; }
        public static string LuceneNullToken { get; set; }

        static Config()
        {
            var appPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);            
            DataPath = Path.Combine(appPath, Database.DATA_DIRECTORY_NAME);

            LuceneNullToken = LuceneUtils.DEFAULT_NULL_TOKEN;
        }
    }
}
