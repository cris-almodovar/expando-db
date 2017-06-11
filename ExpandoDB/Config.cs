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

        /// <summary>
        /// Gets or sets a value indicating whether this instance is automatic facet enabled.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is automatic facet enabled; otherwise, <c>false</c>.
        /// </value>
        public static bool IsAutoFacetEnabled { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is automatic document values enabled.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is automatic document values enabled; otherwise, <c>false</c>.
        /// </value>
        public static bool IsAutoDocValuesEnabled { get; set; }

        static Config()
        {
            var appPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);            
            DataPath = Path.Combine(appPath, Database.DATA_DIRECTORY_NAME);

            LuceneNullToken = LuceneUtils.DEFAULT_NULL_TOKEN;

            IsAutoFacetEnabled = true;
            IsAutoDocValuesEnabled = true;
        }
    }
}
