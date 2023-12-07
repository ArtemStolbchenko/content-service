using ContentService.Model;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace ContentService.Database
{
    public interface IDatabaseHelper
    {
        public void SaveAll(List<ContentItem> contents);
        public void SaveTimeStamp();
        public DateTime GetTimeStamp();
        public TimeSpan Ping();
        public List<ContentItem> GetAll();
    }
}
