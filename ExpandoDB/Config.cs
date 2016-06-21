using ExpandoDB.Search;
using System.IO;
using System.Reflection;

namespace ExpandoDB
{
    /// <summary>
    /// 
    /// </summary>
    public static class Config
    {
        /// <summary>
        /// Gets or sets the Storage Engine data path.
        /// </summary>
        /// <value>
        /// The data path.
        /// </value>
        public static string DataPath { get; set; }
        /// <summary>
        /// Gets or sets the Lucene null token.
        /// </summary>
        /// <value>
        /// The lucene null token.
        /// </value>
        public static string LuceneNullToken { get; set; }

        static Config()
        {
            var appPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);            
            DataPath = Path.Combine(appPath, Database.DATA_DIRECTORY_NAME);

            LuceneNullToken = LuceneUtils.DEFAULT_NULL_TOKEN;
        }
    }
}
