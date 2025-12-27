using Shared.Models;
using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Vector = Shared.Models.Vector;

namespace Worker.Services
{
    /// <summary>
    /// TCP Worker-клиент для распределённых вычислений
    /// </summary>
    public class TcpWorkerClient
    {
        private readonly string _masterIp;
        private readonly int _masterPort;
        private readonly int _workerId;
        private TcpClient _client;
        private NetworkStream _stream;
        private bool _isRunning;

        // Данные воркера
        private Matrix _localMatrix;
        private Vector _localVector;

        public TcpWorkerClient(int workerId, string masterIp, int masterPort)
        {
            _workerId = workerId;
            _masterIp = masterIp;
            _masterPort = masterPort;
        }

        /// <summary>
        /// Запустить воркер (простая версия без переподключения)
        /// </summary>
        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                Console.WriteLine($"[Worker {_workerId}] Подключение к мастеру {_masterIp}:{_masterPort}...");

                _client = new TcpClient();
                await _client.ConnectAsync(_masterIp, _masterPort);
                _stream = _client.GetStream();
                _isRunning = true;

                Console.WriteLine($"[Worker {_workerId}] Подключён к мастеру");

                // Основной цикл обработки сообщений
                await ProcessMessagesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Worker {_workerId}] Ошибка: {ex.Message}");
            }
            finally
            {
                Stop();
            }
        }

        /// <summary>
        /// Безопасное чтение ровно указанного количества байт (при необходимости — в цикле)
        /// </summary>
        private static async Task ReadExactAsync(NetworkStream stream, byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            int total = 0;
            while (total < count)
            {
                int read = await stream.ReadAsync(buffer, offset + total, count - total, cancellationToken);
                if (read == 0)
                    throw new Exception("Соединение закрыто во время чтения данных");
                total += read;
            }
        }

        /// <summary>
        /// Обработка входящих сообщений от мастера
        /// </summary>
        private async Task ProcessMessagesAsync(CancellationToken cancellationToken)
        {
            // Буфер теперь просто для чтения частей, если нужно, 
            // но ReadExactAsync читает прямо в целевой массив.

            while (_isRunning && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // 1. Читаем длину
                    var lengthBuffer = new byte[4];
                    await ReadExactAsync(_stream, lengthBuffer, 0, 4, cancellationToken);
                    int messageLength = BitConverter.ToInt32(lengthBuffer, 0);

                    if (messageLength <= 0) continue;

                    // 2. Читаем данные
                    byte[] data = new byte[messageLength];
                    await ReadExactAsync(_stream, data, 0, messageLength, cancellationToken);

                    // 3. Десериализуем (Binary)
                    using var ms = new MemoryStream(data);
                    using var reader = new BinaryReader(ms);
                    var message = TcpMessage.ReadFrom(reader);

                    // 4. Обрабатываем
                    var response = await HandleMessageAsync(message);

                    // 5. Отправляем ответ
                    if (response != null)
                    {
                        await SendMessageAsync(response);
                    }
                }
                catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
                {
                    Console.WriteLine($"[Worker {_workerId}] Ошибка: {ex.Message}");
                    // Небольшая пауза при ошибке, чтобы не заспамить лог
                    await Task.Delay(100, cancellationToken);
                }
            }
        }

        /// <summary>
        /// Обработка конкретного сообщения
        /// </summary>
        private async Task<TcpMessage> HandleMessageAsync(TcpMessage message)
        {
            Console.WriteLine($"[Worker {_workerId}] Получено сообщение: {message.Type}");

            try
            {
                switch (message.Type)
                {
                    case MessageType.Initialize:
                        return HandleInitialize(message);

                    case MessageType.ComputeMatrixVector:
                        return HandleComputeMatrixVector(message);

                    case MessageType.ComputeDotProduct:
                        return HandleComputeDotProduct(message);

                    case MessageType.UpdateVector:
                        return HandleUpdateVector(message);

                    case MessageType.Shutdown:
                        _isRunning = false;
                        // Воркер отправляет стандартный ответ "ОК, я получил команду"
                        return TcpMessage.CreateResult(_workerId);

                    default:
                        throw new Exception($"Неизвестный тип сообщения: {message.Type}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Worker {_workerId}] Ошибка обработки {message.Type}: {ex.Message}");
                return TcpMessage.CreateError(_workerId, ex.Message);
            }
        }

        /// <summary>
        /// Инициализация воркера с локальной частью матрицы и вектора
        /// </summary>
        private TcpMessage HandleInitialize(TcpMessage message)
        {
            // --- ИЗМЕНЕНИЕ ЗДЕСЬ ---
            // Теперь message.MatrixData имеет тип double[][], и мы вызываем
            // новый конструктор Matrix, который умеет с ним работать.
            _localMatrix = new Matrix(message.MatrixData);
            _localVector = new Vector(message.VectorData);

            Console.WriteLine($"[Worker {_workerId}] Инициализирован. Матрица: {_localMatrix.Rows}x{_localMatrix.Cols}, Вектор: {_localVector.Size}");

            return TcpMessage.CreateResult(_workerId);
        }

        /// <summary>
        /// Вычисление локальной части A*p
        /// </summary>
        private TcpMessage HandleComputeMatrixVector(TcpMessage message)
        {
            var p = new Vector(message.VectorData);

            // Вычисляем локальную часть A*p
            var localResult = _localMatrix.Multiply(p);

            Console.WriteLine($"[Worker {_workerId}] Вычислен A*p: размер результата {localResult.Size}");

            return TcpMessage.CreateResult(_workerId, localResult.Data);
        }

        /// <summary>
        /// Вычисление локальной части скалярного произведения
        /// </summary>
        private TcpMessage HandleComputeDotProduct(TcpMessage message)
        {
            var v1Sub = new Vector(message.VectorData);
            var v2Sub = new Vector(message.Vector2Data);

            // Вычисляем локальное скалярное произведение
            double localDot = v1Sub.Dot(v2Sub);

            Console.WriteLine($"[Worker {_workerId}] Вычислено скалярное произведение: {localDot}");

            return TcpMessage.CreateResult(_workerId, dotProduct: localDot);
        }

        /// <summary>
        /// Обновление локального вектора
        /// </summary>
        private TcpMessage HandleUpdateVector(TcpMessage message)
        {
            _localVector = new Vector(message.VectorData);

            Console.WriteLine($"[Worker {_workerId}] Обновлён локальный вектор");

            return TcpMessage.CreateResult(_workerId);
        }

        /// <summary>
        /// Отправка сообщения мастеру
        /// </summary>
        private async Task SendMessageAsync(TcpMessage message)
        {
            if (_stream == null) return;

            // Сериализация в бинарный формат
            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms);
            message.WriteTo(writer);

            byte[] data = ms.ToArray();

            // Отправка длины
            byte[] lengthBuffer = BitConverter.GetBytes(data.Length);
            await _stream.WriteAsync(lengthBuffer, 0, 4);

            // Отправка данных
            await _stream.WriteAsync(data, 0, data.Length);
            await _stream.FlushAsync();
        }

        /// <summary>
        /// Остановить воркер
        /// </summary>
        public void Stop()
        {
            _isRunning = false;
            try { _stream?.Close(); } catch { }
            try { _client?.Close(); } catch { }
            Console.WriteLine($"[Worker {_workerId}] Остановлен");
        }
    }
}
