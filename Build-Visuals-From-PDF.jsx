#target indesign
// Rebuilds a PDF page-by-page in InDesign with all TEXT removed but images
// and vector graphics (fills, rule lines) kept in their original positions.
// Driven by a VISUAL_SPEC JSON (from PdfVisualExtractor) injected the same
// way ReportLayout.exe injects REPORT_CONFIG:
//   var VISUAL_SPEC = { ... };
//   <this file's source>
(function () {
    var spec = (typeof VISUAL_SPEC !== 'undefined') ? VISUAL_SPEC : null;
    var log = [];
    var MAX_ITEMS_PER_PAGE = 300; // safety cap against decorative-noise pages
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
        if (!spec) throw new Error('VISUAL_SPEC was not provided.');
        if (!spec.outputIdml) throw new Error('spec.outputIdml is required.');
        var pages = spec.pages || [];
        if (pages.length === 0) throw new Error('No pages in VISUAL_SPEC.');

        app.scriptPreferences.userInteractionLevel = UserInteractionLevels.NEVER_INTERACT;
        app.scriptPreferences.measurementUnit = MeasurementUnits.POINTS;

        var doc = app.documents.add();
        doc.documentPreferences.properties = {
            pageWidth: pages[0].width, pageHeight: pages[0].height, facingPages: false, pagesPerDocument: 1
        };

        var colorCache = {};
        function color(rgb) {
            var name = 'C_' + rgb[0] + '_' + rgb[1] + '_' + rgb[2];
            if (colorCache[name]) return colorCache[name];
            var c = doc.colors.itemByName(name);
            if (!c.isValid) c = doc.colors.add({ name: name, model: ColorModel.PROCESS, space: ColorSpace.RGB, colorValue: rgb });
            colorCache[name] = c;
            return c;
        }

        for (var pi = 0; pi < pages.length; pi++) {
            var ps = pages[pi];
            var page = (pi === 0) ? doc.pages[0] : doc.pages.add(LocationOptions.AT_END);
            if (pi > 0 && (ps.width !== pages[0].width || ps.height !== pages[0].height)) {
                page.layoutRule = LayoutRuleOptions.OFF;
                page.resize(CoordinateSpaces.INNER_COORDINATES, AnchorPoint.TOP_LEFT_ANCHOR, ResizeMethods.REPLACING_CURRENT_DIMENSIONS_WITH, [ps.width, ps.height]);
            }
            var ox = page.bounds[1], oy = page.bounds[0];
            var itemsPlaced = 0, itemsSkipped = 0;

            if (ps.background) {
                var bg = page.rectangles.add();
                bg.geometricBounds = [oy, ox, oy + ps.height, ox + ps.width];
                bg.fillColor = color(ps.background); bg.strokeWeight = 0;
            }

            var rects = ps.rects || [];
            for (var ri = 0; ri < rects.length; ri++) {
                if (itemsPlaced >= MAX_ITEMS_PER_PAGE) { itemsSkipped += (rects.length - ri); break; }
                var r = rects[ri];
                var rect = page.rectangles.add();
                rect.geometricBounds = [oy + r.top, ox + r.left, oy + r.bottom, ox + r.right];
                rect.fillColor = color(r.color); rect.strokeWeight = 0;
                itemsPlaced++;
            }

            var lines = ps.lines || [];
            for (var li = 0; li < lines.length; li++) {
                if (itemsPlaced >= MAX_ITEMS_PER_PAGE) { itemsSkipped += (lines.length - li); break; }
                var ln = lines[li];
                var gl = page.graphicLines.add();
                gl.paths.item(0).entirePath = [[ox + ln.x1, oy + ln.y1], [ox + ln.x2, oy + ln.y2]];
                gl.strokeColor = color(ln.color); gl.strokeWeight = ln.width;
                itemsPlaced++;
            }

            var images = ps.images || [];
            for (var ii = 0; ii < images.length; ii++) {
                try {
                    var im = images[ii];
                    var frame = page.rectangles.add();
                    frame.geometricBounds = [oy + im.top, ox + im.left, oy + im.bottom, ox + im.right];
                    frame.place(new File(im.file));
                    frame.fit(FitOptions.CONTENT_TO_FRAME);
                } catch (imgErr) { say('page ' + (pi + 1) + ' image ' + ii + ': ' + imgErr.message); }
            }

            say('page ' + (pi + 1) + ': ' + rects.length + ' rect(s), ' + lines.length + ' line(s), ' + images.length + ' image(s)' + (itemsSkipped > 0 ? (' -- ' + itemsSkipped + ' skipped (cap)') : ''));
        }

        doc.recompose();
        if (spec.outputIndd) doc.save(new File(spec.outputIndd));
        doc.exportFile(ExportFormat.INDESIGN_MARKUP, new File(spec.outputIdml), false);
        if (spec.outputPreviewPdf) doc.exportFile(ExportFormat.PDF_TYPE, new File(spec.outputPreviewPdf), false);
        say('OK');
        result(true, 'Rebuilt ' + pages.length + ' page(s) without text.');
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
