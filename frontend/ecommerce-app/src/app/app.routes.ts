import { Routes } from '@angular/router';
import { authGuard, adminGuard, depositoGuard } from './core/guards/auth.guard';

export const routes: Routes = [
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
    path: 'productos',
    loadComponent: () => import('./features/productos/components/producto-list/producto-list.component').then(m => m.ProductoListComponent)
  },
  {
    path: 'productos/:id',
    loadComponent: () => import('./features/productos/components/producto-detail/producto-detail.component').then(m => m.ProductoDetailComponent)
  },
  {
    path: 'carrito',
    loadComponent: () => import('./features/carrito/components/carrito/carrito.component').then(m => m.CarritoComponent),
    canActivate: [authGuard]
  },
  {
    path: 'pedidos',
    loadComponent: () => import('./features/pedidos/components/pedido-list/pedido-list.component').then(m => m.PedidoListComponent),
    canActivate: [authGuard]
  },
  {
    path: 'pedidos/:id',
    loadComponent: () => import('./features/pedidos/components/pedido-detail/pedido-detail.component').then(m => m.PedidoDetailComponent),
    canActivate: [authGuard]
  },
  {
    path: 'checkout',
    loadComponent: () => import('./features/pedidos/components/checkout/checkout.component').then(m => m.CheckoutComponent),
    canActivate: [authGuard]
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
    canActivate: [adminGuard]
  },
  {
    path: 'admin/pedidos',
    loadComponent: () => import('./features/pedidos/components/pedidos-admin/pedidos-admin.component').then(m => m.PedidosAdminComponent),
    canActivate: [adminGuard]
  },
  {
    path: 'admin/usuarios',
    loadComponent: () => import('./features/admin/components/usuarios-admin/usuarios-admin.component').then(m => m.UsuariosAdminComponent),
    canActivate: [adminGuard]
  },
  {
    path: 'deposito',
    loadComponent: () => import('./features/deposito/components/deposito-panel/deposito-panel.component').then(m => m.DepositoPanelComponent),
    canActivate: [depositoGuard]
  },
  {
    path: '**',
    redirectTo: '/productos'
  }
];
