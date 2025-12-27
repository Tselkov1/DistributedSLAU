import { Component, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { SolverService, WorkerNode } from '../../services/solver.service';

@Component({
  selector: 'app-nodes-list',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="nodes-container">
      <div class="add-node-form">
        <h4>Добавить узел-воркер</h4>
        <div class="form-row">
          <div class="form-group">
            <label>IP-адрес:</label>
            <input type="text" 
                   [(ngModel)]="newNode.ipAddress" 
                   placeholder="127.0.0.1"
                   class="form-input">
          </div>
          <div class="form-group">
            <label>Порт:</label>
            <input type="number" 
                   [(ngModel)]="newNode.port" 
                   placeholder="5000"
                   class="form-input">
          </div>
          <button class="btn btn-add" (click)="addNode()">
            ➕ Добавить
          </button>
        </div>

        <div class="quick-actions">
          <button class="btn btn-secondary" (click)="addLocalWorkers(2)">
            Добавить 2 локальных воркера
          </button>
          <button class="btn btn-secondary" (click)="addLocalWorkers(4)">
            Добавить 4 локальных воркера
          </button>
        </div>
      </div>

      <div class="nodes-list">
        <h4>Список узлов ({{ workers.length }})</h4>
        
        <div *ngIf="workers.length === 0" class="empty-state">
          <p>Узлы не добавлены</p>
          <p class="hint">Добавьте хотя бы один узел для распределённых вычислений</p>
        </div>

        <div *ngIf="workers.length > 0" class="workers-grid">
          <div *ngFor="let worker of workers; let i = index" class="worker-card">
            <div class="worker-info">
              <span class="worker-id">Узел {{ i + 1 }}</span>
              <span class="worker-address">{{ worker.ipAddress }}:{{ worker.port }}</span>
            </div>
            <button class="btn-remove" (click)="removeNode(i)">❌</button>
          </div>
        </div>

        <button *ngIf="workers.length > 0" 
                class="btn btn-primary btn-full" 
                (click)="saveWorkers()"
                [disabled]="isSaving">
          {{ isSaving ? 'Сохранение...' : 'Сохранить конфигурацию' }}
        </button>

        <div *ngIf="statusMessage" 
             class="status-message" 
             [class.success]="statusSuccess" 
             [class.error]="!statusSuccess">
          {{ statusMessage }}
        </div>
      </div>
    </div>
  `,
  styles: [`
    .nodes-container {
      display: grid;
      gap: 30px;
    }

    .add-node-form {
      padding: 20px;
      background: #f8f9fa;
      border-radius: 8px;
      border: 2px solid #e0e0e0;
    }

    .add-node-form h4 {
      margin: 0 0 15px 0;
      color: #667eea;
    }

    .form-row {
      display: grid;
      grid-template-columns: 2fr 1fr auto;
      gap: 15px;
      align-items: end;
    }

    .form-group {
      display: flex;
      flex-direction: column;
      gap: 5px;
    }

    .form-group label {
      font-weight: 600;
      color: #555;
      font-size: 0.9rem;
    }

    .form-input {
      padding: 10px;
      border: 1px solid #ddd;
      border-radius: 6px;
      font-size: 1rem;
      transition: border-color 0.3s;
    }

    .form-input:focus {
      outline: none;
      border-color: #667eea;
    }

    .quick-actions {
      margin-top: 15px;
      display: flex;
      gap: 10px;
    }

    .nodes-list {
      padding: 20px;
      background: white;
      border-radius: 8px;
      border: 2px solid #e0e0e0;
    }

    .nodes-list h4 {
      margin: 0 0 15px 0;
      color: #333;
    }

    .empty-state {
      text-align: center;
      padding: 40px 20px;
      color: #999;
    }

    .empty-state p {
      margin: 10px 0;
    }

    .hint {
      font-size: 0.9rem;
      font-style: italic;
    }

    .workers-grid {
      display: grid;
      gap: 10px;
      margin-bottom: 20px;
    }

    .worker-card {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding: 15px;
      background: #f8f9fa;
      border-radius: 6px;
      border-left: 4px solid #667eea;
    }

    .worker-info {
      display: flex;
      flex-direction: column;
      gap: 5px;
    }

    .worker-id {
      font-weight: 600;
      color: #667eea;
    }

    .worker-address {
      color: #666;
      font-family: 'Courier New', monospace;
    }

    .btn {
      padding: 10px 20px;
      border: none;
      border-radius: 6px;
      font-weight: 600;
      cursor: pointer;
      transition: all 0.3s;
    }

    .btn-add {
      background: #28a745;
      color: white;
      white-space: nowrap;
    }

    .btn-add:hover {
      background: #218838;
      transform: translateY(-2px);
    }

    .btn-remove {
      background: none;
      border: none;
      cursor: pointer;
      font-size: 1.2rem;
      opacity: 0.6;
      transition: opacity 0.3s;
    }

    .btn-remove:hover {
      opacity: 1;
    }

    .btn-primary {
      background: #667eea;
      color: white;
    }

    .btn-primary:hover:not(:disabled) {
      background: #5568d3;
    }

    .btn-primary:disabled {
      background: #ccc;
      cursor: not-allowed;
    }

    .btn-secondary {
      background: #6c757d;
      color: white;
      font-size: 0.9rem;
    }

    .btn-secondary:hover {
      background: #5a6268;
    }

    .btn-full {
      width: 100%;
      padding: 12px;
      font-size: 1rem;
    }

    .status-message {
      margin-top: 15px;
      padding: 12px;
      border-radius: 6px;
      font-weight: 500;
    }

    .status-message.success {
      background: #d4edda;
      color: #155724;
      border: 1px solid #c3e6cb;
    }

    .status-message.error {
      background: #f8d7da;
      color: #721c24;
      border: 1px solid #f5c6cb;
    }

    @media (max-width: 768px) {
      .form-row {
        grid-template-columns: 1fr;
      }

      .quick-actions {
        flex-direction: column;
      }
    }
  `]
})
export class NodesListComponent {
  @Output() configured = new EventEmitter<boolean>();

  workers: WorkerNode[] = [];
  newNode: WorkerNode = { ipAddress: '127.0.0.1', port: 6000 };
  isSaving = false;
  statusMessage = '';
  statusSuccess = false;

  constructor(private solverService: SolverService) {}

  addNode(): void {
    if (!this.newNode.ipAddress || !this.newNode.port) {
      this.statusMessage = 'Заполните все поля';
      this.statusSuccess = false;
      return;
    }

    this.workers.push({ ...this.newNode });
    this.newNode = { ipAddress: '127.0.0.1', port: this.newNode.port + 1 };
    this.statusMessage = '';
  }

  removeNode(index: number): void {
    this.workers.splice(index, 1);
  }

  addLocalWorkers(count: number): void {
    const startPort = 6000;
    this.workers = [];

    for (let i = 0; i < count; i++) {
      this.workers.push({
        ipAddress: '127.0.0.1',
        port: startPort + i
      });
    }

    this.statusMessage = `Добавлено ${count} локальных воркеров`;
    this.statusSuccess = true;
  }

  saveWorkers(): void {
    if (this.workers.length === 0) {
      this.statusMessage = 'Добавьте хотя бы один узел';
      this.statusSuccess = false;
      return;
    }

    this.isSaving = true;
    this.statusMessage = '';

    this.solverService.setWorkers(this.workers).subscribe({
      next: (response) => {
        this.isSaving = false;
        this.statusSuccess = response.success;
        this.statusMessage = response.message || 'Конфигурация сохранена!';
        this.configured.emit(true);
      },
      error: (error) => {
        this.isSaving = false;
        this.statusSuccess = false;
        this.statusMessage = error.error?.error || 'Ошибка сохранения конфигурации';
        this.configured.emit(false);
      }
    });
  }
}