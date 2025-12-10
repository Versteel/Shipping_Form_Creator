# Database Guide

The Shipping Form Creator utilizes two distinct data sources to function: a remote ERP database accessed via ODBC and a local SQLite database for caching and storing user-generated data.

## 1. Remote ERP Database (ODBC)

This is the primary source of truth for all sales order information. The application treats this database as **read-only**.

*   **Connection:** The connection is handled by the `OdbcService`. The connection string and driver details are managed within this service.
*   **Purpose:** To fetch real-time data for:
    *   Sales order headers (customer info, addresses, etc.).
    *   Line item details for each order.
    *   Packing unit information (what makes up each line item).
    *   Lists of orders shipped on a specific date.
*   **Location of Logic:** All interactions with the ERP database are encapsulated within `Services/Implementations/OdbcService.cs`.

## 2. Local SQLite Database

A local SQLite database (`identifier.sqlite`) is used to persist user-generated data and modifications that cannot be stored in the ERP system. This allows the application to be stateful and remember user actions between sessions.

*   **Technology:** **Entity Framework Core (EF Core)** is used as the ORM to manage the SQLite database.
*   **Database File:** The database is a single file named `identifier.sqlite` located in the application's root directory. **This file should be included in `.gitignore` if it is not already.**
*   **Purpose:** To store:
    *   The `ReportModel` and its relationships, effectively caching a version of the report that the user has interacted with.
    *   **Handling Units:** The structure of handling units created by the user via drag-and-drop.
    *   **Packing Unit modifications:** Changes to packing units, such as assigning them to a handling unit or a truck number.
*   **Location of Logic:**
    *   `Data/AppDbContext.cs`: Defines the EF Core context, specifying the `DbSet`s for each model and their relationships.
    *   `Services/Implementations/SqliteService.cs`: Encapsulates all read and write operations to the SQLite database.

### Data Merging Logic

A key feature of the application is how it merges data from these two sources. When a user loads an order:

1.  `OdbcService` fetches the latest, most up-to-date order information from the ERP.
2.  `SqliteService` fetches the locally cached `ReportModel` for the same order, which contains the user's previous modifications (like handling unit assignments).
3.  The `MainViewModel` then performs a merge:
    *   It uses the fresh data from the ERP as the base.
    *   It then copies the user-generated data (e.g., `LineItemPackingUnits`, `HandlingUnits`) from the cached model onto the fresh model.
    *   This ensures that data like quantities and product descriptions are always up-to-date from the ERP, while user organization is not lost.

### Database Migrations

EF Core Migrations are used to manage the schema of the `identifier.sqlite` database.

*   **Location:** `Migrations/`
*   **To create a new migration:** If you change the models in the `Models/` directory in a way that affects the database schema (e.g., adding a new property), you will need to create a new migration. This is typically done via the `dotnet ef migrations add <MigrationName>` command.
*   **To apply migrations:** The application is configured to automatically apply pending migrations on startup. See `App.xaml.cs` and the `AppDbContextFactory.cs`.
