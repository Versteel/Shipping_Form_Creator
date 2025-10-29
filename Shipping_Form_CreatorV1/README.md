# Shipping Form Creator

## Overview

The **Shipping Form Creator** is a Windows desktop application developed for Versteel to streamline the creation, management, and printing of essential shipping documents. Built with WPF, this tool provides a modern and efficient user interface for generating Packing Lists and Bills of Lading.

It connects directly to company databases to pull real-time order information, reducing manual data entry and minimizing errors.

## Key Features

- **Document Generation**: Create and print two main types of shipping documents:
  - Packing Lists
  - Bills of Lading
- **Powerful Search**: Quickly find order information by:
  - A specific 6-digit sales order number.
  - A date range to see all orders that have shipped.
- **Database Integration**: Uses Entity Framework Core to fetch live data from local IBMi using ODBC and save any modifications back to an external SQL Server database.
- **PDF Printing**: Generates professional, print-ready PDF documents using the iText and PDFSharp libraries.
- **Modern UI**: A clean, intuitive, and responsive user interface built with ModernWpfUI and Syncfusion controls.

## Technology Stack

- **Framework**: .NET 9.0
- **Language**: C#
- **UI**: WPF (Windows Presentation Foundation)
  - ModernWpfUI
  - Syncfusion WPF Controls
- **Data Access**: Entity Framework Core
- **PDF Generation**: iText & PDFSharp
- **Logging**: Serilog
- **Architecture**: MVVM (Model-View-ViewModel)

## Getting Started

### Prerequisites

To build and run this project, you will need:
- **.NET 9.0 SDK** (or later)
- **Visual Studio 2022** (or a compatible IDE) with the ".NET desktop development" workload installed.
- Access to the required backend databases.

### Build & Run

1.  Clone the repository to your local machine.
2.  Open the `Shipping_Form_CreatorV1.sln` file in Visual Studio.
3.  Restore the NuGet packages (this should happen automatically, but you can right-click the solution and select "Restore NuGet Packages" if needed).
4.  Set the `Shipping_Form_CreatorV1` project as the startup project.
5.  Press `F5` or click the "Start" button to build and run the application.

## Usage

1.  Launch the application.
2.  Use the search panel on the left to find an order by its number or ship date.
3.  Select either "Packing List" or "Bill of Lading" to view and edit the document.
4.  Make any necessary changes and click "Save".
5.  Click "Print" to generate a PDF of the final document.

## Future Directions: AI Integration

This application is an excellent candidate for AI enhancements to further improve efficiency and accuracy. Potential future features include:

- **OCR for Data Entry**: Automatically scan and parse customer purchase orders to pre-fill shipping forms.
- **Predictive Analytics**: Use machine learning to predict shipment weights, suggest freight classes, and recommend optimal carriers.
- **Anomaly Detection**: Flag unusual orders or potential data entry errors in real-time to prevent costly mistakes.
- **Natural Language UI**: Add a chatbot or voice commands to allow users to perform actions like *"Find order 123456"* conversationally.
