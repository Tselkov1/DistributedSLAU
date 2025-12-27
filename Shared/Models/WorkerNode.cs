using System;

namespace Shared.Models
{
    /// <summary>
    /// Информация об узле-воркере в распределённой системе
    /// </summary>
    public class WorkerNode
    {
        /// <summary>
        /// Уникальный идентификатор воркера
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// IP-адрес воркера
        /// </summary>
        public string IpAddress { get; set; }

        /// <summary>
        /// TCP-порт для подключения к воркеру
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// Статус подключения воркера
        /// </summary>
        public bool IsConnected { get; set; }

        /// <summary>
        /// Время последнего heartbeat от воркера
        /// </summary>
        public DateTime LastHeartbeat { get; set; }

        /// <summary>
        /// Начальная строка матрицы, за которую отвечает данный воркер
        /// </summary>
        public int StartRow { get; set; }

        /// <summary>
        /// Конечная строка матрицы (включительно), за которую отвечает данный воркер
        /// </summary>
        public int EndRow { get; set; }

        /// <summary>
        /// Количество строк, обрабатываемых данным воркером
        /// </summary>
        public int RowCount => EndRow - StartRow + 1;

        /// <summary>
        /// Время подключения воркера
        /// </summary>
        public DateTime? ConnectedAt { get; set; }

        /// <summary>
        /// Количество обработанных задач воркером
        /// </summary>
        public int TasksProcessed { get; set; }

        public WorkerNode()
        {
            IpAddress = "127.0.0.1";
            Port = 6000;
            IsConnected = false;
            LastHeartbeat = DateTime.MinValue;
            TasksProcessed = 0;
        }

        public WorkerNode(int id, string ipAddress, int port) : this()
        {
            Id = id;
            IpAddress = ipAddress;
            Port = port;
        }

        /// <summary>
        /// Получить полный адрес воркера
        /// </summary>
        public string GetFullAddress()
        {
            return $"{IpAddress}:{Port}";
        }

        /// <summary>
        /// Обновить heartbeat
        /// </summary>
        public void UpdateHeartbeat()
        {
            LastHeartbeat = DateTime.UtcNow;
        }

        public override string ToString()
        {
            return $"Worker {Id}: {IpAddress}:{Port} (строки {StartRow}-{EndRow}, {(IsConnected ? "подключён" : "отключён")})";
        }
    }
}