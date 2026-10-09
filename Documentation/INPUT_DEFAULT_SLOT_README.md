# Default input slot

Newly created HDA inputs start with one empty object slot, so an object can be assigned immediately without clicking **Add Slot**.

- Applies to inputs created through the shared input-node setup, including geometry inputs and node-input parameters.
- Both object and HDA input lists start with one empty slot. Transform offsets retain their normal identity defaults, including scale `(1, 1, 1)`.
- Add Slot still adds more entries. Clear still empties a list; drawing the Inspector does not recreate a cleared slot.
- Existing saved input lists and restored presets are not migrated or overwritten. A newly dropped HDA demonstrates the new default.
- This does not change the number of multiparm entries or scalar asset-path fields.

Check in Unity: drop an HDA with several inputs; confirm one empty slot per input; assign an object; switch a fresh input to HDA mode; add/remove/clear slots; recook and save/reopen. Also restore a preset and verify its exact input count. Unity compilation and live cooking remain unverified here.
