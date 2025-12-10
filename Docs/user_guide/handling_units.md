# Handling Units

A "Handling Unit" is a term used in this application to describe a collection of individual packing units (like boxes or skids) that are grouped together for shipment, often on a single pallet. The Handling Units feature allows you to logically organize a shipment before it is physically assembled.

This is one of the most powerful features of the Shipping Form Creator, as it helps create accurate and organized packing lists and Bills of Lading.

## The Handling Units Panel

When viewing a Packing List, the Handling Units panel is visible on the right side of the content area. This panel is your workspace for creating and managing the handling units for an order.

*   **Header:** The panel is labeled "Handling Units" and shows a count of how many units you have created.
*   **Add/Remove Buttons:**
    *   The `+` (plus) button allows you to add a new, empty handling unit to the list. It will be automatically named (e.g., "Unit 1", "Unit 2").
    *   The `-` (minus) button next to each handling unit allows you to remove it.
*   **Handling Unit List:** Each handling unit you create is displayed as a card in this panel.

## Organizing a Shipment with Handling Units

The core workflow for this feature is to move packing units from the line items on the left into the handling units on the right.

### Drag and Drop

The easiest way to organize your shipment is by using **drag and drop**:

1.  **Locate a Packing Unit:** In the main packing list view on the left, find a packing unit (a "BOX" or "SKID") that you want to place into a handling unit.
2.  **Click and Drag:** Click on the packing unit and drag it across the screen to the right.
3.  **Drop onto a Handling Unit:** As you hover over a handling unit in the panel, it will be highlighted. Release the mouse button to "drop" the packing unit inside.

The packing unit will now appear nested under the handling unit it was dropped into. The application automatically keeps track of the total weight and contents of each handling unit.

### Unassigning a Packing Unit

If you make a mistake, you can remove a packing unit from a handling unit. Simply find the unit inside its handling unit on the right, and use the associated "unassign" button (often a delete or 'x' icon). The packing unit will return to its original position in the line item list.

### Removing a Handling Unit

If you remove a handling unit that contains packing units, the application will warn you. If you proceed, all contained packing units will be unassigned and will return to their original locations in the line item list.

## Why Use Handling Units?

*   **Clarity:** It provides a clear plan for the shipping department on how to stage and load the freight.
*   **Accuracy:** It ensures the Bill of Lading accurately reflects the number of pieces being shipped (e.g., 5 pallets instead of 50 individual boxes).
*   **Efficiency:** This logical organization can be done at a desk, saving time on the shipping dock.

Remember to **Save Changes** after you have organized your handling units to ensure your work is preserved.
