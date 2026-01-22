import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';

import { EmprendedorRoutingModule } from './emprendedor-routing.module';
import { DashboardComponent } from './components/dashboard/dashboard.component';
import { ProductosListComponent } from './components/productos-list/productos-list.component';
import { PedidosListComponent } from './components/pedidos-list/pedidos-list.component';
import { ProductoFormComponent } from './components/producto-form/producto-form.component';


@NgModule({
  declarations: [
    DashboardComponent,
    ProductosListComponent,
    PedidosListComponent,
    ProductoFormComponent
  ],
  imports: [
    CommonModule,
    ReactiveFormsModule,
    FormsModule,
    RouterModule,
    EmprendedorRoutingModule
  ]
})
export class EmprendedorModule { }
