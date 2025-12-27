import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClientModule } from '@angular/common/http';

// Импорт дочерних компонентов
import { FileUploadComponent } from './components/file-upload/file-upload.component';
import { SolverPanelComponent } from './components/solver-panel/solver-panel.component';
import { ComparisonResultComponent } from './components/comparison-result/comparison-result.component';
import { NodesListComponent } from './components/nodes-list/nodes-list.component'; // Новый компонент
// Импорт сервиса
import { SolverService } from './services/solver.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [
    CommonModule,
    HttpClientModule, 
    FileUploadComponent,
    NodesListComponent,
    SolverPanelComponent,
    ComparisonResultComponent
  ],
  providers: [SolverService],
  templateUrl: './app.html',
  styleUrls: ['./app.css']
})
export class AppComponent {
  title = 'Распределённое решение СЛАУ';
  
  // Флаги готовности данных
  matrixReady = false;
  vectorReady = false;
  workersConfigured = false;

  /**
   * Этот метод вызывается, когда компонент загрузки (app-file-upload)
   * сообщает об успешной загрузке файла или генерации системы.
   */
  onDataReady(event: { fileType: string; success: boolean }): void {
    if (!event.success) return;

    // Если на сервере была сгенерирована полная система (матрица + вектор)
    if (event.fileType === 'system') {
      this.matrixReady = true;
      this.vectorReady = true;
    } 
    
    // Если загружена только матрица
    else if (event.fileType === 'matrix') {
      this.matrixReady = true;
    } 
    // Если загружен только вектор
    else if (event.fileType === 'vector') {
      this.vectorReady = true;
    }
  }
onWorkersConfigured(isConfigured: boolean): void {
  this.workersConfigured = isConfigured;
  }
  // Геттер, который проверяет, можно ли запускать решение
  get canStartSolving(): boolean {
    return this.matrixReady && this.vectorReady && this.workersConfigured;
  }
}