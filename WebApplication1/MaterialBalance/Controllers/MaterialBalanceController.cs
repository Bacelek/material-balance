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

    [HttpPost("deleteFlows")]
    public async Task<IActionResult> DeleteFlows([FromBody] IEnumerable<Guid> flowsId)
    {
        try
        {
            if (flowsId == null)
            {
                return BadRequest();
            }

            await _dbProvider.DeleteFlows(flowsId);
            return Ok();
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message, ex);
        }
    }
    
    [HttpPost("getFlows")]
    public async Task<IActionResult> GetFlows([FromBody] IEnumerable<Guid> flowsId)
    {
        try
        {
            if (flowsId == null)
            {
                return BadRequest();
            }
            
            var flows = await _dbProvider.GetFlows(flowsId);
            return Ok(flows);
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message, ex);
        }
    }
    
    [HttpPost("createGraph")]
    public async Task<IActionResult> CreateGraph([FromBody] IEnumerable<Guid> flowIds)
    {
        try
        {
            var flows = await _dbProvider.GetFlows(flowIds);
            
            var graph = _graphService.CreateGraph(flows);
        
            return Ok(graph);
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message, ex);
        }
    }
    
    [HttpPost("createTask")]
    public async Task<IActionResult> StartComputation([FromBody] IEnumerable<Guid> flowsId)
    {
        try
        {
            if (flowsId == null)
                return BadRequest();

            var taskId = await _dbProvider.CreateSolverTask(flowsId);

            return Ok(taskId);
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message, ex);
        }
    }
    
    [HttpPost("getStatus")]
    public async Task<IActionResult> GetStatus(Guid taskId)
    {
        try
        {
            var task = await _dbProvider.GetSolverTask(taskId);
            if (task == null)
                return BadRequest();

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
            throw new Exception(ex.Message, ex);
        }
    }
    
}