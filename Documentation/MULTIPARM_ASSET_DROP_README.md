# Multiparm asset drag and drop

## Changes

Each supported non-ramp multiparm has a **Drop assets here to add entries** area below its entries. Multiple multiparms on one HDA each receive their own area.

- Select multiple assets in Unity's Project window and drag them onto the target area.
- One entry is appended for each distinct valid asset path in the drop, in the order supplied by Unity.
- Each new entry receives that asset's project-relative path. Other parameters, such as Weight, retain their Houdini instance defaults.
- Existing entries and other multiparms remain in place. Assets already present in the list can be added again.
- Folders and scene objects are ignored. Several subassets with the same asset path produce one entry, since the field stores paths rather than subasset IDs.
- The batch uses the existing parameter modifier and cooking workflow. With automatic cooking enabled, the plugin applies the batch before the resulting cook. With automatic cooking disabled, press Recook to apply it.

## Supported input field

This targets the Unity asset selector shown for a Houdini scalar string parameter with the `heuassetpath` tag. Each entry must contain exactly one such field, with no choice menu. It must be visible and enabled. Fields inside ordinary folders are supported; a nested multiparm has its own destination and is not treated as its parent's input.

Geometry/node inputs, ordinary untagged strings, tuple asset fields, ramps and entries containing several asset-path fields are not supported by this addition.

For a populated list, the drop area is shown only when its first entry has one matching field. An empty list has no instantiated field metadata, so it shows a drop area provisionally. The plugin inserts and inspects the first new entry during the batch. If the template is unsupported, or an insertion/assignment fails, it attempts to remove all entries added by that drop and logs a warning. If rollback itself fails, the warning asks you to inspect the list.

## Quick check in Unity

1. Use an HDA with two separate multiparms, each containing one asset-path field and a Weight field.
2. Drop three prefabs into the first list. Check that its count increases by three, paths match and new weights use the HDA defaults.
3. Check that existing values and the second list are unchanged.
4. Clear the second list and drop two assets into its empty drop area.
5. Disable automatic cooking, drop another batch and press Recook to apply it.

Source syntax and integration checks were performed. Unity compilation and an actual HDA cook must still be verified in the Unity project.
