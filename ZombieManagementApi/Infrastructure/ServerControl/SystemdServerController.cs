using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Options;
using ZombieManagementApi.Infrastructure.Configuration;

namespace ZombieManagementApi.Infrastructure.ServerControl;

public sealed class SystemdServerController : IServerController
{
    private readonly ZomboidOptions _options;

    public SystemdServerController(IOptions<ZomboidOptions> options)
    {
        _options = options.Value;
    }

    public async Task<ServiceStatusResult> GetStatusAsync(CancellationToken cancellationToken)
    {
        var result = await RunSystemctlAsync($"is-active {_options.SystemdServiceName}", cancellationToken);
        var status = result.StdOut.Trim();

        if (result.ExitCode == 4 || string.Equals(status, "unknown", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"systemd unit '{_options.SystemdServiceName}' not found on this host.");

        var isOnline = string.Equals(status, "active", StringComparison.OrdinalIgnoreCase);
        return new ServiceStatusResult(isOnline, string.IsNullOrWhiteSpace(status) ? result.StdErr.Trim() : status);
    }

    public async Task<ServiceOperationResult> StartAsync(CancellationToken cancellationToken)
    {
        var status = await GetStatusAsync(cancellationToken);
        if (status.IsOnline) return new ServiceOperationResult(true, "Server is already online.");

        var result = await RunSystemctlAsync($"start {_options.SystemdServiceName}", cancellationToken);
        return result.ExitCode != 0
            ? new ServiceOperationResult(false, string.IsNullOrWhiteSpace(result.StdErr) ? result.StdOut : result.StdErr)
            : new ServiceOperationResult(true, "Start command sent.");
    }

    public async Task<ServiceOperationResult> StopAsync(CancellationToken cancellationToken)
    {
        var status = await GetStatusAsync(cancellationToken);
        if (!status.IsOnline) return new ServiceOperationResult(true, "Server is already offline.");

        var result = await RunSystemctlAsync($"stop {_options.SystemdServiceName}", cancellationToken);
        return result.ExitCode != 0
            ? new ServiceOperationResult(false, string.IsNullOrWhiteSpace(result.StdErr) ? result.StdOut : result.StdErr)
            : new ServiceOperationResult(true, "Stop command sent.");
    }

    private async Task<ProcessResult> RunSystemctlAsync(string args, CancellationToken cancellationToken)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "systemctl",
                Arguments = args,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        var stdOut = new StringBuilder();
        var stdErr = new StringBuilder();
        process.OutputDataReceived += (_, e) => { if (!string.IsNullOrWhiteSpace(e.Data)) stdOut.AppendLine(e.Data); };
        process.ErrorDataReceived += (_, e) => { if (!string.IsNullOrWhiteSpace(e.Data)) stdErr.AppendLine(e.Data); };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(Math.Max(1, _options.CommandTimeoutSeconds)));
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);
        await process.WaitForExitAsync(linked.Token);

        return new ProcessResult(process.ExitCode, stdOut.ToString(), stdErr.ToString());
    }

    private sealed record ProcessResult(int ExitCode, string StdOut, string StdErr);
}
