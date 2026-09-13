using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;

// A minimal, self-contained PDF reader used only to MEASURE an existing PDF's
// page geometry and colors (margins, ink/accent/gradient colors) so a fresh
// InDesign template can be built to match. No PDF text/content is reproduced;
// only numeric positions, font sizes and color values are read.
//
// Deliberately not a general-purpose PDF library: it supports the common
// subset produced by mainstream page-layout tools (InDesign, Illustrator,
// Word-to-PDF) -- FlateDecode streams, classic and compressed (ObjStm)
// objects, simple content-stream text/fill/shading operators. Anything it
// cannot resolve falls back to a sane default rather than throwing.
namespace PdfTemplate {

public enum PKind { Null, Bool, Number, String, Name, Array, Dict, Ref, Stream, Operator }

public sealed class PVal {
    public PKind Kind;
    public bool Bool;
    public double Number;
    public string Text; // String/Name/Operator payload
    public List<PVal> Items; // Array
    public Dictionary<string, PVal> Map; // Dict / Stream dict
    public int RefNum, RefGen;
    public byte[] StreamRaw; // Stream raw (still-encoded) bytes

    public static readonly PVal NullVal = new PVal { Kind = PKind.Null };
    public static PVal Num(double n) { return new PVal { Kind = PKind.Number, Number = n }; }
    public static PVal Nm(string s) { return new PVal { Kind = PKind.Name, Text = s }; }

    public double AsNumber(double fallback = 0) { return Kind == PKind.Number ? Number : fallback; }
    public string AsName() { return (Kind == PKind.Name || Kind == PKind.String) ? Text : null; }

    public PVal Get(string key) {
        if (Map != null && Map.ContainsKey(key)) return Map[key];
        return null;
    }
}

public sealed class PdfLexer {
    byte[] b; public int Pos;
    public PdfLexer(byte[] bytes, int start) { b = bytes; Pos = start; }
    public int Length { get { return b.Length; } }

    static bool IsWhite(byte c) { return c == 0 || c == 9 || c == 10 || c == 12 || c == 13 || c == 32; }
    static bool IsDelim(byte c) { return c == (byte)'(' || c == (byte)')' || c == (byte)'<' || c == (byte)'>' || c == (byte)'[' || c == (byte)']' || c == (byte)'{' || c == (byte)'}' || c == (byte)'/' || c == (byte)'%'; }

    public void SkipWhite() {
        while (Pos < b.Length) {
            if (b[Pos] == (byte)'%') { while (Pos < b.Length && b[Pos] != 10 && b[Pos] != 13) Pos++; }
            else if (IsWhite(b[Pos])) Pos++;
            else break;
        }
    }

    public bool Match(string s) {
        SkipWhite();
        int save = Pos;
        for (int i = 0; i < s.Length; i++) {
            if (Pos >= b.Length || b[Pos] != (byte)s[i]) { Pos = save; return false; }
            Pos++;
        }
        return true;
    }

    // Reads one PDF token/object starting at current position (assumes SkipWhite done by caller as needed).
    public PVal ReadValue() {
        SkipWhite();
        if (Pos >= b.Length) return PVal.NullVal;
        byte c = b[Pos];
        if (c == (byte)'/') return ReadName();
        if (c == (byte)'(') return ReadLiteralString();
        if (c == (byte)'<') {
            if (Pos + 1 < b.Length && b[Pos + 1] == (byte)'<') return ReadDictOrStream();
            return ReadHexString();
        }
        if (c == (byte)'[') return ReadArray();
        if (c == (byte)'-' || c == (byte)'+' || c == (byte)'.' || (c >= (byte)'0' && c <= (byte)'9')) return ReadNumberOrRef();
        return ReadKeyword();
    }

