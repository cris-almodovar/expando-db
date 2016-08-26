using RestSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TikaOnDotNet.TextExtraction;

namespace FileIndexer
{
    /// <summary>
    /// This sample program reads the contents of your "My Documents" folder and looks for doc, docx, and pdf files
    /// that are less than 100 MB in size; it then extracts the text and metadata from the files using the Tika library 
    /// (https://kevm.github.io/tikaondotnet), and then sends all these data (as JSON) to a running ExpandoDB server instance.
    /// <para>
    /// The Tika library supports lots of other file types - it would be easy to modify this sample to implement 
    /// a file crawler that periodically scans your "My Documents" folder to index its contents.
    /// </para>
    /// <para>
    /// Once the documents are indexed, you can go to http://localhost:9000/db/documents and query the documents collection
    /// like so: 
    /// <![CDATA[
    /// http://localhost:9000/db/documents?select=FilePath,CreatedDate&where="machine learning"^4 AND "logistic regression"&highlight=true
    /// ]]>  
    /// </para>
    /// </summary>
    static class Program
    {
        const string EXPANDO_DB_URL = "http://localhost:9000/db";
        const int ONE_MB = 1 * 1024 * 1024;
        const int TEN_MB = 10 * 1024 * 1024;
        const int TWENTY_MB = 20 * 1024 * 1024;
        const int FIFTY_MB = 50 * 1024 * 1024;
        const int ONE_HUNDRED_MB = 100 * 1024 * 1024;

        static void Main(string[] args)
        {
            var startFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            if (args.Length > 0)
                startFolder = args[0];

            if (!Directory.Exists(startFolder))
                throw new InvalidOperationException($"Start folder does not exist: {startFolder}");

            var restClient = new RestClient(EXPANDO_DB_URL);
            
            var filterMasks = new[] { "*.doc", "*.docx", "*.pdf", "*.ppt", "*.pptx" };
            Func<FileInfo, bool> fileCheck = fi => fi.Length <= ONE_HUNDRED_MB;  // File size less than 100 MB

            Console.WriteLine("-----------------------------------------------------------------------------------");
            Console.WriteLine($"FileIndexer starting at: {startFolder}");

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var allDocumentFiles = GetFiles(startFolder, filterMasks, fileCheck);            
            var processedCount = 0;

            var options = new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount };
            Parallel.ForEach(allDocumentFiles, options, file =>
            {
                try
                {
                    // Lets create our document object
                    dynamic document = new ExpandoObject();
                    document.FilePath = file.FullName;
                    document.Size = file.Length;
                    document.CreatedDate = file.CreationTimeUtc;
                    document.LastModifiedDate = file.LastWriteTimeUtc;

                    // Now let's extract the text from the file.                    

                    var textExtractor = new TextExtractor();
                    var result = textExtractor.Extract(file.FullName);
                    var text = result.Text;
                    var contentType = result.ContentType;

                    var sizeCategory = "";
                    if (file.Length < ONE_MB)
                        sizeCategory = "Less than 1 MB";
                    else if (file.Length >= ONE_MB && file.Length < TEN_MB)
                        sizeCategory = "Between 1 MB to 10 MB";
                    else if (file.Length >= TEN_MB && file.Length < TWENTY_MB)
                        sizeCategory = "Between 10 MB to 20 MB";
                    else if (file.Length >= TWENTY_MB && file.Length < FIFTY_MB)
                        sizeCategory = "Between 20 MB to 50 MB";
                    else if (file.Length >= FIFTY_MB && file.Length < ONE_HUNDRED_MB)
                        sizeCategory = "Between 50 MB to 100 MB";

                    var dateFormat = "yyyy/MMM/dd";
                    var categories = new[]
                    {
                        $"File Size:{sizeCategory}",
                        $"Last Modified Date:{file.LastWriteTimeUtc.ToString(dateFormat)}"
                    };

                    document.Text = text;
                    document.ContentType = contentType;
                    document._categories = categories;

                    // Now lets submit the document to ExpandoDB via the REST API
                    var request = new RestRequest("/documents", Method.POST) { DateFormat = DateFormat.ISO_8601 };
                    request.AddJsonBody(document);

                    var response = restClient.Post(request);
                    response.Validate();

                    processedCount += 1;

                }
                catch (Exception ex)
                {
                    Console.WriteLine($"There was an error processing '{file.FullName}' - {ex.Message}");
                }
            }
            );

            stopwatch.Stop();

            Console.WriteLine("-----------------------------------------------------------------------------------");            
            Console.WriteLine($"Processed {processedCount} {String.Join(",", filterMasks)} files in {stopwatch.Elapsed}");

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
                    bool include = true;
                    FileInfo fileInfo = null;

                    if (predicate != null)
                    {
                        try
                        {
                            fileInfo = new FileInfo(fileName);
                            include = predicate(fileInfo);
                        }
                        catch { }
                    }

                    if (include && fileInfo != null)
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
