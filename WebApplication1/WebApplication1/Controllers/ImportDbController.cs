using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Database;
using WebApplication1.Database.Models;

namespace WebApplication1.Controllers;

public class ImportDbController : Controller
{
    private readonly ApplicationDbContext _applicationDbContext;

    public ImportDbController(ApplicationDbContext applicationDbContext)
    {
        _applicationDbContext =  applicationDbContext;
    }
    // GET
    public IActionResult Index()
    {
            return View();
    }

    [HttpPost]
    public async Task<IActionResult> Import(IFormFile jsonFile)
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
            ViewBag.Message = e;
        }
        catch (DbUpdateException e)
        {
            ViewBag.Message = e;
        }
        return View("Index");
    }
}