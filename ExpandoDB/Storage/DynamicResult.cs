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
    public class DynamicResult : IEnumerable<ExpandoObject>
    {
        private IEnumerable<string> _result;

        public DynamicResult(IEnumerable<string> result)
        {
            _result = result;
        }

        public IEnumerator<ExpandoObject> GetEnumerator()
        {
            foreach (var json in _result)
            {
                if (String.IsNullOrWhiteSpace(json))
                    continue;

                var dictionary = JSON.Deserialize<Dictionary<string, object>>(json, Options.ISO8601);
                yield return dictionary.ToExpando();
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _result.GetEnumerator();
        }
    }
}
