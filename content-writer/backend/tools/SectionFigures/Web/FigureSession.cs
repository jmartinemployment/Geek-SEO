using SectionFigures.Models;

namespace SectionFigures.Web;

public sealed class FigureSession
{
    private readonly object _gate = new();
    private FigureJobFile? _jobs;

    public FigureJobFile? Jobs
    {
        get
        {
            lock (_gate)
            {
                return _jobs;
            }
        }
    }

    public void SetJobs(FigureJobFile jobs)
    {
        lock (_gate)
        {
            _jobs = jobs;
        }
    }
}
