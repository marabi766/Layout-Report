using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.IO;

namespace PdfTemplate {

public enum PageArchetype { Body, Divider, Exhibit, Sparse }

public sealed class PageFeatures {
    public int PageNumber;
    public double ImageCoverage;
    public int[] BackgroundColor;
    public double BackgroundLuminance;
    public int TextRunCount;
    public double AvgFontSize, MaxFontSize;
    public bool HasSmallCaptionBand;
    public PageArchetype Archetype;
}

// Turns raw PDF measurements into the same TEMPLATE_SPEC shape the
// build-template-from-spec.jsx script expects: page size, margins and a
// small color palette. No page CONTENT (text/images) is carried over.
public static class PdfTemplateSpecBuilder {
    const double SmallCaptionSize = 11;

    // Measures margins + ink/accent color from one page's own text runs.
    static void AnalyzeTextMetrics(PdfDocument doc, PVal page, out Dictionary<string, object> margins, out int[] ink, out int[] accent, out string warning) {
        double[] box = doc.GetMediaBox(page);
        double pageWidth = box[2], pageHeight = box[3];
        byte[] content = doc.GetPageContent(page);
        ContentAnalysis analysis = PdfContentAnalyzer.Analyze(content, doc, doc.GetResources(page));

        double headerCut = pageHeight * 0.15;
        double footerCut = pageHeight * 0.88;
        var header = new List<TextRun>(); var footer = new List<TextRun>(); var body = new List<TextRun>();
        foreach (var r in analysis.TextRuns) {
            double topY = pageHeight - (r.Y + 0.75 * r.Size);
            double bottomY = pageHeight - (r.Y - 0.2 * r.Size);
            bool small = r.Size <= SmallCaptionSize && r.Size > 0.5;
            if (bottomY < headerCut && small) header.Add(r);
            else if (topY > footerCut && small) footer.Add(r);
            else body.Add(r);
        }

        double marginTop, marginBottom, marginLeft, marginRight;
        warning = null;

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
            if (marginRight < pageWidth * 0.02 || marginRight > pageWidth * 0.4) marginRight = Math.Min(marginLeft, pageWidth * 0.15);

            marginTop = Math.Round(marginTop, 1);
            marginBottom = Math.Round(marginBottom, 1);
            marginLeft = Math.Round(marginLeft, 1);
            marginRight = Math.Round(marginRight, 1);
        }

        margins = new Dictionary<string, object> { { "top", marginTop }, { "bottom", marginBottom }, { "left", marginLeft }, { "right", marginRight } };
        ink = ModeColor(body) ?? new[] { 35, 31, 32 };
        accent = ModeColor(header) ?? ModeColor(footer) ?? new[] { 0, 82, 204 };
    }

    // Prefers an actual gradient/shading; falls back to the largest filled
    // rectangle or full-bleed image's flat/sampled color; else a sane default.
    static void AnalyzeCoverColors(PdfDocument doc, PVal coverPage, out int[] gStart, out int[] gEnd) {
        byte[] coverContent = doc.GetPageContent(coverPage);
        ContentAnalysis coverAnalysis = PdfContentAnalyzer.Analyze(coverContent, doc, doc.GetResources(coverPage));
        gStart = coverAnalysis.GradientStart;
        gEnd = coverAnalysis.GradientEnd;
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
    }

