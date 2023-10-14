using System.Diagnostics;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using IndiceInvertido.Models;
using IndiceInvertido.Util;

namespace IndiceInvertido.Controllers;

public class SearchController : Controller
{
    private readonly ILogger<SearchController> _logger;

    private static InvertedIndex? _invertedIndex;

    public SearchController(ILogger<SearchController> logger)
    {
        _logger = logger;
    }
    
    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    [HttpGet("Authors")]
    public IActionResult Authors()
    {
        return View();
    }
    
    [HttpGet("SearchResult")]
    public async Task<IActionResult> SearchResult(string query)
    {
        if (_invertedIndex is null)
            _invertedIndex = new InvertedIndex();

        List<string> result = await _invertedIndex.Search(query);
        
        return View(result);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
    
    public IActionResult OpenHtml(string fileName)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        string path = $"{Directory.GetParent(Environment.CurrentDirectory)?.FullName}\\IndiceInvertido\\Data\\htmlFiles";
        string filePath = Path.Combine(path, fileName);
        
        string fileHtml = System.IO.File.ReadAllText(filePath, Encoding.GetEncoding(1252));
        return Content(fileHtml, "text/html");
    }
}