using MaterialBalance.Interfaces;
using Microsoft.AspNetCore.Mvc;
using MaterialBalance.API.Request;

namespace MaterialBalance.Controllers;

[Route("api")]
public class MaterialBalanceController : Controller
{
    private readonly IDBProvider _dbProvider;
    private readonly IGraphService _graphService;

    public MaterialBalanceController(IDBProvider dbProvider, IGraphService graphService)
    {
        _dbProvider = dbProvider;
        _graphService = graphService;
    }
    
    [HttpPost("addFlows")]
    public async Task<IActionResult> AddFlows([FromBody] IEnumerable<Flow>? flows)
    {
        if (flows == null)
        {
            return BadRequest("Список потоков не может быть null.");
        }
            
        try
        {
            await _dbProvider.AddFlows(flows);
            return Ok();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpDelete("deleteFlows")]
    public async Task<IActionResult> DeleteFlows([FromBody] IEnumerable<Guid>? flowsId)
    {
        if (flowsId == null)
        {
            return BadRequest("Список идентификаторов не может быть null.");
        }
        try
        {
            await _dbProvider.DeleteFlows(flowsId);
            return Ok();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    } 
    
    [HttpGet("getFlows")]
    public async Task<IActionResult> GetFlows([FromQuery] IEnumerable<Guid>? flowsId)
    {
        if (flowsId == null)
        {
            return BadRequest("Список идентификаторов не может быть null.");
        }
        try
        {
            var flows = await _dbProvider.GetFlows(flowsId);
            return Ok(flows);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
    
    
    [HttpPost("createTask")]
    public async Task<IActionResult> CreateTask([FromBody] IEnumerable<Guid>? flowsId)
    {
        if (flowsId == null)
        { 
            return BadRequest("Список идентификаторов не может быть null.");
        }
        try
        {
            var taskId = await _dbProvider.CreateSolverTask(flowsId);
            return Ok(taskId);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
    
    [HttpGet("getStatus")]
    public async Task<IActionResult> GetStatus(Guid? taskId)
    {
        if (taskId == null)
        { 
            return BadRequest("Идентификатор не может быть null.");
        }
        try
        {
            var task = await _dbProvider.GetSolverTask(taskId);
            if (task == null)
            {
                return NotFound($"Задача с Id={taskId} не найдена.");
            }

            return Ok(new
            {
                task.Id,
                Status = task.Status.ToString(),
                task.CreatedTime,
                task.StartedTime,
                task.CompletedTime,
                task.Result
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
    
}