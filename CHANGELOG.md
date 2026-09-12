# Changelog

## Unreleased

### Changed

- Merged the separate image-placeholder launcher/edition into the single application. **Replace images with placeholders** is now an ordinary Layout Settings option instead of a dedicated `Start-Placeholders.vbs` launcher and settings profile.
- Added a password-protected sign-in prompt at startup, checked against a stored SHA-256 hash.
- Added a version number and copyright notice to the main window footer.
- Packaged the application as a single MSI installer (WiX), replacing the zip + `Start.vbs` distribution.

## 2.0.0-preview.1 - 2026-09-10

### Added

- All ten requested groups: presets, layout controls, installed PDF presets, template validator, batch queue, generated contents, report metadata, results dashboard, last settings and update checks.
- Preset import/export, schema checks, exact font dropdown, separate heading/table colors and custom/template margin modes.
- Sequential queue with per-report titles, continue-on-error, safe stop after current and batch summary.
- Editable contents with stable page-number recalculation; no live TOC/hyperlinks.
- Public release check with authenticated GitHub CLI fallback for private repositories; no automatic installer execution.
- Portable settings tests and simulated InDesign orchestration tests; Windows build/settings CI job.

### Changed

- Preview install and shortcuts are separate from v1.1.1.
- Source compilation includes all C# files; settings persist outside installed files.
- Default preset preserves v1.1.1 typography and margins.
- Required fonts are resolved directly; the build no longer traverses the complete InDesign font library.

### Fixed

- A full `app.fonts` scan could stall validation before Word import and eventually break the COM call.
- The former stop action only skipped later queue items. **Cancel Current Job** now signals the active ExtendScript, checks cancellation throughout long loops and closes the working copy safely.
- Avoided resetting and immediately recreating an already-enabled table header, which could invalidate a real InDesign cell during integrity validation.
- Log text now wraps inside the application instead of requiring horizontal scrolling.
- Newly selected reports require an explicit editable title, with a **Set Selected Title** action for clarity.
- Numbered heading levels use an editable separator (default `-`), converting forms such as `11,2)` to `11-2)` while leaving ordinary prose untouched.
- Added independent Heading 3 typography, bullet/numbered paragraph formatting and indent controls.
- English-only table cells are explicitly left-to-right; table-header horizontal alignment and all-cell vertical alignment are configurable, defaulting to Center and Middle.
- Heading font/color are explicitly reapplied after clearing Word overrides, and table-header alignment is applied to its paragraphs rather than the containing text object.
- Added the dedicated `Report Table Header` paragraph style; the configured header alignment is applied through that style.
- Added opt-in Word inline-image import with proportional downscaling, configurable text-frame width/height limits, optional paragraph centering, image metrics and floating-image warnings.
- Use InDesign 21.3's direct `Story.allGraphics` array instead of treating it as an `everyItem()` collection.
- Isolated inline-image sizing failures per graphic so unsupported Word image containers produce a warning instead of aborting the complete report.
- Measure overset anchored images through inner-coordinate anchors, allowing Word-imported WMF and raster graphics to be resized before pagination.
- Convert long one-row/one-cell Word layout containers to normal flowing text when they cannot split across pages.
- Added a separate image-placeholder launcher and settings profile; it preserves 74 tested image positions as empty anchored frames for later manual placement.
- Place centered images and placeholders above their anchor line so they reserve vertical space and do not cover body text.
- The Windows test harness now loads the compiled executable as an assembly on Windows PowerShell.

### Validation status

- Portable tests and Windows compilation/settings tests pass. Real InDesign 21.3 acceptance completed with a 74-image DOCX: the retained-image output completed at 83 pages, and the placeholder output completed with 74 empty frames and 123 pages.

## 1.1.1 - 2026-09-10

### Fixed

- PDF export failure in InDesign 21.3 caused by reading `pdfExportPreferences` from the document instead of the application.

## 1.1.0 - 2026-09-09

### Added

- English Windows UI.
- Supplied logo across window, EXE, taskbar, tray, Desktop, Start Menu, and header.
- Per-user installation and shortcuts.
- Tray minimize, restore, output-folder, and exit commands.
- Guarded success dialog and tray notification.
- Complete README and Project Memory.

### Changed

- Table checks use stable IDs and the internal text model.
- Automatic kashidas are disabled.
- Numbered heading spacing is normalized to one space.

### Fixed

- False table-change failures after RTL cell reordering.
- False empty-cell comparisons while tables were overset.
