namespace MaterialBalance.API.Request;

public class SolverTask
{
    public Guid Id { get; set; }
    public StatusType Status { get; set; }  
    public DateTime CreatedTime { get; set; }
    public DateTime? StartedTime { get; set; }
    public DateTime? CompletedTime { get; set; }
    public ResultType? Result { get; set; }    
    public List<Guid> FlowsId  { get; set; }
}