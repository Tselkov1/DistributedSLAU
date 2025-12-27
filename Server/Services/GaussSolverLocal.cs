using Shared.Utils;
using Shared.Models;
using System;
using System.Diagnostics;

namespace Server.Services
{
    public class GaussSolverLocal
    {
        public SolverResult Solve(Matrix A, Vector b, double tolerance = 1e-10)
        {
            var result = new SolverResult
            {
                Method = "Метод Гаусса (последовательный)",
                Success = false
            };

            var stopwatch = Stopwatch.StartNew();

            try
            {
                if (A.Rows != A.Cols)
                {
                    result.ErrorMessage = "Матрица должна быть квадратной";
                    return result;
                }

                if (A.Rows != b.Size)
                {
                    result.ErrorMessage = "Размерность вектора b не соответствует матрице A";
                    return result;
                }

                int n = A.Rows;
                var matrix = A.Clone();
                var vector = b.Clone();

                for (int k = 0; k < n - 1; k++)
                {
                    int maxRow = k;
                    double maxValue = Math.Abs(matrix.Data[k, k]);

                    for (int i = k + 1; i < n; i++)
                    {
                        double absValue = Math.Abs(matrix.Data[i, k]);
                        if (absValue > maxValue)
                        {
                            maxValue = absValue;
                            maxRow = i;
                        }
                    }

                    if (MathHelper.AreEqual(maxValue, 0, tolerance))
                    {
                        result.ErrorMessage = $"Матрица вырождена или плохо обусловлена (строка {k})";
                        return result;
                    }

                    if (maxRow != k)
                        SwapRows(matrix, vector, k, maxRow);

                    for (int i = k + 1; i < n; i++)
                    {
                        double factor = matrix.Data[i, k] / matrix.Data[k, k];
                        for (int j = k; j < n; j++)
                            matrix.Data[i, j] -= factor * matrix.Data[k, j];
                        vector[i] -= factor * vector[k];
                    }
                }

                if (MathHelper.AreEqual(matrix.Data[n - 1, n - 1], 0, tolerance))
                {
                    result.ErrorMessage = "Матрица вырождена или плохо обусловлена";
                    return result;
                }

                var solution = new Vector(n);
                for (int i = n - 1; i >= 0; i--)
                {
                    double sum = 0;
                    for (int j = i + 1; j < n; j++)
                        sum += matrix.Data[i, j] * solution[j];
                    solution[i] = (vector[i] - sum) / matrix.Data[i, i];
                }

                stopwatch.Stop();
                result.Solution = solution;
                result.Residual = SolverResult.ComputeResidual(A, solution, b);
                result.ElapsedMilliseconds = stopwatch.Elapsed.TotalMilliseconds;
                result.Iterations = 0;
                result.Success = true;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                result.ErrorMessage = $"Ошибка при решении: {ex.Message}";
                result.ElapsedMilliseconds = stopwatch.Elapsed.TotalMilliseconds;
            }

            return result;
        }

        private void SwapRows(Matrix matrix, Vector vector, int row1, int row2)
        {
            int n = matrix.Cols;
            for (int j = 0; j < n; j++)
                (matrix.Data[row1, j], matrix.Data[row2, j]) = (matrix.Data[row2, j], matrix.Data[row1, j]);
            (vector[row1], vector[row2]) = (vector[row2], vector[row1]);
        }
    }
}