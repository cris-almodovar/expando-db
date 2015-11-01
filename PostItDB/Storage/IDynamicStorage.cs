using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PostItDB.Storage
{
    /// <summary>
    /// 
    /// </summary>
    public interface IDynamicStorage
    {        
        Task<Guid> InsertAsync(ExpandoObject content);
        Task<ExpandoObject> GetAsync(Guid guid);
        Task<IEnumerable<ExpandoObject>> GetAsync(IList<Guid> guids);
        Task<int> UpdateAsync(ExpandoObject content);
        Task<int> DeleteAsync(Guid guid);
    }
}