    PVal ReadName() {
        Pos++; // '/'
        StringBuilder sb = new StringBuilder();
        while (Pos < b.Length && !IsWhite(b[Pos]) && !IsDelim(b[Pos])) {
            if (b[Pos] == (byte)'#' && Pos + 2 < b.Length) {
                int hi = HexVal(b[Pos + 1]), lo = HexVal(b[Pos + 2]);
                if (hi >= 0 && lo >= 0) { sb.Append((char)(hi * 16 + lo)); Pos += 3; continue; }
            }
            sb.Append((char)b[Pos]); Pos++;
        }
        return PVal.Nm(sb.ToString());
    }
    static int HexVal(byte c) {
        if (c >= (byte)'0' && c <= (byte)'9') return c - (byte)'0';
        if (c >= (byte)'a' && c <= (byte)'f') return c - (byte)'a' + 10;
        if (c >= (byte)'A' && c <= (byte)'F') return c - (byte)'A' + 10;
        return -1;
    }

    PVal ReadLiteralString() {
        Pos++; int depth = 1; List<byte> outb = new List<byte>();
        while (Pos < b.Length && depth > 0) {
            byte c = b[Pos++];
            if (c == (byte)'\\' && Pos < b.Length) {
                byte n = b[Pos++];
                switch ((char)n) {
                    case 'n': outb.Add(10); break;
                    case 'r': outb.Add(13); break;
                    case 't': outb.Add(9); break;
                    case '(': outb.Add((byte)'('); break;
                    case ')': outb.Add((byte)')'); break;
                    case '\\': outb.Add((byte)'\\'); break;
                    default: outb.Add(n); break;
                }
            } else if (c == (byte)'(') { depth++; outb.Add(c); }
            else if (c == (byte)')') { depth--; if (depth > 0) outb.Add(c); }
            else outb.Add(c);
        }
        return new PVal { Kind = PKind.String, Text = Encoding.GetEncoding("ISO-8859-1").GetString(outb.ToArray()) };
    }

    PVal ReadHexString() {
        Pos++; StringBuilder hex = new StringBuilder();
        while (Pos < b.Length && b[Pos] != (byte)'>') { if (!IsWhite(b[Pos])) hex.Append((char)b[Pos]); Pos++; }
        if (Pos < b.Length) Pos++; // '>'
        if (hex.Length % 2 == 1) hex.Append('0');
        byte[] bytes = new byte[hex.Length / 2];
        for (int i = 0; i < bytes.Length; i++) bytes[i] = Convert.ToByte(hex.ToString(i * 2, 2), 16);
        return new PVal { Kind = PKind.String, Text = Encoding.GetEncoding("ISO-8859-1").GetString(bytes) };
    }

    PVal ReadArray() {
        Pos++; var items = new List<PVal>();
        while (true) {
            SkipWhite();
            if (Pos >= b.Length || b[Pos] == (byte)']') { if (Pos < b.Length) Pos++; break; }
            items.Add(ReadValue());
        }
        return new PVal { Kind = PKind.Array, Items = items };
    }

    PVal ReadDictOrStream() {
        Pos += 2; var map = new Dictionary<string, PVal>();
        while (true) {
            SkipWhite();
            if (Pos + 1 < b.Length && b[Pos] == (byte)'>' && b[Pos + 1] == (byte)'>') { Pos += 2; break; }
            if (Pos >= b.Length) break;
            if (b[Pos] != (byte)'/') { ReadValue(); continue; } // tolerate malformed content
            PVal key = ReadName();
            PVal val = ReadValue();
            map[key.Text] = val;
        }
        int save = Pos;
        SkipWhite();
        if (Match("stream")) {
            if (Pos < b.Length && b[Pos] == 13) Pos++;
            if (Pos < b.Length && b[Pos] == 10) Pos++;
            int start = Pos;
            int len = -1;
            if (map.ContainsKey("Length") && map["Length"].Kind == PKind.Number) len = (int)map["Length"].Number;
            int end;
            if (len >= 0 && start + len <= b.Length) {
                end = start + len;
                int probe = end; PdfLexer look = new PdfLexer(b, probe); look.SkipWhite();
                if (!look.Match("endstream")) end = FindEndstream(start);
            } else {
                end = FindEndstream(start);
            }
            byte[] raw = new byte[Math.Max(0, end - start)];
            Array.Copy(b, start, raw, 0, raw.Length);
            Pos = end;
            SkipWhite(); Match("endstream");
            return new PVal { Kind = PKind.Stream, Map = map, StreamRaw = raw };
        }
        Pos = save;
        return new PVal { Kind = PKind.Dict, Map = map };
    }

