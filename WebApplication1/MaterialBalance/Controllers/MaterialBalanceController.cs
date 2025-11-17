using MaterialBalance.Interfaces;
using Microsoft.AspNetCore.Mvc;
using MaterialBalance.API.Request;

namespace MaterialBalance.Controllers;

[Route("api")]
public class MaterialBalanceController : Controller
{
    private readonly IDBProvider _dbProvider;

    public MaterialBalanceController(IDBProvider  dbProvider)
    {
        _dbProvider = dbProvider;
    }
    
    [HttpPost("addFlows")]
    public async Task<IActionResult> AddFlows([FromBody] IEnumerable<Flow> flows)
    {
        try
        {
            if (flows == null)
            {
                return BadRequest();
            }

            await _dbProvider.AddFlows(flows);
            return Ok();
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message, ex);
        }
    }

    
    
}