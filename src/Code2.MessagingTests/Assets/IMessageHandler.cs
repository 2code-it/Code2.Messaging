using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Code2.MessagingTests.Assets;
public interface IMessageHandler<M>
{
	Task Handle(M message, CancellationToken cancellationToken = default);
}
