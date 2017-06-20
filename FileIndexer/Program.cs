using org.apache.tika;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
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
        const int THIRTY_MB = 30 * 1024 * 1024;
        const int FORTY_MB = 40 * 1024 * 1024;
        const int FIFTY_MB = 50 * 1024 * 1024;

        static readonly TextExtractor _textExtractor = new TextExtractor();
        static int _processedCount = 0;
        

        static void Main(string[] args)
        {
            var startFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            if (args.Length > 0)
                startFolder = args[0];

            if (!Directory.Exists(startFolder))
                throw new InvalidOperationException($"Start folder does not exist: {startFolder}");
            
            
            var filterMasks = new[] { "*.doc", "*.docx", "*.pdf", "*.ppt", "*.pptx" };
            Func<FileInfo, bool> fileCheck = fi => fi.Length <= FIFTY_MB;  // File size less than 50 MB                      

            Console.WriteLine("-----------------------------------------------------------------------------------");
            Console.WriteLine($"FileIndexer starting at: {startFolder}");

            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var batchSize = 4;  //Environment.ProcessorCount;

            var allDocumentFiles = GetFiles(startFolder, filterMasks, fileCheck);

            var fileTheadMap = new Dictionary<FileInfo, Thread>();

            var threadCompletedSignals = new ManualResetEvent[batchSize];
            for (var i = 0; i < batchSize; i++)
                threadCompletedSignals[i] = new ManualResetEvent(false);

            foreach (var batch in allDocumentFiles.InSetsOf(batchSize))
            {
                fileTheadMap.Clear();
                for (var i = 0; i < batchSize; i++)
                    threadCompletedSignals[i].Reset();

                var filesInBatch = batch.ToList();
                for (var i = 0; i < filesInBatch.Count; i++)
                {
                    var file = filesInBatch[i];
                    var mre = threadCompletedSignals[i];

                    var thread = new Thread(new ParameterizedThreadStart(ProcessFile)) { IsBackground = true, Priority = ThreadPriority.BelowNormal };
                    fileTheadMap.Add(file, thread);
                    thread.Start(new Tuple<FileInfo, ManualResetEvent>(file, mre));
                }

                WaitHandle.WaitAll(threadCompletedSignals, TimeSpan.FromSeconds(30), false);                       
                foreach (var file in fileTheadMap.Keys)
                {
                    var thread = fileTheadMap[file];                    
                    if (thread.ThreadState == System.Threading.ThreadState.Running)
                    {
                        thread.Abort();
                        Console.WriteLine($"Processing of {file.FullName} was cancelled because it took more than 30 secs.");
                    }
                }                

                GC.Collect();
            }

            stopwatch.Stop();
            for (var i = 0; i < batchSize; i++)
                threadCompletedSignals[i].Dispose();

            Console.WriteLine("-----------------------------------------------------------------------------------");            
            Console.WriteLine($"Processed {_processedCount} {String.Join(",", filterMasks)} files in {stopwatch.Elapsed}");
            Console.WriteLine($"Peak working set: {Process.GetCurrentProcess().PeakWorkingSet64} bytes");           

            Console.WriteLine("\n\nPRESS ENTER TO EXIT");
            Console.ReadLine();
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Console.WriteLine(e.ToString());
        }

        static void ProcessFile(object args)
        {            
            var data = args as Tuple<FileInfo, ManualResetEvent>;
            if (data == null)
                return;

            var file = data.Item1;
            var mre = data.Item2;

            Console.WriteLine($"Processing '{file}'.");

            try
            {
                // Lets create our document object
                dynamic document = new ExpandoObject();
                document.FilePath = file.FullName;
                document.Size = file.Length;
                document.CreatedDate = file.CreationTimeUtc;
                document.LastModifiedDate = file.LastWriteTimeUtc;

                // Now let's extract the text from the file.

                var result = _textExtractor.Extract(file.FullName);
                var text = result.Text;
                var metadata = result.Metadata;
                var contentType = result.ContentType;

                var sizeCategory = "";
                if (file.Length < ONE_MB)
                    sizeCategory = "Less than 1 MB";
                else if (file.Length >= ONE_MB && file.Length < TEN_MB)
                    sizeCategory = "Between 1 MB to 10 MB";
                else if (file.Length >= TEN_MB && file.Length < TWENTY_MB)
                    sizeCategory = "Between 10 MB to 20 MB";
                else if (file.Length >= TWENTY_MB && file.Length < THIRTY_MB)
                    sizeCategory = "Between 20 MB to 30 MB";
                else if (file.Length >= THIRTY_MB && file.Length < FORTY_MB)
                    sizeCategory = "Between 30 MB to 40 MB";
                else if (file.Length >= FORTY_MB && file.Length <= FIFTY_MB)
                    sizeCategory = "Between 40 MB to 50 MB";

                foreach (var key in metadata.Keys.Where(k => !String.IsNullOrWhiteSpace(k)))
                {
                    var fieldName = key.ToLowerInvariant().Trim();
                    if (fieldName != "author" &&
                        fieldName != "authors" &&
                        fieldName != "title")
                        continue;

                    var authorOrTitle = metadata[key];
                    if (String.IsNullOrWhiteSpace(authorOrTitle))
                        continue;

                    // Make sure the author or title value is not a Date/Time string
                    if (IsMaybeDateTime(authorOrTitle))
                    {
                        Console.WriteLine($"Invalid author or title metadata value: {authorOrTitle}");
                        continue;
                    }

                    authorOrTitle = authorOrTitle.Trim();
                    switch (fieldName)
                    {
                        case "author":
                        case "authors":
                            document.Author = authorOrTitle;                            
                            break;

                        case "title":
                            document.Title = authorOrTitle;
                            break;
                    }
                }

                document.Text = text;
                document.ContentType = contentType;
                document.SizeCategory = sizeCategory;                

                // Now lets submit the document to ExpandoDB via the REST API
                var request = new RestRequest("/my-documents", Method.POST) { DateFormat = DateFormat.ISO_8601 };
                request.AddJsonBody(document);

                var restClient = new RestClient(EXPANDO_DB_URL);
                var response = restClient.Post(request);
                response.Validate();

                Interlocked.Increment(ref _processedCount);                
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
            }
            finally
            {
                mre.Set();
            }
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
                    foreach (string childFolder in Directory.EnumerateDirectories(currentFolder))
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
                            var matchingFiles = Directory.EnumerateFiles(currentFolder, mask);
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

        private static bool IsMaybeDateTime(this string value)
        {
            if (String.IsNullOrWhiteSpace(value))
                return false;

            var length = value.Length;

            if (!(length >= DateTimeFormat.DATE_ONLY.Length &&
                  length <= DateTimeFormat.DATE_HHMMSSFFFFFFF_UTC.Length + 5
               ))
                return false;

            if (!(Char.IsNumber(value[0]) && Char.IsNumber(value[1]) && Char.IsNumber(value[2]) && Char.IsNumber(value[3]) &&
                  value[4] == '-' &&
                  Char.IsNumber(value[5]) && Char.IsNumber(value[6]) &&
                  value[7] == '-' &&
                  Char.IsNumber(value[8]) && Char.IsNumber(value[9])
               ))
                return false;

            if (length > DateTimeFormat.DATE_ONLY.Length &&
                  value[10] != 'T'
               )
                return false;

            if (length == DateTimeFormat.DATE_HHMM_UTC.Length ||
                length == DateTimeFormat.DATE_HHMMSS_UTC.Length ||
                length == DateTimeFormat.DATE_HHMMSSF_UTC.Length ||
                length == DateTimeFormat.DATE_HHMMSSFF_UTC.Length ||
                length == DateTimeFormat.DATE_HHMMSSFFF_UTC.Length ||
                length == DateTimeFormat.DATE_HHMMSSFFFF_UTC.Length ||
                length == DateTimeFormat.DATE_HHMMSSFFFFF_UTC.Length ||
                length == DateTimeFormat.DATE_HHMMSSFFFFFF_UTC.Length ||
                length == DateTimeFormat.DATE_HHMMSSFFFFFFF_UTC.Length
               )
            {
                if (!value.EndsWith("Z", StringComparison.InvariantCulture))
                    return false;
            }

            return true;
        }

    }

    /// <summary>
    /// Defines the DateTime formats supported by the ExpandoDB JSON serializer/deserializer.
    /// </summary>
    public static class DateTimeFormat
    {
        /// <summary>
        /// Date in "yyyy-MM-dd" format
        /// </summary>
        public const string DATE_ONLY = "yyyy-MM-dd";
        /// <summary>
        /// Date and time in "yyyy-MM-ddTHH:mm" format
        /// </summary>
        public const string DATE_HHMM = "yyyy-MM-ddTHH:mm";
        /// <summary>
        /// Date and time in "yyyy-MM-ddTHH:mmZ" format
        /// </summary>
        public const string DATE_HHMM_UTC = "yyyy-MM-ddTHH:mmZ";
        /// <summary>
        /// Date and time in "yyyy-MM-ddTHH:mmzzz" format
        /// </summary>
        public const string DATE_HHMM_TIMEZONE = "yyyy-MM-ddTHH:mmzzz";
        /// <summary>
        /// Date amd time in "yyyy-MM-ddTHH:mm:ss" format.
        /// </summary>
        public const string DATE_HHMMSS = "yyyy-MM-ddTHH:mm:ss";
        /// <summary>
        /// Date amd time in xxx format
        /// </summary>
        public const string DATE_HHMMSS_UTC = "yyyy-MM-ddTHH:mm:ssZ";
        /// <summary>
        /// Date amd time in "yyyy-MM-ddTHH:mm:sszzz" format
        /// </summary>
        public const string DATE_HHMMSS_TIMEZONE = "yyyy-MM-ddTHH:mm:sszzz";
        /// <summary>
        /// Date amd time in "yyyy-MM-ddTHH:mm:ss.f" format
        /// </summary>
        public const string DATE_HHMMSSF = "yyyy-MM-ddTHH:mm:ss.f";
        /// <summary>
        /// Date amd time in "yyyy-MM-ddTHH:mm:ss.fZ" format
        /// </summary>
        public const string DATE_HHMMSSF_UTC = "yyyy-MM-ddTHH:mm:ss.fZ";
        /// <summary>
        /// Date amd time in "yyyy-MM-ddTHH:mm:ss.fzzz" format
        /// </summary>
        public const string DATE_HHMMSSF_TIMEZONE = "yyyy-MM-ddTHH:mm:ss.fzzz";
        /// <summary>
        /// Date amd time in "yyyy-MM-ddTHH:mm:ss.ff" format
        /// </summary>
        public const string DATE_HHMMSSFF = "yyyy-MM-ddTHH:mm:ss.ff";
        /// <summary>
        /// Date amd time in "yyyy-MM-ddTHH:mm:ss.ffZ" format
        /// </summary>
        public const string DATE_HHMMSSFF_UTC = "yyyy-MM-ddTHH:mm:ss.ffZ";
        /// <summary>
        /// Date amd time in "yyyy-MM-ddTHH:mm:ss.ffzzz" format
        /// </summary>
        public const string DATE_HHMMSSFF_TIMEZONE = "yyyy-MM-ddTHH:mm:ss.ffzzz";
        /// <summary>
        /// Date amd time in "yyyy-MM-ddTHH:mm:ss.fff" format
        /// </summary>
        public const string DATE_HHMMSSFFF = "yyyy-MM-ddTHH:mm:ss.fff";
        /// <summary>
        /// Date amd time in "yyyy-MM-ddTHH:mm:ss.fffZ" format
        /// </summary>
        public const string DATE_HHMMSSFFF_UTC = "yyyy-MM-ddTHH:mm:ss.fffZ";
        /// <summary>
        /// Date amd time in "yyyy-MM-ddTHH:mm:ss.fffzzz" format
        /// </summary>
        public const string DATE_HHMMSSFFF_TIMEZONE = "yyyy-MM-ddTHH:mm:ss.fffzzz";
        /// <summary>
        /// Date amd time in "yyyy-MM-ddTHH:mm:ss.ffff" format
        /// </summary>
        public const string DATE_HHMMSSFFFF = "yyyy-MM-ddTHH:mm:ss.ffff";
        /// <summary>
        /// Date amd time in "yyyy-MM-ddTHH:mm:ss.ffffZ" format
        /// </summary>
        public const string DATE_HHMMSSFFFF_UTC = "yyyy-MM-ddTHH:mm:ss.ffffZ";
        /// <summary>
        /// Date amd time in "yyyy-MM-ddTHH:mm:ss.ffffzzz" format
        /// </summary>
        public const string DATE_HHMMSSFFFF_TIMEZONE = "yyyy-MM-ddTHH:mm:ss.ffffzzz";
        /// <summary>
        /// Date amd time in "yyyy-MM-ddTHH:mm:ss.fffff" format
        /// </summary>
        public const string DATE_HHMMSSFFFFF = "yyyy-MM-ddTHH:mm:ss.fffff";
        /// <summary>
        /// Date amd time in "yyyy-MM-ddTHH:mm:ss.fffffZ" format
        /// </summary>
        public const string DATE_HHMMSSFFFFF_UTC = "yyyy-MM-ddTHH:mm:ss.fffffZ";
        /// <summary>
        /// Date amd time in "yyyy-MM-ddTHH:mm:ss.fffffzzz" format
        /// </summary>
        public const string DATE_HHMMSSFFFFF_TIMEZONE = "yyyy-MM-ddTHH:mm:ss.fffffzzz";
        /// <summary>
        /// Date amd time in "yyyy-MM-ddTHH:mm:ss.ffffff" format
        /// </summary>
        public const string DATE_HHMMSSFFFFFF = "yyyy-MM-ddTHH:mm:ss.ffffff";
        /// <summary>
        /// Date amd time in "yyyy-MM-ddTHH:mm:ss.ffffffZ" format
        /// </summary>
        public const string DATE_HHMMSSFFFFFF_UTC = "yyyy-MM-ddTHH:mm:ss.ffffffZ";
        /// <summary>
        /// Date amd time in "yyyy-MM-ddTHH:mm:ss.ffffffzzz" format
        /// </summary>
        public const string DATE_HHMMSSFFFFFF_TIMEZONE = "yyyy-MM-ddTHH:mm:ss.ffffffzzz";
        /// <summary>
        /// Date amd time in "yyyy-MM-ddTHH:mm:ss.fffffff" format
        /// </summary>
        public const string DATE_HHMMSSFFFFFFF = "yyyy-MM-ddTHH:mm:ss.fffffff";
        /// <summary>
        /// Date amd time in "yyyy-MM-ddTHH:mm:ss.fffffffZ" format
        /// </summary>
        public const string DATE_HHMMSSFFFFFFF_UTC = "yyyy-MM-ddTHH:mm:ss.fffffffZ";
        /// <summary>
        /// Date amd time in "yyyy-MM-ddTHH:mm:ss.fffffffzzz" format
        /// </summary>
        public const string DATE_HHMMSSFFFFFFF_TIMEZONE = "yyyy-MM-ddTHH:mm:ss.fffffffzzz";
    }

    /// <summary>
    /// 
    /// </summary>
    public static class Extension
    {
        /// <summary>
        /// Ins the sets of.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source">The source.</param>
        /// <param name="size">The size.</param>
        /// <returns></returns>
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
