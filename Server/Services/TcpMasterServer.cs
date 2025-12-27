// --- НАЧАЛО ФАЙЛА Server/Services/TcpMasterServer.cs ---

using Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Server.Services
{
    /// <summary>
    /// TCP Master-сервер для координации распределённых вычислений.
    /// Теперь это Singleton-сервис, управляемый ASP.NET Core.
    /// </summary>
    public class TcpMasterServer
    {
        private readonly int _port;
        private TcpListener _listener;
        private readonly List<WorkerConnection> _workers;
        private bool _isRunning;
        private int _nextWorkerId = 1; // присваиваем id автоматически при подключении

        // VIRTUAL добавлен для возможности мокирования свойства в тестах
        public virtual List<WorkerNode> Workers => _workers.Select(w => w.Node).ToList();

        public bool IsRunning => _isRunning;

        public TcpMasterServer(int port = 5000)
        {
            _port = port;
            _workers = new List<WorkerConnection>();
        }

        /// <summary>
        /// Запустить Master-сервер и цикл приема подключений
        /// </summary>
        public virtual async Task StartAsync()
        {
            // Если сервер уже запущен, ничего не делаем
            if (_isRunning) return;

            try
            {
                _listener = new TcpListener(IPAddress.Any, _port);

                // Эта опция позволяет немедленно переиспользовать сокет после его закрытия
                _listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

                _listener.Start();
                _isRunning = true;

                Console.WriteLine($"[Master] Сервер запущен на порту {_port}");

                // Запускаем цикл приема подключений в фоне
                _ = Task.Run(AcceptLoop);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Master] Ошибка запуска сервера: {ex.Message}");
                _isRunning = false;
                throw;
            }
        }

        /// <summary>
        /// Цикл принятия входящих подключений от воркеров
        /// </summary>
        private async Task AcceptLoop()
        {
            while (_isRunning)
            {
                try
                {
                    var client = await _listener.AcceptTcpClientAsync();
                    var stream = client.GetStream();

                    // Создаём запись для воркера
                    var endpoint = client.Client.RemoteEndPoint as IPEndPoint;
                    var assignedId = _nextWorkerId++;
                    var node = new WorkerNode
                    {
                        Id = assignedId,
                        IpAddress = endpoint?.Address.ToString() ?? "unknown",
                        Port = endpoint?.Port ?? 0,
                        IsConnected = true,
                        StartRow = 0,
                        EndRow = 0
                    };

                    var worker = new WorkerConnection
                    {
                        Node = node,
                        Client = client,
                        Stream = stream
                    };

                    lock (_workers)
                    {
                        _workers.Add(worker);
                    }

                    Console.WriteLine($"[Master] Принято подключение от {node.IpAddress}:{node.Port} -> VorkerId={node.Id}");
                }
                catch (ObjectDisposedException)
                {
                    // listener остановлен, это нормальное завершение цикла
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Master] Ошибка в AcceptLoop: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Подключить воркеров по списку адресов
        /// </summary>
        public virtual async Task ConnectWorkersAsync(List<WorkerNode> nodes)
        {
            Console.WriteLine($"[Master] Подключение {nodes.Count} воркеров (инициирую исходящие подключения)...");

            var tasks = new List<Task>();

            foreach (var node in nodes)
            {
                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        var client = new TcpClient();
                        await client.ConnectAsync(node.IpAddress, node.Port);

                        var worker = new WorkerConnection
                        {
                            Node = node,
                            Client = client,
                            Stream = client.GetStream()
                        };

                        lock (_workers)
                        {
                            _workers.Add(worker);
                        }

                        node.IsConnected = true;
                        node.LastHeartbeat = DateTime.UtcNow;

                        Console.WriteLine($"[Master] Подключён воркер {node.Id} ({node.IpAddress}:{node.Port})");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Master] Не удалось подключить воркер {node.Id}: {ex.Message}");
                        node.IsConnected = false;
                    }
                }));
            }

            await Task.WhenAll(tasks);

            Console.WriteLine($"[Master] Всего подключено воркеров: {_workers.Count}");
        }

        /// <summary>
        /// Надёжное чтение ровно count байт из потока
        /// </summary>
        private static async Task ReadExactAsync(NetworkStream stream, byte[] buffer, int offset, int count)
        {
            int total = 0;
            while (total < count)
            {
                int read = await stream.ReadAsync(buffer, offset + total, count - total);
                if (read == 0)
                    throw new Exception("Соединение закрыто при чтении ответа");
                total += read;
            }
        }

        /// <summary>
        /// Инициализировать воркеров с частями матрицы и вектора
        /// </summary>
        // VIRTUAL добавлен для тестов
        public virtual async Task InitializeWorkersAsync(Matrix A, Vector b)
        {
            if (_workers.Count == 0)
                throw new Exception("Нет подключённых воркеров");

            int n = A.Rows;
            int workersCount = _workers.Count;
            int rowsPerWorker = n / workersCount;
            int remainder = n % workersCount;

            Console.WriteLine($"[Master] Инициализация воркеров. Матрица {n}x{n}, воркеров: {workersCount}");

            var tasks = new List<Task>();
            int currentRow = 0;

            for (int i = 0; i < workersCount; i++)
            {
                int workerRows = rowsPerWorker + (i < remainder ? 1 : 0);
                int startRow = currentRow;
                int endRow = currentRow + workerRows - 1;

                _workers[i].Node.StartRow = startRow;
                _workers[i].Node.EndRow = endRow;

                var subMatrix = A.GetSubMatrix(startRow, endRow);
                var subVector = b.GetSubVector(startRow, endRow);

                var worker = _workers[i];
                tasks.Add(Task.Run(async () =>
                {
                    var message = TcpMessage.CreateInitialize(worker.Node.Id, subMatrix, subVector);
                    var response = await SendAndReceiveAsync(worker, message);

                    if (response.Type == MessageType.Error)
                    {
                        Console.WriteLine($"[Master] Ошибка инициализации воркера {worker.Node.Id}: {response.ErrorMessage}");
                    }
                    else
                    {
                        Console.WriteLine($"[Master] Воркер {worker.Node.Id} инициализирован (строки {startRow}-{endRow})");
                    }
                }));

                currentRow += workerRows;
            }

            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// Распределённое вычисление A*p
        /// </summary>
        // VIRTUAL добавлен для тестов
        public virtual async Task<Vector> ComputeMatrixVectorAsync(Vector p)
        {
            var tasks = new List<Task<Vector>>();

            foreach (var worker in _workers)
            {
                tasks.Add(Task.Run(async () =>
                {
                    var message = TcpMessage.CreateComputeMatrixVector(worker.Node.Id, p);
                    var response = await SendAndReceiveAsync(worker, message);

                    if (response.Type == MessageType.Error)
                    {
                        throw new Exception($"Воркер {worker.Node.Id}: {response.ErrorMessage}");
                    }

                    return new Vector(response.ResultData);
                }));
            }

            var results = await Task.WhenAll(tasks);

            int totalSize = results.Sum(v => v.Size);
            var fullResult = new Vector(totalSize);

            int offset = 0;
            for (int i = 0; i < results.Length; i++)
            {
                for (int j = 0; j < results[i].Size; j++)
                {
                    fullResult[offset + j] = results[i][j];
                }
                offset += results[i].Size;
            }

            return fullResult;
        }

        /// <summary>
        /// Распределённое вычисление скалярного произведения
        /// </summary>
        // VIRTUAL добавлен для тестов
        public virtual async Task<double> ComputeDotProductAsync(Vector v1, Vector v2)
        {
            if (v1.Size != v2.Size)
                throw new ArgumentException("Векторы должны быть одинакового размера");

            var tasks = new List<Task<double>>();

            foreach (var worker in _workers)
            {
                tasks.Add(Task.Run(async () =>
                {
                    var v1Sub = v1.GetSubVector(worker.Node.StartRow, worker.Node.EndRow);
                    var v2Sub = v2.GetSubVector(worker.Node.StartRow, worker.Node.EndRow);

                    var message = TcpMessage.CreateComputeDotProduct(worker.Node.Id, v1Sub, v2Sub);
                    var response = await SendAndReceiveAsync(worker, message);

                    if (response.Type == MessageType.Error)
                    {
                        throw new Exception($"Воркер {worker.Node.Id}: {response.ErrorMessage}");
                    }

                    return response.DotProductResult;
                }));
            }

            var results = await Task.WhenAll(tasks);

            return results.Sum();
        }

        /// <summary>
        /// Обновить локальные векторы на воркерах
        /// </summary>
        // VIRTUAL добавлен для тестов (если понадобится)
        public virtual async Task UpdateVectorAsync(Vector vector)
        {
            var tasks = new List<Task>();

            foreach (var worker in _workers)
            {
                tasks.Add(Task.Run(async () =>
                {
                    var subVector = vector.GetSubVector(worker.Node.StartRow, worker.Node.EndRow);
                    var message = TcpMessage.CreateUpdateVector(worker.Node.Id, subVector);
                    await SendAndReceiveAsync(worker, message);
                }));
            }

            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// Отправить сообщение и получить ответ (надёжно читает длину и данные)
        /// </summary>
        private async Task<TcpMessage> SendAndReceiveAsync(WorkerConnection worker, TcpMessage message)
        {
            if (worker?.Stream == null)
                throw new InvalidOperationException("Нет сетевого потока для воркера");

            try
            {
                // 1. Сериализуем сообщение в память (Binary)
                using var ms = new MemoryStream();
                using var writer = new BinaryWriter(ms);
                message.WriteTo(writer);
                byte[] data = ms.ToArray();

                // 2. Отправляем длину + данные
                byte[] lengthBuffer = BitConverter.GetBytes(data.Length);
                await worker.Stream.WriteAsync(lengthBuffer, 0, 4);
                await worker.Stream.WriteAsync(data, 0, data.Length);
                await worker.Stream.FlushAsync();

                // 3. Читаем длину ответа
                byte[] responseLengthBuffer = new byte[4];
                await ReadExactAsync(worker.Stream, responseLengthBuffer, 0, 4);
                int responseLength = BitConverter.ToInt32(responseLengthBuffer, 0);

                if (responseLength <= 0)
                    throw new Exception("Некорректная длина ответа");

                // 4. Читаем тело ответа (Binary)
                byte[] responseData = new byte[responseLength];
                await ReadExactAsync(worker.Stream, responseData, 0, responseLength);

                // 5. Десериализуем
                using var msResponse = new MemoryStream(responseData);
                using var reader = new BinaryReader(msResponse);
                return TcpMessage.ReadFrom(reader);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Master] Ошибка с воркером {worker.Node?.Id}: {ex.Message}");
                return TcpMessage.CreateError(worker.Node?.Id ?? -1, ex.Message);
            }
        }

        /// <summary>
        /// Отключить всех воркеров
        /// </summary>
        public virtual async Task ShutdownWorkersAsync()
        {
            Console.WriteLine("[Master] Отключение воркеров...");

            var message = TcpMessage.CreateShutdown();

            foreach (var worker in _workers)
            {
                try
                {
                    await SendAndReceiveAsync(worker, message);
                    try { worker.Client.Close(); } catch { }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Master] Ошибка при отключении воркера {worker.Node.Id}: {ex.Message}");
                }
            }

            _workers.Clear();
        }

        /// <summary>
        /// Остановить сервер
        /// </summary>
        public virtual void Stop()
        {
            _isRunning = false;
            try { _listener?.Stop(); } catch { }
            lock (_workers)
            {
                foreach (var w in _workers)
                {
                    try { w.Stream?.Close(); } catch { }
                    try { w.Client?.Close(); } catch { }
                }
                _workers.Clear();
            }
            _nextWorkerId = 1; // Сбрасываем счетчик ID
            Console.WriteLine("[Master] Сервер остановлен");
        }

        /// <summary>
        /// Внутренний класс для хранения подключения к воркеру
        /// </summary>
        private class WorkerConnection
        {
            public WorkerNode Node { get; set; }
            public TcpClient Client { get; set; }
            public NetworkStream Stream { get; set; }
        }
    }
}