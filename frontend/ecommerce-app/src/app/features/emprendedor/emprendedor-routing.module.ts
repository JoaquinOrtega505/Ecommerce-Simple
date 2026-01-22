import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { DashboardComponent } from './components/dashboard/dashboard.component';
import { ProductosListComponent } from './components/productos-list/productos-list.component';
import { ProductoFormComponent } from './components/producto-form/producto-form.component';
import { PedidosListComponent } from './components/pedidos-list/pedidos-list.component';

const routes: Routes = [
  {
    path: '',
    redirectTo: 'dashboard',
    pathMatch: 'full'
  },
  {
    path: 'dashboard',
    component: DashboardComponent
  },
  {
    path: 'productos',
    component: ProductosListComponent
  },
  {
    path: 'productos/nuevo',
    component: ProductoFormComponent
  },
  {
    path: 'productos/editar/:id',
    component: ProductoFormComponent
  },
  {
    path: 'pedidos',
    component: PedidosListComponent
  }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class EmprendedorRoutingModule { }
