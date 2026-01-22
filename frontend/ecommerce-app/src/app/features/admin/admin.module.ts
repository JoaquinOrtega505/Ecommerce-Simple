import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule } from '@angular/forms';
import { AdminRoutingModule } from './admin-routing.module';
import { AdminDashboardComponent } from './components/admin-dashboard/admin-dashboard.component';
import { TiendasListComponent } from './components/tiendas-list/tiendas-list.component';
import { TiendaFormComponent } from './components/tienda-form/tienda-form.component';

@NgModule({
  declarations: [
    AdminDashboardComponent,
    TiendasListComponent,
    TiendaFormComponent
  ],
  imports: [
    CommonModule,
    ReactiveFormsModule,
    AdminRoutingModule
  ]
})
export class AdminModule { }
