using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace VideoDetailTransfer.Media;

public sealed class FfprobeVideoProbe : IVideoProbe
{
    private readonly string _ffprobePath;

    // Keep options shared; ffprobe outputs snake_case property names.
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public FfprobeVideoProbe(string ffprobePath)
    {
        if (string.IsNullOrWhiteSpace(ffprobePath))
            throw new ArgumentException("ffprobePath is null/empty.", nameof(ffprobePath));

        _ffprobePath = ffprobePath;
    }

    public async Task<ProbeResult> ProbeAsync(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Video path is null/empty.", nameof(path));

        if (!File.Exists(path))
            throw new FileNotFoundException("Video file not found.", path);

        if (!File.Exists(_ffprobePath))
            throw new FileNotFoundException("ffprobe not found.", _ffprobePath);

        // Important: argument string with correct quoting (note: no double quote at the end)
        // Also include -hide_banner to reduce noise, though we use -v error anyway.
        string args = $"-hide_banner -v error -show_format -show_streams -of json \"{path}\"";

        ProcessStartInfo psi = new ProcessStartInfo
        {
            FileName = _ffprobePath,
            Arguments = args,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        using Process proc = new Process { StartInfo = psi };

        try
        {
            if (!proc.Start())
                throw new InvalidOperationException("Failed to start ffprobe process.");

            // Read both streams asynchronously to avoid deadlocks.
            Task<string> stdoutTask = proc.StandardOutput.ReadToEndAsync();
            Task<string> stderrTask = proc.StandardError.ReadToEndAsync();

            await proc.WaitForExitAsync().ConfigureAwait(false);

            string stdout = (await stdoutTask.ConfigureAwait(false)).Trim();
            string stderr = (await stderrTask.ConfigureAwait(false)).Trim();

            if (proc.ExitCode != 0)
            {
                throw new InvalidOperationException(
                    $"ffprobe failed (exit code {proc.ExitCode}).\n" +
                    $"Args: {args}\n" +
                    (!string.IsNullOrWhiteSpace(stderr) ? $"stderr:\n{stderr}\n" : "") +
                    (!string.IsNullOrWhiteSpace(stdout) ? $"stdout:\n{stdout}\n" : ""));
            }

            if (string.IsNullOrWhiteSpace(stdout))
            {
                throw new InvalidOperationException(
                    $"ffprobe returned no output.\nArgs: {args}\n" +
                    (!string.IsNullOrWhiteSpace(stderr) ? $"stderr:\n{stderr}" : ""));
            }

            ProbeResult? result;
            try
            {
                result = JsonSerializer.Deserialize<ProbeResult>(stdout, JsonOptions);
            }
            catch (JsonException je)
            {
                // Help debug by showing the first part of stdout.
                string preview = stdout.Length > 2000 ? stdout[..2000] + "…" : stdout;
                throw new InvalidOperationException(
                    $"ffprobe output was not valid JSON or did not match ProbeResult.\n" +
                    $"Args: {args}\n" +
                    (!string.IsNullOrWhiteSpace(stderr) ? $"stderr:\n{stderr}\n" : "") +
                    $"stdout preview:\n{preview}", je);
            }

            if (result is null)
                throw new InvalidOperationException("Failed to deserialize ffprobe output (null result).");

            // Optional: attach raw stderr for diagnostics if you store it
            // result.RawStderr = stderr;

            return result;
        }
        catch (Exception ex) when (ex is not FileNotFoundException)
        {
            // Wrap with extra context
            throw new InvalidOperationException(
                $"Error probing video with ffprobe.\nffprobe: {_ffprobePath}\nvideo: {path}", ex);
        }
    }
}
