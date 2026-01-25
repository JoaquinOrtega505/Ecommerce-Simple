import { Component } from '@angular/core';
import { RouterOutlet, Router, NavigationEnd } from '@angular/router';
import { NavbarComponent } from './shared/components/navbar/navbar.component';
import { CommonModule } from '@angular/common';
import { filter } from 'rxjs/operators';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, NavbarComponent, CommonModule],
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss'
})
export class AppComponent {
  title = 'ecommerce-app';
  showNavbar = true;

  constructor(private router: Router) {
    this.router.events.pipe(
      filter(event => event instanceof NavigationEnd)
    ).subscribe((event: any) => {
      const hiddenRoutes = ['/login', '/register', '/verify-email', '/onboarding', '/tienda'];
      const url = event.urlAfterRedirects;

      // Ocultar navbar en rutas específicas o si la URL comienza con /emprendedor o /tienda
      this.showNavbar = !hiddenRoutes.includes(url) && !url.startsWith('/emprendedor') && !url.startsWith('/tienda');
    });
  }
}
