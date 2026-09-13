using System;
using System.Collections.Generic;
using System.Text;

namespace PdfTemplate {

public struct Mat {
    public double a, b, c, d, e, f;
    public static Mat Identity { get { return new Mat { a = 1, d = 1 }; } }
    public static Mat Make(double a, double b, double c, double d, double e, double f) {
        return new Mat { a = a, b = b, c = c, d = d, e = e, f = f };
    }
    // Result applies X first, then Y: Result(p) = Y(X(p))
    public static Mat Concat(Mat x, Mat y) {
        return new Mat {
            a = x.a * y.a + x.b * y.c,
            b = x.a * y.b + x.b * y.d,
            c = x.c * y.a + x.d * y.c,
            d = x.c * y.b + x.d * y.d,
            e = x.e * y.a + x.f * y.c + y.e,
            f = x.e * y.b + x.f * y.d + y.f,
        };
    }
    public double TX(double x, double y) { return a * x + c * y + e; }
    public double TY(double x, double y) { return b * x + d * y + f; }
    public double Scale() { double det = a * d - b * c; return Math.Sqrt(Math.Abs(det)); }
}

public sealed class TextRun {
    public double X, Y; // baseline start, in page space (PDF units: origin bottom-left)
    public double Size; // effective font size in page space
    public int Chars;   // approximate character count (for a rough width estimate)
    public int[] Color; // RGB 0-255
}

public sealed class FillRect {
    public double X0, Y0, X1, Y1;
    public int[] Color;
}

public sealed class Line {
    public double X0, Y0, X1, Y1;
    public int[] Color;
    public double Width; // page-space stroke width
}

public sealed class ImagePlacement {
    public double X0, Y0, X1, Y1;
    public PVal ImageObj;
}

public sealed class ContentAnalysis {
    public List<TextRun> TextRuns = new List<TextRun>();
    public List<FillRect> FillRects = new List<FillRect>();
    public List<Line> Lines = new List<Line>();
    public List<ImagePlacement> Images = new List<ImagePlacement>();
    public int[] GradientStart, GradientEnd; // null if no shading found
}

public static class PdfContentAnalyzer {
    class GState {
        public Mat Ctm = Mat.Identity;
        public int[] FillColor = new int[] { 0, 0, 0 };
        public int[] StrokeColor = new int[] { 0, 0, 0 };
        public double LineWidth = 1;
    }

    // A "subpath" is a run of points already transformed into page space at
    // the moment each construction operator ran (re-using the CTM active at
    // that time; content streams essentially never change the CTM mid-path,
    // so this is safe in practice). Curves are flattened to their endpoint --
    // acceptable for report-style rules/borders, which are almost always
    // straight lines and axis-aligned rectangles.
    class Subpath { public List<double[]> Points = new List<double[]>(); public bool IsRect; }

