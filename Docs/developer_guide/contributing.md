# Contributing Guide

We welcome contributions to the Shipping Form Creator project. This guide outlines the process and standards for contributing.

## Getting Started

1.  **Clone the Repository:** Make sure you have a local copy of the project.
2.  **Environment Setup:**
    *   You will need Visual Studio or JetBrains Rider with .NET development tools installed.
    *   The project is configured to work with a specific ODBC data source for the ERP system. For development outside of the Versteel network, you may need to mock the `OdbcService` or work with local data only. Contact the project maintainers for guidance.
3.  **Build the Project:** Open the solution file (`Shipping_Form_Creator.sln`) and build the solution to ensure all dependencies are restored.

## How to Contribute

1.  **Find an Issue or Feature:** Look for existing issues in the issue tracker or propose a new feature.
2.  **Create a Branch:** Create a new feature branch for your work (e.g., `feature/add-new-field` or `bugfix/fix-print-alignment`).
3.  **Write Code:** Make your changes, adhering to the existing coding style and architecture.
    *   **Follow MVVM:** Ensure that business logic resides in the ViewModel or services, not in the View's code-behind.
    *   **Use Services:** If your feature involves data access or other distinct responsibilities, encapsulate that logic in a service.
    *   **Update Models:** If you change the data model, create a new EF Core migration (see the Database guide).
4.  **Test Your Changes:** Run the application and thoroughly test your feature or bugfix to ensure it works as expected and does not introduce new issues.
5.  **Submit a Pull Request:** Push your branch and open a pull request against the `main` branch. Provide a clear description of the changes you have made.

## Coding Style and Conventions

*   **C#:** Follow the existing coding style regarding naming conventions (e.g., `_privateFields`, `PascalCaseProperties`, `PascalCaseMethods`).
*   **XAML:** Maintain a clean and readable XAML structure. Use `d:DataContext` to enable IntelliSense for data bindings in your IDE.
*   **Comments:** Add comments only where necessary to explain complex logic. Avoid redundant comments that state the obvious.
*   **Error Handling:** Implement appropriate `try-catch` blocks for operations that can fail, such as database calls or file I/O. Use the `DialogService` to present errors to the user.

## Contact

If you have questions, please reach out to the project maintainers.
