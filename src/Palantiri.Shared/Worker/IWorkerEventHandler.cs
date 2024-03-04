using Palantiri.Shared.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Palantiri.Shared.Consumer
{
    public interface IWorkerEventHandler<T> where T: IIdentifiable<Guid>
    {
        Task<IEnumerable<T>> Handle(IEnumerable<T> events, CancellationToken token);
    }
}
