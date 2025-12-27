using Shared.Models;
using System;
using System.Collections.Generic;
using Shared.Utils;
namespace Shared.Models
{
    public class SolverResult
    {
        public Vector Solution { get; set; }
        public double ElapsedMilliseconds { get; set; }
        public int Iterations { get; set; }
        public double Residual { get; set; }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public string Method { get; set; }
        public List<double> ResidualHistory { get; set; }

        public SolverResult()
        {
            ResidualHistory = new List<double>();
        }

        public static double ComputeResidual(Matrix A, Vector x, Vector b)
        {
            if (A == null || x == null || b == null)
                throw new ArgumentNullException("A, x или b не могут быть null");

            if (A.Cols != x.Size || A.Rows != b.Size)
                throw new ArgumentException("Размерности матрицы и векторов не совпадают");

            var Ax = A.Multiply(x);
            var diff = Ax.Subtract(b);
            return diff.Norm();
        }

        public override string ToString()
        {
            return $"Метод: {Method}\n" +
                   $"Время: {ElapsedMilliseconds:F2} мс\n" +
                   $"Итерации: {Iterations}\n" +
                   $"Невязка: {Shared.Utils.MathHelper.FormatScientific(Residual)}\n" +
                   $"Успех: {Success}";
        }
    }

    public class ComparisonResult
    {
        public SolverResult GaussResult { get; set; }
        public SolverResult ConjugateGradientResult { get; set; }

        public double Speedup =>
            GaussResult != null && ConjugateGradientResult != null
                ? GaussResult.ElapsedMilliseconds / ConjugateGradientResult.ElapsedMilliseconds
                : 0;

        public double ResidualDifference =>
            GaussResult != null && ConjugateGradientResult != null
                ? Math.Abs(GaussResult.Residual - ConjugateGradientResult.Residual)
                : 0;

        public string GetSummary()
        {
            return $"=== Сравнение методов ===\n\n" +
                   $"Метод Гаусса:\n{GaussResult}\n\n" +
                   $"Метод сопряжённых градиентов (распределённый):\n{ConjugateGradientResult}\n\n" +
                   $"Ускорение: {Speedup:F2}x\n" +
                   $"Разница в невязке: {Shared.Utils.MathHelper.FormatScientific(ResidualDifference)}";
        }
    }

    public class SolverStatus
    {
        public bool IsRunning { get; set; }
        public string CurrentMethod { get; set; }
        public int CurrentIteration { get; set; }
        public double CurrentResidual { get; set; }
        public double Progress { get; set; }
        public string Message { get; set; }
        public DateTime StartTime { get; set; }
        public List<WorkerNode> Workers { get; set; }

        public SolverStatus()
        {
            Workers = new List<WorkerNode>();
        }
    }
}