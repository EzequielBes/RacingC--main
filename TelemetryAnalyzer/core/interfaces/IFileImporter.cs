using System.Collections.Generic;
using System.Threading.Tasks;
using TelemetryAnalyzer.Core.Models; // Assuming TelemetrySession is in Core.Models
using TelemetryAnalyzer.Core.Interfaces;

namespace TelemetryAnalyzer.Core.Interfaces
{
    public interface IFileImporter
    {
        System.Threading.Tasks.Task<System.Collections.Generic.List<TelemetryAnalyzer.Core.Models.TelemetryData>> ImportAsync(string filePath);
    }
}

