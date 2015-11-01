using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jil;

namespace PostItDB.Storage
{
    public class ExpandoList : IEnumerable<ExpandoObject>
    {
        private IEnumerable<string> _jsonResults;

        public ExpandoList(IEnumerable<string> jsonResults)
        {
            _jsonResults = jsonResults;
        }

        public IEnumerator<ExpandoObject> GetEnumerator()
        {
            foreach (var json in _jsonResults)
            {
                if (String.IsNullOrWhiteSpace(json))
                    continue;

                var dictionary = JSON.Deserialize<Dictionary<string, object>>(json, Options.ISO8601);
                yield return dictionary.ToExpando();
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