    public static ContentAnalysis Analyze(byte[] content, PdfDocument doc, PVal resources) {
        var result = new ContentAnalysis();
        var stack = new Stack<GState>();
        var gs = new GState();
        var operands = new List<PVal>();
        Mat tm = Mat.Identity, tlm = Mat.Identity;
        double fontSize = 0;
        var path = new List<Subpath>();
        double curX = 0, curY = 0, startX = 0, startY = 0;

        var lex = new PdfLexer(content, 0);
        while (lex.Pos < lex.Length) {
            lex.SkipWhite();
            if (lex.Pos >= lex.Length) break;
            PVal v;
            try { v = lex.ReadValue(); } catch { break; }
            if (v.Kind != PKind.Operator) { operands.Add(v); continue; }

            string op = v.Text;
            switch (op) {
                case "q": stack.Push(Clone(gs)); break;
                case "Q": if (stack.Count > 0) gs = stack.Pop(); break;
                case "cm":
                    if (operands.Count >= 6) {
                        Mat m = Mat.Make(N(operands, 0), N(operands, 1), N(operands, 2), N(operands, 3), N(operands, 4), N(operands, 5));
                        gs.Ctm = Mat.Concat(m, gs.Ctm);
                    }
                    break;
                case "w": if (operands.Count >= 1) gs.LineWidth = N(operands, 0); break;
                case "g": if (operands.Count >= 1) { int gv = (int)Math.Round(N(operands, 0) * 255); gs.FillColor = new[] { gv, gv, gv }; } break;
                case "G": if (operands.Count >= 1) { int gv = (int)Math.Round(N(operands, 0) * 255); gs.StrokeColor = new[] { gv, gv, gv }; } break;
                case "rg":
                    if (operands.Count >= 3) gs.FillColor = new[] { (int)Math.Round(N(operands, 0) * 255), (int)Math.Round(N(operands, 1) * 255), (int)Math.Round(N(operands, 2) * 255) };
                    break;
                case "RG":
                    if (operands.Count >= 3) gs.StrokeColor = new[] { (int)Math.Round(N(operands, 0) * 255), (int)Math.Round(N(operands, 1) * 255), (int)Math.Round(N(operands, 2) * 255) };
                    break;
                case "k":
                    if (operands.Count >= 4) gs.FillColor = CmykToRgb(operands);
                    break;
                case "K":
                    if (operands.Count >= 4) gs.StrokeColor = CmykToRgb(operands);
                    break;
                case "scn": case "SCN":
                    if (operands.Count >= 3) {
                        int[] c = { (int)Math.Round(N(operands, 0) * 255), (int)Math.Round(N(operands, 1) * 255), (int)Math.Round(N(operands, 2) * 255) };
                        if (op == "scn") gs.FillColor = c; else gs.StrokeColor = c;
                    } else if (operands.Count == 1 && operands[0].Kind == PKind.Name) {
                        TryResolvePatternGradient(operands[0].Text, resources, doc, result);
                    }
                    break;
                case "sh":
                    if (operands.Count >= 1 && operands[0].Kind == PKind.Name) TryResolveShadingGradient(operands[0].Text, resources, doc, result);
                    break;

                // ---- Path construction (coordinates transformed to page space now) ----
                case "m":
                    if (operands.Count >= 2) {
                        curX = N(operands, 0); curY = N(operands, 1); startX = curX; startY = curY;
                        var sp = new Subpath(); sp.Points.Add(new[] { gs.Ctm.TX(curX, curY), gs.Ctm.TY(curX, curY) });
                        path.Add(sp);
                    }
                    break;
                case "l":
                    if (operands.Count >= 2 && path.Count > 0) {
                        curX = N(operands, 0); curY = N(operands, 1);
                        path[path.Count - 1].Points.Add(new[] { gs.Ctm.TX(curX, curY), gs.Ctm.TY(curX, curY) });
                    }
                    break;
                case "c":
                    if (operands.Count >= 6 && path.Count > 0) { curX = N(operands, 4); curY = N(operands, 5); path[path.Count - 1].Points.Add(new[] { gs.Ctm.TX(curX, curY), gs.Ctm.TY(curX, curY) }); }
                    break;
                case "v":
                    if (operands.Count >= 4 && path.Count > 0) { curX = N(operands, 2); curY = N(operands, 3); path[path.Count - 1].Points.Add(new[] { gs.Ctm.TX(curX, curY), gs.Ctm.TY(curX, curY) }); }
                    break;
                case "y":
                    if (operands.Count >= 4 && path.Count > 0) { curX = N(operands, 2); curY = N(operands, 3); path[path.Count - 1].Points.Add(new[] { gs.Ctm.TX(curX, curY), gs.Ctm.TY(curX, curY) }); }
                    break;
                case "h":
                    if (path.Count > 0) path[path.Count - 1].Points.Add(new[] { gs.Ctm.TX(startX, startY), gs.Ctm.TY(startX, startY) });
                    break;
                case "re":
                    if (operands.Count >= 4) {
                        double x = N(operands, 0), y = N(operands, 1), rw = N(operands, 2), rh = N(operands, 3);
                        var sp = new Subpath { IsRect = true };
                        sp.Points.Add(new[] { gs.Ctm.TX(x, y), gs.Ctm.TY(x, y) });
                        sp.Points.Add(new[] { gs.Ctm.TX(x + rw, y), gs.Ctm.TY(x + rw, y) });
                        sp.Points.Add(new[] { gs.Ctm.TX(x + rw, y + rh), gs.Ctm.TY(x + rw, y + rh) });
                        sp.Points.Add(new[] { gs.Ctm.TX(x, y + rh), gs.Ctm.TY(x, y + rh) });
                        path.Add(sp);
                        curX = x; curY = y; startX = x; startY = y;
                    }
                    break;

                // ---- Painting: consumes the accumulated path, then clears it ----
                case "f": case "F": case "f*":
                    PaintFill(path, gs, result); path.Clear(); break;
                case "S":
                    PaintStroke(path, gs, result); path.Clear(); break;
                case "s":
                    if (path.Count > 0) path[path.Count - 1].Points.Add(new[] { gs.Ctm.TX(startX, startY), gs.Ctm.TY(startX, startY) });
                    PaintStroke(path, gs, result); path.Clear(); break;
                case "B": case "B*":
                    PaintFill(path, gs, result); PaintStroke(path, gs, result); path.Clear(); break;
                case "b": case "b*":
                    if (path.Count > 0) path[path.Count - 1].Points.Add(new[] { gs.Ctm.TX(startX, startY), gs.Ctm.TY(startX, startY) });
                    PaintFill(path, gs, result); PaintStroke(path, gs, result); path.Clear(); break;
                case "n":
                    path.Clear(); break;

                case "Tf":
                    if (operands.Count >= 2) fontSize = N(operands, 1);
                    break;
                case "Tm":
                    if (operands.Count >= 6) { tm = Mat.Make(N(operands, 0), N(operands, 1), N(operands, 2), N(operands, 3), N(operands, 4), N(operands, 5)); tlm = tm; }
                    break;
                case "Td":
                    if (operands.Count >= 2) { Mat move = Mat.Make(1, 0, 0, 1, N(operands, 0), N(operands, 1)); tlm = Mat.Concat(move, tlm); tm = tlm; }
                    break;
                case "TD":
                    if (operands.Count >= 2) { Mat move = Mat.Make(1, 0, 0, 1, N(operands, 0), N(operands, 1)); tlm = Mat.Concat(move, tlm); tm = tlm; }
                    break;
                case "T*":
                    tm = tlm;
                    break;
                case "Tj": case "'": case "\"":
                    EmitTextRun(operands, result, gs, tm, fontSize, op);
                    break;
                case "TJ":
                    EmitTextRunArray(operands, result, gs, tm, fontSize);
                    break;
                case "BT":
                    tm = Mat.Identity; tlm = Mat.Identity;
                    break;
                case "Do":
                    if (operands.Count >= 1 && operands[0].Kind == PKind.Name) TryRecordImage(operands[0].Text, resources, doc, gs.Ctm, result);
                    break;
            }
            if (op != "BI") operands.Clear(); // BI (inline image) not supported; operand reset only for normal ops
        }
        return result;
    }

