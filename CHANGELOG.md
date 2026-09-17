# Changelog

## 1.1.2 - 2026-09-17

### Added

- **Thread Body Pages**: new button that links the first text frame found on each page of an existing `.indd`, in page order, over a chosen page range. For repairing a document assembled from separately-built pieces (InDesign never auto-threads an Insert Pages or copy/paste merge) -- not needed for a normal Build Report run, since its own page flow is already threaded as it builds. Safe to re-run on an already-threaded document.

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
