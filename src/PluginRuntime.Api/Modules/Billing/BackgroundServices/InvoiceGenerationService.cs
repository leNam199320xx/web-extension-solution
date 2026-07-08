using PluginRuntime.Api.Modules.Billing.Services;

namespace PluginRuntime.Api.Modules.Billing.BackgroundServices;

/// <summary>
/// Background service that generates monthly invoices on the 1st of each month.
/// Runs a periodic check and triggers invoice generation when the day is the 1st.
/// </summary>
public sealed class InvoiceGenerationService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<InvoiceGenerationService> _logger;
    private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(1);

    public InvoiceGenerationService(
        IServiceProvider serviceProvider,
        ILogger<InvoiceGenerationService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("InvoiceGenerationService started. Checking hourly for 1st of month.");

        using var timer = new PeriodicTimer(CheckInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (DateTime.UtcNow.Day == 1)
                {
                    await GenerateInvoicesAsync(stoppingToken);

                    // After generating, wait until the next day to avoid re-triggering within the same day
                    var nextDay = DateTime.UtcNow.Date.AddDays(1) - DateTime.UtcNow;
                    if (nextDay > TimeSpan.Zero)
                    {
                        await Task.Delay(nextDay, stoppingToken);
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during invoice generation check");
            }

            try
            {
                await timer.WaitForNextTickAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }

        _logger.LogInformation("InvoiceGenerationService stopped.");
    }

    private async Task GenerateInvoicesAsync(CancellationToken ct)
    {
        _logger.LogInformation("1st of month detected. Starting monthly invoice generation.");

        using var scope = _serviceProvider.CreateScope();
        var invoiceService = scope.ServiceProvider.GetRequiredService<IInvoiceService>();

        await invoiceService.GenerateMonthlyInvoicesAsync(ct);

        _logger.LogInformation("Monthly invoice generation completed successfully.");
    }
}
