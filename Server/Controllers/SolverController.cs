// --- НАЧАЛО ФАЙЛА Server/Controllers/SolverController.cs ---

using Microsoft.AspNetCore.Mvc;
using Server.Services;
using Shared.Models;
using Shared.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Vector = Shared.Models.Vector;

namespace Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SolverController : ControllerBase
    {
        private readonly TcpMasterServer _master;
        private readonly SolverStateService _state; // Сервис для хранения состояния между запросами

        // Сервисы внедряются через конструктор. ASP.NET Core предоставит нам
        // единственные (Singleton) экземпляры этих сервисов для каждого запроса.
        public SolverController(TcpMasterServer masterServer, SolverStateService solverStateService)
        {
            _master = masterServer;
            _state = solverStateService; // Сохраняем ссылку на сервис состояния
        }

        [HttpPost("upload/matrix")]
        public async Task<IActionResult> UploadMatrix(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                    return BadRequest("Файл не выбран");

                using var reader = new StreamReader(file.OpenReadStream());
                string content = await reader.ReadToEndAsync();

                // Сохраняем данные в сервис состояния
                _state.CurrentMatrix = Matrix.FromString(content);

                bool symmetric = MathHelper.IsSymmetric(_state.CurrentMatrix);
                bool positiveDefinite = MathHelper.IsPositiveDefinite(_state.CurrentMatrix);
                double condition = MathHelper.ConditionNumber(_state.CurrentMatrix);

                return Ok(new
                {
                    success = true,
                    rows = _state.CurrentMatrix.Rows,
                    cols = _state.CurrentMatrix.Cols,
                    symmetric,
                    positiveDefinite,
                    conditionNumber = MathHelper.FormatScientific(condition),
                    message = $"Матрица загружена: {_state.CurrentMatrix.Rows}x{_state.CurrentMatrix.Cols}"
                });
            }
            catch (Exception ex) { return BadRequest(new { success = false, error = ex.Message }); }
        }

        [HttpPost("upload/vector")]
        public async Task<IActionResult> UploadVector(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                    return BadRequest("Файл не выбран");

                using var reader = new StreamReader(file.OpenReadStream());
                string content = await reader.ReadToEndAsync();

                // Сохраняем данные в сервис состояния
                _state.CurrentVector = Vector.FromString(content);

                return Ok(new { success = true, size = _state.CurrentVector.Size, message = $"Вектор загружён: размер {_state.CurrentVector.Size}" });
            }
            catch (Exception ex) { return BadRequest(new { success = false, error = ex.Message }); }
        }

        [HttpPost("workers")]
        public IActionResult SetWorkers([FromBody] List<WorkerNodeDto> workers)
        {
            // Сохраняем данные в сервис состояния
            _state.WorkerNodes = workers.Select((w, i) => new WorkerNode { Id = i, IpAddress = w.IpAddress, Port = w.Port, IsConnected = false }).ToList();
            return Ok(new { success = true, count = _state.WorkerNodes.Count, message = $"Настроено воркеров: {_state.WorkerNodes.Count}" });
        }

        [HttpGet("workers")]
        public IActionResult GetWorkers() => Ok(_state.WorkerNodes);

        [HttpPost("generate")]
        public IActionResult GenerateTestSystem([FromBody] GenerateOptions options)
        {
            try
            {
                var random = new Random(options.Seed ?? DateTime.Now.Millisecond);

                // Сохраняем сгенерированные данные в сервис состояния, чтобы они были доступны для других запросов
                _state.CurrentMatrix = Matrix.CreateRandomSymmetricPositiveDefinite(options.Size, random);
                _state.CurrentVector = Vector.Random(options.Size, random);

                double condition = MathHelper.ConditionNumber(_state.CurrentMatrix);

                return Ok(new { success = true, matrixSize = $"{_state.CurrentMatrix.Rows}x{_state.CurrentMatrix.Cols}", vectorSize = _state.CurrentVector.Size, conditionNumber = MathHelper.FormatScientific(condition), message = "Тестовая система сгенерирована" });
            }
            catch (Exception ex) { return BadRequest(new { success = false, error = ex.Message }); }
        }

        [HttpPost("start")]
        public IActionResult StartSolving([FromBody] SolverOptions options)
        {
            if (_state.CurrentStatus != null && _state.CurrentStatus.IsRunning)
            {
                return BadRequest(new { success = false, error = "Процесс решения уже запущен. Дождитесь его завершения." });
            }

            // Читаем данные из сервиса состояния. Теперь они будут доступны после генерации.
            if (_state.CurrentMatrix == null || _state.CurrentVector == null)
                return BadRequest(new { success = false, error = "Матрица или вектор не были сгенерированы или загружены" });

            if (_state.CurrentMatrix.Rows != _state.CurrentVector.Size)
                return BadRequest(new { success = false, error = "Размерности матрицы и вектора не совпадают" });

            _state.CurrentStatus = new SolverStatus { IsRunning = true, Message = "Подготовка к запуску...", StartTime = DateTime.UtcNow };

            _ = Task.Run(async () =>
            {
                try
                {
                    try
                    {
                        if (options.UseGauss)
                        {
                            _state.CurrentStatus.CurrentMethod = "Метод Гаусса";
                            _state.CurrentStatus.Message = "Решение локально...";
                            var gaussSolver = new GaussSolverLocal();
                            _state.LastGaussResult = gaussSolver.Solve(_state.CurrentMatrix, _state.CurrentVector);
                            Console.WriteLine($"[API] Гаусс: {_state.LastGaussResult.ElapsedMilliseconds:F2} мс, невязка: {MathHelper.FormatScientific(_state.LastGaussResult.Residual)}");
                        }
                        {
                            _state.CurrentStatus.CurrentMethod = "Метод сопряжённых градиентов (локальный)";
                            _state.CurrentStatus.Message = "Решение CG (1 поток)...";

                            // Создаем экземпляр нового класса (убедитесь, что вы создали файл ConjugateGradientSolverLocal.cs)
                            var cgLocalSolver = new ConjugateGradientSolverLocal();

                            // Запускаем решение и сохраняем в новое поле LastCGLocalResult (которое вы добавили в SolverStateService)
                            _state.LastCGLocalResult = cgLocalSolver.Solve(
                                _state.CurrentMatrix,
                                _state.CurrentVector,
                                options.MaxIterations,
                                options.Tolerance
                            );

                            Console.WriteLine($"[API] CG Local: {_state.LastCGLocalResult.ElapsedMilliseconds:F2} мс, итераций: {_state.LastCGLocalResult.Iterations}");
                        }
                        if (options.UseDistributed)
                        {
                            _state.CurrentStatus.CurrentMethod = "Метод сопряжённых градиентов (распределённый)";
                            _state.CurrentStatus.Message = "Проверка подключенных воркеров...";

                            if (_master.Workers.Count(w => w.IsConnected) == 0)
                                throw new Exception("Ошибка: Нет подключенных воркеров для выполнения задачи.");

                            _state.CurrentStatus.Message = $"Подключено воркеров: {_master.Workers.Count}. Начинаем вычисления...";
                            var cgSolver = new ConjugateGradientSolverDistributed(_master, _state.CurrentMatrix, _state.CurrentVector);
                            cgSolver.SetProgressCallback(status => _state.CurrentStatus = status);
                            _state.LastCGResult = await cgSolver.SolveAsync(maxIterations: options.MaxIterations, tolerance: options.Tolerance);
                            Console.WriteLine($"[API] CG: {_state.LastCGResult.ElapsedMilliseconds:F2} мс, итераций: {_state.LastCGResult.Iterations}, невязка: {MathHelper.FormatScientific(_state.LastCGResult.Residual)}");
                            //await _master.ShutdownWorkersAsync();
                        }

                        _state.CurrentStatus.Message = "Решение завершено";
                        _state.CurrentStatus.Progress = 100;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[API] Ошибка в фоновой задаче: {ex.Message}");
                        if (_state.CurrentStatus != null) _state.CurrentStatus.Message = $"Ошибка: {ex.Message}";
                    }
                }
                finally
                {
                    if (_state.CurrentStatus != null)
                    {
                        _state.CurrentStatus.IsRunning = false;
                    }
                    Console.WriteLine("[API] Фоновая задача завершена, флаг IsRunning сброшен.");
                }
            });

            return Ok(new { success = true, message = "Решение запущено" });
        }

        [HttpGet("status")]
        public IActionResult GetStatus()
        {
            if (_state.CurrentStatus == null)
            {
                return Ok(new SolverStatus { IsRunning = false, Message = "Решение не запускалось" });
            }
            return Ok(_state.CurrentStatus);
        }

        [HttpGet("result")]
        public IActionResult GetResult()
        {
            if (_state.LastGaussResult == null && _state.LastCGResult == null && _state.LastCGLocalResult == null)
                return NotFound("Результаты не найдены");

            var comparison = new ComparisonResult { GaussResult = _state.LastGaussResult, ConjugateGradientResult = _state.LastCGResult };

            return Ok(new
            {
       
                gaussResult = _state.LastGaussResult != null ? new
                {
                    method = _state.LastGaussResult.Method,
                    elapsedMs = _state.LastGaussResult.ElapsedMilliseconds,
                    iterations = _state.LastGaussResult.Iterations,
                    residual = _state.LastGaussResult.Residual,
                    success = _state.LastGaussResult.Success,
                    solutionSize = _state.LastGaussResult.Solution?.Size ?? 0
                } : null,
                // ИСПРАВЛЕНИЕ: Используем _state.LastCGResult
                cgResult = _state.LastCGResult != null ? new
                {
                    method = _state.LastCGResult.Method,
                    elapsedMs = _state.LastCGResult.ElapsedMilliseconds,
                    iterations = _state.LastCGResult.Iterations,
                    residual = _state.LastCGResult.Residual,
                    success = _state.LastCGResult.Success,
                    solutionSize = _state.LastCGResult.Solution?.Size ?? 0,
                    residualHistory = _state.LastCGResult.ResidualHistory
                } : null,
                cgLocalResult = _state.LastCGLocalResult != null ? new
                {
                    method = _state.LastCGLocalResult.Method,
                    elapsedMs = _state.LastCGLocalResult.ElapsedMilliseconds,
                    iterations = _state.LastCGLocalResult.Iterations,
                    residual = _state.LastCGLocalResult.Residual,
                    success = _state.LastCGLocalResult.Success,
                    solutionSize = _state.LastCGLocalResult.Solution?.Size ?? 0,
                    residualHistory = _state.LastCGLocalResult.ResidualHistory
                } : null,
                speedup = comparison.Speedup,
                residualDifference = comparison.ResidualDifference,
                summary = comparison.GetSummary()
            });
        }

        [HttpPost("clear")]
        public IActionResult Clear()
        {
            // Вызываем метод очистки на сервисе состояния
            _state.Clear();
            return Ok(new { success = true, message = "Данные очищены" });
        }
    }

    // Классы DTO (Data Transfer Object)
    public class WorkerNodeDto { public string IpAddress { get; set; } public int Port { get; set; } }
    public class SolverOptions { public bool UseGauss { get; set; } = true; public bool UseDistributed { get; set; } = true; public int MaxIterations { get; set; } = 10000; public double Tolerance { get; set; } = 1e-6; public int MasterPort { get; set; } = 5000; }
    public class GenerateOptions { public int Size { get; set; } = 100; public int? Seed { get; set; } }
}