    static int[] CmykToRgb(List<PVal> operands) {
        double c = N(operands, 0), m2 = N(operands, 1), y2 = N(operands, 2), k2 = N(operands, 3);
        return new[] {
            (int)Math.Round(255 * (1 - c) * (1 - k2)),
            (int)Math.Round(255 * (1 - m2) * (1 - k2)),
            (int)Math.Round(255 * (1 - y2) * (1 - k2)) };
    }

    static void PaintFill(List<Subpath> path, GState gs, ContentAnalysis result) {
        foreach (var sp in path) {
            if (sp.Points.Count < 2) continue;
            double x0 = double.MaxValue, y0 = double.MaxValue, x1 = double.MinValue, y1 = double.MinValue;
            foreach (var p in sp.Points) { x0 = Math.Min(x0, p[0]); y0 = Math.Min(y0, p[1]); x1 = Math.Max(x1, p[0]); y1 = Math.Max(y1, p[1]); }
            if (x1 - x0 < 0.1 || y1 - y0 < 0.1) continue; // degenerate (a stroked-only line etc.)
            result.FillRects.Add(new FillRect { X0 = x0, Y0 = y0, X1 = x1, Y1 = y1, Color = gs.FillColor });
        }
    }

    static void PaintStroke(List<Subpath> path, GState gs, ContentAnalysis result) {
        double width = Math.Max(0.25, gs.LineWidth * gs.Ctm.Scale());
        foreach (var sp in path) {
            for (int i = 0; i + 1 < sp.Points.Count; i++) {
                var p0 = sp.Points[i]; var p1 = sp.Points[i + 1];
                if (Math.Abs(p0[0] - p1[0]) < 0.05 && Math.Abs(p0[1] - p1[1]) < 0.05) continue; // zero-length
                result.Lines.Add(new Line { X0 = p0[0], Y0 = p0[1], X1 = p1[0], Y1 = p1[1], Color = gs.StrokeColor, Width = width });
            }
        }
    }

    static GState Clone(GState g) { return new GState { Ctm = g.Ctm, FillColor = g.FillColor, StrokeColor = g.StrokeColor, LineWidth = g.LineWidth }; }
    static double N(List<PVal> ops, int i) { return i < ops.Count ? ops[i].AsNumber() : 0; }

    static void EmitTextRun(List<PVal> operands, ContentAnalysis result, GState gs, Mat tm, double fontSize, string op) {
        string s = null;
        for (int i = operands.Count - 1; i >= 0; i--) if (operands[i].Kind == PKind.String) { s = operands[i].Text; break; }
        if (s == null) return;
        Mat trm = Mat.Concat(tm, gs.Ctm);
        double x = trm.TX(0, 0), y = trm.TY(0, 0);
        double effSize = fontSize * trm.Scale();
        result.TextRuns.Add(new TextRun { X = x, Y = y, Size = effSize, Chars = s.Length, Color = gs.FillColor });
    }

