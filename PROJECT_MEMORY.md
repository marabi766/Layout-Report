# Project Memory

## Purpose and current state

Report Layout automates Persian DOCX-to-InDesign page layout on Windows. Version `v1.1.0` targets Windows 10/11 and InDesign 21.3, uses an English UI, accepts Persian content, and produces INDD, IDML, PDF, logs, and JSON status.

## Product decisions

- Use installed InDesign as the authoritative layout engine.
- Keep the UI English and the workflow limited to selecting a report/template, entering a title, and building.
- Keep output editable and preserve source files by opening a template copy.
- Map Word Heading styles to InDesign hierarchy.
- Disable automatic kashidas and normalize numbered-heading separators to one space.
- Show success only after INDD, IDML, and PDF exist and are nonempty.
- Use the supplied logo in the app, EXE, taskbar, tray, Desktop, and Start Menu.
- Hide to the tray on minimize.

## Components

- `src/ReportLayout.cs`: WinForms UI, COM connection, tray, success dialog, errors.
- `Layout-Report.jsx`: Word import, styles, flow, tables, validation, export.
- `Build-and-Run.ps1`: per-user install, compilation, icon, shortcuts.
- `Start.vbs` and `Start.cmd`: launchers.
- `assets/Template.idml` and `assets/Document fonts`: bundled layout assets.
- `tests/flow-tests.cjs`: portable tests.

## InDesign conventions

Facing Pages is off. Persistent decoration belongs on a Parent page. Margins define the body frame. The bundled Parent is `D1-Main Body`. Running headers should have Script Label `REPORT_HEADER`. Body uses IRNazanin; headings use Modam. Tables stay native, editable, and repeat their first row.

## Known constraints

The engine is optimized for the bundled single-page Persian template. Word images are intentionally skipped. Complex or nested tables, formulas, text boxes, tracked changes, and elaborate notes may need manual work. Content that cannot fit a full page stops the build. Windows compilation, shortcuts, COM, and InDesign behavior require target-machine testing. Asset redistribution follows original licenses.

## Validation history

- The initial manual report opened in InDesign 21.3 with no Preflight errors.
- Returned PDF and IDML included all source paragraphs and table cells.
- RTL cell reordering caused a false table-change error; checks now use stable table/cell IDs.
- Overset `Cell.contents` could appear empty; checks now read the internal text model with character fallback.
- Tests cover headings, threaded overflow, progress safeguards, table identity/content, overset-aware cell reads, and heading spacing.
- v1.1.0 added English UI, icons, tray behavior, shortcuts, and guarded success notification.

## Next acceptance test

1. Install with `Start.vbs` on Windows.
2. Confirm all icon locations and tray restore.
3. Build the known China report.
4. Confirm success only after all outputs exist.
5. Review PDF and Preflight.
6. Verify kashidas are None and numbered headings use one normal space.
7. Record results here and retain logs on failure.

## Release procedure

Update versions and docs; run tests; validate ICO sizes; build ZIP and checksum; commit to `main`; create annotated tag; push branch and tag; create GitHub Release; attach ZIP/checksum; perform and record Windows/InDesign acceptance.
