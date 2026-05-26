using MaterialBalance.API.Request;
using MathNet.Numerics.LinearAlgebra;

namespace MaterialBalance.Interfaces;

public interface ISolverService
{
    Task<SolverResult> Solve(IEnumerable<Flow> flows);
    
    QpData PrepareQpData(IEnumerable<Flow> flows, Graph graph);
    
    Vector<double> FindAvailablePoint(QpData data);
    
    Vector<double> ActiveSet(QpData data);
}