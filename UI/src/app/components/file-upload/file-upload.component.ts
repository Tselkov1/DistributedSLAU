import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { SolverService } from '../../services/solver.service';

@Component({
  selector: 'app-file-upload',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="upload-card">
      <h3>{{ title }}</h3>
      
      <div class="upload-area" 
           [class.drag-over]="isDragOver"
           (dragover)="onDragOver($event)"
           (dragleave)="onDragLeave($event)"
           (drop)="onDrop($event)"
           (click)="fileInput.click()">
        <div class="upload-icon">📄</div>
        <p>Перетащите файл или нажмите для выбора</p>
        <input #fileInput 
               type="file" 
               (change)="onFileSelected($event)" 
               accept=".txt" 
               hidden>
      </div>

      <div *ngIf="selectedFile" class="file-info">
        <p><strong>Выбран файл:</strong> {{ selectedFile.name }}</p>
        <p><strong>Размер:</strong> {{ formatFileSize(selectedFile.size) }}</p>
      </div>

      <button class="btn btn-primary" 
              (click)="upload()" 
              [disabled]="!selectedFile || isUploading">
        {{ isUploading ? 'Загрузка...' : 'Загрузить' }}
      </button>

      <div *ngIf="uploadStatus" class="status-message" 
           [class.success]="!uploadFailed" 
           [class.error]="uploadFailed">
        {{ uploadStatus }}
      </div>

      <!-- Секция генерации тестовых данных -->
      <div *ngIf="!selectedFile" class="test-generation-section">
        <div class="divider">
          <span>или</span>
        </div>

        <h4>Генерация тестовой системы на сервере</h4>
        
        <div class="form-group">
          <label for="matrixSize">Размер матрицы (N×N):</label>
          <input 
            type="number" 
            id="matrixSize"
            [(ngModel)]="matrixSize" 
            min="2" 
            max="10000"
            class="form-control"
            placeholder="Например: 100">
          <small>Рекомендуемый диапазон: 10-5000. Генерация выполняется на сервере.</small>
        </div>

        <div class="form-group">
          <label for="sparsity">Заполненность матрицы (%):</label>
          <div class="slider-container">
            <input 
              type="range" 
              id="sparsity"
              [(ngModel)]="sparsity" 
              min="1" 
              max="100"
              class="slider">
            <span class="slider-value">{{ sparsity }}%</span>
          </div>
          <small>{{ getSparsityDescription() }}</small>
        </div>

        <div class="form-group">
          <label>
            <input 
              type="checkbox" 
              [(ngModel)]="diagonalDominance"
              class="checkbox">
            Диагональное преобладание (улучшает сходимость)
          </label>
        </div>

        <div class="generation-info">
          <p><strong>Будет сгенерировано на сервере:</strong></p>
          <ul>
            <li>Матрица размером {{ matrixSize }}×{{ matrixSize }}</li>
            <li>≈{{ calculateNonZeroElements() }} ненулевых элементов</li>
            <li>Вектор правой части размером {{ matrixSize }}</li>
            <li>Примерное время: {{ estimateGenerationTime() }}</li>
          </ul>
        </div>

        <div *ngIf="getPerformanceWarning()" class="warning-box">
          ⚠️ {{ getPerformanceWarning() }}
        </div>

        <button class="btn btn-secondary" 
                (click)="generateTest()"
                [disabled]="isUploading || !isValidSize()">
          <span class="btn-icon">{{ isUploading ? '⏳' : '✨' }}</span>
          {{ getButtonText() }}
        </button>

        <div *ngIf="isUploading && matrixSize > 1000" class="progress-info">
          <div class="spinner"></div>
          <p>Генерация большой матрицы на сервере. Пожалуйста, подождите...</p>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .upload-card {
      padding: 20px;
      border: 2px solid #e0e0e0;
      border-radius: 8px;
      background: #fafafa;
    }

    .upload-card h3 {
      margin: 0 0 15px 0;
      color: #667eea;
      font-size: 1.3rem;
    }

    .upload-card h4 {
      margin: 15px 0 10px 0;
      color: #555;
      font-size: 1.1rem;
    }

    .upload-area {
      border: 2px dashed #ccc;
      border-radius: 8px;
      padding: 40px 20px;
      text-align: center;
      cursor: pointer;
      transition: all 0.3s;
      background: white;
    }

    .upload-area:hover, .upload-area.drag-over {
      border-color: #667eea;
      background: #f0f4ff;
    }

    .upload-icon {
      font-size: 3rem;
      margin-bottom: 10px;
    }

    .file-info {
      margin: 15px 0;
      padding: 15px;
      background: white;
      border-radius: 6px;
      border-left: 4px solid #667eea;
    }

    .file-info p {
      margin: 5px 0;
      color: #555;
    }

    .divider {
      margin: 20px 0;
      text-align: center;
      position: relative;
    }

    .divider::before {
      content: '';
      position: absolute;
      top: 50%;
      left: 0;
      right: 0;
      height: 1px;
      background: #ddd;
    }

    .divider span {
      position: relative;
      background: #fafafa;
      padding: 0 15px;
      color: #999;
      font-weight: 500;
    }

    .test-generation-section {
      margin-top: 20px;
    }

    .form-group {
      margin-bottom: 20px;
    }

    .form-group label {
      display: block;
      margin-bottom: 8px;
      color: #555;
      font-weight: 500;
      font-size: 0.95rem;
    }

    .form-control {
      width: 100%;
      padding: 10px;
      border: 2px solid #e0e0e0;
      border-radius: 6px;
      font-size: 1rem;
      transition: border-color 0.3s;
      box-sizing: border-box;
    }

    .form-control:focus {
      outline: none;
      border-color: #667eea;
    }

    .slider-container {
      display: flex;
      align-items: center;
      gap: 15px;
    }

    .slider {
      flex: 1;
      height: 6px;
      border-radius: 3px;
      background: linear-gradient(to right, #ff6b6b, #feca57, #48dbfb, #1dd1a1);
      outline: none;
      -webkit-appearance: none;
    }

    .slider::-webkit-slider-thumb {
      -webkit-appearance: none;
      appearance: none;
      width: 20px;
      height: 20px;
      border-radius: 50%;
      background: white;
      cursor: pointer;
      transition: all 0.3s;
      box-shadow: 0 2px 4px rgba(0,0,0,0.2);
      border: 2px solid #667eea;
    }

    .slider::-webkit-slider-thumb:hover {
      transform: scale(1.2);
      box-shadow: 0 0 8px rgba(102, 126, 234, 0.6);
    }

    .slider::-moz-range-thumb {
      width: 20px;
      height: 20px;
      border-radius: 50%;
      background: white;
      cursor: pointer;
      border: 2px solid #667eea;
      transition: all 0.3s;
      box-shadow: 0 2px 4px rgba(0,0,0,0.2);
    }

    .slider-value {
      min-width: 50px;
      font-weight: 600;
      color: #667eea;
      font-size: 1.1rem;
      text-align: center;
    }

    .checkbox {
      margin-right: 8px;
      width: 18px;
      height: 18px;
      cursor: pointer;
      accent-color: #667eea;
    }

    small {
      display: block;
      margin-top: 5px;
      color: #888;
      font-size: 0.85rem;
    }

    .generation-info {
      background: white;
      padding: 15px;
      border-radius: 6px;
      border-left: 4px solid #6c757d;
      margin-bottom: 15px;
    }

    .generation-info p {
      margin: 0 0 8px 0;
      color: #555;
      font-weight: 500;
    }

    .generation-info ul {
      margin: 0;
      padding-left: 20px;
      color: #666;
    }

    .generation-info li {
      margin: 5px 0;
    }

    .warning-box {
      background: #fff3cd;
      border: 2px solid #ffc107;
      border-radius: 6px;
      padding: 12px;
      margin-bottom: 15px;
      color: #856404;
      font-weight: 500;
      display: flex;
      align-items: center;
      gap: 8px;
    }

    .btn {
      width: 100%;
      padding: 12px;
      border: none;
      border-radius: 6px;
      font-size: 1rem;
      font-weight: 600;
      cursor: pointer;
      transition: all 0.3s;
      margin-top: 10px;
      display: flex;
      align-items: center;
      justify-content: center;
      gap: 8px;
    }

    .btn-icon {
      font-size: 1.2rem;
    }

    .btn-primary {
      background: #667eea;
      color: white;
    }

    .btn-primary:hover:not(:disabled) {
      background: #5568d3;
      transform: translateY(-2px);
      box-shadow: 0 4px 8px rgba(102, 126, 234, 0.4);
    }

    .btn-primary:disabled {
      background: #ccc;
      cursor: not-allowed;
    }

    .btn-secondary {
      background: #6c757d;
      color: white;
    }

    .btn-secondary:hover:not(:disabled) {
      background: #5a6268;
      transform: translateY(-2px);
      box-shadow: 0 4px 8px rgba(108, 117, 125, 0.4);
    }

    .btn-secondary:disabled {
      background: #ccc;
      cursor: not-allowed;
    }

    .status-message {
      margin-top: 15px;
      padding: 12px;
      border-radius: 6px;
      font-weight: 500;
      animation: slideIn 0.3s ease;
    }

    @keyframes slideIn {
      from {
        opacity: 0;
        transform: translateY(-10px);
      }
      to {
        opacity: 1;
        transform: translateY(0);
      }
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

    .progress-info {
      margin-top: 15px;
      padding: 15px;
      background: #e7f3ff;
      border-radius: 6px;
      border-left: 4px solid #2196F3;
      display: flex;
      align-items: center;
      gap: 15px;
      animation: slideIn 0.3s ease;
    }

    .spinner {
      width: 24px;
      height: 24px;
      border: 3px solid #e0e0e0;
      border-top: 3px solid #667eea;
      border-radius: 50%;
      animation: spin 1s linear infinite;
    }

    @keyframes spin {
      0% { transform: rotate(0deg); }
      100% { transform: rotate(360deg); }
    }

    .progress-info p {
      margin: 0;
      color: #1976D2;
      font-weight: 500;
    }
  `]
})
export class FileUploadComponent {
  @Input() title: string = 'Загрузка файла';
  @Input() fileType: 'matrix' | 'vector' = 'matrix';
  @Output() uploadSuccess = new EventEmitter<{ fileType: string; success: boolean }>();

  selectedFile: File | null = null;
  isDragOver = false;
  isUploading = false;
  uploadStatus = '';
  uploadFailed = false;

  // Параметры генерации
  matrixSize: number = 100;
  sparsity: number = 50; // процент (будет конвертирован в 0-1)
  diagonalDominance: boolean = true;

  constructor(private solverService: SolverService) {}

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      this.selectedFile = input.files[0];
      this.uploadStatus = '';
      this.uploadFailed = false;
    }
  }

  onDragOver(event: DragEvent): void {
    event.preventDefault();
    this.isDragOver = true;
  }

  onDragLeave(event: DragEvent): void {
    event.preventDefault();
    this.isDragOver = false;
  }

  onDrop(event: DragEvent): void {
    event.preventDefault();
    this.isDragOver = false;

    if (event.dataTransfer?.files && event.dataTransfer.files.length > 0) {
      this.selectedFile = event.dataTransfer.files[0];
      this.uploadStatus = '';
      this.uploadFailed = false;
    }
  }

  upload(): void {
    if (!this.selectedFile) return;

    this.isUploading = true;
    this.uploadStatus = '';
    this.uploadFailed = false;

    const uploadObservable = this.fileType === 'matrix'
      ? this.solverService.uploadMatrix(this.selectedFile)
      : this.solverService.uploadVector(this.selectedFile);

    uploadObservable.subscribe({
      next: (response) => {
        this.isUploading = false;
        this.uploadFailed = false;
        this.uploadStatus = response.message || 'Загрузка успешна!';
        this.uploadSuccess.emit({ fileType: this.fileType, success: true });
      },
      error: (error) => {
        this.isUploading = false;
        this.uploadFailed = true;
        this.uploadStatus = error.error?.error || error.error?.message || 'Ошибка загрузки файла';
        this.uploadSuccess.emit({ fileType: this.fileType, success: false });
      }
    });
  }

  generateTest(): void {
    if (!this.isValidSize()) {
      this.uploadStatus = 'Неверный размер матрицы';
      this.uploadFailed = true;
      return;
    }

    this.isUploading = true;
    this.uploadStatus = '';
    this.uploadFailed = false;

    console.log(`[Component] Начало генерации: size=${this.matrixSize}, sparsity=${this.sparsity}%, diagonal=${this.diagonalDominance}`);

    // ВАЖНО: Конвертируем процент в доли (0-1) для сервера
    const sparsityValue = this.sparsity / 100;

    // Отправляем только параметры на сервер
    this.solverService.generateTestSystem(
      this.matrixSize, 
      sparsityValue,
      this.diagonalDominance
    ).subscribe({
      next: (response) => {
        console.log('[Component] Генерация успешна:', response);
        this.isUploading = false;
        this.uploadFailed = false;
        this.uploadStatus = response.message || 
          `✅ Система ${this.matrixSize}×${this.matrixSize} успешно сгенерирована на сервере!`;
        this.uploadSuccess.emit({ fileType: 'system', success: true });
      },
      error: (error) => {
        console.error('[Component] Ошибка генерации:', error);
        this.isUploading = false;
        this.uploadFailed = true;
        this.uploadStatus = error.error?.error || error.error?.message || 'Ошибка генерации';
        this.uploadSuccess.emit({ fileType: 'system', success: false });
      }
    });
  }

  isValidSize(): boolean {
    return this.matrixSize >= 2 && this.matrixSize <= 10000;
  }

  calculateNonZeroElements(): string {
    const totalElements = this.matrixSize * this.matrixSize;
    const nonZero = Math.round(totalElements * (this.sparsity / 100));
    
    if (nonZero >= 1000000) {
      return (nonZero / 1000000).toFixed(2) + 'M';
    } else if (nonZero >= 1000) {
      return (nonZero / 1000).toFixed(1) + 'K';
    }
    return nonZero.toString();
  }

  estimateGenerationTime(): string {
    if (this.matrixSize < 100) {
      return '< 1 сек';
    } else if (this.matrixSize < 500) {
      return '1-3 сек';
    } else if (this.matrixSize < 1000) {
      return '3-10 сек';
    } else if (this.matrixSize < 5000) {
      return '10-60 сек';
    } else {
      return '1-5 мин';
    }
  }

  getPerformanceWarning(): string | null {
    const nonZeroElements = Math.round(this.matrixSize * this.matrixSize * (this.sparsity / 100));
    
    if (this.matrixSize > 5000 && this.sparsity > 50) {
      return 'Очень большая матрица с высокой заполненностью. Генерация и решение могут занять несколько минут.';
    } else if (this.matrixSize > 2000 && this.sparsity > 70) {
      return 'Большая плотная матрица может потребовать значительных ресурсов памяти.';
    } else if (nonZeroElements > 10000000) {
      return 'Более 10 миллионов ненулевых элементов. Рекомендуется уменьшить размер или заполненность.';
    }
    return null;
  }

  getButtonText(): string {
    if (this.isUploading) {
      if (this.matrixSize > 1000) {
        return 'Генерация (это может занять время)...';
      }
      return 'Генерация...';
    }
    return 'Сгенерировать на сервере';
  }

  getSparsityDescription(): string {
    if (this.sparsity <= 10) {
      return 'Очень разреженная матрица';
    } else if (this.sparsity <= 30) {
      return 'Разреженная матрица';
    } else if (this.sparsity <= 70) {
      return 'Умеренная заполненность';
    } else if (this.sparsity <= 90) {
      return 'Плотная матрица';
    } else {
      return 'Почти полная матрица';
    }
  }

  formatFileSize(bytes: number): string {
    if (bytes < 1024) return bytes + ' B';
    if (bytes < 1024 * 1024) return (bytes / 1024).toFixed(2) + ' KB';
    return (bytes / (1024 * 1024)).toFixed(2) + ' MB';
  }
}