using Server.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. Настройка сервисов (Dependency Injection)
// ---------------------------------------------
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Настройка CORS для Angular
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Регистрируем сервис для хранения состояния
builder.Services.AddSingleton<SolverStateService>();

// --- ДОБАВЛЕНО: Регистрируем TcpMasterServer как Singleton ---
// Считываем порт из конфигурации или берем 5000 по умолчанию
int masterPortConfig = builder.Configuration.GetValue<int>("TcpSettings:MasterPort", 9000); 
builder.Services.AddSingleton<TcpMasterServer>(sp => new TcpMasterServer(masterPortConfig));

// 2. Построение приложения
// ----------------------------------------------------
var app = builder.Build();

// --- ДОБАВЛЕНО: Запуск TCP сервера при старте приложения ---
// Получаем экземпляр из DI-контейнера и запускаем его
var tcpServer = app.Services.GetRequiredService<TcpMasterServer>();
try
{
    // StartAsync запускает AcceptLoop в фоне (Task.Run), поэтому await здесь не заблокирует запуск HTTP
    await tcpServer.StartAsync();
}
catch (Exception ex)
{
    Console.WriteLine($"!!! Не удалось запустить TCP сервер: {ex.Message}");
}

// Настройка конвейера HTTP
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAngular");
app.UseAuthorization();
app.MapControllers();

var apiPort = 5001; // Убедитесь, что в launchSettings.json настроен этот порт для HTTPS или HTTP

Console.WriteLine("=== Distributed SLAE Solver Server ===");
Console.WriteLine($"API запущен: http://localhost:{apiPort}");
Console.WriteLine($"TCP Master слушает порт: {masterPortConfig}");
Console.WriteLine("Сервер готов к работе.");

app.Run();