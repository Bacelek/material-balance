using MaterialBalance.API.Request;
using MaterialBalance.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace MaterialBalance.Controllers;

[Route("api")]
public class MaterialBalanceController : Controller
{
    private readonly IDBProvider _dbProvider;

    public MaterialBalanceController(IDBProvider  dbProvider)
    {
        _dbProvider = dbProvider;
        _dbProvider.
    }
    
    [HttpPost("addFlows")]
    public async Task<IActionResult> AddFlows([FromBody] IEnumerable<Flow> flows)
    {
        await _dbProvider.AddFlows(flows);
        return Ok();
    }

    
    
}