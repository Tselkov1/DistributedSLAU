using System;
using System.Threading;
using System.Threading.Tasks;
using Worker.Services;

namespace Worker
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            // Разбор именованных аргументов
            int workerId = 0;
            string masterIp = "127.0.0.1";
            int masterPort = 5000;

            for (int i = 0; i < args.Length - 1; i++)
            {
                switch (args[i])
                {
                    case "--id": workerId = int.Parse(args[++i]); break;
                    case "--master-ip": masterIp = args[++i]; break;
                    case "--master-port": masterPort = int.Parse(args[++i]); break;
                }
            }

            Console.WriteLine($"[Worker {workerId}] Подключение к {masterIp}:{masterPort}");
            var worker = new TcpWorkerClient(workerId, masterIp, masterPort);
            var cts = new CancellationTokenSource();

            Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

            try
            {
                await worker.StartAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine($"[Worker {workerId}] Остановлен.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Worker {workerId}] Ошибка: {ex.Message}");
            }

            Console.WriteLine($"[Worker {workerId}] Завершён.");
            Console.ReadKey();
        }
    }
}