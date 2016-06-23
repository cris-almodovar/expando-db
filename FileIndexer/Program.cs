using RestSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Toxy;

namespace FileIndexer
{
    /// <summary>
    /// This sample program reads the contents of your "My Documents" folder and looks for doc, docx, and pdf files
    /// that are less than 100 MB in size; it then extracts the text and metadata from the files using the toxy library 
    /// (https://github.com/tonyqus/toxy), and then sends all these data (as JSON) to a running ExpandoDB server instance.
    /// <para>
    /// The toxy library supports lots of other file types - it would be easy to modify this sample to implement 
    /// a file crawler that periodically scans your "My Documents" folder to index its contents.
    /// </para>
    /// <para>
    /// Once the documents are indexed, you can go to http://localhost:9000/db/documents and query the documents collection
    /// like so: 
    /// <![CDATA[
    /// http://localhost:9000/db/documents?select=FileName,LastWriteTimeUtc&where="machine learning" AND "logistic regression"&highlight=true
    /// ]]>  
    /// </para>
    /// </summary>
    static class Program
    {
        const string EXPANDO_DB_URL = "http://localhost:9000/db";

        static void Main(string[] args)
        {
            var startFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            if (args.Length > 0)
                startFolder = args[0];

            if (!Directory.Exists(startFolder))
                throw new InvalidOperationException($"Start folder does not exist: {startFolder}");

            var restClient = new RestClient(EXPANDO_DB_URL);
            
            var filterMasks = new[] { "*.doc", "*.docx", "*.pdf"};
            Func<FileInfo, bool> fileCheck = fi => fi.Length <= 100 * 1024 * 1024;  // File size less than 100 MB

            Console.WriteLine("-----------------------------------------------------------------------------------");
            Console.WriteLine($"FileIndexer starting at: {startFolder}");

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var allDocumentFiles = GetFiles(startFolder, filterMasks, fileCheck);            
            var processedCount = 0;

            foreach (var file in allDocumentFiles)
            {
                try
                {
                    // Lets create our document object
                    dynamic document = new ExpandoObject();                     
                    document.FileName = file.FullName;
                    document.Size = file.Length;
                    document.CreationTimeUtc = file.CreationTimeUtc;
                    document.LastAccessTimeUtc = file.LastAccessTimeUtc;
                    document.LastWriteTimeUtc = file.LastWriteTimeUtc;

                    // Now let's extract the text from the file.
                    // WARNING - the toxy library doesn't handle multi-column pdf files correctly.

                    var context = new ParserContext(file.FullName);
                    var textParser = ParserFactory.CreateText(context) as ITextParser;
                    var text = textParser.Parse();

                    document.Text = text;

                    if (file.Extension.ToLower().StartsWith(".doc", StringComparison.InvariantCulture))
                    {
                        // Lets's extract metadata from doc, docx files

                        var metadataParser = ParserFactory.CreateMetadata(context) as IMetadataParser;
                        var metadata = metadataParser.Parse();

                        var dictionary = document as IDictionary<string, object>;
                        foreach (var fieldName in metadata.GetNames())                        
                            dictionary[fieldName] = metadata.Get(fieldName).Value;                        
                    }


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
