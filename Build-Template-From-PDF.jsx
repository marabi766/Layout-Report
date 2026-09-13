#target indesign
// Builds a Cover + Body-Master InDesign template (.indd/.idml) from a
// TEMPLATE_SPEC JSON (page size, margins, colors) produced by measuring an
// arbitrary reference PDF. No text or imagery from that PDF is reproduced --
// only generic placeholder labels and measured geometry/color.
//
// Injected the same way ReportLayout.exe injects REPORT_CONFIG:
//   var TEMPLATE_SPEC = { ... };
//   <this file's source>
(function () {
    var spec = (typeof TEMPLATE_SPEC !== 'undefined') ? TEMPLATE_SPEC : null;
    var log = [];
    function say(s) { log.push(s); }
    function result(ok, message) {
        var value = { ok: ok, message: message };
        if (spec && spec.outputResult) {
            var f = new File(spec.outputResult);
            f.encoding = 'UTF-8'; f.open('w'); f.write(jsonStringify(value)); f.close();
        }
    }
    function jsonStringify(v) {
        if (v === null || v === undefined) return 'null';
        if (typeof v === 'string') return '"' + v.replace(/["\\]/g, '\\$&').replace(/\r?\n/g, '\\n') + '"';
        if (typeof v === 'number' || typeof v === 'boolean') return String(v);
        if (v instanceof Array) { var p = []; for (var i = 0; i < v.length; i++) p.push(jsonStringify(v[i])); return '[' + p.join(',') + ']'; }
        var parts = []; for (var k in v) if (v.hasOwnProperty(k)) parts.push(jsonStringify(k) + ':' + jsonStringify(v[k])); return '{' + parts.join(',') + '}';
    }

    try {
        if (!spec) throw new Error('TEMPLATE_SPEC was not provided.');
        if (!spec.outputIdml) throw new Error('spec.outputIdml is required.');

        app.scriptPreferences.userInteractionLevel = UserInteractionLevels.NEVER_INTERACT;
        app.scriptPreferences.measurementUnit = MeasurementUnits.POINTS;

        var pageWidth = spec.pageWidth || 612;
        var pageHeight = spec.pageHeight || 792;
        var m = spec.margins || { top: 72, bottom: 54, left: 54, right: 54 };
        var colorsSpec = spec.colors || {};

        var doc = app.documents.add();
        doc.documentPreferences.properties = {
            pageWidth: pageWidth, pageHeight: pageHeight, facingPages: false, pagesPerDocument: 2
        };

        function color(name, rgb) {
            var c = doc.colors.itemByName(name);
            if (!c.isValid) c = doc.colors.add({ name: name });
            c.properties = { model: ColorModel.PROCESS, space: ColorSpace.RGB, colorValue: rgb };
            return c;
        }
        var accent = color('Template Accent', colorsSpec.accent || [0, 82, 204]);
        var ink = color('Template Ink', colorsSpec.ink || [35, 31, 32]);
        var white = doc.swatches.itemByName('Paper');

        var gStart = colorsSpec.coverGradientStart || [40, 70, 160];
        var gEnd = colorsSpec.coverGradientEnd || [10, 20, 60];
        var coverStart = color('Template Cover Start', gStart);
        var coverEnd = color('Template Cover End', gEnd);
        var coverGradient = doc.gradients.itemByName('Template Cover Gradient');
        if (!coverGradient.isValid) coverGradient = doc.gradients.add({ name: 'Template Cover Gradient' });
        coverGradient.type = GradientType.LINEAR;
        coverGradient.gradientStops[0].stopColor = coverStart;
        coverGradient.gradientStops[0].location = 0;
        coverGradient.gradientStops[1].stopColor = coverEnd;
        coverGradient.gradientStops[1].location = 100;

        // Choose readable text-on-cover color from the gradient's average luminance.
        var avg = [(gStart[0] + gEnd[0]) / 2, (gStart[1] + gEnd[1]) / 2, (gStart[2] + gEnd[2]) / 2];
        var luminance = 0.299 * avg[0] + 0.587 * avg[1] + 0.114 * avg[2];
        var coverText = luminance < 140 ? white : ink;

        // Coordinates are LOCAL to the target page; converted to spread space
        // via that page's own bounds (a master page can sit on either half of
        // a 2-up spread even when facingPages is off).
        function oy(page) { return page.bounds[0]; }
        function ox(page) { return page.bounds[1]; }
        function rect(page, y1, x1, y2, x2, fill) {
            var r = page.rectangles.add();
            r.geometricBounds = [oy(page) + y1, ox(page) + x1, oy(page) + y2, ox(page) + x2];
            r.fillColor = fill; r.strokeWeight = 0;
            return r;
        }
        var enUS = null;
        try { enUS = app.languagesWithVendors.itemByName('English: USA'); if (!enUS.isValid) enUS = null; } catch (langErr) { enUS = null; }
        function text(page, y1, x1, y2, x2, contents, font, size, col, align, leading) {
            var t = page.textFrames.add();
            t.geometricBounds = [oy(page) + y1, ox(page) + x1, oy(page) + y2, ox(page) + x2];
            t.contents = contents;
            var range = t.texts[0];
            range.appliedFont = app.fonts.itemByName(font);
            range.pointSize = size;
            range.fillColor = col;
            if (enUS) try { range.appliedLanguage = enUS; } catch (applyLangErr) {}
            if (align) range.justification = align;
            if (leading) range.leading = leading;
            return t;
        }

        // ================= MASTER SPREAD (body pages) =================
        var master = doc.masterSpreads[0];
        master.namePrefix = 'D1'; master.baseName = 'Main Body';
        var mpage = master.pages[0];
        mpage.marginPreferences.properties = { top: m.top, bottom: m.bottom, left: m.left, right: m.right };

        // Generic header rule: a thin accent-colored line just above the body
        // column, spanning the same width as the text column.
        rect(mpage, m.top - 8, m.left, m.top - 7.25, pageWidth - m.right, accent);

        // Footer: REPORT_HEADER label (overwritten per report by the app) + page number.
        var footerY1 = pageHeight - m.bottom + 8;
        var footerY2 = footerY1 + 16;
        var headerFrame = text(mpage, footerY1, m.left, footerY2, pageWidth - m.right - 40,
            'Report Title Placeholder', 'Arial\tBold', 8, ink, Justification.CENTER_ALIGN);
        headerFrame.label = 'REPORT_HEADER';
        var pageNumFrame = text(mpage, footerY1, pageWidth - m.right - 36, footerY2, pageWidth - m.right,
            '', 'Arial\tBold', 8, ink, Justification.RIGHT_ALIGN);
        pageNumFrame.texts[0].insertionPoints[0].contents = SpecialCharacters.AUTO_PAGE_NUMBER;

        // ================= PAGE 1: cover =================
        var cover = doc.pages[0];
        cover.appliedMaster = null;
        var bg = rect(cover, 0, 0, pageHeight, pageWidth, coverGradient);
        bg.gradientFillStart = [ox(cover), oy(cover)]; bg.gradientFillAngle = -55;
        rect(cover, 40, 40, 46, 46, accent).label = 'LOGO_PLACEHOLDER';
        text(cover, 60, 40, 100, pageWidth - 60, 'Organization Name', 'Times New Roman', 22, coverText, Justification.LEFT_ALIGN, 26);
        text(cover, pageHeight * 0.28, 40, pageHeight * 0.53, pageWidth - 52, 'Report Title\nGoes Here Over\nThree Lines', 'Times New Roman\tBold', 40, coverText, Justification.LEFT_ALIGN, 46);
        text(cover, pageHeight * 0.55, 40, pageHeight * 0.62, pageWidth - 52, 'One or two sentence subtitle describing what this report covers.', 'Arial', 13, coverText, Justification.LEFT_ALIGN, 18);
        text(cover, pageHeight * 0.8, 40, pageHeight * 0.9, 260, 'Authors\nPlaceholder Name\nPlaceholder Name\n\nEditor\nPlaceholder Name\n\nDesign\nPlaceholder Name', 'Arial', 9, coverText, Justification.LEFT_ALIGN, 13);
        text(cover, pageHeight - 45, 40, pageHeight - 30, 200, 'Month Year', 'Arial', 10, coverText);

        // ================= PAGE 2: body sample =================
        var body = doc.pages[1];
        body.appliedMaster = master;
        var bodyFrame = text(body, m.top, m.left, pageHeight - m.bottom, pageWidth - m.right,
            'Heading Placeholder\rThis is placeholder body text showing where imported report paragraphs will flow inside the measured text column. \u2014 Bold lead-in sentences can introduce bullet-style points, followed by regular continuation text describing the point in more detail.\rA second paragraph continues here to show normal paragraph spacing and the body typeface at its working size.',
            'Arial', 11, ink);
        var paras = bodyFrame.paragraphs;
        paras[0].appliedFont = app.fonts.itemByName('Times New Roman\tBold');
        paras[0].pointSize = 26; paras[0].fillColor = ink; paras[0].spaceAfter = 14;
        for (var pi = 1; pi < paras.length; pi++) {
            paras[pi].appliedFont = app.fonts.itemByName('Arial');
            paras[pi].pointSize = 11; paras[pi].leading = 16; paras[pi].fillColor = ink; paras[pi].spaceAfter = 10;
        }

        doc.recompose();

        if (spec.outputIndd) doc.save(new File(spec.outputIndd));
        doc.exportFile(ExportFormat.INDESIGN_MARKUP, new File(spec.outputIdml), false);
        if (spec.outputPreviewPdf) doc.exportFile(ExportFormat.PDF_TYPE, new File(spec.outputPreviewPdf), false);
        say('OK');
        result(true, 'Template built.');
    } catch (e) {
        say('ERROR: ' + e.message + ' (line ' + e.line + ')');
        result(false, e.message);
    } finally {
        if (spec && spec.outputLog) {
            var f = new File(spec.outputLog);
            f.encoding = 'UTF-8'; f.open('w'); f.write(log.join('\r\n')); f.close();
        }
    }
})();
