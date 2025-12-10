# Architectural Overview

The Shipping Form Creator is a desktop application built with **WPF (Windows Presentation Foundation)** on the **.NET** framework. It follows the **Model-View-ViewModel (MVVM)** design pattern to maintain a separation of concerns between the user interface (View), the application logic (ViewModel), and the data (Model).

## Core Technologies

*   **.NET & WPF:** The foundation of the application, providing the UI framework and runtime environment.
*   **Entity Framework Core (EF Core):** Used as the Object-Relational Mapper (ORM) for interacting with the local SQLite database.
*   **SQLite:** A local, file-based database used for caching user-generated data and modifications.
*   **ODBC (Open Database Connectivity):** Used to connect to and fetch data from the primary ERP system.
*   **Serilog:** For structured logging.
*   **GongSolutions.Wpf.DragDrop:** A library used to enable drag-and-drop functionality, particularly for the Handling Units feature.
*   **Syncfusion Controls:** The application utilizes some Syncfusion controls for WPF, such as `SfTextBoxExt` and `IntegerTextBox`, to provide an enhanced user experience.

## MVVM Pattern Implementation

The project is structured around the MVVM pattern:

### 1. Models
*   **Location:** `Shipping_Form_CreatorV1/Models/`
*   **Description:** These are simple C# classes (POCOs) that represent the data entities of the application, such as `ReportModel`, `LineItem`, `HandlingUnit`, etc. They do not contain any business logic. These models are used by Entity Framework Core to define the database schema.

### 2. Views
*   **Location:** `Shipping_Form_CreatorV1/` and `Shipping_Form_CreatorV1/Components/`
*   **Description:** These are the XAML files that define the user interface. The main window is `MainWindow.xaml`, and it hosts various user controls (`.xaml` files in the `Components` directory) within a `Frame` element. The views are responsible for displaying data and capturing user input. They are bound to properties and commands in the ViewModel. There is minimal code-behind (`.xaml.cs`) in the views; it is typically limited to UI-specific logic like navigation or handling events that are difficult to manage from the ViewModel.

### 3. ViewModels
*   **Location:** `Shipping_Form_CreatorV1/ViewModels/`
*   **Description:** This is the heart of the application's logic.
    *   `MainViewModel.cs` is the primary and only ViewModel in this application. It exposes data to the View through data-bound properties and exposes actions through `ICommand` properties.
    *   It is responsible for orchestrating data retrieval from the services, managing the application's state, and handling user interactions (like button clicks) that are forwarded from the View.
    *   It implements `INotifyPropertyChanged` to inform the View when a property's value has changed, allowing the UI to update automatically.

## Project Structure

*   `Shipping_Form_CreatorV1/`: The main WPF application project.
    *   `App.xaml.cs`: The application entry point. This is where the dependency injection container is configured and services are registered.
    *   `MainWindow.xaml`: The shell of the application.
    *   `Components/`: Contains the various user controls that make up the different pages of the application.
    *   `Data/`: Contains the Entity Framework `AppDbContext` and its factory.
    *   `Migrations/`: EF Core database migration files.
    *   `Models/`: Data models (POCOs).
    *   `Services/`: Contains the business logic services (`OdbcService`, `SqliteService`, `PrintService`).
    *   `Utilities/`: Helper classes and extension methods.
    *   `ViewModels/`: The `MainViewModel`.

## Application Flow

1.  **Startup:** `App.xaml.cs` starts, configures the services, and creates an instance of `MainWindow` and `MainViewModel`.
2.  **Initialization:** `MainWindow` is displayed. It sets its `DataContext` to the `MainViewModel`. The default view (`PackingListPage`) is loaded into the `ContentFrame`.
3.  **User Interaction:**
    *   The user interacts with a View (e.g., clicks a button).
    *   The View, via a data binding, invokes a `Command` on the `MainViewModel`.
    *   The `MainViewModel` executes the command, which may involve calling one or more services.
4.  **Data Retrieval/Storage:**
    *   The `MainViewModel` calls the `OdbcService` to get data from the ERP.
    *   It calls the `SqliteService` to get or save locally cached modifications.
    *   The services return data models to the `MainViewModel`.
5.  **UI Update:**
    *   The `MainViewModel` updates its properties with the new data.
    *   Because the `MainViewModel` implements `INotifyPropertyChanged`, the View is automatically notified of the changes and updates the display accordingly.
