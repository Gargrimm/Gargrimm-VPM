# lilToon Light Cleaner

An editor tool to disable Rim Light and Backlight on multiple lilToon materials at once.

## Installation

Install the package from the Gargrimm VPM listing in VCC or ALCOM, or install it locally by selecting `package.json` under **Window > Package Manager > + > Add package from disk...**. Open **Tools > Gargrimm > lilToon Light Cleaner**.

## Usage

- Select materials or GameObjects and click **Add selection**, or drag them into the window. GameObjects contribute the shared materials of all renderers on themselves and their children, including inactive children. Duplicate materials are added only once.
- Choose the options to disable and click **Disable on all materials**.
- Changes support Undo and are saved to the shared material assets, affecting every object that uses them.

The tool sets `_UseRim` and `_UseBacklight` to `0` only where the material exposes the selected properties. Materials without matching properties are skipped.
