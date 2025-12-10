# Packing Lists

The Packing List is the primary document you will work with in the Shipping Form Creator. It provides a detailed, itemized view of a sales order, which you can then organize and prepare for shipment.

## Loading a Packing List

To load a packing list, ensure the "Packing List" view is active (it's the default view when you open the application). Then, use the search panel to find an order by its number and suffix.

When an order is loaded, the content area will display the Packing List, showing:
*   **Header Information:** Details about the order, such as the customer, ship-to address, and order number.
*   **Line Items:** A list of all products on the order, including their description, quantity, and other details pulled from the ERP system.
*   **Packing Units:** For each line item, the system displays the individual units that need to be packed (e.g., boxes, skids), along with their weight and dimensions.

## The Packing List View

The packing list itself is composed of several key components:

*   **Report Header:** Displays the Versteel logo, ship-to/sold-to addresses, and other high-level order information.
*   **Line Item Details:** Each item on the order is listed.
*   **Packing Unit Details:** Below each line item, you'll find the specific packing units (cartons/boxes or skids). This is where you can see the weight and dimensions for each piece.
*   **Handling Units Panel:** On the right side of the packing list view, there is a dedicated panel for managing `Handling Units`. This is a critical feature for organizing your shipment and is covered in its own section.

## Saving Your Work

As you make changes to the packing list, such as organizing items into handling units, it's important to save your progress. Click the **Save** button in the action panel.

Your changes are saved to a local database on your computer. This means:
*   Your work is not lost if you close the application.
*   When you reload the same order later, your previously organized handling units will be restored.
*   The application intelligently merges your saved work with any new data from the ERP system. For example, if a quantity changes in the ERP, the application will update the line item but preserve the handling unit structure you created.

This local caching ensures that the information from the ERP remains the source of truth for order details, while your organizational work is preserved.
