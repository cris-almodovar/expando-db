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
using System.IO.Compression;
using System.Threading;

namespace Loader
{
    /// <summary>
    /// This sample program loads the reuters-21578 dataset into ExpandoDB; this dataset contains Reuters news articles from the 80's.
    /// </summary>
    class Program
    {
        static void Main()
        {
            var expandoDbUrl = ConfigurationManager.AppSettings["ExpandoDbUrl"] ?? "http://localhost:9000";
            var restClient = new RestClient(expandoDbUrl);

            Console.WriteLine("ExpandoDB URL: " + expandoDbUrl);

            var appDirectory = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            Directory.SetCurrentDirectory(appDirectory);            

            var dataset = ConfigurationManager.AppSettings["Dataset"] ?? @"reuters\reuters-21578.zip";
            var datasetFolder = Path.GetDirectoryName(dataset);

            if (Directory.EnumerateFiles(datasetFolder, "*.sgm").Count() == 0)
                ZipFile.ExtractToDirectory(dataset, datasetFolder);
            
            var sgmlFiles = Directory.GetFiles(datasetFolder, "*.sgm");

            var dropRequest = new RestRequest("/db/reuters?drop=true", Method.DELETE);
            restClient.Execute(dropRequest);

            Thread.Sleep(TimeSpan.FromSeconds(5));

            Console.WriteLine(String.Format("Importing {0} sgml files from folder: '{1}'.", sgmlFiles.Length, Path.GetFullPath(datasetFolder)));

            var stopwatch = new Stopwatch();
            stopwatch.Start();            

            var schemaRequest = new RestRequest("db/_schemas/reuters", Method.PUT);
            var schema = new
            {
                Name = "reuters",
                Fields = new List<object>
                {
                    new
                    {
                        Name = "date",
                        DataType = "DateTime",                        
                        FacetSettings = new
                        {
                            FacetName = "Publish Date",
                            IsHierarchical = true,
                            HierarchySeparator = "/",
                            FormatString = "yyyy/MMM/dd"
                        }
                    },
                    new
                    {
                        Name = "themes",
                        DataType = "Array",
                        ArrayElementDataType = "Text",
                        FacetSettings = new
                        {
                            FacetName = "Themes",
                            IsHierarchical = false
                        }
                    },
                }
            };

            schemaRequest.AddJsonBody(schema);
            var response = restClient.Execute(schemaRequest);

            var options = new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount };
            Parallel.ForEach(sgmlFiles, options, filePath =>
            {
                var fileName = Path.GetFileName(filePath);
                Console.WriteLine("Processing file: " + fileName);

                using (var reader = new StreamReader(filePath))
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
                                var title = text["TITLE"]?.InnerText;
                                var body = text["BODY"]?.InnerText;                                                             

                                DateTime dateTime;
                                DateTime.TryParse(date, out dateTime);

                                var themes = new List<string>();
                                var topicsNode = reuters["TOPICS"];                                
                                if (topicsNode != null)
                                {
                                    foreach (XmlNode childNode in topicsNode.ChildNodes)
                                        if (!String.IsNullOrWhiteSpace(childNode.InnerText))
                                        {
                                            var theme = childNode.InnerText.Replace(@"/", @"\/");
                                            themes.Add(theme);
                                        }
                                }

                                var document = new
                                {
                                    date = dateTime > DateTime.MinValue ? (DateTime?)dateTime : null,
                                    title = title,
                                    text = body,
                                    themes = themes
                                };

                                var restRequest = new RestRequest
                                {
                                    Resource = "db/reuters",
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
                Console.WriteLine("Finished processing file: " + fileName);
            }

            );

            stopwatch.Stop();
            Console.WriteLine("Finished importing files. Elapsed = " + stopwatch.Elapsed);
            Console.ReadLine();
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
