// --- START OF FILE Server/Services/SolverStateService.cs ---

using Shared.Models;
using System.Collections.Generic;
using Vector = Shared.Models.Vector;

namespace Server.Services
{
    /// <summary>
    /// Сервис для хранения состояния решателя между HTTP-запросами.
    /// Регистрируется как Singleton, поэтому данные сохраняются на всё время жизни приложения.
    /// </summary>
    public class SolverStateService
    {
        public Matrix CurrentMatrix { get; set; }
        public Vector CurrentVector { get; set; }
        public List<WorkerNode> WorkerNodes { get; set; } = new();
        public SolverStatus CurrentStatus { get; set; }
        public SolverResult LastGaussResult { get; set; }
        public SolverResult LastCGResult { get; set; }
        public SolverResult LastCGLocalResult { get; set; }
        public void Clear()
        {
            CurrentMatrix = null;
            CurrentVector = null;
            LastGaussResult = null;
            LastCGResult = null;
            LastCGLocalResult = null;
            CurrentStatus = null;
            WorkerNodes.Clear();
        }
    }
}
// --- END OF FILE Server/Services/SolverStateService.cs ---