# Testing

Run `node tests/flow-tests.cjs` for portable logic tests.

For Windows smoke testing, install from a clean ZIP, confirm the installation directory and every icon/shortcut, then minimize and restore through the tray.

For InDesign acceptance, build the known DOCX with the bundled template. Verify nonempty INDD, IDML, and PDF files; success notifications; complete content; clean Preflight; one-space numbered headings; and kashidas set to None.

Retain `report-log.txt`, `result.json`, `windows-error.txt`, and `Review-Needed.indd` on failure.
