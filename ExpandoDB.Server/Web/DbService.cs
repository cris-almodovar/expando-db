using Nancy;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;
using System.Threading;
using Nancy.ModelBinding;
using ExpandoDB.Search;
using System.Diagnostics;

namespace ExpandoDB.Server.Web
{
    public class DbService : NancyModule
    {
        private readonly Database _db;
        private readonly ILog _log = LogManager.GetLogger(typeof(DbService).Name);

        public DbService(Database db) : base("/db")
        {
            _db = db;

            Post["/{collection}", true] = OnInsertAsync;
            Get["/{collection}", true] = OnSearchAsync;

        }

        private async Task<object> OnSearchAsync(dynamic req, CancellationToken token)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var collectionName = (string)req["collection"];
            var searchCriteria = this.Bind<SearchCriteria>();
            
            var collection = _db[collectionName];
            var result = await collection.SearchAsync(searchCriteria);

            stopwatch.Stop();

            var responseDto = new SearchResponseDto(searchCriteria, result.HitCount, result.TotalHitCount, result.PageCount)
            {
                Elapsed = stopwatch.Elapsed.ToString(),
                Items = from c in result.Items
                        select c.AsExpando()
            };           

            return responseDto;            
        }

        private async Task<object> OnInsertAsync(dynamic req, CancellationToken token)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var collectionName = (string)req["collection"];
            if (String.IsNullOrWhiteSpace(collectionName))
                throw new InvalidOperationException("The collection parameter is null or empty");

            var excludedFields = new[] { "collection" };
            var dictionary = this.Bind<DynamicDictionary>(excludedFields);
            var content = new Content(dictionary.ToDictionary());
            
            var collection = _db[collectionName];
            var guid = await collection.InsertAsync(content);

            stopwatch.Stop();     

            return new InsertResponseDto
            {
                _id = guid,
                Elapsed = stopwatch.Elapsed.ToString()
            };
        }
    }    
}
