using RestSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Toxy;

namespace FileIndexer
{
    static class Program
    {
        static void Main(string[] args)
        {
            var startFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            if (args.Length > 0)
                startFolder = args[0];

            if (!Directory.Exists(startFolder))
                throw new InvalidOperationException($"Start folder does not exist: {startFolder}");

            var restClient = new RestClient("http://localhost:9000/db");
            
            var filterMasks = new[] { "*.doc", "*.docx", "*.pdf"};
            Func<FileInfo, bool> isFileSizeLessThan20MB = fi => fi.Length <= 20 * 1024 * 1024;  // File size less than 20 MB

            Console.WriteLine("-----------------------------------------------------------------------------------");
            Console.WriteLine($"FileIndexer starting at: {startFolder}");

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var allDocumentFiles = GetFiles(startFolder, filterMasks, isFileSizeLessThan20MB);            
            var processedCount = 0;

            foreach (var file in allDocumentFiles)
            {
                try
                {
                    var context = new ParserContext(file.FullName);
                    var metadataParser = ParserFactory.CreateMetadata(context) as IMetadataParser;
                    var metadata = metadataParser.Parse();

                    var textParser = ParserFactory.CreateText(context) as ITextParser;
                    var text = textParser.Parse();

                    var dictionary = new Dictionary<string, object>();
                    dictionary["Text"] = text;
                    dictionary["FileName"] = file.FullName;
                    dictionary["Size"] = file.Length;
                    dictionary["CreationTimeUtc"] = file.CreationTimeUtc;
                    dictionary["LastAccessTimeUtc"] = file.LastAccessTimeUtc;
                    dictionary["LastWriteTimeUtc"] = file.LastWriteTimeUtc;

                    foreach (var fieldName in metadata.GetNames())
                    {
                        dictionary[fieldName] = metadata.Get(fieldName).Value;
                    }

                    var request = new RestRequest("/documents", Method.POST) { DateFormat = DateFormat.ISO_8601 };
                    request.AddJsonBody(dictionary);

                    var response = restClient.Post(request);
                    response.Validate();

                    processedCount += 1;

                }
                catch { }
            }

            stopwatch.Stop();

            Console.WriteLine("-----------------------------------------------------------------------------------");            
            Console.WriteLine($"Processed {processedCount} *.docx, *.doc, *.pdf files in {stopwatch.Elapsed}");

            Console.WriteLine("\n\nPRESS ENTER TO EXIT");
            Console.ReadLine();
        }

        static IEnumerable<FileInfo> GetFiles(string currentFolder, IEnumerable<string> filterMasks, Func<FileInfo, bool> predicate = null)
        {
            Queue<string> queue = new Queue<string>();
            queue.Enqueue(currentFolder);

            while (queue.Count > 0)
            {
                currentFolder = queue.Dequeue();
                try
                {
                    foreach (string childFolder in Directory.GetDirectories(currentFolder))
                        queue.Enqueue(childFolder);
                }
                catch { }

                var fileNames = new HashSet<string>();

                foreach (var mask in filterMasks)
                {
                    try
                    {
                        if (!String.IsNullOrWhiteSpace(mask))
                        {
                            var matchingFiles = Directory.GetFiles(currentFolder, mask);
                            foreach (var file in matchingFiles)
                                fileNames.Add(file);
                        }
                    }
                    catch { }
                }

                foreach (var fileName in fileNames)
                {   
                    var fileInfo = new FileInfo(fileName);
                    bool include = true;

                    if (predicate != null)
                    {
                        try
                        {
                            include = predicate(fileInfo);
                        }
                        catch { }
                    }

                    if (include)
                        yield return fileInfo;
                }

            }
        }

        static void Validate(this IRestResponse response)
        {
            if (response.ErrorException != null)
                throw response.ErrorException;

            switch (response.ResponseStatus)
            {
                case ResponseStatus.Aborted:
                    throw new Exception("The API request was aborted");
                case ResponseStatus.Error:
                    throw new Exception("The API request failed due to an error");
                case ResponseStatus.TimedOut:
                    throw new Exception("The API request failed due to a timeout error");
            }

            if (response.StatusCode != HttpStatusCode.OK)
                throw new Exception($"The API request failed due to an error. HTTP status code is: {response.StatusCode}");            
        }

    }
}
