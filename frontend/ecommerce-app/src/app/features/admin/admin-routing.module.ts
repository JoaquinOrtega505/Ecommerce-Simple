import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AdminDashboardComponent } from './components/admin-dashboard/admin-dashboard.component';
import { TiendasListComponent } from './components/tiendas-list/tiendas-list.component';
import { TiendaFormComponent } from './components/tienda-form/tienda-form.component';
import { UsuariosAdminComponent } from './components/usuarios-admin/usuarios-admin.component';

const routes: Routes = [
  {
    path: '',
    component: AdminDashboardComponent,
    children: [
      { path: '', redirectTo: 'tiendas', pathMatch: 'full' },
      { path: 'tiendas', component: TiendasListComponent },
      { path: 'tiendas/nueva', component: TiendaFormComponent },
      { path: 'tiendas/editar/:id', component: TiendaFormComponent },
      { path: 'usuarios', component: UsuariosAdminComponent }
    ]
  }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class AdminRoutingModule { }
