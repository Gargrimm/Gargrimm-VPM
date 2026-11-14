# Blend Shape Organizer

An alternative editor window for blend shapes on a Skinned Mesh Renderer, intended for Unity 2022.3.22f1.

## Installation

Install the package from the Gargrimm VPM listing in VCC or ALCOM, or add it as a local package. Avoid keeping a second copy in `Assets`.

## Usage

Open **Tools > Gargrimm > Blend Shape Organizer** and select an object with a Skinned Mesh Renderer. Disable **Follow selection** or assign a component manually to keep it selected.

- Search for blend shape or group names without case sensitivity.
- Names such as `= Face =` or `=== Face ===` start a group. Subsequent blend shapes belong to it until the next marker.
- Names beginning with `vrc.` appear in the **VRC** group without changing the current group.
- Blend shapes before the first marker appear in **Ungrouped**.
- Markers remain editable because they are also blend shapes. Groups with the same name are combined.
- Changes support Undo and prefab instance overrides. The window complements the built-in inspector.

## Copy weights to another object

Select the source renderer and click **Copy weights**. Select or assign the destination renderer and click **Paste matching weights**. All blend shapes are copied regardless of the search filter or expanded groups. Names must match exactly, including case; unmatched names are skipped. Pasting supports Undo and reports the number of matches. Copied values are held in editor memory and are lost when scripts recompile or Unity restarts.
