using Shared.Models;
using Shared.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Vector = Shared.Models.Vector;

namespace Server.Services
{
    public class ConjugateGradientSolverLocal
    {
        public SolverResult Solve(Matrix A, Vector b, int maxIterations = 10000, double tolerance = 1e-6)
        {
            var result = new SolverResult
            {
                Method = "Метод сопряжённых градиентов (локальный)",
                Success = false,
                ResidualHistory = new List<double>()
            };

            var stopwatch = Stopwatch.StartNew();

            try
            {
                int n = A.Rows;
                Vector x = Vector.Zero(n); // Начальное приближение x0 = 0

                // r0 = b - A*x0
                // Так как x0 = 0, то A*x0 = 0, следовательно r0 = b
                Vector r = b.Clone();
                Vector p = r.Clone();

                double rsold = r.Dot(r);

                for (int i = 0; i < maxIterations; i++)
                {
                    double currentResidual = Math.Sqrt(rsold);
                    result.ResidualHistory.Add(currentResidual);

                    if (currentResidual < tolerance)
                    {
                        result.Iterations = i;
                        result.Success = true;
                        break;
                    }

                    // Ap = A * p
                    Vector Ap = A.Multiply(p);

                    // alpha = (r^T * r) / (p^T * A * p)
                    double pAp = p.Dot(Ap);

                    if (Math.Abs(pAp) < 1e-14)
                    {
                        result.ErrorMessage = "Деление на ноль (pAp слишком мал)";
                        break;
                    }

                    double alpha = rsold / pAp;

                    // x = x + alpha * p
                    x = x.Add(p.Multiply(alpha));

                    // r = r - alpha * Ap
                    r = r.Subtract(Ap.Multiply(alpha));

                    double rsnew = r.Dot(r);

                    // beta = rsnew / rsold
                    double beta = rsnew / rsold;

                    // p = r + beta * p
                    p = r.Add(p.Multiply(beta));

                    rsold = rsnew;
                }

                if (!result.Success)
                {
                    result.Iterations = maxIterations;
                    result.ErrorMessage = "Достигнут лимит итераций";
                }

                stopwatch.Stop();
                result.Solution = x;
                result.Residual = SolverResult.ComputeResidual(A, x, b); // Пересчитываем точную невязку в конце
                result.ElapsedMilliseconds = stopwatch.Elapsed.TotalMilliseconds;
                result.Success = true;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                result.ErrorMessage = $"Ошибка: {ex.Message}";
                result.ElapsedMilliseconds = stopwatch.Elapsed.TotalMilliseconds;
            }

            return result;
        }
    }
}