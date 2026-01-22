import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface UploadImageResponse {
  url: string;
  message: string;
}

@Injectable({
  providedIn: 'root'
})
export class UploadService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/upload`;

  uploadImage(file: File, folder: string = 'tiendas'): Observable<UploadImageResponse> {
    const formData = new FormData();
    formData.append('file', file);
    formData.append('folder', folder);

    return this.http.post<UploadImageResponse>(`${this.apiUrl}/image`, formData);
  }

  deleteImage(imageUrl: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/image?imageUrl=${encodeURIComponent(imageUrl)}`);
  }
}
