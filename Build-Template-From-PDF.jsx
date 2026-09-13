#target indesign
// Builds a multi-master InDesign template (.indd/.idml) from a TEMPLATE_SPEC
// JSON produced by scanning an arbitrary reference PDF page-by-page: one
// master spread per distinct page role found (Body / Exhibit / Divider /
// Sparse), plus a cover page. No text or imagery from that PDF is
// reproduced -- only generic placeholder labels and measured geometry/color.
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
        var masterSpecs = spec.masters || [];
        var coverColors = (spec.colors || {});

        var doc = app.documents.add();
        doc.documentPreferences.properties = {
            pageWidth: pageWidth, pageHeight: pageHeight, facingPages: false, pagesPerDocument: 1
        };

        var colorCache = {};
        function color(name, rgb) {
            if (colorCache[name]) return colorCache[name];
            var c = doc.colors.itemByName(name);
            if (!c.isValid) c = doc.colors.add({ name: name });
            c.properties = { model: ColorModel.PROCESS, space: ColorSpace.RGB, colorValue: rgb };
            colorCache[name] = c;
            return c;
        }
        var white = doc.swatches.itemByName('Paper');

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
        function readableTextColor(rgb) {
            var luminance = 0.299 * rgb[0] + 0.587 * rgb[1] + 0.114 * rgb[2];
            return luminance < 140 ? white : color('__ink_on_dark', [35, 31, 32]);
        }

        // ---- Cover: full-bleed gradient (or flat, if start==end) ----
        var gStart = coverColors.coverGradientStart || [25, 63, 201];
        var gEnd = coverColors.coverGradientEnd || [9, 35, 83];
        var coverStart = color('Cover Start', gStart);
        var coverEnd = color('Cover End', gEnd);
        var coverGradient = doc.gradients.itemByName('Cover Gradient');
        if (!coverGradient.isValid) coverGradient = doc.gradients.add({ name: 'Cover Gradient' });
        coverGradient.type = GradientType.LINEAR;
        coverGradient.gradientStops[0].stopColor = coverStart; coverGradient.gradientStops[0].location = 0;
        coverGradient.gradientStops[1].stopColor = coverEnd; coverGradient.gradientStops[1].location = 100;
        var coverText = readableTextColor([(gStart[0] + gEnd[0]) / 2, (gStart[1] + gEnd[1]) / 2, (gStart[2] + gEnd[2]) / 2]);

        var cover = doc.pages[0];
        cover.appliedMaster = null;
        var bg = rect(cover, 0, 0, pageHeight, pageWidth, coverGradient);
        bg.gradientFillStart = [ox(cover), oy(cover)]; bg.gradientFillAngle = -55;
        rect(cover, 40, 40, 46, 46, color('Accent Fallback', [0, 82, 204])).label = 'LOGO_PLACEHOLDER';
        text(cover, 60, 40, 100, pageWidth - 60, 'Organization Name', 'Times New Roman', 22, coverText, Justification.LEFT_ALIGN, 26);
        text(cover, pageHeight * 0.28, 40, pageHeight * 0.53, pageWidth - 52, 'Report Title\nGoes Here Over\nThree Lines', 'Times New Roman\tBold', 40, coverText, Justification.LEFT_ALIGN, 46);
        text(cover, pageHeight * 0.55, 40, pageHeight * 0.62, pageWidth - 52, 'One or two sentence subtitle describing what this report covers.', 'Arial', 13, coverText, Justification.LEFT_ALIGN, 18);
        text(cover, pageHeight * 0.8, 40, pageHeight * 0.9, 260, 'Authors\nPlaceholder Name\nPlaceholder Name\n\nEditor\nPlaceholder Name\n\nDesign\nPlaceholder Name', 'Arial', 9, coverText, Justification.LEFT_ALIGN, 13);
        text(cover, pageHeight - 45, 40, pageHeight - 30, 200, 'Month Year', 'Arial', 10, coverText);

        // ---- One master spread + one sample page per detected role ----
        var isFirstMaster = true;
        for (var mi = 0; mi < masterSpecs.length; mi++) {
            var ms = masterSpecs[mi];
            var role = ms.role || ('Style' + (mi + 1));
            var master;
            if (isFirstMaster) { master = doc.masterSpreads[0]; isFirstMaster = false; }
            else master = doc.masterSpreads.add();
            master.namePrefix = 'M' + (mi + 1);
            master.baseName = role;
            var mpage = master.pages[0];

            var mcolors = ms.colors || {};
            var accent = color(role + ' Accent', mcolors.accent || [0, 82, 204]);
            var ink = color(role + ' Ink', mcolors.ink || [35, 31, 32]);
            var background = mcolors.background || [255, 255, 255];
            var isWhiteBg = background[0] > 250 && background[1] > 250 && background[2] > 250;

            if (!isWhiteBg) rect(mpage, 0, 0, pageHeight, pageWidth, color(role + ' BG', background));

            if (role === 'Body' || role === 'Exhibit') {
                var m = ms.margins || { top: 72, bottom: 54, left: 54, right: 54 };
                mpage.marginPreferences.properties = { top: m.top, bottom: m.bottom, left: m.left, right: m.right };
                rect(mpage, m.top - 8, m.left, m.top - 7.25, pageWidth - m.right, accent);
                var footerY1 = pageHeight - m.bottom + 8, footerY2 = footerY1 + 16;
                var headerFrame = text(mpage, footerY1, m.left, footerY2, pageWidth - m.right - 40, 'Report Title Placeholder', 'Arial\tBold', 8, ink, Justification.CENTER_ALIGN);
                headerFrame.label = 'REPORT_HEADER';
                var pageNumFrame = text(mpage, footerY1, pageWidth - m.right - 36, footerY2, pageWidth - m.right, '', 'Arial\tBold', 8, ink, Justification.RIGHT_ALIGN);
                pageNumFrame.texts[0].insertionPoints[0].contents = SpecialCharacters.AUTO_PAGE_NUMBER;
            } else {
                // Divider / Sparse: no flowing text column -- just a placeholder
                // headline (and an image placeholder box if the source pages in
                // this cluster were mostly full-bleed photos/illustrations).
                var roleText = readableTextColor(background);
                if (role !== 'Sparse') {
                    text(mpage, pageHeight * 0.08, 40, pageHeight * 0.22, pageWidth - 40, role + ' Heading Placeholder', 'Times New Roman\tBold', 30, roleText, Justification.LEFT_ALIGN, 36);
                }
                if (ms.hasImage) {
                    var imgTop = role === 'Sparse' ? pageHeight * 0.06 : pageHeight * 0.28;
                    var imgBox = rect(mpage, imgTop, pageWidth * 0.1, pageHeight * 0.94, pageWidth * 0.9, white);
                    imgBox.strokeWeight = 1; imgBox.strokeColor = accent; imgBox.fillColor = white;
                    imgBox.label = 'IMAGE_PLACEHOLDER';
                    text(mpage, (imgTop + pageHeight * 0.94) / 2 - 6, pageWidth * 0.1, (imgTop + pageHeight * 0.94) / 2 + 6, pageWidth * 0.9, 'Full-bleed image placeholder', 'Arial', 10, ink, Justification.CENTER_ALIGN);
                }
            }

            var samplePage = doc.pages.add(LocationOptions.AT_END);
            samplePage.appliedMaster = master;
            if (role === 'Body' || role === 'Exhibit') {
                var m2 = ms.margins || { top: 72, bottom: 54, left: 54, right: 54 };
                var sampleFrame = text(samplePage, m2.top, m2.left, pageHeight - m2.bottom, pageWidth - m2.right,
                    role + ' Heading Placeholder\rThis is placeholder body text showing where imported report paragraphs will flow inside the measured text column for the "' + role + '" page style (seen on ' + (ms.pageCount || 1) + ' page(s) of the reference PDF).',
                    'Arial', 11, ink);
                var paras = sampleFrame.paragraphs;
                paras[0].appliedFont = app.fonts.itemByName('Times New Roman\tBold');
                paras[0].pointSize = 26; paras[0].fillColor = ink; paras[0].spaceAfter = 14;
                for (var pi = 1; pi < paras.length; pi++) { paras[pi].appliedFont = app.fonts.itemByName('Arial'); paras[pi].pointSize = 11; paras[pi].leading = 16; paras[pi].fillColor = ink; paras[pi].spaceAfter = 10; }
            }
            say(role + ': master built from ' + (ms.pageCount || 1) + ' matching page(s), representative page ' + ms.representativePage);
        }

        doc.recompose();

        if (spec.outputIndd) doc.save(new File(spec.outputIndd));
        doc.exportFile(ExportFormat.INDESIGN_MARKUP, new File(spec.outputIdml), false);
        if (spec.outputPreviewPdf) doc.exportFile(ExportFormat.PDF_TYPE, new File(spec.outputPreviewPdf), false);
        say('OK');
        result(true, 'Template built with ' + masterSpecs.length + ' page style(s).');
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
