using System.Data.Common;
using System.Text;
using System;
using MaterialBalance.Configurations;
using MaterialBalance.Interfaces;
using Microsoft.Extensions.Options;
using Npgsql;
using System.Collections.Generic;
using MaterialBalance.API.Request;
using Google.OrTools.LinearSolver;
using MathNet.Numerics.LinearAlgebra;

public class SolverService : ISolverService
{
    private readonly IGraphService _graphService;

        public SolverService(IGraphService graphService)
        {
            _graphService = graphService;
        }
        
        public async Task<SolverResult> Solve(IEnumerable<Flow> flows)
        {
            
            Graph graph;
            graph = _graphService.CreateGraph(flows);
            
            if (!graph.IsConnectedGraph)
            {
                return new SolverResult { Status = SolverResultStatus.GraphConnectedError};
            }
            
            QpData data;
            data = PrepareQpData(flows, graph);

            Vector<double> x0;
            
            try
            {
                x0 = FindAvailablePoint(data);
            }
            catch (Exception ex)
            {
                return new SolverResult { Status = SolverResultStatus.AvailablePointError};
            }
            data.x0 = x0;
            Vector<double> solution;
            solution = ActiveSet(data);
            
            return new SolverResult
            {
                Status = SolverResultStatus.Optimal,
                FlowsData = solution.AsArray()
            };
        }

    
        public QpData PrepareQpData(IEnumerable<Flow> flows, Graph graph)
        {
            var flowList = new List<Flow>(flows);
            int n = flowList.Count;
            
            var nodes = graph.Nodes;
            int totalNodes = nodes.Count;
            var nodeIndex = new Dictionary<Guid, int>();
            for (int i = 0; i < totalNodes; i++)
                nodeIndex[nodes[i]] = i;
            
            var A = Matrix<double>.Build.Dense(totalNodes, n);
            for (int i = 0; i < n; i++)
            {
                var flow = flowList[i];
                if (flow.SourceNodeId != Guid.Empty)
                {
                    int sourceIndex = nodeIndex[flow.SourceNodeId];
                    A[sourceIndex, i] = -1.0;
                }
                if (flow.TargetNodeId != Guid.Empty)
                {
                    int targetIndex = nodeIndex[flow.TargetNodeId];
                    A[targetIndex, i] = 1.0;
                }
            }
            
            var b = Vector<double>.Build.Dense(totalNodes);

            var l = Vector<double>.Build.Dense(n);
            var u = Vector<double>.Build.Dense(n);
            var xNominal = Vector<double>.Build.Dense(n);

            double epsilonWeight = 1e-6;
            var W = Matrix<double>.Build.Diagonal(n, n, i => epsilonWeight);
            var c = Vector<double>.Build.Dense(n);

            for (int i = 0; i < n; i++)
            {
                l[i] = flowList[i].LowerBound;
                u[i] = flowList[i].UpperBound;
                xNominal[i] = (l[i] + u[i]) / 2.0;
                c[i] = -epsilonWeight * xNominal[i];
            }

            return new QpData
            {
                W = W,
                c = c,
                A = A,
                b = b,
                l = l,
                u = u,
                x0 = xNominal
            };
        }
        
        
        public Vector<double> FindAvailablePoint(QpData data)
        {
            
            int n = data.A.ColumnCount;
            int m = data.A.RowCount;

            Solver solver = Solver.CreateSolver("GLOP");

            Variable[] vars = new Variable[n];
            for (int i = 0; i < n; i++)
            {
                vars[i] = solver.MakeNumVar(data.l[i], data.u[i], $"x_{i}");
            }

            for (int i = 0; i < m; i++)
            {
                Constraint ct = solver.MakeConstraint(data.b[i], data.b[i], $"balance_{i}");
                for (int j = 0; j < n; j++)
                {
                    ct.SetCoefficient(vars[j], data.A[i, j]);
                }
            }
            
            solver.Objective().SetMinimization();

            Solver.ResultStatus status = solver.Solve();
            if (status != Solver.ResultStatus.OPTIMAL && status != Solver.ResultStatus.FEASIBLE)
                throw new Exception();

            Vector<double> result = Vector<double>.Build.Dense(n);
            for (int i = 0; i < n; i++)
                result[i] = vars[i].SolutionValue();

            return result;
        }
        
