# Report Layout v1.1.1

This release replaces the previous v1.1.1 package (zip + Start.vbs) with a single-file MSI installer, and adds a sign-in gate and application branding.

## Added

- Single MSI installer (`ReportLayout-1.1.1-Setup.msi`) — installs to Program Files, creates Start Menu and Desktop shortcuts. Replaces the zip + `Start.vbs` distribution.
- Sign-in prompt at startup. Access is gated by a password whose SHA-256 hash is checked at launch; the plaintext password is never stored in the app.
- Version number and copyright notice ("Copyright © 2026 Mohammad Arabi. All rights reserved.") shown at the bottom of the main window.

## Fixed

- Read and restore PDF export preferences from the InDesign application object.
- Prevent the export failure caused by requesting `pdfExportPreferences` from the document.

## Package

Download `ReportLayout-1.1.1-Setup.msi` and run it.
