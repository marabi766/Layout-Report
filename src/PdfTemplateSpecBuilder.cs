using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.IO;

namespace PdfTemplate {

// Turns raw PDF measurements into the same TEMPLATE_SPEC shape the
// build-template-from-spec.jsx script expects: page size, margins and a
// small color palette. No page CONTENT (text/images) is carried over.
public static class PdfTemplateSpecBuilder {
    const double SmallCaptionSize = 11;

    public static Dictionary<string, object> BuildSpec(string pdfPath, int coverPageNumber, int bodyPageNumber) {
        var doc = PdfDocument.Load(pdfPath);
        var pages = doc.GetPages();
        if (pages.Count == 0) throw new Exception("No pages found in this PDF.");
        if (bodyPageNumber < 1 || bodyPageNumber > pages.Count) throw new Exception("Body page must be between 1 and " + pages.Count + ".");
        if (coverPageNumber < 1 || coverPageNumber > pages.Count) throw new Exception("Cover page must be between 1 and " + pages.Count + ".");

        PVal bodyPage = pages[bodyPageNumber - 1];
        double[] box = doc.GetMediaBox(bodyPage);
        double pageWidth = box[2], pageHeight = box[3];

        byte[] bodyContent = doc.GetPageContent(bodyPage);
        PVal bodyResources = doc.GetResources(bodyPage);
        ContentAnalysis bodyAnalysis = PdfContentAnalyzer.Analyze(bodyContent, doc, bodyResources);

        double headerCut = pageHeight * 0.15;
        double footerCut = pageHeight * 0.88;
        var header = new List<TextRun>(); var footer = new List<TextRun>(); var body = new List<TextRun>();
        foreach (var r in bodyAnalysis.TextRuns) {
            double topY = pageHeight - (r.Y + 0.75 * r.Size);
            double bottomY = pageHeight - (r.Y - 0.2 * r.Size);
            bool small = r.Size <= SmallCaptionSize && r.Size > 0.5;
            if (bottomY < headerCut && small) header.Add(r);
            else if (topY > footerCut && small) footer.Add(r);
            else body.Add(r);
        }

        double marginTop, marginBottom, marginLeft, marginRight;
        string warning = null;

        if (body.Count == 0) {
            marginTop = Math.Round(pageHeight * 0.12, 1);
            marginBottom = Math.Round(pageHeight * 0.08, 1);
            marginLeft = Math.Round(pageWidth * 0.12, 1);
            marginRight = Math.Round(pageWidth * 0.08, 1);
            warning = "No body text detected on the chosen page; used proportional default margins.";
        } else {
            if (header.Count > 0) {
                double headerBottom = header.Max(h => pageHeight - (h.Y - 0.2 * h.Size));
                marginTop = headerBottom + 6;
            } else {
                marginTop = body.Min(w => pageHeight - (w.Y + 0.75 * w.Size));
            }

            if (footer.Count > 0) {
                double footerTop = footer.Min(f => pageHeight - (f.Y + 0.75 * f.Size));
                marginBottom = pageHeight - (footerTop - 6);
            } else {
                double bodyBottom = body.Max(w => pageHeight - (w.Y - 0.2 * w.Size));
                marginBottom = pageHeight - bodyBottom;
            }

            marginLeft = ModeRound(body.Select(w => w.X).ToList(), 2);
            double[] rightEdges = body.Select(w => w.X + w.Chars * w.Size * 0.5).ToArray();
            marginRight = pageWidth - Percentile(rightEdges, 0.90);
            // The character-width estimate above has no real font metrics behind
            // it, so a stray long line can push this past the page edge or
            // negative. Fall back to mirroring the left margin when that happens.
            if (marginRight < pageWidth * 0.02 || marginRight > pageWidth * 0.4) marginRight = Math.Min(marginLeft, pageWidth * 0.15);

            marginTop = Math.Round(marginTop, 1);
            marginBottom = Math.Round(marginBottom, 1);
            marginLeft = Math.Round(marginLeft, 1);
            marginRight = Math.Round(marginRight, 1);
        }

        int[] ink = ModeColor(body) ?? new[] { 35, 31, 32 };
        int[] accent = ModeColor(header) ?? ModeColor(footer) ?? new[] { 0, 82, 204 };

        // Cover page: prefer an actual gradient/shading; otherwise fall back to
        // the largest filled rectangle's flat color; otherwise a sane default.
        PVal coverPage = pages[coverPageNumber - 1];
        byte[] coverContent = doc.GetPageContent(coverPage);
        PVal coverResources = doc.GetResources(coverPage);
        ContentAnalysis coverAnalysis = PdfContentAnalyzer.Analyze(coverContent, doc, coverResources);
        int[] gStart = coverAnalysis.GradientStart;
        int[] gEnd = coverAnalysis.GradientEnd;
        double[] coverBox = doc.GetMediaBox(coverPage);
        double coverArea = coverBox[2] * coverBox[3];
        if (gStart == null || gEnd == null) {
            var biggestImage = coverAnalysis.Images
                .Where(im => Math.Abs((im.X1 - im.X0) * (im.Y1 - im.Y0)) > coverArea * 0.5)
                .OrderByDescending(im => Math.Abs((im.X1 - im.X0) * (im.Y1 - im.Y0)))
                .FirstOrDefault();
            if (biggestImage != null) {
                int[] a, b;
                if (TrySampleImageCorners(doc, biggestImage.ImageObj, out a, out b)) { gStart = a; gEnd = b; }
            }
        }
        if (gStart == null || gEnd == null) {
            var biggestRect = coverAnalysis.FillRects.OrderByDescending(r => Math.Abs((r.X1 - r.X0) * (r.Y1 - r.Y0))).FirstOrDefault();
            if (biggestRect != null && Math.Abs((biggestRect.X1 - biggestRect.X0) * (biggestRect.Y1 - biggestRect.Y0)) > coverArea * 0.3) {
                gStart = biggestRect.Color; gEnd = biggestRect.Color;
            }
        }
        gStart = gStart ?? new[] { 25, 63, 201 };
        gEnd = gEnd ?? new[] { 9, 35, 83 };

        var spec = new Dictionary<string, object> {
            { "pageWidth", Math.Round(pageWidth, 2) },
            { "pageHeight", Math.Round(pageHeight, 2) },
            { "margins", new Dictionary<string, object> {
                { "top", marginTop }, { "bottom", marginBottom }, { "left", marginLeft }, { "right", marginRight }
            } },
            { "colors", new Dictionary<string, object> {
                { "ink", ink }, { "accent", accent },
                { "coverGradientStart", gStart }, { "coverGradientEnd", gEnd }
            } },
            { "sourcePdf", pdfPath }, { "coverPage", coverPageNumber }, { "bodyPage", bodyPageNumber },
        };
        if (warning != null) spec["warning"] = warning;
        return spec;
    }

