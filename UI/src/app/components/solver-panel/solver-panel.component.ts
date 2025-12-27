import { Component, Input, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subscription } from 'rxjs';
import { SolverService, SolverStatus } from '../../services/solver.service';

@Component({
  selector: 'app-solver-panel',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="solver-panel">
      <!-- Блок настроек -->
      <div class="settings-card" [class.disabled]="isRunning">
        <div class="card-header">
          <h3>⚙️ Параметры решения</h3>
        </div>
        
        <div class="settings-content">
          <div class="methods-group">
            <label class="checkbox-label">
              <input type="checkbox" [(ngModel)]="options.useGauss" [disabled]="isRunning">
              <span class="check-text">
                <strong>Метод Гаусса</strong>
                <small>Прямой метод (точный, но медленный на больших матрицах)</small>
              </span>
            </label>
            <label class="checkbox-label locked">
              <input type="checkbox" [checked]="true" disabled>
              <span class="check-text">
                <strong>CG (Локальный)</strong>
                <small>Однопоточный (для расчета ускорения)</small>
              </span>
            </label>
            <label class="checkbox-label">
              <input type="checkbox" [(ngModel)]="options.useConjugateGradient" [disabled]="isRunning">
              <span class="check-text">
                <strong>Метод сопряжённых градиентов</strong>
                <small>Итерационный метод (быстрый для разреженных матриц)</small>
              </span>
            </label>
          </div>

          <div class="params-grid">
            <div class="form-group">
              <label>Макс. итераций:</label>
              <input type="number" [(ngModel)]="options.maxIterations" [disabled]="isRunning" class="form-control">
            </div>

            <div class="form-group">
              <label>Точность (ε):</label>
              <input type="text" [(ngModel)]="toleranceStr" [disabled]="isRunning" class="form-control" placeholder="1e-6">
            </div>
          </div>
        </div>
      </div>

      <!-- Кнопка запуска -->
      <div class="action-area">
        <button class="btn-start" 
                (click)="startSolving()" 
                [disabled]="!canStart || isRunning || !hasSelectedMethod()">
          <span *ngIf="!isRunning">🚀 Запустить решение</span>
          <span *ngIf="isRunning">⏳ Выполняется...</span>
        </button>
      <div *ngIf="!canStart && !isRunning" class="warning-msg">
        ⚠️ Необходимо загрузить данные (Шаг 1) и настроить узлы (Шаг 2)
      </div>
        <!-- Предупреждения -->
        <div *ngIf="!canStart && !isRunning" class="warning-msg">
          ⚠️ Сначала загрузите матрицу и вектор (Шаг 1)
        </div>
        <div *ngIf="!hasSelectedMethod() && !isRunning" class="warning-msg">
          ⚠️ Выберите хотя бы один метод
        </div>
      </div>

      <!-- Статус выполнения -->
      <div *ngIf="isRunning || status" class="status-card" [class.active]="isRunning">
        <h4>Мониторинг процесса</h4>
        
        <div *ngIf="status" class="status-grid">
          <div class="status-item">
            <span class="label">Текущий метод:</span>
            <span class="value highlight">{{ status.currentMethod || 'Инициализация...' }}</span>
          </div>

          <div class="status-item" *ngIf="status.currentIteration">
            <span class="label">Итерация:</span>
            <span class="value">{{ status.currentIteration }}</span>
          </div>

          <div class="status-item full-width" *ngIf="status.message">
            <span class="label">Статус:</span>
            <span class="value">{{ status.message }}</span>
          </div>

          <div class="status-item full-width" *ngIf="status.currentResidual !== undefined && status.currentResidual > 0">
            <span class="label">Невязка (ошибка):</span>
            <span class="value mono">{{ status.currentResidual | number:'1.1-15' }}</span>
          </div>
        </div>
        <div *ngIf="status?.workers && status!.workers!.length > 0" class="workers-monitor">
          <h5>Активные узлы ({{ status!.workers!.length }})</h5>
          <div class="workers-list-mini">
            <div *ngFor="let w of status!.workers" class="worker-badge">
              <span class="status-dot"></span>
              {{ w.ipAddress }}:{{ w.port }}
            </div>
          </div>
        </div>ы
        <!-- Прогресс бар -->
        <div class="progress-container" *ngIf="status?.progress !== undefined">
          <div class="progress-bar">
            <div class="progress-fill" [style.width.%]="status!.progress"></div>
          </div>
          <div class="progress-text">{{ status!.progress | number:'1.0-0' }}%</div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .solver-panel {
      display: flex;
      flex-direction: column;
      gap: 25px;
    }

    /* Настройки */
    .settings-card {
      background: #f8f9fa;
      border: 1px solid #e0e0e0;
      border-radius: 8px;
      transition: opacity 0.3s;
    }

    .settings-card.disabled {
      opacity: 0.7;
      pointer-events: none;
    }

    .card-header {
      padding: 15px 20px;
      border-bottom: 1px solid #e0e0e0;
      background: #f1f3f5;
      border-radius: 8px 8px 0 0;
    }

    .card-header h3 { margin: 0; font-size: 1.1rem; color: #495057; }

    .settings-content { padding: 20px; }

    .methods-group {
      display: flex;
      flex-direction: column;
      gap: 15px;
      margin-bottom: 20px;
    }

    .checkbox-label {
      display: flex;
      align-items: flex-start;
      gap: 12px;
      cursor: pointer;
      padding: 8px;
      border-radius: 6px;
      transition: background 0.2s;
    }

    .checkbox-label:hover { background: #e9ecef; }
    
    .checkbox-label input {
      margin-top: 5px;
      width: 18px;
      height: 18px;
      accent-color: #667eea;
    }

    .check-text { display: flex; flex-direction: column; }
    .check-text strong { color: #333; }
    .check-text small { color: #666; margin-top: 2px; }

    .params-grid {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 20px;
    }

    .form-group label {
      display: block;
      margin-bottom: 6px;
      font-weight: 500;
      color: #555;
    }

    .form-control {
      width: 100%;
      padding: 10px;
      border: 1px solid #ced4da;
      border-radius: 4px;
      font-size: 1rem;
      box-sizing: border-box;
    }

    .form-control:focus {
      border-color: #667eea;
      outline: none;
    }
        /* Воркеры */
    .workers-monitor {
      margin: 15px 0;
      padding-top: 10px;
      border-top: 1px dashed #eee;
    }

    .workers-monitor h5 {
      margin: 0 0 10px 0;
      font-size: 0.9rem;
      color: #555;
    }

    .workers-list-mini {
      display: flex;
      flex-wrap: wrap;
      gap: 8px;
    }

    .worker-badge {
      background: #f0fdf4;
      border: 1px solid #bbf7d0;
      color: #166534;
      padding: 4px 10px;
      border-radius: 12px;
      font-size: 0.85rem;
      display: flex;
      align-items: center;
      gap: 6px;
    }

    .status-dot {
      width: 8px;
      height: 8px;
      background: #22c55e;
      border-radius: 50%;
      display: inline-block;
      animation: pulse 2s infinite;
    }

    @keyframes pulse {
      0% { opacity: 1; }
      50% { opacity: 0.4; }
      100% { opacity: 1; }
    }
    /* Кнопка и действия */
    .action-area {
      text-align: center;
    }

    .btn-start {
      background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
      color: white;
      border: none;
      padding: 15px 40px;
      font-size: 1.2rem;
      border-radius: 50px;
      cursor: pointer;
      font-weight: bold;
      box-shadow: 0 4px 15px rgba(102, 126, 234, 0.4);
      transition: all 0.3s ease;
    }

    .btn-start:hover:not(:disabled) {
      transform: translateY(-2px);
      box-shadow: 0 6px 20px rgba(102, 126, 234, 0.6);
    }

    .btn-start:disabled {
      background: #adb5bd;
      cursor: not-allowed;
      box-shadow: none;
    }

    .warning-msg {
      margin-top: 10px;
      color: #856404;
      background-color: #fff3cd;
      padding: 10px;
      border-radius: 4px;
      display: inline-block;
      font-weight: 500;
    }

    /* Статус панель */
    .status-card {
      background: white;
      border: 2px solid #667eea;
      border-radius: 8px;
      padding: 20px;
      animation: slideDown 0.4s ease;
    }

    .status-card h4 {
      margin: 0 0 15px 0;
      color: #667eea;
      border-bottom: 1px solid #eee;
      padding-bottom: 8px;
    }

    .status-grid {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 15px;
      margin-bottom: 15px;
    }

    .status-item {
      display: flex;
      flex-direction: column;
    }

    .status-item.full-width { grid-column: 1 / -1; }

    .status-item .label { font-size: 0.85rem; color: #666; margin-bottom: 2px; }
    .status-item .value { font-weight: 600; color: #333; }
    .status-item .value.highlight { color: #667eea; }
    .status-item .value.mono { font-family: monospace; }

    /* Прогресс бар */
    .progress-container {
      margin-top: 10px;
    }

    .progress-bar {
      height: 10px;
      background: #e9ecef;
      border-radius: 5px;
      overflow: hidden;
      margin-bottom: 5px;
    }

    .progress-fill {
      height: 100%;
      background: #20c997;
      transition: width 0.3s ease;
      background-image: linear-gradient(45deg,rgba(255,255,255,.15) 25%,transparent 25%,transparent 50%,rgba(255,255,255,.15) 50%,rgba(255,255,255,.15) 75%,transparent 75%,transparent);
      background-size: 1rem 1rem;
      animation: progress-stripes 1s linear infinite;
    }

    .progress-text {
      text-align: right;
      font-size: 0.85rem;
      color: #666;
      font-weight: bold;
    }

    @keyframes slideDown {
      from { opacity: 0; transform: translateY(-10px); }
      to { opacity: 1; transform: translateY(0); }
    }

    @keyframes progress-stripes {
      0% { background-position: 1rem 0; }
      100% { background-position: 0 0; }
    }
  `]
})
export class SolverPanelComponent implements OnDestroy {
  @Input() canStart = false;

  options = {
    useGauss: true,
    useConjugateGradient: true,
    maxIterations: 10000,
    tolerance: 0.000001
  };

  toleranceStr = '1e-6';
  isRunning = false;
  status: SolverStatus | null = null;
  
  private solveSubscription?: Subscription;

  constructor(private solverService: SolverService) {}

  ngOnDestroy(): void {
    // Отписываемся при уничтожении компонента, чтобы не было утечек памяти
    this.solveSubscription?.unsubscribe();
  }

  hasSelectedMethod(): boolean {
    return this.options.useGauss || this.options.useConjugateGradient;
  }

  startSolving(): void {
    if (!this.canStart || this.isRunning || !this.hasSelectedMethod()) return;

    // Сброс предыдущих результатов
    this.solverService.clearResults();
    this.status = null;
    this.isRunning = true;
    
    // Парсим строку точности в число
    const tol = parseFloat(this.toleranceStr);
    this.options.tolerance = isNaN(tol) ? 1e-6 : tol;

    console.log('[Panel] Запуск решения с параметрами:', this.options);

    // 1. Отправляем команду Start
    this.solveSubscription = this.solverService.startSolving(this.options).subscribe({
      next: () => {
        // 2. Начинаем опрашивать статус (Long Polling или интервал)
        this.monitorStatus();
      },
      error: (error) => {
        console.error('Ошибка запуска:', error);
        this.isRunning = false;
        alert('Не удалось запустить решение: ' + (error.error?.message || error.message));
      }
    });
  }

  private monitorStatus(): void {
    // watchStatus сам завершится (complete), когда isRunning станет false на сервере
    this.solveSubscription = this.solverService.watchStatus().subscribe({
      next: (currentStatus) => {
        this.status = currentStatus;
        this.isRunning = currentStatus.isRunning;
      },
      error: (err) => {
        console.error('Ошибка мониторинга:', err);
        this.isRunning = false;
      },
      complete: () => {
        console.log('[Panel] Решение завершено, запрашиваем результаты...');
        this.isRunning = false;
        // 3. Забираем финальные результаты
        this.solverService.getResult().subscribe(); 
      }
    });
  }
}