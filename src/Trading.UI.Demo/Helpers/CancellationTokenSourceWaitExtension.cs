using System;
using System.Threading;
using System.Threading.Tasks;

namespace Trading.UI.Demo.Helpers
{
    public static class CancellationTokenSourceWaitExtension
    {
        public static async Task Wait(this CancellationTokenSource tokenSource, TimeSpan timeSpan)
        {
            try
            {
                await Task.Delay(timeSpan, tokenSource.Token);
            }
            catch (TaskCanceledException)
            {
            }
            finally
            {
                tokenSource.Dispose();
            }
        }
    }
}