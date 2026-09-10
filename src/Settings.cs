using System;
using System.IO;
using System.ComponentModel;
using System.Collections.Generic;
using System.Text;
using System.Web.Script.Serialization;

public enum MarginSource { Legacy, Template, Custom }
public enum BodyAlignment { Justified, Right }
public enum TableHeaderAlignment { Center, Right, Left }
public enum TableCellVerticalAlignment { Middle, Top, Bottom }
public sealed class PdfNames : StringConverter {
    public static string[] Names = new string[] { "Application settings", "[High Quality Print]", "[Smallest File Size]", "[Press Quality]", "[PDF/X-4:2008]" };
    public override bool GetStandardValuesSupported(ITypeDescriptorContext context) { return true; }
    public override bool GetStandardValuesExclusive(ITypeDescriptorContext context) { return false; }
    public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context) { return new StandardValuesCollection(Names); }
}
public sealed class FontNames : StringConverter {
    public static string[] Names = new string[] { "IRNazanin\tRegular", "IRNazanin\tBold", "Modam\tExtraBold  [ @mimvid ]", "Modam\tSemiBold  [ @mimvid ]", "Modam\tMedium  [ @mimvid ]", "Times New Roman\tRegular" };
    public override bool GetStandardValuesSupported(ITypeDescriptorContext context) { return true; }
    public override bool GetStandardValuesExclusive(ITypeDescriptorContext context) { return false; }
    public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context) { return new StandardValuesCollection(Names); }
}
public sealed class LayoutOptions {
    [Browsable(false)] public int SchemaVersion { get; set; }
    [Category("01. Fonts"), DisplayName("Body font"), TypeConverter(typeof(FontNames))] public string BodyFont { get; set; }
    [Category("01. Fonts"), DisplayName("Body bold font"), TypeConverter(typeof(FontNames))] public string BoldFont { get; set; }
    [Category("01. Fonts"), DisplayName("Heading 1 font"), TypeConverter(typeof(FontNames))] public string Heading1Font { get; set; }
    [Category("01. Fonts"), DisplayName("Heading 2 font"), TypeConverter(typeof(FontNames))] public string Heading2Font { get; set; }
    [Category("01. Fonts"), DisplayName("English table font"), TypeConverter(typeof(FontNames))] public string EnglishFont { get; set; }
    [Category("02. Body"), DisplayName("Font size (pt)")] public double BodySize { get; set; }
    [Category("02. Body"), DisplayName("Line spacing (pt)")] public double BodyLeading { get; set; }
    [Category("02. Body"), DisplayName("Paragraph gap (pt)")] public double BodyAfter { get; set; }
    [Category("02. Body"), DisplayName("Alignment")] public BodyAlignment Alignment { get; set; }
    [Category("03. Headings"), DisplayName("Heading 1 size (pt)")] public double H1Size { get; set; }
    [Category("03. Headings"), DisplayName("Heading 1 leading (pt)")] public double H1Leading { get; set; }
    [Category("03. Headings"), DisplayName("Heading 1 space before (pt)")] public double H1Before { get; set; }
    [Category("03. Headings"), DisplayName("Heading 1 space after (pt)")] public double H1After { get; set; }
    [Category("03. Headings"), DisplayName("Heading 2 size (pt)")] public double H2Size { get; set; }
    [Category("03. Headings"), DisplayName("Heading 2 leading (pt)")] public double H2Leading { get; set; }
    [Category("03. Headings"), DisplayName("Heading 2 space before (pt)")] public double H2Before { get; set; }
    [Category("03. Headings"), DisplayName("Heading 2 space after (pt)")] public double H2After { get; set; }
    [Category("03. Headings"), DisplayName("Heading 3 font"), TypeConverter(typeof(FontNames))] public string Heading3Font { get; set; }
    [Category("03. Headings"), DisplayName("Heading 3 size (pt)")] public double H3Size { get; set; }
    [Category("03. Headings"), DisplayName("Heading 3 leading (pt)")] public double H3Leading { get; set; }
    [Category("03. Headings"), DisplayName("Heading 3 space before (pt)")] public double H3Before { get; set; }
    [Category("03. Headings"), DisplayName("Heading 3 space after (pt)")] public double H3After { get; set; }
    [Category("03. Headings"), DisplayName("Number separator"), Description("One character placed between heading-number levels; for example 11-2.")] public string HeadingNumberSeparator { get; set; }
    [Category("03. Headings"), DisplayName("Heading color (#RRGGBB)")] public string AccentColor { get; set; }
    [Category("04. Page"), DisplayName("Margin source"), Description("Legacy preserves v1.1.1 geometry. Template reads the first page margins. Custom uses the values below.")] public MarginSource Margins { get; set; }
    [Category("04. Page"), DisplayName("Top margin (mm)")] public double MarginTop { get; set; }
    [Category("04. Page"), DisplayName("Bottom margin (mm)")] public double MarginBottom { get; set; }
    [Category("04. Page"), DisplayName("Left margin (mm)")] public double MarginLeft { get; set; }
    [Category("04. Page"), DisplayName("Right margin (mm)")] public double MarginRight { get; set; }
    [Category("04. Page"), DisplayName("Parent page name"), Description("Exact InDesign parent name. Leave blank to use the first page's applied parent.")] public string ParentName { get; set; }
    [Category("05. Tables"), DisplayName("Font size (pt)")] public double TableSize { get; set; }
    [Category("05. Tables"), DisplayName("Line spacing (pt)")] public double TableLeading { get; set; }
    [Category("05. Tables"), DisplayName("English font size (pt)")] public double EnglishSize { get; set; }
    [Category("05. Tables"), DisplayName("English leading (pt)")] public double EnglishLeading { get; set; }
    [Category("05. Tables"), DisplayName("Cell padding (pt)")] public double CellPadding { get; set; }
    [Category("05. Tables"), DisplayName("Border width (pt)")] public double TableBorder { get; set; }
    [Category("05. Tables"), DisplayName("Table color (#RRGGBB)")] public string TableColor { get; set; }
    [Category("05. Tables"), DisplayName("First row is header")] public bool TableHeader { get; set; }
    [Category("05. Tables"), DisplayName("Repeat header on next pages")] public bool RepeatHeader { get; set; }
    [Category("05. Tables"), DisplayName("Header alignment")] public TableHeaderAlignment HeaderAlignment { get; set; }
    [Category("05. Tables"), DisplayName("Vertical alignment")] public TableCellVerticalAlignment CellVerticalAlignment { get; set; }
    [Category("06. Lists"), DisplayName("Format bullet paragraphs")] public bool FormatBullets { get; set; }
    [Category("06. Lists"), DisplayName("Bullet indent (pt)")] public double BulletIndent { get; set; }
    [Category("06. Lists"), DisplayName("Format numbered paragraphs")] public bool FormatNumberedLists { get; set; }
    [Category("06. Lists"), DisplayName("Numbered indent (pt)")] public double NumberedIndent { get; set; }
    [Category("07. Images"), DisplayName("Import inline images"), Description("Imports Word images that are In Line with Text. Floating images are not resized automatically.")] public bool ImportInlineImages { get; set; }
    [Category("07. Images"), DisplayName("Replace images with placeholders"), Description("Keeps each inline image position and size as a numbered editable frame, but removes the actual image for manual placement later.")] public bool ReplaceImagesWithPlaceholders { get; set; }
    [Category("07. Images"), DisplayName("Maximum width (% of text frame)")] public double ImageMaxWidthPercent { get; set; }
    [Category("07. Images"), DisplayName("Maximum height (% of text frame)")] public double ImageMaxHeightPercent { get; set; }
    [Category("07. Images"), DisplayName("Center image paragraph")] public bool CenterInlineImages { get; set; }
    [Category("06. Cover"), DisplayName("Title size (pt)")] public double CoverSize { get; set; }
    [Category("06. Cover"), DisplayName("Title leading (pt)")] public double CoverLeading { get; set; }
    [Category("07. Contents"), DisplayName("Generate table of contents"), Description("Editable Heading 1/2 entries with page numbers. Regenerate after manual pagination changes.")] public bool TocEnabled { get; set; }
    [Category("07. Contents"), DisplayName("Contents title")] public string TocTitle { get; set; }
    [Category("07. Contents"), DisplayName("Include Heading 2")] public bool TocLevel2 { get; set; }
    [Category("08. PDF"), DisplayName("PDF export preset"), TypeConverter(typeof(PdfNames)), Description("Validate Template loads exact installed names. Application settings preserves the existing export preferences.")] public string PdfPreset { get; set; }
    public LayoutOptions() {
        SchemaVersion=1; BodyFont="IRNazanin\tRegular"; BoldFont="IRNazanin\tBold"; Heading1Font="Modam\tExtraBold  [ @mimvid ]"; Heading2Font="Modam\tSemiBold  [ @mimvid ]"; Heading3Font="Modam\tSemiBold  [ @mimvid ]"; EnglishFont="Times New Roman\tRegular";
        BodySize=13; BodyLeading=19; BodyAfter=6; H1Size=15; H1Leading=24; H1Before=10; H1After=8; H2Size=12; H2Leading=20; H2Before=8; H2After=6; H3Size=11; H3Leading=18; H3Before=6; H3After=4; HeadingNumberSeparator="-";
        AccentColor="#154F9E"; TableColor="#154F9E"; Margins=MarginSource.Legacy; ParentName=""; MarginTop=30; MarginBottom=30; MarginLeft=35; MarginRight=35;
        TableSize=11.5; TableLeading=16; EnglishSize=10; EnglishLeading=14; CellPadding=5; TableBorder=.4; TableHeader=true; RepeatHeader=true; HeaderAlignment=TableHeaderAlignment.Center; CellVerticalAlignment=TableCellVerticalAlignment.Middle;
        FormatBullets=true; BulletIndent=18; FormatNumberedLists=true; NumberedIndent=18;
        ImportInlineImages=true; ReplaceImagesWithPlaceholders=false; ImageMaxWidthPercent=100; ImageMaxHeightPercent=80; CenterInlineImages=true;
        CoverSize=26; CoverLeading=40; TocTitle="فهرست مطالب"; TocLevel2=true; PdfPreset="Application settings";
    }
    public static LayoutOptions Preset(string name) {
        LayoutOptions o=new LayoutOptions();
        if(name=="Book Summary") { o.BodySize=12; o.BodyLeading=18; o.H1Size=16; o.H1Before=14; o.AccentColor="#245B50"; o.TableColor=o.AccentColor; }
        if(name=="Research Report") { o.TocEnabled=true; o.H1Before=14; o.H2Before=10; }
        if(name=="Compact Report") { o.BodySize=12; o.BodyLeading=16; o.BodyAfter=4; o.H1Size=14; o.H1Leading=20; o.H1Before=8; o.H2Size=11.5; o.H2Leading=17; o.H2Before=6; o.TableSize=10.5; o.TableLeading=14; o.CellPadding=3; }
        return o;
    }
    public void Validate() {
        if(SchemaVersion!=1) throw new Exception("Unsupported layout preset schema version.");
        foreach(string value in new string[]{BodyFont,BoldFont,Heading1Font,Heading2Font,Heading3Font,EnglishFont}) if(String.IsNullOrWhiteSpace(value)||!value.Contains("\t")) throw new Exception("Font names must use family + TAB + style. Use the dropdown after Validate Template.");
        foreach(double v in new double[]{BodySize,H1Size,H2Size,H3Size,TableSize,EnglishSize,CoverSize}) Range(v,6,72,"Font size");
        foreach(double v in new double[]{BodyLeading,H1Leading,H2Leading,H3Leading,TableLeading,EnglishLeading,CoverLeading}) Range(v,6,120,"Line spacing");
        foreach(double v in new double[]{BodyAfter,H1Before,H1After,H2Before,H2After,H3Before,H3After}) Range(v,0,100,"Paragraph spacing");
        foreach(double v in new double[]{MarginTop,MarginBottom,MarginLeft,MarginRight}) Range(v,0,150,"Margin");
        Range(CellPadding,0,40,"Cell padding"); Range(TableBorder,0,10,"Table border"); Range(BulletIndent,0,100,"Bullet indent"); Range(NumberedIndent,0,100,"Numbered indent"); Range(ImageMaxWidthPercent,10,100,"Image maximum width"); Range(ImageMaxHeightPercent,10,100,"Image maximum height");
        if(BodyLeading<BodySize||H1Leading<H1Size||H2Leading<H2Size||H3Leading<H3Size||TableLeading<TableSize||EnglishLeading<EnglishSize||CoverLeading<CoverSize) throw new Exception("Line spacing must be at least the font size.");
        foreach(string c in new string[]{AccentColor,TableColor}) if(c==null||!System.Text.RegularExpressions.Regex.IsMatch(c,@"^#[0-9a-fA-F]{6}$")) throw new Exception("Colors must be #RRGGBB, for example #154F9E.");
        if(String.IsNullOrEmpty(HeadingNumberSeparator)||HeadingNumberSeparator.Length!=1||System.Text.RegularExpressions.Regex.IsMatch(HeadingNumberSeparator,@"[\s0-9\u06F0-\u06F9\u0660-\u0669()]")) throw new Exception("Heading number separator must be one non-numeric character, for example -.");
        if(!Enum.IsDefined(typeof(MarginSource),Margins)||!Enum.IsDefined(typeof(BodyAlignment),Alignment)||!Enum.IsDefined(typeof(TableHeaderAlignment),HeaderAlignment)||!Enum.IsDefined(typeof(TableCellVerticalAlignment),CellVerticalAlignment)) throw new Exception("Unknown margin or alignment option.");
        if(TocEnabled&&String.IsNullOrWhiteSpace(TocTitle)) throw new Exception("Enter a table of contents title.");
        if(String.IsNullOrWhiteSpace(PdfPreset)) throw new Exception("Select a PDF preset or Application settings.");
    }
    static void Range(double v,double min,double max,string label) { if(Double.IsNaN(v)||Double.IsInfinity(v)||v<min||v>max) throw new Exception(label+" must be between "+min+" and "+max+"."); }
}
public sealed class QueueItem { public string Report {get;set;} public string Title {get;set;} public QueueItem(){Report="";Title="";} }
public sealed class UserSettings {
    public LayoutOptions Layout {get;set;} public string Template {get;set;} public string Output {get;set;}
    public string Subtitle {get;set;} public string Author {get;set;} public string Organization {get;set;} public string Date {get;set;}
    public bool Cover {get;set;} public bool ContinueOnError {get;set;} public string Repository {get;set;} public bool CheckUpdatesAtStart {get;set;}
    public List<QueueItem> Reports {get;set;}
    public UserSettings(){Layout=new LayoutOptions();Reports=new List<QueueItem>();Cover=true;ContinueOnError=true;Repository="marabi766/report-layout";Date="";}
}
public static class SettingsStore {
    public static readonly string Folder=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"ReportLayoutData");
    public static string ProfileName="settings.json";
    public static string FileName { get { return Path.Combine(Folder,ProfileName); } }
    public static string Serialize(object value) { return new JavaScriptSerializer().Serialize(value); }
    public static void Write(string path,object value) {
        string dir=Path.GetDirectoryName(Path.GetFullPath(path));Directory.CreateDirectory(dir);
        string temp=Path.Combine(dir,"settings-"+Guid.NewGuid().ToString("N")+".tmp");
        try { File.WriteAllText(temp,Serialize(value),new UTF8Encoding(false)); if(File.Exists(path))File.Replace(temp,path,null);else File.Move(temp,path); }
        finally {if(File.Exists(temp))File.Delete(temp);}
    }
    public static T Read<T>(string path) { return new JavaScriptSerializer().Deserialize<T>(File.ReadAllText(path,Encoding.UTF8)); }
}
