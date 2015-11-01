using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;

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
                
                yield return json.ToExpando();
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
