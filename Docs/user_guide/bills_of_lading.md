# Bills of Lading (BOL)

The Bill of Lading (BOL) is a legally required document that provides the carrier and driver with all the necessary information to process a shipment. The Shipping Form Creator automates the creation of this document based on the data in the Packing List.

## Generating a Bill of Lading

1.  **Load an Order:** First, load the desired sales order using the search panel, just as you would for a packing list.
2.  **Navigate to the BOL View:** Click the **Bill of Lading** button in the left navigation panel.

The application will automatically take the line item data from the order and generate a summarized view suitable for a BOL.

## Understanding the BOL View

The Bill of Lading view is different from the Packing List. Instead of showing every single line item, it groups items together into a summary that is required by freight carriers.

Key components of the BOL view include:

*   **Header Information:** Contains all the standard BOL fields, such as shipper and consignee details, carrier name, and trailer number. Many of these fields are editable.
*   **Summary of Articles:** This is the most important section. The application automatically groups all the packing units from the order by their commodity type. For each type, it calculates and displays:
    *   **Total Pieces:** The total count of handling units or individual boxes/skids.
    *   **Description:** The type of commodity (e.g., "CHAIRS", "TABLE LEGS").
    *   **Total Weight:** The combined weight of all items in that group.
    *   **NMFC Code:** The National Motor Freight Classification code, automatically assigned based on the commodity type.
    *   **Class:** The freight class, also automatically assigned.
*   **Special Instructions:** Any notes or instructions flagged for the BOL from the order data will be displayed here.
*   **Totals:** The total number of pieces and the total weight for the entire shipment are calculated and displayed at the bottom.

## Multi-Truck Orders on the BOL

If an order has been split into multiple trucks (see the `Multi-Truck Shipments` section), you can use the dropdown menu at the top of the BOL view to filter the document for a specific truck.

*   **ALL:** Shows a consolidated BOL for the entire order.
*   **TRUCK 1, TRUCK 2, etc.:** Shows a separate BOL for the items assigned to that specific truck.

This allows you to print a distinct Bill of Lading for each truck in the shipment.

## Printing the BOL

Once you have reviewed the information and selected the correct truck (if applicable), click the **Print** button in the action panel to print the Bill of Lading.