    static void EmitTextRunArray(List<PVal> operands, ContentAnalysis result, GState gs, Mat tm, double fontSize) {
        PVal arr = null;
        foreach (var o in operands) if (o.Kind == PKind.Array) arr = o;
        if (arr == null) return;
        int chars = 0;
        foreach (var item in arr.Items) if (item.Kind == PKind.String) chars += item.Text.Length;
        if (chars == 0) return;
        Mat trm = Mat.Concat(tm, gs.Ctm);
        double x = trm.TX(0, 0), y = trm.TY(0, 0);
        double effSize = fontSize * trm.Scale();
        result.TextRuns.Add(new TextRun { X = x, Y = y, Size = effSize, Chars = chars, Color = gs.FillColor });
    }

    static void TryRecordImage(string xobjectName, PVal resources, PdfDocument doc, Mat ctm, ContentAnalysis result) {
        if (resources == null) return;
        PVal xobjects = doc.Get(resources, "XObject");
        if (xobjects == null) return;
        PVal img = doc.Get(xobjects, xobjectName);
        if (img == null || img.Kind != PKind.Stream) return;
        PVal subtype = img.Get("Subtype");
        if (subtype == null || subtype.AsName() != "Image") return;
        // An image is placed by mapping the unit square through the CTM.
        double x0 = ctm.TX(0, 0), y0 = ctm.TY(0, 0);
        double x1 = ctm.TX(1, 1), y1 = ctm.TY(1, 1);
        result.Images.Add(new ImagePlacement {
            X0 = Math.Min(x0, x1), Y0 = Math.Min(y0, y1), X1 = Math.Max(x0, x1), Y1 = Math.Max(y0, y1),
            ImageObj = img
        });
    }

    static void TryResolvePatternGradient(string patternName, PVal resources, PdfDocument doc, ContentAnalysis result) {
        if (resources == null) return;
        PVal patterns = doc.Get(resources, "Pattern");
        if (patterns == null) return;
        PVal pat = doc.Get(patterns, patternName);
        if (pat == null) return;
        PVal shading = doc.Get(pat, "Shading");
        if (shading != null) ExtractShadingColors(shading, doc, result);
    }

    static void TryResolveShadingGradient(string shadingName, PVal resources, PdfDocument doc, ContentAnalysis result) {
        if (resources == null) return;
        PVal shadings = doc.Get(resources, "Shading");
        if (shadings == null) return;
        PVal sh = doc.Get(shadings, shadingName);
        if (sh != null) ExtractShadingColors(sh, doc, result);
    }

    static void ExtractShadingColors(PVal shading, PdfDocument doc, ContentAnalysis result) {
        PVal fn = doc.Get(shading, "Function");
        if (fn == null) return;
        if (fn.Kind == PKind.Array && fn.Items.Count > 0) fn = doc.Resolve(fn.Items[0]);
        int[] c0 = ReadColorArray(doc.Get(fn, "C0"));
        int[] c1 = ReadColorArray(doc.Get(fn, "C1"));
        if (c0 != null) result.GradientStart = c0;
        if (c1 != null) result.GradientEnd = c1;
        if (c0 == null || c1 == null) {
            PVal functions = doc.Get(fn, "Functions");
            if (functions != null && functions.Kind == PKind.Array && functions.Items.Count > 0) {
                PVal first = doc.Resolve(functions.Items[0]);
                PVal last = doc.Resolve(functions.Items[functions.Items.Count - 1]);
                int[] a = ReadColorArray(doc.Get(first, "C0"));
                int[] bb = ReadColorArray(doc.Get(last, "C1"));
                if (a != null) result.GradientStart = a;
                if (bb != null) result.GradientEnd = bb;
            }
        }
    }

    static int[] ReadColorArray(PVal arr) {
        if (arr == null || arr.Kind != PKind.Array) return null;
        if (arr.Items.Count == 1) { int g = (int)Math.Round(arr.Items[0].AsNumber() * 255); return new[] { g, g, g }; }
        if (arr.Items.Count == 3) return new[] { (int)Math.Round(arr.Items[0].AsNumber() * 255), (int)Math.Round(arr.Items[1].AsNumber() * 255), (int)Math.Round(arr.Items[2].AsNumber() * 255) };
        if (arr.Items.Count == 4) {
            double c = arr.Items[0].AsNumber(), m = arr.Items[1].AsNumber(), y = arr.Items[2].AsNumber(), k = arr.Items[3].AsNumber();
            return new[] { (int)Math.Round(255 * (1 - c) * (1 - k)), (int)Math.Round(255 * (1 - m) * (1 - k)), (int)Math.Round(255 * (1 - y) * (1 - k)) };
        }
        return null;
    }
}
}
