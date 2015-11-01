using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PostItDB.Storage
{
    public interface IJsonStorage
    {
        Task<Guid> InsertAsync(ExpandoObject json);
        Task<ExpandoObject> GetAsync(Guid guid);
        Task<IList<ExpandoObject>> GetAsync(IList<Guid> guids);
        Task<int> UpdateAsync(ExpandoObject json);
        Task<int> DeleteAsync(Guid guid);
    }
}
