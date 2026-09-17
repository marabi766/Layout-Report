#target indesign
// Threads the main text frame of each page in a range, in page order, so an
// already-built InDesign document (e.g. one assembled by combining several
// separately-built documents/pages) behaves like a single flowing story
// instead of disconnected per-page frames. Only the FIRST text frame found
// on each page is threaded; pages with no text frame are skipped and noted.
// Nothing is deleted and no content is moved -- this only sets the logical
// next/previous link between frames that are already there.
//
// Injected the same way other tools in this app inject their spec:
//   var THREAD_SPEC = { inddPath: "...", startPage: 2, endPage: 25 };
//   <this file's source>
(function () {
    var spec = (typeof THREAD_SPEC !== 'undefined') ? THREAD_SPEC : null;
    var log = [];
    function say(s) { log.push(s); }
    function jsonStringify(v) {
        if (v === null || v === undefined) return 'null';
        if (typeof v === 'string') return '"' + v.replace(/["\\]/g, '\\$&').replace(/\r?\n/g, '\\n') + '"';
        if (typeof v === 'number' || typeof v === 'boolean') return String(v);
        if (v instanceof Array) { var p = []; for (var i = 0; i < v.length; i++) p.push(jsonStringify(v[i])); return '[' + p.join(',') + ']'; }
        var parts = []; for (var k in v) if (v.hasOwnProperty(k)) parts.push(jsonStringify(k) + ':' + jsonStringify(v[k])); return '{' + parts.join(',') + '}';
    }
    function result(ok, message, extra) {
        var value = { ok: ok, message: message };
        if (extra) for (var k in extra) value[k] = extra[k];
        if (spec && spec.outputResult) {
            var f = new File(spec.outputResult);
            f.encoding = 'UTF-8'; f.open('w'); f.write(jsonStringify(value)); f.close();
        }
    }

    try {
        if (!spec) throw new Error('THREAD_SPEC was not provided.');
        app.scriptPreferences.userInteractionLevel = UserInteractionLevels.NEVER_INTERACT;

        var doc = null;
        if (spec.inddPath) {
            for (var i = 0; i < app.documents.length; i++) {
                if (app.documents[i].fullName.fsName === spec.inddPath) { doc = app.documents[i]; break; }
            }
            if (!doc) doc = app.open(new File(spec.inddPath));
        } else {
            if (app.documents.length === 0) throw new Error('No document is open, and no file path was given.');
            doc = app.activeDocument;
        }

        var startPage = Math.max(1, spec.startPage || 1);
        var endPage = Math.min(doc.pages.length, spec.endPage || doc.pages.length);
        if (startPage >= endPage) throw new Error('Start page must be before end page (got ' + startPage + '-' + endPage + ', document has ' + doc.pages.length + ' page(s)).');

        var frames = [], skipped = [];
        for (var p = startPage; p <= endPage; p++) {
            var page = doc.pages[p - 1];
            var items = page.allPageItems;
            var frame = null;
            for (var ii = 0; ii < items.length; ii++) {
                if (items[ii] instanceof TextFrame) { frame = items[ii]; break; }
            }
            if (frame) frames.push({ page: p, frame: frame });
            else skipped.push(p);
        }
        if (frames.length < 2) throw new Error('Found ' + frames.length + ' page(s) with a text frame in that range -- need at least 2 to thread.');

        var links = [], failedLinks = [];
        for (var t = 0; t < frames.length - 1; t++) {
            var already = false;
            try { already = frames[t].frame.nextTextFrame != null && frames[t].frame.nextTextFrame.id === frames[t + 1].frame.id; } catch (chkErr) {}
            if (!already) {
                try { frames[t].frame.nextTextFrame = frames[t + 1].frame; } catch (linkErr) {
                    failedLinks.push(frames[t].page + '->' + (frames[t + 1].page) + ': ' + linkErr.message);
                    continue;
                }
            }
            links.push(frames[t].page + '->' + frames[t + 1].page);
        }
        say('Threaded ' + links.length + ' link(s): ' + links.join(', '));
        if (failedLinks.length) say('Failed link(s): ' + failedLinks.join(' | '));
        if (skipped.length) say('Skipped (no text frame found): ' + skipped.join(', '));

        doc.save();
        say('OK');
        result(true, 'Threaded ' + frames.length + ' page(s) from page ' + startPage + ' to ' + endPage + '.', { framesLinked: frames.length, skippedPages: skipped, failedLinks: failedLinks.length });
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
