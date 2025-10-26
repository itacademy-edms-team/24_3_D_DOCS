using System.Diagnostics;
using System.Text;

namespace RusalProject.Services.Pandoc;

public class PandocService : IPandocService
{
    private readonly ILogger<PandocService> _logger;
    private readonly string _workspacePath;

    public PandocService(ILogger<PandocService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _workspacePath = configuration["Pandoc:WorkspacePath"] ?? "/workspace";
    }

    public async Task<string> ConvertMarkdownToPdfAsync(string markdownContent, string templateContent, string documentTitle)
    {
        try
        {
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var inputFile = $"input_{timestamp}.md";
            var templateFile = $"template_{timestamp}.tex";
            var outputFile = $"output_{timestamp}.pdf";

            // Write input files to shared workspace
            var workspaceInputFile = Path.Combine(_workspacePath, inputFile);
            var workspaceTemplateFile = Path.Combine(_workspacePath, templateFile);
            var workspaceOutputFile = Path.Combine(_workspacePath, outputFile);

            await File.WriteAllTextAsync(workspaceInputFile, markdownContent, Encoding.UTF8);
            await File.WriteAllTextAsync(workspaceTemplateFile, templateContent, Encoding.UTF8);

            // Prepare docker exec command
            var arguments = $"exec rusal_pandoc pandoc --from markdown --to pdf --template /workspace/{templateFile} --pdf-engine=xelatex --output /workspace/{outputFile} /workspace/{inputFile}";

            _logger.LogInformation($"Executing docker: docker {arguments}");

            var processInfo = new ProcessStartInfo
            {
                FileName = "docker",
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = processInfo };
            process.Start();

            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                _logger.LogError($"Pandoc conversion failed. Exit code: {process.ExitCode}, Error: {error}, Output: {output}");
                throw new Exception($"Pandoc conversion failed: {error}");
            }

            _logger.LogInformation($"Pandoc conversion successful. Output: {output}");

            // Clean up input files
            try
            {
                File.Delete(workspaceInputFile);
                File.Delete(workspaceTemplateFile);
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Failed to clean up temporary files: {ex.Message}");
            }

            return workspaceOutputFile;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during markdown to PDF conversion");
            throw;
        }
    }

    public async Task<bool> ValidateTemplateAsync(string templateContent)
    {
        try
        {
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var templateFile = $"validate_template_{timestamp}.tex";
            var workspaceTemplateFile = Path.Combine(_workspacePath, templateFile);

            await File.WriteAllTextAsync(workspaceTemplateFile, templateContent, Encoding.UTF8);

            // Attempt to compile a minimal document with the template to validate it
            var arguments = $"exec rusal_pandoc pandoc --from latex --to latex --template /workspace/{templateFile} --output /dev/null /dev/null";

            _logger.LogInformation($"Executing template validation: docker {arguments}");

            var processInfo = new ProcessStartInfo
            {
                FileName = "docker",
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = processInfo };
            process.Start();

            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                _logger.LogWarning($"Template validation failed. Exit code: {process.ExitCode}, Error: {error}, Output: {output}");
                return false;
            }

            _logger.LogInformation($"Template validation successful. Output: {output}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during template validation");
            return false;
        }
        finally
        {
            // Clean up temporary template file
            try
            {
                var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                var templateFile = $"validate_template_{timestamp}.tex";
                var workspaceTemplateFile = Path.Combine(_workspacePath, templateFile);
                if (System.IO.File.Exists(workspaceTemplateFile))
                {
                    System.IO.File.Delete(workspaceTemplateFile);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Failed to clean up temporary template file after validation: {ex.Message}");
            }
        }
    }
}
