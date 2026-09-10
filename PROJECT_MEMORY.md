# Project Memory

## Purpose and current state

Report Layout automates Persian DOCX-to-InDesign page layout on Windows. Current work is `v2.0.0-preview.1`; the last user-tested version is `v1.1.1`. Target: Windows 10/11 and InDesign 21.3. English UI; Persian report content. Outputs: INDD, IDML, PDF, logs, JSON status.

## Product decisions

- Use installed InDesign as the authoritative layout engine.
- Keep the UI English. Version 2 adds tabs for Reports, Layout Settings, Cover & Details, Results, Preferences, Log.
- Keep output editable and preserve source files by opening a template copy.
- Map Word Heading styles to InDesign hierarchy.
- Disable automatic kashidas and normalize numbered-heading separators to one space.
- Show success only after INDD, IDML, and PDF exist and are nonempty.
- Use the supplied logo in the app, EXE, taskbar, tray, Desktop, and Start Menu.
- Hide to the tray on minimize.
- All ten requested feature groups are implemented; keep preview status until Windows/InDesign acceptance.
- Install preview under ReportLayoutPreview with separate shortcuts; leave stable installation untouched.
- Use four built-in formatting presets plus versioned flat JSON options; unknown/invalid engine options fail early.
- Legacy margins preserve the previously tested body geometry; Template/Custom margins are explicit choices.
- Validate on an opened copy before destructive layout operations; close validation copies without saving.
- Sequential batches use per-job folders and a batch summary. Cancel uses a shared request file checked at safe engine checkpoints, closes the current working copy and skips remaining reports. Never close user documents.
- Contents is generated editable text with heading labels and stabilized page numbers, not native live TOC/hyperlinks. Regenerate after manual edits.
- Settings stay in LocalAppData/ReportLayoutData; startup update checks are opt-in. Public API plus authenticated gh fallback. Never extract credentials or automatically execute updates.

## Components

- `src/ReportLayout.cs`: WinForms UI, COM connection, tray, success dialog, errors.
- `src/Settings.cs`: preset/property-grid schema, option validation, atomic user-settings persistence.
- `Layout-Report.jsx`: Word import, styles, flow, tables, validation, export.
- `Build-and-Run.ps1`: per-user install, compilation, icon, shortcuts.
- `Start.vbs` and `Start.cmd`: launchers.
- `assets/Template.idml` and `assets/Document fonts`: bundled layout assets.
- `tests/flow-tests.cjs`: portable tests.

## InDesign conventions

Facing Pages is off. Persistent decoration belongs on a Parent page. Margins define the body frame. The bundled Parent is `D1-Main Body`. Running headers should have Script Label `REPORT_HEADER`. Body uses IRNazanin; headings use Modam. Tables stay native, editable, and repeat their first row.

## Known constraints

The engine is optimized for the bundled single-page Persian template. Word images set to In Line with Text can be imported and proportionally reduced to configurable text-frame limits; floating images remain unsupported and generate warnings. Complex or nested tables, formulas, text boxes, tracked changes, and elaborate notes may need manual work. Content that cannot fit a full page stops the build. Windows compilation, shortcuts, COM, and InDesign behavior require target-machine testing. Asset redistribution follows original licenses.

## Validation history

- The initial manual report opened in InDesign 21.3 with no Preflight errors.
- Returned PDF and IDML included all source paragraphs and table cells.
- RTL cell reordering caused a false table-change error; checks now use stable table/cell IDs.
- Overset `Cell.contents` could appear empty; checks now read the internal text model with character fallback.
- Tests cover headings, threaded overflow, progress safeguards, table identity/content, overset-aware cell reads, and heading spacing.
- v1.1.0 added English UI, icons, tray behavior, shortcuts, and guarded success notification.
- v1.1.1 fixed PDF export on InDesign 21.3 by using application-level PDF export preferences.
- User-provided v1.1.1 result: ok=true, 21 pages, one table, no warnings.
- v2 preview: portable flow/options/simulated-DOM tests pass; no Windows compiler or InDesign runtime was available here. Do not describe the preview as Windows-tested.
- Windows build/settings tests and CI job are included but have not been executed in this workspace.

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
