import { Routes } from '@angular/router';
import { authGuard, adminGuard, depositoGuard, superAdminGuard } from './core/guards/auth.guard';
import { emailVerifiedGuard } from './core/guards/email-verified.guard';

export const routes: Routes = [
  {
    path: 'admin',
    loadChildren: () => import('./features/admin/admin.module').then(m => m.AdminModule),
    canActivate: [superAdminGuard]
  },
  {
    path: 'emprendedor',
    loadChildren: () => import('./features/emprendedor/emprendedor.module').then(m => m.EmprendedorModule),
    canActivate: [adminGuard]
  },
  {
    path: '',
    redirectTo: '/productos',
    pathMatch: 'full'
  },
  {
    path: 'login',
    loadComponent: () => import('./features/auth/components/login/login.component').then(m => m.LoginComponent)
  },
  {
    path: 'register',
    loadComponent: () => import('./features/auth/components/register/register.component').then(m => m.RegisterComponent)
  },
  {
    path: 'verify-email',
    loadComponent: () => import('./features/auth/components/email-verification/email-verification.component').then(m => m.EmailVerificationComponent)
  },
  {
    path: 'onboarding',
    loadComponent: () => import('./features/emprendedor/components/onboarding-wizard/onboarding-wizard.component').then(m => m.OnboardingWizardComponent),
    canActivate: [authGuard, emailVerifiedGuard, adminGuard]
  },
  {
    path: 'productos',
    loadComponent: () => import('./features/productos/components/producto-list/producto-list.component').then(m => m.ProductoListComponent),
    canActivate: [authGuard, emailVerifiedGuard]
  },
  {
    path: 'productos/:id',
    loadComponent: () => import('./features/productos/components/producto-detail/producto-detail.component').then(m => m.ProductoDetailComponent),
    canActivate: [authGuard, emailVerifiedGuard]
  },
  {
    path: 'carrito',
    loadComponent: () => import('./features/carrito/components/carrito/carrito.component').then(m => m.CarritoComponent),
    canActivate: [authGuard, emailVerifiedGuard]
  },
  {
    path: 'pedidos',
    loadComponent: () => import('./features/pedidos/components/pedido-list/pedido-list.component').then(m => m.PedidoListComponent),
    canActivate: [authGuard, emailVerifiedGuard]
  },
  {
    path: 'pedidos/:id',
    loadComponent: () => import('./features/pedidos/components/pedido-detail/pedido-detail.component').then(m => m.PedidoDetailComponent),
    canActivate: [authGuard, emailVerifiedGuard]
  },
  {
    path: 'checkout',
    loadComponent: () => import('./features/pedidos/components/checkout/checkout.component').then(m => m.CheckoutComponent),
    canActivate: [authGuard, emailVerifiedGuard]
  },
  {
    path: 'pago/return',
    loadComponent: () => import('./features/pagos/pago-return/pago-return.component').then(m => m.PagoReturnComponent)
  },
  {
    path: 'pago/success',
    loadComponent: () => import('./features/pagos/pago-success/pago-success.component').then(m => m.PagoSuccessComponent)
  },
  {
    path: 'pago/failure',
    loadComponent: () => import('./features/pagos/pago-failure/pago-failure.component').then(m => m.PagoFailureComponent)
  },
  {
    path: 'pago/pending',
    loadComponent: () => import('./features/pagos/pago-pending/pago-pending.component').then(m => m.PagoPendingComponent)
  },
  {
    path: 'admin/productos',
    loadComponent: () => import('./features/productos/components/producto-admin/producto-admin.component').then(m => m.ProductoAdminComponent),
    canActivate: [adminGuard, emailVerifiedGuard]
  },
  {
    path: 'admin/pedidos',
    loadComponent: () => import('./features/pedidos/components/pedidos-admin/pedidos-admin.component').then(m => m.PedidosAdminComponent),
    canActivate: [adminGuard, emailVerifiedGuard]
  },
  {
    path: 'admin/usuarios',
    loadComponent: () => import('./features/admin/components/usuarios-admin/usuarios-admin.component').then(m => m.UsuariosAdminComponent),
    canActivate: [adminGuard, emailVerifiedGuard]
  },
  {
    path: 'deposito',
    loadComponent: () => import('./features/deposito/components/deposito-panel/deposito-panel.component').then(m => m.DepositoPanelComponent),
    canActivate: [depositoGuard, emailVerifiedGuard]
  },
  {
    path: '**',
    redirectTo: '/productos'
  }
];
