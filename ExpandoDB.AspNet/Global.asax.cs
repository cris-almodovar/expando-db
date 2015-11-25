using ExpandoDB.Rest;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.SessionState;

[assembly: log4net.Config.XmlConfigurator(ConfigFile = "log4net.config", Watch = true)]

namespace ExpandoDB.AspNet
{
    public class Global : System.Web.HttpApplication
    {

        protected void Application_Start(object sender, EventArgs e)
        {
            var dbPath = ConfigurationManager.AppSettings["DbPath"] ?? "db";
            Config.DbPath = Server.MapPath(dbPath);
        }        
    }
}