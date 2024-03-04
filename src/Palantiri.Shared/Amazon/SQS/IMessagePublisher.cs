using Amazon.SQS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Palantiri.Shared.Amazon.SQS
{
    public interface IMessagePublisher
    {
        Task PublishAsync<T>(T message) where T : class;
        Task PublishAsync<T>(IEnumerable<T> messages) where T : class;
    }
}
