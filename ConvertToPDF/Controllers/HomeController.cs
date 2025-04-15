using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ConvertToPDF.Models;

namespace ConvertToPDF.Controllers;

public class HomeController : Controller
{

    public HomeController()
    {
       
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    [HttpPost]
    [RequestSizeLimit(20_000_000)] // Limite el tamaño en MB: 20 MB
    public async Task<IActionResult> ConvertToPdf(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded.");

        var extension = Path.GetExtension(file.FileName);
        var fileNameWithoutExt = Path.GetFileNameWithoutExtension(file.FileName);

        var tempDir = Path.GetTempPath();
        var inputPath = Path.Combine(tempDir, fileNameWithoutExt + extension);
        var outputPath = Path.Combine(tempDir, fileNameWithoutExt + ".pdf");

        try
        {
            // Guardar temporalmente el archivo
            using (var stream = new FileStream(inputPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Ejecutar unoconv
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "unoconv",
                    Arguments = $"--output=\"{outputPath}\" --format=pdf \"{inputPath}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            string stdOut = await process.StandardOutput.ReadToEndAsync();
            string stdErr = await process.StandardError.ReadToEndAsync();
            process.WaitForExit();

            if (process.ExitCode != 0 || !System.IO.File.Exists(outputPath))
            {
                return StatusCode(500, $"Error al convertir el archivo: {stdErr}");
            }

            var pdfBytes = await System.IO.File.ReadAllBytesAsync(outputPath);

            System.IO.File.Delete(inputPath);
            System.IO.File.Delete(outputPath);

            return File(pdfBytes, "application/pdf", fileNameWithoutExt + ".pdf");
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error interno: {ex.Message}");
        }
    }
}