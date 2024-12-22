using System.Threading;
using System.Threading.Tasks;

namespace Code2.MessagingTests.Assets;
public interface IQueryHandler<M, R>
{
	Task<R> Handle(M message, CancellationToken cancellationToken = default);
}
