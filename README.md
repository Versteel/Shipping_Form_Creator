# 📦 Shipping Form Creator

**Shipping Form Creator** is a Windows desktop application designed to generate, save, and print packing lists and bills of lading based on ERP order data. It supports integration with iSeries via ODBC and SQL Server for caching and multi-user access.

---

## 🚀 Features

- Load ERP data via ODBC (iSeries Access ODBC Driver)
- Automatically populates packing list and bill of lading forms
- Supports editing and saving reports with a SQL Server backend
- Fast search by order number
- Simple UI with modern styling
- Multi-user support over a shared network database
- Print-ready output with packing unit summaries

---

## 🛠 Requirements

- **Windows 10+**
- **.NET 9.0 Runtime**
- **SQL Server 2022 (Express or higher)** installed on a network-accessible machine
- **ODBC Driver**: iSeries Access ODBC Driver must be installed manually
- **Network access** to the database server

---

## 📦 Installation

1. **Install the iSeries Access ODBC Driver** on the client machine manually.
2. **Ensure SQL Server is running** on the network (e.g., `reportingpc\SQLEXPRESS`) and has:
   - TCP/IP enabled
   - Port 1433 open
   - Proper login permissions for users
3. **Deploy the App**:
   - Place the `.exe` and dependencies into a folder (can include it in a ClickOnce or MSIX installer if desired)
   - Run the app — no config required unless you change database/server settings

---

## ⚙ Configuration

App connection settings are configured in code:

```csharp
options.UseSqlServer(
    "Server=reportingpc,1433;Database=ShippingFormsDb;Integrated Security=SSPI;Encrypt=False;TrustServerCertificate=True;MultipleActiveResultSets=True;"
);
