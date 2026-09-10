# Report Layout v2.0.0-preview.1

## Reliability fixes

- Removed the full InDesign font-library scan that could stall validation before Word import.
- Added cooperative cancellation of the active report at safe engine checkpoints.
- Cancellation closes the working copy and prevents remaining queued reports from starting.
- Added detailed validation-stage logging and cancellation regression coverage.
- Added proportional sizing for overset inline WMF/raster images using inner-coordinate anchors.
- Added recovery for long single-cell Word layout containers that cannot paginate as InDesign table rows.
- Added `Start-Placeholders.vbs`, a separate edition that replaces imported images with empty anchored frames for manual placement later.
- Centered images and placeholders reserve their own above-line space instead of covering body text.

Feature-complete preview of the ten requested improvements: four formatting presets, editable layout settings, PDF presets, template validation, batch reports, generated contents, cover metadata, result dashboard, remembered settings, and update checking.

Extract the complete ZIP and run Start.vbs (or Start.cmd). Preview installs separately as Report Layout Preview. Retain v1.1.1 for production.

Portable tests and Windows compilation/settings tests pass. Real InDesign 21.3 acceptance was completed with a 74-image DOCX: retained-image output completed at 83 pages, while placeholder output completed with 74 empty frames at 123 pages. Contents is generated editable text with page numbers, not live TOC/hyperlinks. Update checks target stable releases and never install automatically.

Send startup-error.txt for setup errors. For report errors, send the job folder's job-config.json, result.json, report-log.txt, windows-error.txt and PDF/IDML where available.
