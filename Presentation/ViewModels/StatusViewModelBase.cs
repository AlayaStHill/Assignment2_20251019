using CommunityToolkit.Mvvm.ComponentModel;

namespace Presentation.ViewModels;

public partial class StatusViewModelBase : ObservableObject
{
    // Refererar alltid till den senaste instansen av CancellationTokenSource som skapats i HideStatusSoon. När statusCts.Cancel() anropas (då ett nytt statusmeddelande visas) avbryts just den instansen, via token i Task.Delay.. Är null tills första gången ClearStatusAfterAsync() körs.
    private CancellationTokenSource? _statusCts;

    [ObservableProperty]
    private string? _statusMessage;

    [ObservableProperty]
    private string? _statusColor;

    public void SetStatus(string? message, string? color, int clearAfterMs = 3000)
    {
        StatusMessage = message;
        StatusColor = color;
        // Fire-and-forget – kör i bakgrunden tills den är klar utan att blockera. Den här metoden ska kunna avslutas direkt ändå.
        _ = ClearStatusAfterAsync(clearAfterMs);
    }

    // Ingen ct skickas in som parameter. Metoden sköter sin egen avbrytlogik
    public async Task ClearStatusAfterAsync(int ms = 3000)
    {
        // Avbryt eventuell tidigare timer = aldrig två timers igång samtidigt. Dispose avlastar operativsystemet direkt från att hålla liv i onödiga resurser, tills garbage collector städar.  
        _statusCts?.Cancel();
        _statusCts?.Dispose();
        // Skapa en ny cts-instans som gäller för just den här pågående väntan. Fältet refererar nu till denna nya instans 
        _statusCts = new CancellationTokenSource();
        // Hämtar en token från den nya instansen (via fältet). Tokenen är bara en kopia som används lokalt här, men är kopplad till _statusCts.
        CancellationToken ctoken = _statusCts.Token;

        try
        {
            // Väntar i 3000 ms, men avbryts direkt om HideStatusSoon anropas igen, (för körs statusCts.Cancel() på föregående cts).
            await Task.Delay(ms, ctoken);
            // Om väntan inte avbryts, rensa status efter 3 sek.
            StatusMessage = null;
            StatusColor = null;
        }
        // Kastas om statusCts.Cancel() anropas på cts (alltså när en ny HideStatusSoon startas).
        catch (TaskCanceledException)
        {
            // Ignorera – det betyder att ett nytt statusmeddelande avbröt den gamla väntan.
        }
    }
}