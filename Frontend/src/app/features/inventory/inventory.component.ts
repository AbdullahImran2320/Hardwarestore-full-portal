import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ProductService } from '../../core/services/product.service';
import { CategoryService } from '../../core/services/category.service'; // NEW
import { Product, CreateProduct, UpdateProduct, AdjustStock } from '../../core/models/product.model';
import { Category } from '../../core/models/category.model'; // NEW
import { AuthService } from '../../core/services/auth.service'; // adjust path
import { ProductCost } from '../../core/models/product.model'; // adjust relative path if needed


@Component({
  selector: 'app-inventory',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './inventory.component.html',
  styleUrl: './inventory.component.scss'
})
export class InventoryComponent implements OnInit {
 authService = inject(AuthService);

  products = signal<Product[]>([]);
  isLoading = signal(true);
  errorMsg = signal<string | null>(null);
  searchTerm = signal('');
  categoryFilter = signal('All');

  allCategories = signal<Category[]>([]); // NEW — list from the Categories API, for the dropdown

  isModalOpen = signal(false);
  isEditMode = signal(false);
  isSaving = signal(false);
  formError = signal<string | null>(null);
  editingId: number | null = null;

  form: CreateProduct = this.emptyForm();

  categories = computed(() => {
    const cats = new Set(this.products().map(p => p.category));
    return ['All', ...Array.from(cats)];
  });

  filteredProducts = computed(() => {
    const term = this.searchTerm().toLowerCase().trim();
    const cat = this.categoryFilter();
    return this.products().filter(p => {
      const matchesTerm = !term || p.name.toLowerCase().includes(term) || p.category.toLowerCase().includes(term);
      const matchesCat = cat === 'All' || p.category === cat;
      return matchesTerm && matchesCat;
    });
  });

  constructor(private productService: ProductService, private categoryService: CategoryService) {} // CHANGED

  ngOnInit() {
    this.loadProducts();
    this.loadCategories(); // NEW
  }

  loadProducts() {
    this.isLoading.set(true);
    this.errorMsg.set(null);
    this.productService.getAll().subscribe({
      next: (data) => { this.products.set(data); this.isLoading.set(false); },
      error: () => { this.errorMsg.set('Could not load products. Is the API running?'); this.isLoading.set(false); }
    });
  }

  // NEW
  loadCategories() {
    this.categoryService.getAll().subscribe({
      next: (data) => this.allCategories.set(data),
      error: () => {} // non-critical — dropdown will just be empty if this fails
    });
  }
  // Add Category modal
isCategoryModalOpen = signal(false);
newCategoryName = signal('');
categoryFormError = signal<string | null>(null);
isCategorySaving = signal(false);

openAddCategoryModal() {
  this.newCategoryName.set('');
  this.categoryFormError.set(null);
  this.isCategoryModalOpen.set(true);
}

closeCategoryModal() {
  this.isCategoryModalOpen.set(false);
}

saveCategory() {
  const name = this.newCategoryName().trim();
  if (!name) {
    this.categoryFormError.set('Category name is required.');
    return;
  }

  this.isCategorySaving.set(true);
  this.categoryFormError.set(null);
  this.categoryService.create({ name }).subscribe({
    next: () => {
      this.isCategorySaving.set(false);
      this.closeCategoryModal();
      this.loadCategories(); // refresh so the Add Product dropdown updates immediately
    },
    error: (err) => {
      this.isCategorySaving.set(false);
      this.categoryFormError.set(err?.error?.message || 'Failed to create category.');
    }
  });
}
deleteCategory(c: Category) {
  if (!confirm(`Delete category "${c.name}"?`)) return;

  this.categoryFormError.set(null);
  this.categoryService.delete(c.id).subscribe({
    next: () => this.loadCategories(),
    error: (err) => {
      this.categoryFormError.set(err?.error?.message || 'Failed to delete category.');
    }
  });
}

  stockStatus(p: Product): 'ok' | 'low' | 'out' {
    if (p.stockQty <= 0) return 'out';
    if (p.stockQty <= p.reorderLevel) return 'low';
    return 'ok';
  }

  openAddModal() {
    this.isEditMode.set(false);
    this.editingId = null;
    this.form = this.emptyForm();
    this.formError.set(null);
    this.isModalOpen.set(true);
  }

  openEditModal(p: Product) {
    this.isEditMode.set(true);
    this.editingId = p.id;
    this.form = {
      name: p.name,
      categoryId: p.categoryId, // CHANGED
      unit: p.unit,
      stockQty: p.stockQty, // shown read-only in edit mode — see template
      purchasePrice: 0,     // backend doesn't return this in ProductDTO; left blank intentionally
      salePrice: p.salePrice,
      reorderLevel: p.reorderLevel
    };
    this.formError.set(null);
    this.isModalOpen.set(true);
  }

