using Amazon.SQS;
using Amazon.SQS.Model;
using Palantiri.Shared.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Palantiri.Shared.Amazon.SQS
{
    public interface IMessageConsumer
    {
        Task ConsumeAsync<T>(Func<IEnumerable<T>, CancellationToken, Task<IEnumerable<T>>> handler, CancellationToken token) where T : IIdentifiable<Guid>;
        Task<T> ConsumeAsync<T>() where T : IIdentifiable<Guid>;
        Task<IEnumerable<T>> ConsumeListAsync<T>() where T : IIdentifiable<Guid>;
        Task DeleteMessagesAsync(IEnumerable<Message> messagesToDelete);
    }
}
