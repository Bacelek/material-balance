using MathNet.Numerics.LinearAlgebra;

namespace MaterialBalance.API.Request;

public class QpData
{
    public Matrix<double> W { get; set; }
    public Vector<double> c { get; set; }
    public Matrix<double> A { get; set; }
    public Vector<double> b { get; set; }
    public Vector<double> l { get; set; }
    public Vector<double> u { get; set; }
    public Vector<double> x0 { get; set; }
}