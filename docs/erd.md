# Entity Relationship Diagram (ERD)

The data model for PropertyOps Hub enforces multi-unit tenancy (`PropertyId`), append-only movement ledgers, recipe BOM breakdowns, POS sales ingestion, and balanced double-entry accounting journals.

---

## Complete Entity Relationship Diagram

```mermaid
erDiagram
    GROUP ||--o{ PROPERTY : owns
    PROPERTY ||--o{ OUTLET : contains
    PROPERTY ||--o{ ITEM : catalogs
    PROPERTY ||--o{ SUPPLIER : registers
    PROPERTY ||--o{ PURCHASE_ORDER : places
    PROPERTY ||--o{ GOODS_RECEIPT : receives
    PROPERTY ||--o{ STOCK_MOVEMENT : tracks
    PROPERTY ||--o{ STOCK_COUNT : audits
    PROPERTY ||--o{ SALES_CHECK : records
    PROPERTY ||--o{ JOURNAL_BATCH : generates
    PROPERTY ||--o{ INTERFACE_SYSTEM : monitors

    ITEM ||--o{ UOM_CONVERSION : has
    UNIT_OF_MEASURE ||--o{ ITEM : "base UoM"
    UNIT_OF_MEASURE ||--o{ UOM_CONVERSION : "from/to UoM"

    ITEM ||--o{ RECIPE_LINE : ingredient
    RECIPE ||--o{ RECIPE_LINE : contains
    OUTLET ||--o{ RECIPE : offers

    SUPPLIER ||--o{ PURCHASE_ORDER : receives
    PURCHASE_ORDER ||--o{ PO_LINE : specifies
    ITEM ||--o{ PO_LINE : orders

    PURCHASE_ORDER ||--o{ GOODS_RECEIPT : fulfills
    GOODS_RECEIPT ||--o{ GRN_LINE : details
    ITEM ||--o{ GRN_LINE : receives
    GRN_LINE ||--o{ STOCK_MOVEMENT : generates

    STOCK_COUNT ||--o{ STOCK_COUNT_LINE : lists
    ITEM ||--o{ STOCK_COUNT_LINE : counts

    OUTLET ||--o{ SALES_CHECK : generates
    SALES_CHECK ||--o{ SALES_LINE : details
    SALES_CHECK ||--o{ TENDER_LINE : paid_with
    ITEM ||--o{ SALES_LINE : maps_to

    JOURNAL_BATCH ||--o{ JOURNAL_LINE : contains
    GL_ACCOUNT ||--o{ JOURNAL_LINE : debits_credits
    COST_CENTER ||--o{ JOURNAL_LINE : allocates

    INTERFACE_SYSTEM ||--o{ INTERFACE_HEALTH_LOG : logs
    INTERFACE_SYSTEM ||--o{ INCIDENT : triggers

    PROPERTY {
        uuid Id PK
        uuid GroupId FK
        string Code
        string Name
        string Timezone
        string Currency
    }

    OUTLET {
        uuid Id PK
        uuid PropertyId FK
        string Code
        string Name
        string RevenueCenterType
    }

    ITEM {
        uuid Id PK
        uuid PropertyId FK
        string Sku
        string Name
        string Category
        uuid BaseUomId FK
        decimal CurrentCost
        decimal ReorderLevel
    }

    UNIT_OF_MEASURE {
        uuid Id PK
        string Code
        string Name
        string Type
    }

    UOM_CONVERSION {
        uuid Id PK
        uuid ItemId FK
        uuid FromUomId FK
        uuid ToUomId FK
        decimal ConversionFactor
    }

    RECIPE {
        uuid Id PK
        uuid PropertyId FK
        uuid OutletId FK
        string MenuItemCode
        string Name
        decimal StandardPortionCost
    }

    RECIPE_LINE {
        uuid Id PK
        uuid RecipeId FK
        uuid ItemId FK
        decimal Quantity
        uuid UomId FK
        decimal WastePercentage
    }

    STOCK_MOVEMENT {
        uuid Id PK
        uuid PropertyId FK
        uuid ItemId FK
        datetime TimestampUtc
        string MovementType
        decimal Quantity
        decimal UnitCost
        decimal TotalValue
        string ReferenceNumber
    }

    SALES_CHECK {
        uuid Id PK
        uuid PropertyId FK
        uuid OutletId FK
        string CheckNumber
        datetime BusinessDate
        datetime OpenedAt
        datetime ClosedAt
        decimal Subtotal
        decimal TaxAmount
        decimal TotalAmount
        string Status
    }

    SALES_LINE {
        uuid Id PK
        uuid SalesCheckId FK
        uuid ItemId FK
        int LineNumber
        string ItemDescription
        decimal Quantity
        decimal UnitPrice
        decimal LineTotal
        boolean IsVoid
    }

    TENDER_LINE {
        uuid Id PK
        uuid SalesCheckId FK
        string TenderType
        decimal Amount
        string RoomNumber
        string GuestFolioId
        string PmsPostingStatus
    }

    JOURNAL_BATCH {
        uuid Id PK
        uuid PropertyId FK
        string IdempotencyKey UK
        date BusinessDate
        string BatchType
        decimal TotalDebit
        decimal TotalCredit
        string Status
        datetime ExportedAtUtc
    }

    JOURNAL_LINE {
        uuid Id PK
        uuid JournalBatchId FK
        uuid GlAccountId FK
        uuid CostCenterId FK
        decimal Debit
        decimal Credit
        string Description
    }

    INTERFACE_SYSTEM {
        uuid Id PK
        uuid PropertyId FK
        string SystemType
        string Name
        string EndpointUrl
        int ExpectedSyncIntervalMinutes
        string CurrentStatus
        datetime LastHeartbeatUtc
    }
```
