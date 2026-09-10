# Testing

## Portable checks (executed for v2 preview)

```text
node tests/flow-tests.cjs
node tests/options-tests.cjs
node tests/engine-tests.cjs
```

The last suite simulates InDesign's DOM. It verifies control flow and contracts, not actual InDesign typography or API compatibility. No new preview PDF has been rendered on real InDesign in this environment.

## Windows build checks (must run on Windows)

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tests\Windows-Tests.ps1
```

This compiles all C# files, round-trips every preset, rejects invalid input, tests update comparison and atomic settings persistence. It does not launch the UI or create shortcuts. The same test is configured in GitHub Actions; do not mark it passed until a Windows run actually succeeds.

## Preview acceptance checklist

1. Close both stable and preview apps. Extract ZIP, run Start.vbs. Confirm Report Layout Preview shortcuts, tray icon and installation. Stable v1.1.1 must remain untouched.
2. Test at 100%, 125%, 150% display scaling; all six tabs and action buttons must remain accessible. Test minimizing/restoring and a second app launch.
3. Validate the bundled template without selecting a DOCX. Source IDML hash must not change. Confirm fonts/PDF preset names populate dropdowns.
4. Validate copies with no parent, no header label, missing font, insufficient margins or nonexistent PDF preset. Expect clear errors, closed validation copy and result.json.
5. Build the known report with default Economic Report preset and no TOC. Compare with the approved v1.1.1 21-page output; review all pages, table continuity, bold text, footnotes, fonts, kashidas and heading gaps.
6. Change body/heading fonts and sizes, line spacing, color, before/after spacing, margins, cover size, table font/padding/border/color and header repetition. Confirm each option changes the expected output without altering source DOCX/template.
7. Test all four presets. Export/import a custom JSON preset; invalid or unsupported values must be rejected. Confirm settings survive restart.
8. Build with and without cover and with long Persian title, subtitle, author, organization and date. Long text must either fit or report overset, never silently disappear. Check PDF metadata.
9. Enable contents with Heading 1 only, then Heading 1/2. Use enough headings for multiple TOC pages. Verify every listed number against final document page labels, with and without cover. Confirm body/table text unchanged. Manually edited output requires regenerating contents.
10. Select High Quality Print, Smallest File Size, Press Quality, installed PDF/X-4 and one custom preset using exact local names. Verify PDF properties and restoration of InDesign's previous export preferences on success and error.
11. Queue multiple files including duplicate filenames and one deliberate layout failure. Test continue-on-error both ways and Cancel Current Job during paragraph, table and pagination work. Confirm the working copy closes, later jobs do not start, and unique numbered folders, batch summary and counts remain correct.
12. Open each Results row's PDF, INDD, folder and JSON. No success notification for absent or zero-byte output. Batch-created documents close; user-open documents stay untouched.
13. Test update check on public/private repositories, no releases, offline network, stable upgrade, same version and downgrade. No report files leave the machine, no credentials are stored and no installer runs automatically.
14. Validate end-to-end with the actual InDesign 21.3 installation and run Adobe Preflight. Save logs and mark the preview tested only after this step.

For Windows smoke testing, install from a clean ZIP, confirm the installation directory and every icon/shortcut, then minimize and restore through the tray.

For InDesign acceptance, build the known DOCX with the bundled template. Verify nonempty INDD, IDML, and PDF files; success notifications; complete content; clean Preflight; one-space numbered headings; and kashidas set to None.

Retain `report-log.txt`, `result.json`, `windows-error.txt`, and `Review-Needed.indd` on failure.
