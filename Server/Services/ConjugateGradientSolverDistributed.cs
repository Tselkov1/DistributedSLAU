using Shared.Utils;
using Server.Services;
using Shared.Models;
using Shared.Utils;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Vector = Shared.Models.Vector;

namespace Server.Services
{
    public class ConjugateGradientSolverDistributed
    {
        private readonly TcpMasterServer _master;
        private readonly Matrix _A;
        private readonly Vector _b;
        private SolverStatus _status;
        private Action<SolverStatus> _progressCallback;

        public ConjugateGradientSolverDistributed(TcpMasterServer master, Matrix A, Vector b)
        {
            _master = master ?? throw new ArgumentNullException(nameof(master));
            _A = A ?? throw new ArgumentNullException(nameof(A));
            _b = b ?? throw new ArgumentNullException(nameof(b));

            if (_A.Rows != _A.Cols)
                throw new ArgumentException("Матрица должна быть квадратной");

            if (_A.Rows != _b.Size)
                throw new ArgumentException("Размерность вектора b не соответствует матрице A");

            if (!MathHelper.IsSymmetric(_A))
                Console.WriteLine("[CG] Предупреждение: матрица не симметричная");

            if (!MathHelper.IsPositiveDefinite(_A))
                Console.WriteLine("[CG] Предупреждение: матрица может быть не положительно определённой");

            _status = new SolverStatus
            {
                IsRunning = false,
                CurrentMethod = "Метод сопряжённых градиентов (распределённый)",
                Workers = _master.Workers
            };
        }

        public void SetProgressCallback(Action<SolverStatus> callback)
        {
            _progressCallback = callback;
        }

        public SolverStatus GetStatus() => _status;

        public async Task<SolverResult> SolveAsync(
            int maxIterations = 10000,
            double tolerance = 1e-6,
            Vector initialGuess = null)
        {
            var result = new SolverResult
            {
                Method = "Метод сопряжённых градиентов (распределённый)",
                Success = false,
                ResidualHistory = new System.Collections.Generic.List<double>() // <-- ДОБАВЛЕНА ЭТА СТРОКА
            };

            var stopwatch = Stopwatch.StartNew();

            try
            {
                _status.IsRunning = true;
                _status.StartTime = DateTime.UtcNow;
                _status.Message = "Инициализация воркеров...";
                NotifyProgress();

                await _master.InitializeWorkersAsync(_A, _b);

                int n = _A.Rows;
                Vector x = initialGuess?.Clone() ?? Vector.Zero(n);

                Vector Ax = await _master.ComputeMatrixVectorAsync(x);
                Vector r = _b.Subtract(Ax);
                Vector p = r.Clone();

                double rsold = await _master.ComputeDotProductAsync(r, r);
                Console.WriteLine($"[CG] Начальная невязка: {MathHelper.FormatScientific(Math.Sqrt(rsold))}");

                for (int iteration = 0; iteration < maxIterations; iteration++)
                {
                    _status.CurrentIteration = iteration;
                    _status.CurrentResidual = Math.Sqrt(rsold);
                    _status.Progress = Math.Min(100.0, (double)iteration / maxIterations * 100);
                    _status.Message = $"Итерация {iteration + 1}/{maxIterations}";
                    result.ResidualHistory.Add(_status.CurrentResidual);
                    NotifyProgress();

                    if (Math.Sqrt(rsold) < tolerance)
                    {
                        Console.WriteLine($"[CG] Сходимость достигнута на итерации {iteration}");
                        result.Iterations = iteration;
                        result.Success = true;
                        break;
                    }

                    Vector Ap = await _master.ComputeMatrixVectorAsync(p);
                    double pAp = await _master.ComputeDotProductAsync(p, Ap);

                    if (MathHelper.AreEqual(pAp, 0, 1e-14))
                    {
                        result.ErrorMessage = "Деление на ноль: p^T * Ap слишком мало";
                        break;
                    }

                    double alpha = MathHelper.SafeDivide(rsold, pAp);
                    x = x.Add(p.Multiply(alpha));
                    r = r.Subtract(Ap.Multiply(alpha));

                    double rsnew = await _master.ComputeDotProductAsync(r, r);
                    if (rsnew > rsold)
                        Console.WriteLine($"[CG] Предупреждение: невязка увеличилась на итерации {iteration}");

                    double beta = MathHelper.SafeDivide(rsnew, rsold);
                    p = r.Add(p.Multiply(beta));
                    rsold = rsnew;

                    if ((iteration + 1) % 10 == 0)
                    {
                        Console.WriteLine($"[CG] Итерация {iteration + 1}: невязка = {MathHelper.FormatScientific(Math.Sqrt(rsold))}");
                    }
                }

                if (!result.Success && _status.CurrentIteration >= maxIterations - 1)
                {
                    result.ErrorMessage = $"Достигнуто максимальное количество итераций ({maxIterations})";
                    result.Success = false;
                    result.Iterations = maxIterations;
                }

                stopwatch.Stop();
                result.Solution = x;
                result.Residual = SolverResult.ComputeResidual(_A, x, _b);
                result.ElapsedMilliseconds = stopwatch.Elapsed.TotalMilliseconds;

                _status.Message = result.Success ? "Решение найдено" : "Не удалось найти решение";
                _status.Progress = 100;
                NotifyProgress();

                Console.WriteLine($"[CG] Завершено за {result.ElapsedMilliseconds:F2} мс");
                Console.WriteLine($"[CG] Итераций: {result.Iterations}, Невязка: {MathHelper.FormatScientific(result.Residual)}");
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                result.ErrorMessage = $"Ошибка при решении: {ex.Message}";
                result.ElapsedMilliseconds = stopwatch.Elapsed.TotalMilliseconds;
                _status.Message = $"Ошибка: {ex.Message}";
                NotifyProgress();
            }
            finally
            {
                _status.IsRunning = false;
                NotifyProgress();
            }

            return result;
        }

        private void NotifyProgress() => _progressCallback?.Invoke(_status);
    }
}