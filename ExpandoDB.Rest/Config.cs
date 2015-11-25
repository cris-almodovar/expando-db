using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ExpandoDB.Rest
{
    public static class Config
    {
        public static string DbPath { get; set; }

        static Config()
        {
            var appPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);            
            DbPath = Path.Combine(appPath, Database.DB_DIR_NAME);
        }
    }
}
