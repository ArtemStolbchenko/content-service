using ContentService.Model;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace ContentService.Database
{
    public class DatabaseHelper : IDatabaseHelper
    {
        private readonly IDatabase _database;
        private const string CONTENT_KEY = "contents";
        private const string TIMESTAMP_KEY = "last_update";
        private readonly IRedisCacheBuilder _redisCacheBuilder;
        public DatabaseHelper()
        {
            _redisCacheBuilder = new RedisCacheBuilder();
            _database = _redisCacheBuilder.GetDatabase();
        }
        public DatabaseHelper(IRedisCacheBuilder redisCacheBuilder)
        {
            _redisCacheBuilder = redisCacheBuilder;
            _database = _redisCacheBuilder.GetDatabase();
        }
        public List<ContentItem> GetAll()
        {
            var contentsList = _database.ListRange(CONTENT_KEY);
            List<ContentItem> contents = contentsList.Select(
                serializedObject => System.Text.Json.JsonSerializer.Deserialize<ContentItem>(serializedObject)
                ).ToList();
            
            return contents;
        }
        public void SaveAll(List<ContentItem> contents)
        {

            _database.ListTrim(CONTENT_KEY, 1, 0);
            foreach (var contentItem in contents)
            {
                string serializedObject = JsonConvert.SerializeObject(contentItem);
                _database.ListRightPush(CONTENT_KEY, serializedObject);
            }
        }
        public void SaveTimeStamp()
        {
            DateTime timeStamp = DateTime.Now;
            string serializedObject = JsonConvert.SerializeObject(timeStamp);
            _database.StringSet(TIMESTAMP_KEY, serializedObject);
        }
        public virtual DateTime GetTimeStamp()
        {
            string serializedObject = _database.StringGet(TIMESTAMP_KEY);

            if (serializedObject == null || serializedObject == "")
                return DateTime.MinValue;

            DateTime timestamp = JsonConvert.DeserializeObject<DateTime>(serializedObject);
            return timestamp;
        }
        public TimeSpan Ping()
        {
            return _database.Ping();
        }
    }
}
