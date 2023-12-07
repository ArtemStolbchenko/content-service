using StackExchange.Redis;

namespace ContentService.Database
{
    public class RedisCacheBuilder : IRedisCacheBuilder
    {
        private ConnectionMultiplexer redis;
        
        public IDatabase GetDatabase()
        {
            try
            {
                if (redis == null)
                {
                    redis = ConnectionMultiplexer.Connect(
                        new ConfigurationOptions
                        {
                            EndPoints = { "host.docker.internal:6279" },
                        }
                    );
                }
                PingDatabse();

                return redis.GetDatabase();
            }
            catch (Exception exception)
            {
                Console.WriteLine($"Failed to initialize redis connection!\n{exception.Message}");
                throw exception;
            }
        }
        private void PingDatabse()
        {

            var Pong = redis.GetDatabase().Ping();
            Console.WriteLine($"Ping response received in {Pong}");
        }
    }
}
