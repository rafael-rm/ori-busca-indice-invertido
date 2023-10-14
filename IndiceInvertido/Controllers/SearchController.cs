using System.Diagnostics;
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
}