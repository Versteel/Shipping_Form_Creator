# Searching and Printing

This section covers the final steps of the workflow: finding the information you need and generating printed documents.

## Searching for Orders

The application provides two primary methods for finding sales orders, both located in the center action panel.

### 1. Search by Order Number

This is the most common way to find an order.
*   **Order Number:** Enter the 6-digit sales order number (e.g., `123456`).
*   **Suffix:** Enter the 2-digit suffix (e.g., `00`).
*   Press the `Enter` key or click the **Search** button.

The application will then connect to the ERP system, retrieve the order information, merge it with any locally saved data (like handling units), and display it in the content area.

### 2. Search by Ship Date

This feature is useful for reviewing all orders that were shipped on a particular day.
*   **Date Picker:** Use the calendar control to select the desired ship date.
*   Click the **Search** button.

The application will display a `SEARCH RESULTS` page in the content area. This page lists all the orders shipped on the selected date. From this list, you can click on an individual order to open its Packing List or Bill of Lading.

## Printing Documents

The **Print** button in the action panel is used to generate physical copies of your documents. The printing process is context-aware, meaning it will print whatever you are currently viewing.

### Printing a Packing List

1.  Navigate to the **Packing List** view.
2.  Load the desired sales order.
3.  **For multi-truck shipments:** Use the view options dropdown at the top to select the specific truck you want to print for. If you select "ALL", it will print the complete packing list.
4.  Click the **Print** button.

The application will generate a multi-page packing list document if necessary, complete with page numbers and summary sections.

### Printing a Bill of Lading

1.  Navigate to the **Bill of Lading** view.
2.  Load the desired sales order.
3.  **For multi-truck shipments:** Use the view options dropdown to select the specific truck for which you need a BOL. Each truck requires its own BOL.
4.  Click the **Print** button.

The application will generate a standardized Bill of Lading document ready to be handed to the freight carrier.
