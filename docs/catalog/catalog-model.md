# Catalog & Menu Model Architecture — NextDrop

## 1. Domain Model Hierarchy

```text
Catalog (Aggregate Root)
   ├── Categories (Entity)

MenuItem (Aggregate Root)
   ├── MenuItemVariant (Entity)
   └── ModifierGroup (Entity)
           └── ModifierOption (Entity)

BranchMenuItemAvailability (Entity)
```

## 2. Order Snapshot Compatibility Rules

Catalog items in Sprint 3 represent current active offers. When future Sprints implement Orders, orders MUST NOT store foreign key references to mutable catalog prices.

Every order placement MUST create an immutable order snapshot containing:
- `MenuItemName`
- `VariantName`
- `UnitPrice` (captured at moment of order submission)
- `ModifierName`
- `ModifierPrice`
- `Quantity`

Modifying `MenuItem.BasePrice` or `Variant.Price` in Catalog will never retroactively alter historical order records.
