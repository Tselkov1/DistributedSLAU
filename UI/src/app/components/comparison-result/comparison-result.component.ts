import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { SolverService, ComparisonResult, SolverResult } from '../../services/solver.service';
import { Observable } from 'rxjs';

@Component({
  selector: 'app-comparison-result',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="results-container" *ngIf="result$ | async as result">
      <h3>Результаты сравнения</h3>
      
      <div class="cards-wrapper">
        <!-- Результаты Гаусса -->
        <div class="result-card" *ngIf="result.gaussResult" [class.winner]="isWinner(result.gaussResult, result)">
          <div class="card-header">Метод Гаусса</div>
          <div class="card-body">
            <div class="metric">
              <span class="label">Время:</span>
              <span class="value">{{ result.gaussResult.elapsedMs | number:'1.0-2' }} мс</span>
            </div>
            <div class="metric">
              <span class="label">Невязка:</span>
              <span class="value">{{ result.gaussResult.residual | number:'1.1-15' }}</span>
            </div>
            <div class="metric">
              <span class="label">Статус:</span>
              <span class="status" [class.success]="result.gaussResult.success">{{ result.gaussResult.success ? 'Решено' : 'Ошибка' }}</span>
            </div>
          </div>
        </div>
         <div class="result-card" *ngIf="result.cgLocalResult" [class.winner]="isWinner(result.cgLocalResult, result)">
        <div class="card-header">CG (Локальный)</div>
        <div class="card-body">
        <div class="metric">
            <span class="label">Время:</span>
            <span class="value">{{ result.cgLocalResult.elapsedMs | number:'1.0-2' }} мс</span>
        </div>
        <div class="metric">
            <span class="label">Итераций:</span>
            <span class="value">{{ result.cgLocalResult.iterations }}</span>
        </div>
        <div class="metric">
            <span class="label">Невязка:</span>
            <span class="value">{{ result.cgLocalResult.residual | number:'1.1-15' }}</span>
        </div>
        </div>
    </div>
        <!-- Результаты Сопряженных Градиентов -->
        <div class="result-card" *ngIf="result.cgResult" [class.winner]="isWinner(result.cgResult, result)">
          <div class="card-header">Сопряжённые Градиенты</div>
          <div class="card-body">
            <div class="metric">
              <span class="label">Время:</span>
              <span class="value">{{ result.cgResult.elapsedMs | number:'1.0-2' }} мс</span>
            </div>
            <div class="metric">
              <span class="label">Итераций:</span>
              <span class="value">{{ result.cgResult.iterations }}</span>
            </div>
            <div class="metric">
              <span class="label">Невязка:</span>
              <span class="value">{{ result.cgResult.residual | number:'1.1-15' }}</span>
            </div>
          </div>
        </div>
      </div>

      <!-- Итоги -->
<!-- Итоги -->
<div class="summary-card" *ngIf="result.speedup">
  <div class="summary-title">
    <span>Итоги сравнения</span>
  </div>

  <div class="summary-table">
    <div class="row">
      <div class="cell label">Победитель:</div>
      <div class="cell value">
       {{ getWinnerName(result) }}
      </div>
    </div>

    <div class="row">
      <div class="cell label">Ускорение:</div>
      <div class="cell value">
        {{ result.speedup | number:'1.1-2' }}x
      </div>
    </div>

    <div class="row" *ngIf="result.residualDifference">
      <div class="cell label">Разница в точности:</div>
      <div class="cell value">
        {{ result.residualDifference | number:'1.1-15' }}
      </div>
    </div>
  </div>
  `,
styles: [`
  .results-container {
    background: #ffffff;
    padding: 24px;
    border-radius: 14px;
    border: 1px solid #e5eaf0;
    box-shadow: 0 6px 20px rgba(0, 0, 0, 0.04);
    animation: fadeIn 0.4s ease-out;
  }

  h3 {
    margin: 0 0 18px;
    color: #1e293b;
    font-size: 1.4rem;
    font-weight: 600;
  }

  .cards-wrapper {
    display: flex;
    flex-wrap: wrap;
    gap: 20px;
  }

  .result-card {
    flex: 1;
    min-width: 280px;
    border-radius: 14px;
    background: #f9fafb;
    border: 1px solid #e3e8ef;
    transition: transform 0.25s, box-shadow 0.25s;
    overflow: hidden;
  }

  .result-card:hover {
    transform: translateY(-4px);
    box-shadow: 0 10px 24px rgba(0,0,0,0.06);
  }

  /* --- Подсветка победителя --- */
  .winner {
    background: linear-gradient(135deg, #e3fcec 0%, #ffffff 50%);
    border: 2px solid #3cb371;
    box-shadow: 0 8px 30px rgba(60, 179, 113, 0.25);
  }

  .card-header {
    background: #eef2f6;
    padding: 12px 18px;
    font-weight: 600;
    font-size: 1.05rem;
    border-bottom: 1px solid #e1e6ed;
    color: #334155;
  }

  .winner .card-header {
    background: #dff7e8;
    color: #0f5132;
  }

  .card-body {
    padding: 16px 18px;
  }

  .metric {
    display: flex;
    justify-content: space-between;
    padding: 6px 0;
    margin-bottom: 10px;
    border-bottom: 1px dashed #d7dce3;
  }

  .label {
    color: #6b7280;
    font-size: 0.9rem;
  }

  .value {
    font-weight: 600;
    font-family: 'JetBrains Mono', monospace;
    color: #1f2937;
  }

  .status.success {
    color: #2e7d32;
    font-weight: 600;
  }

    /* --- Итоговый блок --- */
  .summary-card {
    background: #f0f7ff;
    border: 1px solid #cfe0ff;
    border-radius: 14px;
    padding: 20px;
    margin-top: 20px;
    box-shadow: 0 6px 20px rgba(0, 60, 140, 0.08);
  }

  .summary-title {
    font-size: 1.2rem;
    font-weight: 600;
    margin-bottom: 15px;
    color: #1e3a8a;
    padding-left: 6px;
    border-left: 4px solid #3b82f6;
  }

  .summary-table {
    display: grid;
    gap: 10px;
    margin-bottom: 15px;
  }

  .summary-table .row {
    display: grid;
    grid-template-columns: 1fr 1fr;
    padding: 8px 10px;
    background: #ffffff;
    border: 1px solid #e3e8ef;
    border-radius: 8px;
  }

  .summary-table .label {
    color: #6b7280;
    font-size: 0.9rem;
  }

  .summary-table .value {
    font-weight: 600;
    font-size: 1rem;
    text-align: right;
    color: #1f2937;
    font-family: 'JetBrains Mono', monospace;
  }

  .summary-note {
    margin-top: 15px;
    font-size: 0.95rem;
    color: #374151;
    background: #e8f1ff;
    padding: 10px 14px;
    border-radius: 8px;
    border-left: 4px solid #3b82f6;
    line-height: 1.35;
  }

  .speedup-badge {
    display: inline-block;
    background: #3b82f6;
    color: white;
    padding: 6px 12px;
    border-radius: 16px;
    font-size: 0.9rem;
    margin: 8px 0;
    box-shadow: 0 2px 8px rgba(59, 130, 246, 0.3);
  }

  .residual-diff {
    color: #334155;
    font-size: 0.95rem;
  }

  @keyframes fadeIn {
    from { opacity: 0; transform: translateY(8px); }
    to { opacity: 1; transform: translateY(0); }
  }
`]

})
export class ComparisonResultComponent {
  result$: Observable<ComparisonResult | null>;

  constructor(private solverService: SolverService) {
    this.result$ = this.solverService.currentResult$;
  }

   isWinner(current: SolverResult | undefined, all: ComparisonResult): boolean {
    if (!current) return false;
    
    const times = [
      all.gaussResult?.elapsedMs ?? Infinity,
      all.cgResult?.elapsedMs ?? Infinity,
      all.cgLocalResult?.elapsedMs ?? Infinity
    ];

    const minTime = Math.min(...times);
    // Считаем победителем, если время совпадает с минимальным (и оно не Infinity)
    return current.elapsedMs === minTime && minTime !== Infinity;
  }
   getWinnerName(res: ComparisonResult): string {
    // Собираем всех участников, которые успешно завершили работу
    const candidates = [
      res.gaussResult, 
      res.cgResult, 
      res.cgLocalResult
    ].filter(c => c && c.success);

    if (candidates.length === 0) return 'Нет данных';

    // Сортируем по времени (от меньшего к большему)
    candidates.sort((a, b) => a!.elapsedMs - b!.elapsedMs);

    // Возвращаем название метода самого быстрого участника
    // Можно вернуть candidates[0].method (как пришло с сервера) 
    // или сокращенное название:
    const best = candidates[0]!;
    
    if (best.method.includes('Гаусс')) return 'Метод Гаусса';
    if (best.method.includes('распределённый')) return 'CG (Кластер)';
    if (best.method.includes('локальный')) return 'CG (Локальный)';
    
    return best.method;
  }
}