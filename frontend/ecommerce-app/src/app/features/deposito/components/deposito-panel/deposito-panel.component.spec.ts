import { ComponentFixture, TestBed } from '@angular/core/testing';

import { DepositoPanelComponent } from './deposito-panel.component';

describe('DepositoPanelComponent', () => {
  let component: DepositoPanelComponent;
  let fixture: ComponentFixture<DepositoPanelComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [DepositoPanelComponent]
    })
    .compileComponents();
    
    fixture = TestBed.createComponent(DepositoPanelComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
