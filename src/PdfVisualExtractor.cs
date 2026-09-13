using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PdfTemplate {

// Strips all TEXT from a PDF while keeping its images and vector graphics
// (fills, rules/borders) in their original positions, page by page, so the
// result can be rebuilt in InDesign as a text-free visual starting point.
public static class PdfVisualExtractor {
    public static Dictionary<string, object> ExtractAll(string pdfPath, string imagesOutDir, Action<int, int> onProgress = null) {
        var doc = PdfDocument.Load(pdfPath);
        var pages = doc.GetPages();
        if (pages.Count == 0) throw new Exception("No pages found in this PDF.");
        Directory.CreateDirectory(imagesOutDir);

        var pageSpecs = new List<object>();
        int imgCounter = 0, exportedImages = 0, skippedImages = 0;

        for (int i = 0; i < pages.Count; i++) {
            PVal page = pages[i];
            double[] box = doc.GetMediaBox(page);
            double pageWidth = box[2], pageHeight = box[3];
            byte[] content;
            ContentAnalysis analysis;
            try {
                content = doc.GetPageContent(page);
                analysis = PdfContentAnalyzer.Analyze(content, doc, doc.GetResources(page));
            } catch {
                pageSpecs.Add(new Dictionary<string, object> { { "width", pageWidth }, { "height", pageHeight }, { "images", new List<object>() }, { "rects", new List<object>() }, { "lines", new List<object>() } });
                if (onProgress != null) onProgress(i + 1, pages.Count);
                continue;
            }

            var images = new List<object>();
            foreach (var im in analysis.Images) {
                imgCounter++;
                string basePath = Path.Combine(imagesOutDir, "img" + imgCounter.ToString("D4"));
                string ext;
                if (doc.TryExportImage(im.ImageObj, basePath, out ext)) {
                    exportedImages++;
                    images.Add(new Dictionary<string, object> {
                        { "file", basePath + "." + ext },
                        // Convert PDF's bottom-up Y to InDesign's top-down page-local Y.
                        { "top", pageHeight - im.Y1 }, { "left", im.X0 },
                        { "bottom", pageHeight - im.Y0 }, { "right", im.X1 },
                    });
                } else skippedImages++;
            }

            var rects = analysis.FillRects
                .Where(r => (r.X1 - r.X0) * (r.Y1 - r.Y0) < pageWidth * pageHeight * 0.98) // drop full-page background fills (handled as page background instead)
                .Select(r => (object)new Dictionary<string, object> {
                    { "top", pageHeight - r.Y1 }, { "left", r.X0 }, { "bottom", pageHeight - r.Y0 }, { "right", r.X1 }, { "color", r.Color },
                }).ToList();
            var fullPageBg = analysis.FillRects.FirstOrDefault(r => (r.X1 - r.X0) * (r.Y1 - r.Y0) >= pageWidth * pageHeight * 0.98);

            var lines = analysis.Lines.Select(l => (object)new Dictionary<string, object> {
                { "y1", pageHeight - l.Y0 }, { "x1", l.X0 }, { "y2", pageHeight - l.Y1 }, { "x2", l.X1 }, { "color", l.Color }, { "width", Math.Round(l.Width, 2) },
            }).ToList();

            var pageSpec = new Dictionary<string, object> {
                { "width", Math.Round(pageWidth, 2) }, { "height", Math.Round(pageHeight, 2) },
                { "images", images }, { "rects", rects }, { "lines", lines },
            };
            if (fullPageBg != null) pageSpec["background"] = fullPageBg.Color;
            pageSpecs.Add(pageSpec);

            if (onProgress != null) onProgress(i + 1, pages.Count);
        }

        return new Dictionary<string, object> {
            { "pages", pageSpecs }, { "totalPages", pages.Count },
            { "exportedImages", exportedImages }, { "skippedImages", skippedImages },
            { "sourcePdf", pdfPath },
        };
    }
}
}