  closeModal() {
    this.isModalOpen.set(false);
  }

  save() {
    if (!this.form.name.trim() || !this.form.categoryId || !this.form.unit.trim()) { // CHANGED
      this.formError.set('Name, category, and unit are required.');
      return;
    }
    if (this.form.salePrice <= 0) {
      this.formError.set('Sale price must be greater than zero.');
      return;
    }

    this.isSaving.set(true);
    this.formError.set(null);

    if (this.isEditMode() && this.editingId !== null) {
      const updateDto: UpdateProduct = {
        name: this.form.name,
        categoryId: this.form.categoryId, // CHANGED
        unit: this.form.unit,
        purchasePrice: this.form.purchasePrice,
        salePrice: this.form.salePrice,
        reorderLevel: this.form.reorderLevel
      };
      this.productService.update(this.editingId, updateDto).subscribe({
        next: () => { this.isSaving.set(false); this.closeModal(); this.loadProducts(); },
        error: () => { this.isSaving.set(false); this.formError.set('Failed to update product.'); }
      });
    } else {
      this.productService.create(this.form).subscribe({
        next: () => { this.isSaving.set(false); this.closeModal(); this.loadProducts(); },
        error: () => { this.isSaving.set(false); this.formError.set('Failed to create product.'); }
      });
    }
  }

  deleteProduct(p: Product) {
    if (!confirm(`Delete "${p.name}"? This cannot be undone.`)) return;
    this.productService.delete(p.id).subscribe({
      next: () => this.loadProducts(),
      error: () => alert('Failed to delete product. It may be referenced in existing bills.')
    });
  }

  private emptyForm(): CreateProduct {
    return { name: '', categoryId: 0, unit: '', stockQty: 0, purchasePrice: 0, salePrice: 0, reorderLevel: 5 }; // CHANGED
  }
  // Add to the class:
isCostModalOpen = signal(false);
costForm = signal<ProductCost | null>(null);
isCostSaving = signal(false);
costError = signal<string | null>(null);

openCostModal(p: Product) {
  this.costError.set(null);
  this.productService.getCost(p.id).subscribe({
    next: (data) => { this.costForm.set(data); this.isCostModalOpen.set(true); },
    error: () => alert('Failed to load cost data.')
  });
}

closeCostModal() {
  this.isCostModalOpen.set(false);
}

saveCost() {
  const current = this.costForm();
  if (!current) return;

  this.isCostSaving.set(true);
  this.productService.updateCost(current.productId, { purchasePrice: current.purchasePrice }).subscribe({
    next: () => { this.isCostSaving.set(false); this.closeCostModal(); },
    error: () => { this.isCostSaving.set(false); this.costError.set('Failed to update cost.'); }
  });
}

// Manual stock adjustment (restock, correction, damage, etc.)
isStockModalOpen = signal(false);
stockAdjustProduct = signal<Product | null>(null);
stockAdjustType = signal<'Restock' | 'Adjustment' | 'Damage'>('Restock');
stockAdjustQty = signal<number>(0);
stockAdjustReference = signal('');
isStockSaving = signal(false);
stockError = signal<string | null>(null);

openStockModal(p: Product) {
  this.stockAdjustProduct.set(p);
  this.stockAdjustType.set('Restock');
  this.stockAdjustQty.set(0);
  this.stockAdjustReference.set('');
  this.stockError.set(null);
  this.isStockModalOpen.set(true);
}

closeStockModal() {
  this.isStockModalOpen.set(false);
}

saveStockAdjustment() {
  const product = this.stockAdjustProduct();
  if (!product) return;

  const rawQty = this.stockAdjustQty();
  if (!rawQty) {
    this.stockError.set('Enter a non-zero quantity.');
    return;
  }

  // Restock always adds; Damage always removes. Adjustment uses the signed value as typed.
  const type = this.stockAdjustType();
  const quantityChange = type === 'Restock' ? Math.abs(rawQty)
    : type === 'Damage' ? -Math.abs(rawQty)
    : rawQty;

  if (product.stockQty + quantityChange < 0) {
    this.stockError.set(`Cannot reduce stock below zero. Current stock: ${product.stockQty}.`);
    return;
  }

  const dto: AdjustStock = {
    quantityChange,
    type,
    reference: this.stockAdjustReference() || undefined
  };

  this.isStockSaving.set(true);
  this.stockError.set(null);
  this.productService.adjustStock(product.id, dto).subscribe({
    next: () => { this.isStockSaving.set(false); this.closeStockModal(); this.loadProducts(); },
    error: (err) => {
      this.isStockSaving.set(false);
      this.stockError.set(err?.error?.message || 'Failed to adjust stock.');
    }
  });
}
}