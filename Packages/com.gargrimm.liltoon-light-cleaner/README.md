# lilToon Light Cleaner

An editor tool to disable Rim Light and Backlight on multiple lilToon materials at once.

## Installation

Install the package from the Gargrimm VPM listing in VCC or ALCOM, or install it locally by selecting `package.json` under **Window > Package Manager > + > Add package from disk...**. Open **Tools > Gargrimm > lilToon Light Cleaner**.

## Usage

- Select materials in the Project window and click **Add Project selection**, or drag them into the window.
- Choose the options to disable and click **Disable on all materials**.
- Changes support Undo and are saved to the assets.

The tool sets `_UseRim` and `_UseBacklight` to `0` only where the material exposes the selected properties. Materials without matching properties are skipped.
