using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Database;
using WebApplication1.Database.Models;

namespace WebApplication1.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _applicationDbContext;

    public HomeController(ApplicationDbContext applicationDbContext)
    {
        _applicationDbContext =  applicationDbContext;
    }
    // GET
    public IActionResult Index()
    {
            return View();
    }

    public IActionResult Import()
    {
        return View();
    }
    
    [HttpPost]
    public async Task<IActionResult> ImportData(IFormFile jsonFile)
    {
            try
            {
                using var stream = new StreamReader(jsonFile.OpenReadStream());
                var jsonData = await stream.ReadToEndAsync();
                var flows = JsonSerializer.Deserialize<List<Flow>>(jsonData);

                await _applicationDbContext.Flows.AddRangeAsync(flows);
                await _applicationDbContext.SaveChangesAsync();
            }
            catch (JsonException e)
            {
                TempData["Error"] = e.Message;
            }
            catch (DbUpdateException e)
            {
                TempData["Error"] = e.Message;
            }
            return RedirectToAction("Import");
    }
    
    
}