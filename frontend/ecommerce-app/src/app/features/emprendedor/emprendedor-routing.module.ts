import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { LayoutComponent } from './components/layout/layout.component';
import { DashboardComponent } from './components/dashboard/dashboard.component';
import { ProductosListComponent } from './components/productos-list/productos-list.component';
import { ProductoFormComponent } from './components/producto-form/producto-form.component';
import { PedidosListComponent } from './components/pedidos-list/pedidos-list.component';
import { MiTiendaComponent } from './components/mi-tienda/mi-tienda.component';
import { ConfiguracionComponent } from './components/configuracion/configuracion.component';

const routes: Routes = [
  {
    path: '',
    component: LayoutComponent,
    children: [
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
        path: 'mi-tienda',
        component: MiTiendaComponent
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
      },
      {
        path: 'configuracion',
        component: ConfiguracionComponent
      }
    ]
  }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class EmprendedorRoutingModule { }
