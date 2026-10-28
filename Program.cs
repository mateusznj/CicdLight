using Microsoft.UI.Dispatching;

namespace CicdLight;

public static class Program
{
    [STAThread]
    private static void Main()
    {
        WinRT.ComWrappersSupport.InitializeComWrappers();

        using var mutex = new Mutex(true, @"Local\CicdLight.SingleInstance", out var isNew);
        if (!isNew) return;

        Microsoft.UI.Xaml.Application.Start(p =>
        {
            var context = new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread());
            SynchronizationContext.SetSynchronizationContext(context);
            _ = new App();
        });
    }
}

