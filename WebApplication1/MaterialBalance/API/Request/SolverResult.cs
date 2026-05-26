namespace MaterialBalance.API.Request;

public class SolverResult
{
    public SolverResultStatus Status { get; set; }
    public double[] FlowsData { get; set; }
}