    int FindEndstream(int from) {
        byte[] pat = Encoding.ASCII.GetBytes("endstream");
        for (int i = from; i <= b.Length - pat.Length; i++) {
            bool ok = true;
            for (int j = 0; j < pat.Length; j++) if (b[i + j] != pat[j]) { ok = false; break; }
            if (ok) {
                int e = i;
                if (e > from && b[e - 1] == 10) e--;
                if (e > from && b[e - 1] == 13) e--;
                return e;
            }
        }
        return b.Length;
    }

    PVal ReadNumberOrRef() {
        int save = Pos;
        string n1 = ReadRawNumber();
        double d1; double.TryParse(n1, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out d1);
        int save2 = Pos;
        SkipWhite();
        if (Pos < b.Length && b[Pos] >= (byte)'0' && b[Pos] <= (byte)'9') {
            string n2 = ReadRawNumber();
            int save3 = Pos;
            SkipWhite();
            if (Pos < b.Length && b[Pos] == (byte)'R' && (Pos + 1 >= b.Length || IsWhite(b[Pos + 1]) || IsDelim(b[Pos + 1]))) {
                Pos++;
                int g; int.TryParse(n2, out g);
                return new PVal { Kind = PKind.Ref, RefNum = (int)d1, RefGen = g };
            }
            Pos = save3;
            Pos = save2; // not a ref; rewind past lookahead
        } else Pos = save2;
        return PVal.Num(d1);
    }

    string ReadRawNumber() {
        int start = Pos;
        if (Pos < b.Length && (b[Pos] == (byte)'+' || b[Pos] == (byte)'-')) Pos++;
        while (Pos < b.Length && ((b[Pos] >= (byte)'0' && b[Pos] <= (byte)'9') || b[Pos] == (byte)'.')) Pos++;
        return Encoding.ASCII.GetString(b, start, Pos - start);
    }

    PVal ReadKeyword() {
        int start = Pos;
        while (Pos < b.Length && !IsWhite(b[Pos]) && !IsDelim(b[Pos])) Pos++;
        string s = Encoding.ASCII.GetString(b, start, Pos - start);
        if (s == "true") return new PVal { Kind = PKind.Bool, Bool = true };
        if (s == "false") return new PVal { Kind = PKind.Bool, Bool = false };
        if (s == "null") return PVal.NullVal;
        if (s.Length == 0) { Pos++; return PVal.NullVal; } // avoid infinite loop on stray byte
        return new PVal { Kind = PKind.Operator, Text = s };
    }
}

public sealed class PdfDocument {
    byte[] bytes;
    Dictionary<int, PVal> objects = new Dictionary<int, PVal>();
    public double PageWidth = 612, PageHeight = 792;

    public static PdfDocument Load(string path) {
        var doc = new PdfDocument();
        doc.bytes = File.ReadAllBytes(path);
        doc.ScanObjects();
        doc.ExpandObjectStreams();
        return doc;
    }

    void ScanObjects() {
        var re = new Regex(@"(?<!\d)(\d+)[ \t\r\n]+(\d+)[ \t\r\n]+obj\b", RegexOptions.Compiled);
        string ascii = Encoding.GetEncoding("ISO-8859-1").GetString(bytes);
        foreach (Match m in re.Matches(ascii)) {
            int num = int.Parse(m.Groups[1].Value);
            int bodyStart = m.Index + m.Length;
            var lex = new PdfLexer(bytes, bodyStart);
            PVal val;
            try { val = lex.ReadValue(); } catch { continue; }
            objects[num] = val;
        }
    }

    void ExpandObjectStreams() {
        var keys = new List<int>(objects.Keys);
        foreach (var num in keys) {
            PVal obj = objects[num];
            if (obj.Kind != PKind.Stream) continue;
            PVal type = obj.Get("Type");
            if (type == null || type.AsName() != "ObjStm") continue;
            try {
                byte[] data = Decode(obj);
                int n = (int)(obj.Get("N") != null ? obj.Get("N").Number : 0);
                int first = (int)(obj.Get("First") != null ? obj.Get("First").Number : 0);
                var header = new PdfLexer(data, 0);
                var pairs = new List<int[]>();
                for (int i = 0; i < n; i++) {
                    PVal a = header.ReadValue(); PVal b2 = header.ReadValue();
                    pairs.Add(new int[] { (int)a.Number, (int)b2.Number });
                }
                foreach (var pair in pairs) {
                    int objNum = pair[0], offset = pair[1];
                    var lex = new PdfLexer(data, first + offset);
                    PVal val;
                    try { val = lex.ReadValue(); } catch { continue; }
                    if (!objects.ContainsKey(objNum)) objects[objNum] = val;
                }
            } catch { }
        }
    }

