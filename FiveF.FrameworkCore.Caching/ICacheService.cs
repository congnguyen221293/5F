using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FiveF.FrameworkCore.Caching
{
    public interface ICacheService<T>
    {
        public Task<bool> AddAllAsync(IList<Tuple<string, T>> items, DateTimeOffset offset);
        public Task<bool> AddAsync(T item, string cacheKey, DateTimeOffset offset);
        public Task<T> GetAsync(string cacheKey);
        public Task<IDictionary<string, T>> GetAllAsync<T>(IEnumerable<string> cacheKeys);
        public Task<bool> RemoveAsync(string cacheKey);
        public Task<long> RemoveAllAsync(IEnumerable<string> cacheKeys);
    }
}
