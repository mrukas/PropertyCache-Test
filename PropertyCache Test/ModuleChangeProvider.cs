using System.Threading;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Primitives;

namespace PropertyCache_Test
{
    public class ModuleChangeProvider : IActionDescriptorChangeProvider
    {
        public static ModuleChangeProvider Instance { get; } = new ModuleChangeProvider();

        public CancellationTokenSource? TokenSource { get; private set; }

        public void NotifyChange()
        {
            TokenSource?.Cancel();
        }

        public IChangeToken GetChangeToken()
        {
            TokenSource = new CancellationTokenSource();
            return new CancellationChangeToken(TokenSource.Token);
        }
    }
}
