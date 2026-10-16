
using System.Diagnostics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EdgeRemovalService;

public sealed class Worker(
    ILogger<Worker> logger) : BackgroundService
{
    private const string TargetPath =
        @"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe";

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        logger.LogInformation("Edge removal service started.");

        using var timer = new PeriodicTimer(
            TimeSpan.FromSeconds(1));

        await InspectAsync();

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await InspectAsync();
            }
        }
        catch (OperationCanceledException)
            when (stoppingToken.IsCancellationRequested)
        {
            // Normal service shutdown.
        }
    }

    private Task InspectAsync()
    {
        foreach (var process in Process.GetProcessesByName("msedge"))
        {
            using (process)
            {
                try
                {
                    string? path = process.MainModule?.FileName;

                    if (!string.Equals(
                            path,
                            TargetPath,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    int pid = process.Id;

                    logger.LogWarning(
                        "Terminating Edge process: PID={Pid}", pid);

                    // Do not terminate the process tree.
                    process.Kill(entireProcessTree: false);

                    if (!process.WaitForExit(3000))
                    {
                        logger.LogWarning(
                            "Process {Pid} did not exit in time.", pid);
                    }
                }
                catch (InvalidOperationException)
                {
                    // Process exited before inspection.
                }
                catch (System.ComponentModel.Win32Exception ex)
                {
                    logger.LogWarning(ex,
                        "Unable to inspect or terminate a process.");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex,
                        "Unexpected process inspection error.");
                }
            }
        }

        try
        {
            if (File.Exists(TargetPath))
            {
                File.Delete(TargetPath);

                logger.LogWarning(
                    "Deleted Edge executable: {Path}", TargetPath);
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogWarning(ex,
                "Edge executable is protected or access was denied.");
        }
        catch (IOException ex)
        {
            logger.LogWarning(ex,
                "Edge executable is in use or unavailable.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Unexpected file deletion error.");
        }

        return Task.CompletedTask;
    }
}
