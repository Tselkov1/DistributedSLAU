@echo off
chcp 65001 >nul
echo =========================================
echo Distributed SLAE Solver - Quick Start
echo =========================================
echo.

echo =========================================
echo Запуск компонентов
echo =========================================
echo.

REM Запуск Server
echo Запуск Server (порт 5001)...
start "Server" cmd /k "cd Server && dotnet run"
timeout /t 3 >nul
echo + Server запущен
echo.

REM Запуск Workers
echo Запуск Workers...
start "Worker 0" cmd /k "cd Worker && dotnet run -- --id 0 --master-ip 127.0.0.1 --master-port 9000"
timeout /t 1 >nul
start "Worker 1" cmd /k "cd Worker && dotnet run -- --id 1 --master-ip 127.0.0.1 --master-port 9000"
timeout /t 1 >nul
start "Worker 2" cmd /k "cd Worker && dotnet run -- --id 2 --master-ip 127.0.0.1 --master-port 9000"
timeout /t 1 >nul
start "Worker 3" cmd /k "cd Worker && dotnet run -- --id 3 --master-ip 127.0.0.1 --master-port 9000"
timeout /t 1 >nul
echo + Все Workers запущены
echo.



echo =========================================
echo + Все компоненты запущены!
echo =========================================
echo.
echo Доступные интерфейсы:
echo   - UI:         http://localhost:4200
echo   - API:        http://localhost:5001
echo   - Swagger:    http://localhost:5001/swagger
echo.
echo Для остановки закройте все окна консоли
echo.
pause