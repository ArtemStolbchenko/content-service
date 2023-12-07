using RabbitMQ.Client.Events;
using RabbitMQ.Client;
using System.Text;
using System.Threading.Channels;

namespace ContentService.Communication
{
    public interface IRabbitMQManager : IDisposable
    {
        public Task<string> RequestUpdate(CancellationToken cancellationToken = default);
        public void Dispose();
    }
}
