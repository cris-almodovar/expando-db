using RestSharp;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Linq;

namespace Loader
{
    class Program
    {
        static void Main(string[] args)
        {
            var expandoDbUrl = ConfigurationManager.AppSettings["ExpandoDbUrl"] ?? "http://localhost:9000/db";
            var restClient = new RestClient(expandoDbUrl);

            Console.WriteLine("ExpandoDB URL: " + expandoDbUrl);

            var appDirectory = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            Directory.SetCurrentDirectory(appDirectory);            

            var datasetFolder = ConfigurationManager.AppSettings["DatasetFolder"] ?? "reuters";
            var sgmlFiles = Directory.GetFiles(datasetFolder, "*.sgm");

            Console.WriteLine(String.Format("Importing {0} sgml files from folder: '{1}'.", sgmlFiles.Length, Path.GetFullPath(datasetFolder)));

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var batchSize = Environment.ProcessorCount;

            foreach (var batch in sgmlFiles.InSetsOf(batchSize))
            {
                var tasks = new List<Task>();

                foreach (var fileName in batch)
                {
                    Console.WriteLine("Processing file: " + Path.GetFileName(fileName));

                    var task = Task.Factory.StartNew(
                        () =>
                        {
                            using (var reader = new StreamReader(fileName))
                            {                                
                                var buffer = new StringBuilder();

                                var line = reader.ReadLine();
                                while (line != null)
                                {
                                    if (!line.StartsWith("<!DOCTYPE", StringComparison.InvariantCulture))
                                        buffer.AppendLine(line);

                                    if (line.StartsWith("</REUTERS>", StringComparison.InvariantCulture))
                                    {
                                        // We encountered the closing tag, get all the lines accumulated so far
                                        // and convert to XmlDocument.
                                        try
                                        {
                                            var xml = buffer.ToString();
                                            buffer.Clear();

                                            var xDoc = new XmlDocument();
                                            xDoc.Load(new StringReader(xml));
                                            var reuters = xDoc.DocumentElement;

                                            var date = reuters["DATE"].InnerText;
                                            var text = reuters["TEXT"];
                                            var title = text["TITLE"] != null ? text["TITLE"].InnerText : null;
                                            var body = text["BODY"] != null ? text["BODY"].InnerText : null;

                                            DateTime dateTime;
                                            DateTime.TryParse(date, out dateTime);

                                            var document = new
                                            {
                                                date = dateTime > DateTime.MinValue ? (DateTime?)dateTime : null,
                                                title = title,
                                                text = body
                                            };

                                            var restRequest = new RestRequest
                                            {
                                                Resource = "/reuters",
                                                Method = Method.POST,
                                                DateFormat = DateFormat.ISO_8601
                                            };

                                            restRequest.AddJsonBody(document);
                                            restClient.Execute(restRequest);
                                        }
                                        catch (Exception e)
                                        {
                                            Console.WriteLine(e.Message);
                                        }                                        
                                    }

                                    line = reader.ReadLine();
                                }
                            }

                            Console.WriteLine("Finished processing file: " + Path.GetFileName(fileName));
                        },
                        TaskCreationOptions.LongRunning
                   );

                    tasks.Add(task);

                }

                Task.WaitAll(tasks.ToArray());

            }

            stopwatch.Stop();
            Console.WriteLine("Finished importing files. Elapsed = " + stopwatch.Elapsed.ToString());
        }        
    }

    public static class Extension
    {
        public static IEnumerable<IEnumerable<T>> InSetsOf<T>(this IEnumerable<T> source, int size)
        {
            var toReturn = new List<T>(size);
            foreach (var item in source)
            {
                toReturn.Add(item);
                if (toReturn.Count == size)
                {
                    yield return toReturn;
                    toReturn = new List<T>(size);
                }
            }
            if (toReturn.Any())
            {
                yield return toReturn;
            }
        }
    }


}
