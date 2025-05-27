public class MotecFileImporter : IFileImporter
{
    public string[] SupportedExtensions => new[] { ".ldx", ".ld" };

    public bool CanImport(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLower();
        return SupportedExtensions.Contains(extension);
    }

    public async Task<List<TelemetryData>> ImportAsync(string filePath)
    {
        var telemetryDataList = new List<TelemetryData>();
        
        try
        {
            using var fileStream = File.OpenRead(filePath);
            var ldxData = await ParseLDXFileAsync(fileStream);
            
            foreach (var sample in ldxData.Samples)
            {
                telemetryDataList.Add(new TelemetryData
                {
                    Timestamp = sample.Timestamp,
                    SimulatorName = ldxData.SimulatorName,
                    Car = MapCarDataFromLDX(sample),
                    Track = MapTrackDataFromLDX(ldxData.Header),
                    Session = MapSessionDataFromLDX(sample, ldxData.Header)
                });
            }
        }
        catch (Exception ex)
        {
            throw new ImportException($"Erro ao importar arquivo LDX: {ex.Message}", ex);
        }

        return telemetryDataList;
    }

    private async Task<LDXData> ParseLDXFileAsync(Stream stream)
    {
        using var reader = new BinaryReader(stream);
        
        // Ler cabeçalho LDX
        var header = ReadLDXHeader(reader);
        
        // Ler canais de dados
        var channels = ReadChannels(reader, header);
        
        // Ler amostras
        var samples = await ReadSamplesAsync(reader, header, channels);
        
        return new LDXData
        {
            Header = header,
            Channels = channels,
            Samples = samples,
            SimulatorName = DetermineSimulatorFromHeader(header)
        };
    }

    private LDXHeader ReadLDXHeader(BinaryReader reader)
    {
        return new LDXHeader
        {
            Version = reader.ReadUInt32(),
            ChannelCount = reader.ReadUInt32(),
            SampleCount = reader.ReadUInt32(),
            SampleRate = reader.ReadDouble(),
            StartTime = DateTime.FromBinary(reader.ReadInt64())
        };
    }

    // Métodos auxiliares para parsing...
}

// Estruturas auxiliares para LDX
public class LDXData
{
    public LDXHeader Header { get; set; }
    public List<LDXChannel> Channels { get; set; }
    public List<LDXSample> Samples { get; set; }
    public string SimulatorName { get; set; }
}

public class LDXHeader
{
    public uint Version { get; set; }
    public uint ChannelCount { get; set; }
    public uint SampleCount { get; set; }
    public double SampleRate { get; set; }
    public DateTime StartTime { get; set; }
}