        public Vector<double> ActiveSet(QpData data)
        {
            var W = data.W;
            var c = data.c;
            var A = data.A;
            var b = data.b;
            var l = data.l;
            var u = data.u;
            Vector<double> x = data.x0.Clone();

            int n = l.Count;          
            int m = A.RowCount;       
            
            const double tol = 1e-8;
            
            int[] boundStatus = new int[n]; 
            
            for (int i = 0; i < n; i++)
            {
                if (Math.Abs(x[i] - l[i]) <= tol)
                    boundStatus[i] = 1;
                else if (Math.Abs(x[i] - u[i]) <= tol)
                    boundStatus[i] = 2;
            }

            while (true)
            {
                var freeIndices = new List<int>();
                var activeLower = new List<int>();
                var activeUpper = new List<int>();
                for (int i = 0; i < n; i++)
                {
                    if (boundStatus[i] == 0)
                        freeIndices.Add(i);
                    else if (boundStatus[i] == 1)
                        activeLower.Add(i);
                    else 
                        activeUpper.Add(i);
                }

                int nFree = freeIndices.Count;
                
                var W_FF = Matrix<double>.Build.Dense(nFree, nFree);
                for (int r = 0; r < nFree; r++)
                {
                    int i = freeIndices[r];
                    for (int col = 0; col < nFree; col++)
                    {
                        int j = freeIndices[col];
                        W_FF[r, col] = W[i, j];
                    }
                }
                
                var g = W * x + c;
                var g_F = Vector<double>.Build.Dense(nFree);
                for (int r = 0; r < nFree; r++)
                {
                    g_F[r] = g[freeIndices[r]];
                }
                
                var A_F = Matrix<double>.Build.Dense(m, nFree);
                for (int r = 0; r < m; r++)
                {
                    for (int col = 0; col < nFree; col++)
                    {
                        A_F[r, col] = A[r, freeIndices[col]];
                    }
                }

                Vector<double> p_F;
                Vector<double> lambda;
                if (nFree == 0)
                {
                    p_F = Vector<double>.Build.Dense(0);
                    if (m > 0)
                    {
                        lambda = Vector<double>.Build.Dense(m); 
                    }
                    else
                    {
                        lambda = Vector<double>.Build.Dense(0);
                    }
                }
                else
                {
                   int dim = nFree + m;
                   var KKT = Matrix<double>.Build.Dense(dim, dim);
                   var rhs = Vector<double>.Build.Dense(dim);

                   KKT.SetSubMatrix(0, 0, W_FF);
                   KKT.SetSubMatrix(0, nFree, A_F.Transpose());
                   KKT.SetSubMatrix(nFree, 0, A_F);

                   for (int r = 0; r < nFree; r++)
                       rhs[r] = -g_F[r];

                   Vector<double> sol;
                   
                   sol = KKT.Solve(rhs);
                   
                   p_F = sol.SubVector(0, nFree);
                   lambda = sol.SubVector(nFree, m); 
                }
                
                var p = Vector<double>.Build.Dense(n);
                for (int idx = 0; idx < nFree; idx++)
                    p[freeIndices[idx]] = p_F[idx];
                
                if (p_F.L2Norm() <= tol)
                {
                    var ATlambda = Vector<double>.Build.Dense(n);
                    if (m > 0)
                    {
                        ATlambda = A.Transpose() * lambda;
                    }

                    bool allMu = true;
                    double max = 0;
                    int dropIndex = -1;

                    foreach (int i in activeLower)
                    {
                        double mu = g[i] + ATlambda[i];
                        if (mu < -tol) 
                        {
                            allMu = false;
                            if (mu < max) 
                            {
                                max = mu;
                                dropIndex = i;
                            }
                        }
                    }
                    foreach (int i in activeUpper)
                    {
                        double mu = g[i] + ATlambda[i]; 
                        if (mu > tol)
                        {
                            allMu = false;
                            if (mu > max) 
                            {
                                max = mu;
                                dropIndex = i;
                            }
                        }
                    }

                    if (allMu)
                    {
                        return x;
                    }
                    
                    boundStatus[dropIndex] = 0; 
                }
                else
                {
                    double alpha = 1.0;
                    int blockingIndex = -1;
                    int newBoundType = 0; 

                    foreach (int i in freeIndices)
                    {
                        if (p[i] > tol)
                        {
                            double step = (u[i] - x[i]) / p[i];
                            if (step < alpha)
                            {
                                alpha = step;
                                blockingIndex = i;
                                newBoundType = 2; 
                            }
                        }
                        else if (p[i] < -tol)
                        {
                            double step = (l[i] - x[i]) / p[i];
                            if (step < alpha)
                            {
                                alpha = step;
                                blockingIndex = i;
                                newBoundType = 1; 
                            }
                        }
                    }

                    x = x + alpha * p;
                    
                    if (alpha < 1.0 - 1e-12) 
                    {
                        boundStatus[blockingIndex] = newBoundType;
                    }

                    for (int i = 0; i < n; i++)
                    {
                        if (boundStatus[i] == 0)
                        {
                            if (Math.Abs(x[i] - l[i]) <= tol)
                                boundStatus[i] = 1;
                            else if (Math.Abs(x[i] - u[i]) <= tol)
                                boundStatus[i] = 2;
                        }
                    }
                }
            }
        }
}