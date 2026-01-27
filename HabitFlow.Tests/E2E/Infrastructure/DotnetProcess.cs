using System.Collections.Concurrent;
using System.Diagnostics;

namespace HabitFlow.Tests.E2E.Infrastructure;

internal sealed class DotnetProcess : IAsyncDisposable
{
    private const int MaxOutputLines = 200;
    private readonly ConcurrentQueue<string> _output = new();
    private readonly Process _process;

    private DotnetProcess(Process process)
    {
        _process = process;
    }

    public IReadOnlyCollection<string> RecentOutput => _output.ToArray();

    public static DotnetProcess Start(string projectPath, string baseUrl, IDictionary<string, string> environment)
    {
        if (!File.Exists(projectPath))
        {
            throw new FileNotFoundException($"Project not found: {projectPath}");
        }

        var startInfo = new ProcessStartInfo("dotnet",
            $"run --no-build --no-launch-profile --project \"{projectPath}\" --urls \"{baseUrl}\"")
        {
            WorkingDirectory = RepositoryPaths.Root,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        foreach (var (key, value) in environment)
        {
            startInfo.Environment[key] = value;
        }

        var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
        var wrapper = new DotnetProcess(process);

        process.OutputDataReceived += (_, args) => wrapper.AddOutput(args.Data);
        process.ErrorDataReceived += (_, args) => wrapper.AddOutput(args.Data);

        if (!process.Start())
        {
            throw new InvalidOperationException($"Failed to start process for {projectPath}.");
        }

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        return wrapper;
    }

    private void AddOutput(string? line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return;
        }

        _output.Enqueue(line);
        while (_output.Count > MaxOutputLines && _output.TryDequeue(out _))
        {
        }
    }

    public async ValueTask DisposeAsync()
    {
        var hasExited = false;
        try
        {
            hasExited = _process.HasExited;
        }
        catch (InvalidOperationException)
        {
            hasExited = true;
        }

        if (hasExited)
        {
            _process.Dispose();
            return;
        }

        try
        {
            _process.Kill(entireProcessTree: true);
        }
        catch (InvalidOperationException)
        {
            // Process already exited.
        }

        await Task.Run(() => _process.WaitForExit(5000));
        _process.Dispose();
    }
}