    // ===================== Multi-archetype template spec =====================
    // Builds one master-spread spec per distinct page role found across the
    // WHOLE document (Body / Exhibit / Divider / Sparse), instead of a single
    // Cover + Body-Master pair. Each role's representative page is the most
    // "typical" member of its cluster (median text density), so an outlier
    // page does not skew the measurement.
    public static Dictionary<string, object> BuildMultiSpec(string pdfPath, int coverPageNumber) {
        var doc = PdfDocument.Load(pdfPath);
        var pages = doc.GetPages();
        if (pages.Count == 0) throw new Exception("No pages found in this PDF.");
        if (coverPageNumber < 1 || coverPageNumber > pages.Count) throw new Exception("Cover page must be between 1 and " + pages.Count + ".");

        var features = ScanArchetypes(pdfPath);
        double[] box = doc.GetMediaBox(pages[coverPageNumber - 1]);
        int[] gStart, gEnd;
        AnalyzeCoverColors(doc, pages[coverPageNumber - 1], out gStart, out gEnd);

        var masters = new List<object>();
        foreach (var role in new[] { PageArchetype.Body, PageArchetype.Exhibit, PageArchetype.Divider, PageArchetype.Sparse }) {
            var members = features.Where(f => f.Archetype == role).OrderBy(f => f.TextRunCount).ToList();
            if (members.Count == 0) continue;
            var representative = members[members.Count / 2]; // median by text density
            PVal repPage = pages[representative.PageNumber - 1];

            if (role == PageArchetype.Body || role == PageArchetype.Exhibit) {
                Dictionary<string, object> margins; int[] ink, accent; string warning;
                AnalyzeTextMetrics(doc, repPage, out margins, out ink, out accent, out warning);
                var masterEntry = new Dictionary<string, object> {
                    { "role", role.ToString() }, { "pageCount", members.Count }, { "representativePage", representative.PageNumber },
                    { "margins", margins },
                    { "colors", new Dictionary<string, object> { { "ink", ink }, { "accent", accent }, { "background", representative.BackgroundColor } } },
                };
                if (warning != null) masterEntry["warning"] = warning;
                masters.Add(masterEntry);
            } else {
                masters.Add(new Dictionary<string, object> {
                    { "role", role.ToString() }, { "pageCount", members.Count }, { "representativePage", representative.PageNumber },
                    { "colors", new Dictionary<string, object> { { "background", representative.BackgroundColor } } },
                    { "hasImage", representative.ImageCoverage > 0.3 },
                });
            }
        }

        return new Dictionary<string, object> {
            { "pageWidth", Math.Round(box[2], 2) }, { "pageHeight", Math.Round(box[3], 2) },
            { "colors", new Dictionary<string, object> { { "coverGradientStart", gStart }, { "coverGradientEnd", gEnd } } },
            { "masters", masters },
            { "sourcePdf", pdfPath }, { "coverPage", coverPageNumber }, { "totalPages", pages.Count },
        };
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

    // ===================== Multi-archetype page scan =====================
    // Classifies EVERY page into a small set of recurring visual roles so a
    // template can offer more than one master spread (a text-body page looks
    // very different from a full-bleed divider or a chart/exhibit page).
    // Deliberately rule-based (not statistical clustering) so the result is
    // predictable and explainable rather than a black box.
    public static List<PageFeatures> ScanArchetypes(string pdfPath) {
        var doc = PdfDocument.Load(pdfPath);
        var pages = doc.GetPages();
        var list = new List<PageFeatures>();
        for (int i = 0; i < pages.Count; i++) {
            PVal page = pages[i];
            double[] box = doc.GetMediaBox(page);
            double pageWidth = box[2], pageHeight = box[3];
            double pageArea = Math.Max(1, pageWidth * pageHeight);
            byte[] content;
            ContentAnalysis analysis;
            try {
                content = doc.GetPageContent(page);
                analysis = PdfContentAnalyzer.Analyze(content, doc, doc.GetResources(page));
            } catch { continue; }

            double imageArea = analysis.Images.Sum(im => Math.Abs((im.X1 - im.X0) * (im.Y1 - im.Y0)));
            double imageCoverage = Math.Min(1.0, imageArea / pageArea);

            var bigRect = analysis.FillRects
                .Where(r => Math.Abs((r.X1 - r.X0) * (r.Y1 - r.Y0)) > pageArea * 0.5)
                .OrderByDescending(r => Math.Abs((r.X1 - r.X0) * (r.Y1 - r.Y0)))
                .FirstOrDefault();
            int[] bg = new[] { 255, 255, 255 };
            if (bigRect != null) bg = bigRect.Color;
            else if (imageCoverage > 0.5) {
                var biggestImage = analysis.Images.OrderByDescending(im => Math.Abs((im.X1 - im.X0) * (im.Y1 - im.Y0))).First();
                int[] a, b;
                if (TrySampleImageCorners(doc, biggestImage.ImageObj, out a, out b)) bg = Avg(a, b);
            }
            double luminance = 0.299 * bg[0] + 0.587 * bg[1] + 0.114 * bg[2];

            int textCount = analysis.TextRuns.Count;
            double avgSize = textCount > 0 ? analysis.TextRuns.Average(t => t.Size) : 0;
            double maxSize = textCount > 0 ? analysis.TextRuns.Max(t => t.Size) : 0;
            bool hasCaptionBand = analysis.TextRuns.Any(t => t.Size <= SmallCaptionSize && t.Size > 0.5 &&
                (pageHeight - t.Y < pageHeight * 0.15 || pageHeight - t.Y > pageHeight * 0.85));

            var f = new PageFeatures {
                PageNumber = i + 1, ImageCoverage = imageCoverage, BackgroundColor = bg, BackgroundLuminance = luminance,
                TextRunCount = textCount, AvgFontSize = avgSize, MaxFontSize = maxSize, HasSmallCaptionBand = hasCaptionBand,
            };
            f.Archetype = Classify(f);
            list.Add(f);
        }
        return list;
    }

    static int[] Avg(int[] a, int[] b) { return new[] { (a[0] + b[0]) / 2, (a[1] + b[1]) / 2, (a[2] + b[2]) / 2 }; }

    static PageArchetype Classify(PageFeatures f) {
        if (f.TextRunCount == 0) return PageArchetype.Sparse;
        if (f.ImageCoverage > 0.55 && f.TextRunCount < 20) return PageArchetype.Divider;
        if (f.BackgroundLuminance < 235 && f.TextRunCount < 25 && f.MaxFontSize > 20) return PageArchetype.Divider;
        if (f.BackgroundLuminance < 250 && f.BackgroundLuminance >= 200 && f.HasSmallCaptionBand && f.TextRunCount >= 8) return PageArchetype.Exhibit;
        return PageArchetype.Body;
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
