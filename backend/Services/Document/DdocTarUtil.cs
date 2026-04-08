using System.Formats.Tar;
using System.Reflection;

namespace RusalProject.Services.Document;

/// <summary>
/// Создание USTAR TAR для .ddoc (один или несколько файлов).
/// </summary>
public static class DdocTarUtil
{
    public static async Task<MemoryStream> CreateTarWithSingleFileAsync(
        string entryName,
        byte[] data,
        CancellationToken cancellationToken = default)
    {
        return await CreateTarWithFilesAsync(
            new[] { (entryName, data) },
            cancellationToken);
    }

    public static async Task<MemoryStream> CreateTarWithFilesAsync(
        IReadOnlyList<(string Path, byte[] Data)> files,
        CancellationToken cancellationToken = default)
    {
        var tarStream = new MemoryStream();
        using (var tarWriter = new TarWriter(tarStream, leaveOpen: true))
        {
            foreach (var (path, data) in files)
            {
                using var ms = new MemoryStream(data, false);
                await WriteFileToTarCoreAsync(tarWriter, path, ms, cancellationToken);
            }
        }

        tarStream.Position = 0;
        return tarStream;
    }

    internal static async Task WriteFileToTarCoreAsync(
        TarWriter tarWriter,
        string entryName,
        Stream dataStream,
        CancellationToken cancellationToken)
    {
        dataStream.Position = 0;
        using var buffer = new MemoryStream();
        await dataStream.CopyToAsync(buffer, cancellationToken);
        var dataBytes = buffer.ToArray();

        var entry = new UstarTarEntry(TarEntryType.RegularFile, entryName);
        var dataStreamProperty = typeof(UstarTarEntry)
            .GetProperty("DataStream", BindingFlags.Public | BindingFlags.Instance);
        if (dataStreamProperty != null && dataStreamProperty.CanWrite)
        {
            var entryDataStream = new MemoryStream(dataBytes);
            dataStreamProperty.SetValue(entry, entryDataStream);
        }
        else
        {
            var setMethod = dataStreamProperty?.GetSetMethod(nonPublic: true);
            if (setMethod != null)
            {
                var entryDataStream = new MemoryStream(dataBytes);
                setMethod.Invoke(entry, [entryDataStream]);
            }
            else
            {
                throw new InvalidOperationException($"Unable to set DataStream for TAR entry '{entryName}'.");
            }
        }

        await tarWriter.WriteEntryAsync(entry, cancellationToken);
    }
}
