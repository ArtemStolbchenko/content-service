using StackExchange.Redis;

namespace ContentService.Database
{
    public interface IRedisCacheBuilder
    {
        public IDatabase GetDatabase();
    }
}