    public PVal Resolve(PVal v) {
        int guard = 0;
        while (v != null && v.Kind == PKind.Ref && guard++ < 32) {
            objects.TryGetValue(v.RefNum, out v);
        }
        return v ?? PVal.NullVal;
    }
    public PVal Get(PVal dict, string key) { return dict == null ? null : Resolve(dict.Get(key)); }

    public static byte[] InflateZlib(byte[] data) {
        // Skip the 2-byte zlib header (and possible dictionary id) -- .NET's
        // DeflateStream expects raw deflate, not zlib-wrapped deflate.
        int offset = (data.Length > 2 && (data[0] & 0x0F) == 8) ? 2 : 0;
        using (var ms = new MemoryStream(data, offset, data.Length - offset))
        using (var inflater = new DeflateStream(ms, CompressionMode.Decompress))
        using (var outMs = new MemoryStream()) {
            inflater.CopyTo(outMs);
            return outMs.ToArray();
        }
    }

    public byte[] Decode(PVal streamObj) {
        byte[] raw = streamObj.StreamRaw ?? new byte[0];
        PVal filter = Get(streamObj, "Filter");
        List<string> filters = new List<string>();
        if (filter != null) {
            if (filter.Kind == PKind.Name) filters.Add(filter.Text);
            else if (filter.Kind == PKind.Array) foreach (var f in filter.Items) filters.Add(Resolve(f).Text);
        }
        byte[] data = raw;
        foreach (var f in filters) {
            if (f == "FlateDecode" || f == "Fl") { try { data = InflateZlib(data); } catch { } }
            // ASCII85/LZW/RunLength/DCT(JPEG)/CCITT not needed for our text/vector analysis use case.
        }
        return data;
    }

    PVal FindCatalog() {
        foreach (var kv in objects) {
            if (kv.Value.Kind == PKind.Dict || kv.Value.Kind == PKind.Stream) {
                PVal t = kv.Value.Get("Type");
                if (t != null && t.AsName() == "Catalog") return kv.Value;
            }
        }
        return null;
    }

    public List<PVal> GetPages() {
        var pages = new List<PVal>();
        PVal catalog = FindCatalog();
        PVal root = catalog != null ? Get(catalog, "Pages") : null;
        if (root == null) {
            // Fallback: collect every object of /Type /Page directly.
            foreach (var kv in objects) {
                PVal t = kv.Value.Get("Type");
                if (t != null && t.AsName() == "Page") pages.Add(kv.Value);
            }
            return pages;
        }
        WalkPageTree(root, new Dictionary<string, PVal>(), pages, 0);
        return pages;
    }

    void WalkPageTree(PVal node, Dictionary<string, PVal> inherited, List<PVal> pages, int depth) {
        if (node == null || depth > 64) return;
        var mine = new Dictionary<string, PVal>(inherited);
        foreach (var key in new[] { "Resources", "MediaBox", "CropBox", "Rotate" }) {
            PVal v = node.Get(key);
            if (v != null) mine[key] = v;
        }
        PVal type = Get(node, "Type");
        PVal kids = Get(node, "Kids");
        if (kids != null && kids.Kind == PKind.Array) {
            foreach (var kid in kids.Items) WalkPageTree(Resolve(kid), mine, pages, depth + 1);
        } else {
            foreach (var kv in mine) if (node.Get(kv.Key) == null) node.Map[kv.Key] = kv.Value;
            pages.Add(node);
        }
    }

