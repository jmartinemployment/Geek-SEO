using SectionFigures.Models;

namespace SectionFigures.Web;

public enum GenerateRunPhase
{
    Idle,
    Running,
    Complete,
    Failed,
}

public sealed record GenerateRunSnapshot(
    GenerateRunPhase Phase,
    int Total,
    int Succeeded,
    int SkippedExists,
    int Failed,
    string? CurrentRelativePath,
    IReadOnlyList<string> LogLines,
    string? ErrorMessage);

public sealed class GenerateRunner
{
    private readonly object _gate = new();
    private GenerateRunPhase _phase = GenerateRunPhase.Idle;
    private int _total;
    private int _succeeded;
    private int _skippedExists;
    private int _failed;
    private string? _currentPath;
    private string? _errorMessage;
    private readonly List<string> _log = [];
    private CancellationTokenSource? _cts;

    public GenerateRunSnapshot Snapshot()
    {
        lock (_gate)
        {
            return new GenerateRunSnapshot(
                _phase,
                _total,
                _succeeded,
                _skippedExists,
                _failed,
                _currentPath,
                _log.ToList(),
                _errorMessage);
        }
    }

    public bool TryStart(
        IReadOnlyList<FigureJob> jobs,
        string outputRoot,
        bool force,
        int concurrency,
        bool failFast)
    {
        lock (_gate)
        {
            if (_phase == GenerateRunPhase.Running)
            {
                return false;
            }

            _phase = GenerateRunPhase.Running;
            _total = jobs.Count;
            _succeeded = 0;
            _skippedExists = 0;
            _failed = 0;
            _currentPath = null;
            _errorMessage = null;
            _log.Clear();
            _cts = new CancellationTokenSource();
        }

        _ = RunBatchAsync(jobs, outputRoot, force, concurrency, failFast, _cts.Token);
        return true;
    }

    private async Task RunBatchAsync(
        IReadOnlyList<FigureJob> jobs,
        string outputRoot,
        bool force,
        int concurrency,
        bool failFast,
        CancellationToken cancellationToken)
    {
        try
        {
            var openAi = OpenAiImageClient.FromEnvironment();
            var result = await JobGenerator.RunAsync(
                jobs,
                outputRoot,
                openAi,
                FigureAvifEncoder.Default,
                force,
                concurrency,
                failFast,
                OnJobEvent,
                cancellationToken);

            lock (_gate)
            {
                _succeeded = result.Succeeded;
                _skippedExists = result.SkippedExists;
                _failed = result.Failed;
                _currentPath = null;
                _phase = result.Failed > 0 ? GenerateRunPhase.Failed : GenerateRunPhase.Complete;
            }
        }
        catch (Exception ex)
        {
            lock (_gate)
            {
                _errorMessage = ex.Message;
                _phase = GenerateRunPhase.Failed;
                _currentPath = null;
                _log.Add($"ERROR: {ex.Message}");
            }
        }
    }

    private void OnJobEvent(FigureJobEvent evt)
    {
        lock (_gate)
        {
            _currentPath = evt.RelativePath;
            switch (evt.Kind)
            {
                case FigureJobEventKind.Started:
                    _log.Add($"START: {evt.RelativePath}");
                    break;
                case FigureJobEventKind.SkippedExists:
                    _skippedExists++;
                    _log.Add($"SKIP (exists): {evt.RelativePath}");
                    break;
                case FigureJobEventKind.Succeeded:
                    _succeeded++;
                    _log.Add($"OK: {evt.RelativePath}");
                    break;
                case FigureJobEventKind.Failed:
                    _failed++;
                    _log.Add($"FAIL: {evt.RelativePath} — {evt.Message}");
                    break;
            }

            if (_log.Count > 200)
            {
                _log.RemoveRange(0, _log.Count - 200);
            }
        }
    }
}
