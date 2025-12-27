import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable, throwError, BehaviorSubject, interval } from 'rxjs';
import { timeout, catchError, map, switchMap, takeWhile } from 'rxjs/operators';

// Интерфейсы

export interface WorkerNode {
  ipAddress: string;
  port: number;
  status?: 'active' | 'idle' | 'disconnected';
}

export interface SolverStatus {
  isRunning: boolean;
  message: string;
  currentMethod?: string;
  currentIteration?: number;
  currentResidual?: number;
  progress?: number;
  startTime?: string;
  workers?: WorkerNode[];
}

export interface SolverResult {
  method: string;
  elapsedMs: number;
  iterations: number;
  residual: number;
  success: boolean;
  solutionSize: number;
  residualHistory?: number[];
  errorMessage?: string;
}

export interface ComparisonResult {
  gaussResult?: SolverResult;
  cgResult?: SolverResult;
  cgLocalResult?: SolverResult;
  speedup?: number;
  residualDifference?: number;
  summary?: string;
}

@Injectable({
  providedIn: 'root'
})
export class SolverService {
  private apiUrl = 'http://localhost:5188/api/solver';

  // BehaviorSubject для хранения текущего результата
  private currentResultSubject = new BehaviorSubject<ComparisonResult | null>(null);
  public currentResult$ = this.currentResultSubject.asObservable();

  constructor(private http: HttpClient) {}

  // Загрузка матрицы A
  uploadMatrix(file: File): Observable<any> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post(`${this.apiUrl}/upload/matrix`, formData);
  }

  // Загрузка вектора b
  uploadVector(file: File): Observable<any> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post(`${this.apiUrl}/upload/vector`, formData);
  }

  // Генерация тестовой системы (генерация на сервере)
  generateTestSystem(size: number, sparsity: number, diagonalDominance: boolean): Observable<any> {
    console.log(`[Service] Отправка запроса на генерацию: size=${size}, sparsity=${sparsity}, diagonalDominance=${diagonalDominance}`);
    
    const params = {
      size: size,
      sparsity: sparsity, // значение от 0 до 1
      diagonalDominance: diagonalDominance
    };
    
    // Увеличиваем таймаут для больших матриц
    const requestTimeout = size > 5000 ? 300000 : (size > 1000 ? 120000 : 60000);
    
    return this.http.post(`${this.apiUrl}/generate`, params, {
      headers: new HttpHeaders({
        'Content-Type': 'application/json'
      })
    }).pipe(
      timeout(requestTimeout),
      catchError(error => {
        if (error.name === 'TimeoutError') {
          console.error('[Service] Timeout error');
          return throwError(() => ({ 
            error: { error: `Превышено время ожидания (${requestTimeout/1000} сек). Попробуйте уменьшить размер матрицы.` } 
          }));
        }
        console.error('[Service] Error:', error);
        return throwError(() => error);
      })
    );
  }
  setWorkers(workers: WorkerNode[]): Observable<{ success: boolean; message: string }> {
  return this.http.post<{ success: boolean; message: string }>(
    `${this.apiUrl}/workers`,
    workers
  );
  }
  // Запуск решения системы
  startSolving(options: any): Observable<any> {
    return this.http.post(`${this.apiUrl}/start`, options);
  }

  // Получение статуса решения
  getStatus(): Observable<SolverStatus> {
    return this.http.get<SolverStatus>(`${this.apiUrl}/status`);
  }

  // Наблюдение за статусом с периодическим опросом
  watchStatus(): Observable<SolverStatus> {
    return interval(500).pipe(
      switchMap(() => this.getStatus()),
      takeWhile(status => status.isRunning, true) // true = включить последнее значение
    );
  }

  // Получение результата решения
  getResult(): Observable<ComparisonResult> {
    return this.http.get<ComparisonResult>(`${this.apiUrl}/result`).pipe(
      map(result => {
        // Обновляем BehaviorSubject с новыми результатами
        this.currentResultSubject.next(result);
        return result;
      })
    );
  }

  // Очистка результатов
  clearResults(): void {
    this.currentResultSubject.next(null);
  }

  // Очистка данных на сервере
  clear(): Observable<any> {
    return this.http.post(`${this.apiUrl}/clear`, {});
  }
}