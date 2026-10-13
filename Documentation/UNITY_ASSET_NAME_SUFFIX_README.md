# unity_asset_name_suffix — New baked prefab naming

Use a scalar **string detail attribute** on the HDA's cooked output:

```c
s@unity_asset_name_suffix = "_large";
```

New prefab names use **asset name + unity_asset_name_suffix + optional _prefab suffix**. The existing numeric portion of the asset name is preserved.

| Asset name | Infix | Add _prefab Suffix | Filename |
| --- | --- | --- | --- |
| Bridge_1_2 | _large | On | Bridge_1_2_large_prefab.prefab |
| Bridge_1_2 | _large | Off | Bridge_1_2_large.prefab |
| Bridge_1_2 | empty/absent | On | Bridge_1_2_prefab.prefab |
| Bridge_1_2 | empty/absent | Off | Bridge_1_2.prefab |

The value is appended literally: include your own underscore or separator. The combined name is sanitized by the existing filename utility. The existing safeguard avoids adding another `_prefab` when the combined name already ends in that exact suffix.

With a nonempty infix, the new prefab's root GameObject receives the same name as the filename without `.prefab`, with either suffix-toggle setting. When absent or empty, all previous naming behavior is retained, including the prior baked-root naming convention when the toggle is off.

## Scope

- Applies to **new prefab bakes**, including scene-folder baking and explicit destinations.
- Does not rename destination folders, mesh/material assets, child outputs or individual instances.
- Update Prefab keeps its existing target filename; changing this attribute does not rename or move an already baked prefab. Bake a new prefab to use a different infix.
- Does not rename the live HDA root or standalone GameObject bakes.
- Reads detail strings from clonable output parts using the existing root-attribute reader. If several parts have conflicting nonempty values, the first is used with a warning. Set a consistent value across outputs.
- Other owners are ignored; an invalid detail type or tuple size is skipped with a warning.

## Verification

1. Bake with `_large` and Add _prefab Suffix on: verify filename and root name.
2. Bake with the toggle off: verify the infix remains and `_prefab` is absent.
3. Test empty and absent attributes: compare with previous naming.
4. Change the infix and update an existing prefab: its destination filename remains unchanged.
5. Test scene-folder baking, repeated new bakes and conflicting detail values across outputs.

Source integration checked; this new feature still requires Unity verification. The earlier test round passed before this addition.
