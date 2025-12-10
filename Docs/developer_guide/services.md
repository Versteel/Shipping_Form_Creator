# Services Guide

The application's business logic is decoupled from the `MainViewModel` and encapsulated in several services. These services are registered for dependency injection in `App.xaml.cs` and are responsible for specific areas of functionality.

## Service Overview

### 1. `OdbcService`
*   **Interface:** `IOdbcService.cs`
*   **Implementation:** `OdbcService.cs`
*   **Purpose:** This service is responsible for all communication with the remote ERP database via an ODBC connection. It translates the raw data from the ERP into the application's data models (`ReportModel`, `LineItem`, etc.).
*   **Key Methods:**
    *   `GetReportAsync`: Fetches a single sales order by its order number and suffix.
    *   `GetShippedOrdersByDate`: Returns a list of orders that were shipped on a given date.
    *   `GetOrdersByOrderNumber`: Finds all order suffixes for a given base order number.

### 2. `SqliteService`
*   **Interface:** `ISqliteService.cs`
*   **Implementation:** `SqliteService.cs`
*   **Purpose:** This service manages all read/write operations to the local `identifier.sqlite` database using Entity Framework Core. It handles the caching and retrieval of user-generated modifications.
*   **Key Methods:**
    *   `GetReportAsync`: Retrieves a locally cached report by its order number and suffix.
    *   `SaveReportAsync`: Saves or updates a `ReportModel` in the local database. This is the core method for persisting user work.

### 3. `PrintService`
*   **Implementation:** `PrintService.cs`
*   **Purpose:** This service is responsible for generating and printing all documents. It takes the data from the `MainViewModel` and constructs the visual representation of the documents for printing.
*   **Key Methods:**
    *   `BuildAllPackingListPages`: Creates the visual pages for a packing list. It handles multi-page logic, creating `PackingListPageOne` for the first page and `PackingListPageTwoPlus` for subsequent pages.
    *   `PrintPackingListPages`: Sends the generated packing list pages to the printer.
    *   `BuildBillOfLadingPage`: Creates the visual for the Bill of Lading.
    *   `PrintBillOfLadingAsync`: Sends the generated BOL to the printer.

### 4. `UserGroupService`
*   **Implementation:** `UserGroupService.cs`
*   **Purpose:** A simple utility service to check if the current user belongs to a specific Windows user group.
*   **Key Methods:**
    *   `IsCurrentUserInDittoGroup`: This method is used to determine if the current user has special permissions (the "Ditto User"). While the property exists in the `MainViewModel`, the full implementation of any special functionality may not be present.

### 5. `DialogService`
*   **Implementation:** `DialogService.cs`
*   **Purpose:** A utility to show styled message boxes and error dialogs to the user. This keeps the dialog presentation consistent throughout the application.

## Dependency Injection

All of these services (except for the static `DialogService` and `UserGroupService`) are registered in `App.xaml.cs` during application startup. This allows them to be injected into the constructors of the classes that need them (primarily `MainViewModel` and `MainWindow`).

This loosely coupled design makes the application more maintainable and testable, as individual components can be worked on or replaced with minimal impact on the rest of the system.
