# Multi-Truck Shipments

For large orders, it may be necessary to split a single shipment across multiple trucks. The Shipping Form Creator has built-in features to manage this scenario, ensuring that both the Packing Lists and Bills of Lading are accurate for each truck.

## Assigning Items to a Truck

The assignment of items to a truck happens at the **packing unit** level. For each individual box or skid listed under a line item, you can assign a truck number.

1.  **Locate the Packing Unit:** In the Packing List view, find the specific packing unit you want to assign.
2.  **Select the Truck Number:** Next to the packing unit details, you will find a dropdown menu labeled "Truck #". This dropdown contains a list of available trucks (e.g., "TRUCK 1", "TRUCK 2", etc.).
3.  **Choose the Truck:** Select the appropriate truck number from the list.

Repeat this process for all packing units in the shipment. You can assign different packing units from the same line item to different trucks.

## Viewing a Specific Truck

Once you have assigned items to different trucks, you can filter the entire view to see the contents of just one truck.

At the top of the Packing List or Bill of Lading view, you will see a dropdown menu (it may be labeled "View Options" or similar). This menu will contain:
*   **ALL:** The default view, showing all items for the order.
*   **TRUCK 1, TRUCK 2, etc.:** The list of trucks that have items assigned to them.

Selecting a truck from this menu will filter the view to show **only** the packing units, handling units, and summary information relevant to that specific truck.

## How It Affects Documents

### Packing Lists
When you print a Packing List while a specific truck is selected in the view options, the printed document will only contain the items and handling units assigned to that truck. This is useful for giving each driver a manifest for their specific load.

### Bills of Lading
Similarly, when you generate a Bill of Lading, you can use the same view option dropdown to create a separate BOL for each truck. The piece counts, weights, and commodity summaries will all be calculated based only on the items assigned to the selected truck. This is critical for the carrier, as each truck requires its own Bill of Lading.

## Saving Your Assignments

The truck number assignments are saved along with your other changes when you click the **Save Changes** button. This ensures that the next time you load the order, the multi-truck configuration is restored.