    static double ModeRound(List<double> values, double bucket) {
        if (values.Count == 0) return 0;
        var counts = new Dictionary<double, int>();
        foreach (var v in values) {
            double key = Math.Round(v / bucket) * bucket;
            counts[key] = counts.ContainsKey(key) ? counts[key] + 1 : 1;
        }
        return counts.OrderByDescending(kv => kv.Value).First().Key;
    }

    static double Percentile(double[] values, double p) {
        if (values.Length == 0) return 0;
        var s = values.OrderBy(v => v).ToArray();
        double k = (s.Length - 1) * p;
        int f = (int)k, c = Math.Min(f + 1, s.Length - 1);
        if (f == c) return s[f];
        return s[f] + (s[c] - s[f]) * (k - f);
    }

    // Samples a placed image's top-left and bottom-right corners as a cheap
    // stand-in for "gradient start/end" when the cover background turns out to
    // be a photo/illustration rather than a vector shading. DCTDecode (JPEG)
    // bytes are a complete JPEG file as-is, decodable with the .NET Framework's
    // built-in GDI+ image codec -- no extra library needed.
    static bool TrySampleImageCorners(PdfDocument doc, PVal imageObj, out int[] start, out int[] end) {
        start = null; end = null;
        PVal filter = doc.Get(imageObj, "Filter");
        string filterName = filter != null ? (filter.Kind == PKind.Array && filter.Items.Count > 0 ? doc.Resolve(filter.Items[0]).AsName() : filter.AsName()) : null;
        if (filterName != "DCTDecode") return false;
        try {
            using (var ms = new MemoryStream(imageObj.StreamRaw))
            using (var bmp = new Bitmap(ms)) {
                start = AveragePatch(bmp, 0, 0);
                end = AveragePatch(bmp, bmp.Width - 1, bmp.Height - 1);
                return true;
            }
        } catch { return false; }
    }

    static int[] AveragePatch(Bitmap bmp, int cornerX, int cornerY) {
        int size = Math.Max(2, Math.Min(bmp.Width, bmp.Height) / 40);
        int x0 = Math.Max(0, Math.Min(cornerX, bmp.Width - 1) - (cornerX == 0 ? 0 : size));
        int y0 = Math.Max(0, Math.Min(cornerY, bmp.Height - 1) - (cornerY == 0 ? 0 : size));
        int x1 = Math.Min(bmp.Width, x0 + size + 1);
        int y1 = Math.Min(bmp.Height, y0 + size + 1);
        long r = 0, g = 0, b = 0; int n = 0;
        for (int y = y0; y < y1; y++) for (int x = x0; x < x1; x++) { var c = bmp.GetPixel(x, y); r += c.R; g += c.G; b += c.B; n++; }
        if (n == 0) n = 1;
        return new[] { (int)(r / n), (int)(g / n), (int)(b / n) };
    }

    static int[] ModeColor(List<TextRun> runs) {
        if (runs == null || runs.Count == 0) return null;
        var counts = new Dictionary<string, int>();
        var byKey = new Dictionary<string, int[]>();
        foreach (var r in runs) {
            if (r.Color == null) continue;
            string key = r.Color[0] + "," + r.Color[1] + "," + r.Color[2];
            counts[key] = counts.ContainsKey(key) ? counts[key] + 1 : 1;
            byKey[key] = r.Color;
        }
        if (counts.Count == 0) return null;
        string best = counts.OrderByDescending(kv => kv.Value).First().Key;
        return byKey[best];
    }
}
}