    public double[] GetMediaBox(PVal page) {
        PVal mb = Get(page, "MediaBox");
        if (mb != null && mb.Kind == PKind.Array && mb.Items.Count == 4) {
            double[] r = new double[4];
            for (int i = 0; i < 4; i++) r[i] = Resolve(mb.Items[i]).AsNumber();
            return new double[] { r[0], r[1], r[2] - r[0], r[3] - r[1] };
        }
        return new double[] { 0, 0, 612, 792 };
    }

    public byte[] GetPageContent(PVal page) {
        PVal contents = Get(page, "Contents");
        var ms = new MemoryStream();
        if (contents == null) return ms.ToArray();
        if (contents.Kind == PKind.Stream) {
            byte[] d = Decode(contents); ms.Write(d, 0, d.Length);
        } else if (contents.Kind == PKind.Array) {
            foreach (var c in contents.Items) {
                PVal s = Resolve(c);
                if (s.Kind == PKind.Stream) { byte[] d = Decode(s); ms.Write(d, 0, d.Length); ms.WriteByte((byte)'\n'); }
            }
        }
        return ms.ToArray();
    }

    public PVal GetResources(PVal page) { return Get(page, "Resources"); }

    double GetNum(PVal dict, string key, double fallback = 0) {
        PVal v = Get(dict, key);
        return (v != null && v.Kind == PKind.Number) ? v.Number : fallback;
    }

    // Writes an embedded image XObject out to its own file so InDesign can
    // place() a real image (not a placeholder). DCTDecode (JPEG) bytes are
    // already a complete JPEG file. A handful of simple uncompressed/
    // Flate-compressed raster formats (8-bit Gray/RGB/CMYK, no color-space
    // lookup table) are decoded by hand into a PNG. Anything else (JPX,
    // CCITT fax, indexed palettes, ...) is skipped -- the caller falls back
    // to a placeholder for that image.
    public bool TryExportImage(PVal imageObj, string outPathNoExt, out string ext) {
        ext = null;
        PVal filter = imageObj.Get("Filter");
        string filterName = null;
        if (filter != null) {
            if (filter.Kind == PKind.Name) filterName = filter.Text;
            else if (filter.Kind == PKind.Array && filter.Items.Count > 0) filterName = Resolve(filter.Items[filter.Items.Count - 1]).Text;
        }
        try {
            if (filterName == "DCTDecode") {
                ext = "jpg";
                File.WriteAllBytes(outPathNoExt + ".jpg", imageObj.StreamRaw ?? new byte[0]);
                return true;
            }
            if (filterName == null || filterName == "FlateDecode") {
                byte[] data = Decode(imageObj);
                int width = (int)GetNum(imageObj, "Width");
                int height = (int)GetNum(imageObj, "Height");
                int bpc = (int)GetNum(imageObj, "BitsPerComponent", 8);
                PVal cs = Get(imageObj, "ColorSpace");
                string csName = cs != null ? cs.AsName() : null;
                int comps = csName == "DeviceRGB" ? 3 : csName == "DeviceGray" ? 1 : csName == "DeviceCMYK" ? 4 : -1;
                if (width <= 0 || height <= 0 || bpc != 8 || comps <= 0) return false;
                int stride = width * comps;
                if (data.Length < (long)stride * height) return false;
                using (var bmp = new Bitmap(width, height, PixelFormat.Format24bppRgb)) {
                    for (int y = 0; y < height; y++) {
                        int rowBase = y * stride;
                        for (int x = 0; x < width; x++) {
                            int idx = rowBase + x * comps;
                            Color color;
                            if (comps == 3) color = Color.FromArgb(data[idx], data[idx + 1], data[idx + 2]);
                            else if (comps == 1) color = Color.FromArgb(data[idx], data[idx], data[idx]);
                            else {
                                double c = data[idx] / 255.0, m = data[idx + 1] / 255.0, ye = data[idx + 2] / 255.0, k = data[idx + 3] / 255.0;
                                color = Color.FromArgb((int)(255 * (1 - c) * (1 - k)), (int)(255 * (1 - m) * (1 - k)), (int)(255 * (1 - ye) * (1 - k)));
                            }
                            bmp.SetPixel(x, y, color);
                        }
                    }
                    ext = "png";
                    bmp.Save(outPathNoExt + ".png", ImageFormat.Png);
                }
                return true;
            }
        } catch { }
        return false;
    }
}
}
