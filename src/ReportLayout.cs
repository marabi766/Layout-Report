using System;
using System.IO;
using System.Drawing;
using System.Windows.Forms;
using System.Threading;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Web.Script.Serialization;
using System.Collections.Generic;
using Microsoft.Win32;
using System.Diagnostics;
using System.Text;
using System.Net;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using PdfTemplate;

[assembly: AssemblyTitle("Report Layout")]
[assembly: AssemblyVersion("2.0.0.0")]
[assembly: AssemblyInformationalVersion("2.0.0-preview.1")]
public sealed class ReportLayout : Form {
    public const string VersionLabel="2.0.0-preview.1";
    public const string CopyrightNotice="Copyright © 2026 Mohammad Arabi. All rights reserved.";
    const string AccessPasswordHash="08329d8ed0053d1ef450dfecf0b945b09a8c464bbab50c553bf122fe543fe238";
    static string Sha256(string s){using(SHA256 sha=SHA256.Create()){byte[] hash=sha.ComputeHash(Encoding.UTF8.GetBytes(s));StringBuilder sb=new StringBuilder();foreach(byte b in hash)sb.Append(b.ToString("x2"));return sb.ToString();}}
    readonly string root=AppDomain.CurrentDomain.BaseDirectory;
    TabControl tabs=new TabControl();
    TextBox template=new TextBox(),output=new TextBox(),log=new TextBox(),subtitle=new TextBox(),author=new TextBox(),organization=new TextBox(),date=new TextBox(),repository=new TextBox();
    DataGridView queue=new DataGridView(),results=new DataGridView(); PropertyGrid properties=new PropertyGrid();
    CheckBox cover=new CheckBox(),keepGoing=new CheckBox(),updateAtStart=new CheckBox(); ComboBox presets=new ComboBox();
    Button build,validate,stop,update; ProgressBar progress=new ProgressBar(); Label status=new Label();
    List<Control> locked=new List<Control>(); volatile bool stopping; volatile string cancelFile; bool busy,checkingUpdate; string lastOutput; NotifyIcon tray; PictureBox logo;
    LayoutOptions layout=new LayoutOptions();
    [STAThread] public static void Main(){
        Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
        bool created;using(Mutex single=new Mutex(true,"Local\\ReportLayoutPreview",out created)){
            if(!created){MessageBox.Show("Report Layout Preview is already running.","Report Layout");return;}
            if(!SignIn())return;
            try{Application.Run(new ReportLayout());}finally{single.ReleaseMutex();}
        }
    }
    static bool SignIn(){
        using(Form dlg=new Form()){
            dlg.Text="Report Layout - Sign In";dlg.FormBorderStyle=FormBorderStyle.FixedDialog;dlg.StartPosition=FormStartPosition.CenterScreen;
            dlg.ClientSize=new Size(360,160);dlg.MaximizeBox=false;dlg.MinimizeBox=false;dlg.ShowInTaskbar=true;dlg.Font=new Font("Segoe UI",10);
            Label lbl=new Label();lbl.Text="Enter password to continue:";lbl.SetBounds(20,20,320,24);dlg.Controls.Add(lbl);
            TextBox box=new TextBox();box.SetBounds(20,48,320,28);box.UseSystemPasswordChar=true;dlg.Controls.Add(box);
            Label err=new Label();err.ForeColor=Color.Firebrick;err.SetBounds(20,80,320,36);dlg.Controls.Add(err);
            Button ok=new Button();ok.Text="Sign In";ok.SetBounds(160,120,90,32);ok.DialogResult=DialogResult.OK;dlg.Controls.Add(ok);
            Button cancel=new Button();cancel.Text="Exit";cancel.SetBounds(258,120,82,32);cancel.DialogResult=DialogResult.Cancel;dlg.Controls.Add(cancel);
            dlg.AcceptButton=ok;dlg.CancelButton=cancel;
            while(true){
                if(dlg.ShowDialog()!=DialogResult.OK)return false;
                if(Sha256(box.Text)==AccessPasswordHash)return true;
                err.Text="Incorrect password. Please try again.";box.Text="";box.Focus();
            }
        }
    }
    public ReportLayout(){
        Text="Report Layout "+VersionLabel;ClientSize=new Size(1000,760);MinimumSize=new Size(850,650);StartPosition=FormStartPosition.CenterScreen;AutoScaleMode=AutoScaleMode.Dpi;BackColor=Color.FromArgb(245,247,250);Font=new Font("Segoe UI",10);RightToLeft=RightToLeft.No;
        string iconPath=Path.Combine(root,"assets","ReportLayout.ico");Icon=new Icon(iconPath,32,32);
        Panel header=new Panel();header.Height=105;header.Dock=DockStyle.Top;Controls.Add(header);
        logo=new PictureBox();logo.SetBounds(20,10,85,85);logo.SizeMode=PictureBoxSizeMode.Zoom;logo.Image=Image.FromFile(Path.Combine(root,"assets","ReportLayout.png"));header.Controls.Add(logo);
        Label name=new Label();name.Text="Report Layout";name.Font=new Font("Segoe UI",23,FontStyle.Bold);name.ForeColor=Color.FromArgb(21,79,158);name.SetBounds(125,17,650,42);header.Controls.Add(name);
        Label hint=new Label();hint.Text="Word to InDesign | Layout presets, batch reports and review | "+VersionLabel;hint.SetBounds(128,64,800,30);header.Controls.Add(hint);
        Panel bottom=new Panel();bottom.Height=110;bottom.Dock=DockStyle.Bottom;Controls.Add(bottom);
        FlowLayoutPanel actions=new FlowLayoutPanel();actions.Dock=DockStyle.Top;actions.Height=48;bottom.Controls.Add(actions);
        build=ButtonAt(actions,"Build Reports",delegate{StartWork(false);});build.BackColor=Color.FromArgb(21,79,158);build.ForeColor=Color.White;
        validate=ButtonAt(actions,"Validate Template",delegate{StartWork(true);});
        stop=ButtonAt(actions,"Cancel Current Job",RequestStop);stop.Enabled=false;
        ButtonAt(actions,"Open Output Folder",delegate{OpenPath(lastOutput??output.Text);});
        Label footer=new Label();footer.Text="Report Layout "+VersionLabel+"   |   "+CopyrightNotice;footer.Font=new Font("Segoe UI",8);footer.ForeColor=Color.Gray;footer.TextAlign=ContentAlignment.MiddleCenter;footer.Dock=DockStyle.Bottom;footer.Height=18;bottom.Controls.Add(footer);
        progress.Dock=DockStyle.Bottom;progress.Height=7;bottom.Controls.Add(progress);status.Dock=DockStyle.Bottom;status.Height=28;status.Text="Ready";bottom.Controls.Add(status);
        tabs.Dock=DockStyle.Fill;Controls.Add(tabs);tabs.BringToFront();
        TabPage reportsPage=Page("Reports"),layoutPage=Page("Layout Settings"),detailsPage=Page("Cover & Details"),resultPage=Page("Results"),settingsPage=Page("Preferences"),logPage=Page("Log");
        locked.Add(reportsPage);locked.Add(layoutPage);locked.Add(detailsPage);locked.Add(settingsPage);
        TableLayoutPanel panel=new TableLayoutPanel();panel.Dock=DockStyle.Fill;panel.ColumnCount=1;panel.RowCount=6;panel.RowStyles.Add(new RowStyle(SizeType.Absolute,44));panel.RowStyles.Add(new RowStyle(SizeType.Absolute,40));panel.RowStyles.Add(new RowStyle(SizeType.Absolute,44));panel.RowStyles.Add(new RowStyle(SizeType.Absolute,42));panel.RowStyles.Add(new RowStyle(SizeType.Percent,100));panel.RowStyles.Add(new RowStyle(SizeType.Absolute,36));reportsPage.Controls.Add(panel);
        panel.Controls.Add(PathRow("Select Template",template,delegate{using(OpenFileDialog d=new OpenFileDialog()){d.Filter="InDesign templates|*.idml;*.indd;*.indt";if(d.ShowDialog(this)==DialogResult.OK)template.Text=d.FileName;}}),0,0);
        FlowLayoutPanel templateActions=new FlowLayoutPanel();templateActions.Dock=DockStyle.Fill;panel.Controls.Add(templateActions,0,1);
        ButtonAt(templateActions,"Build Template from PDF...",delegate{BuildTemplateFromPdf();});
        panel.Controls.Add(PathRow("Output Folder",output,delegate{using(FolderBrowserDialog d=new FolderBrowserDialog()){if(d.ShowDialog(this)==DialogResult.OK)output.Text=d.SelectedPath;}}),0,2);
        FlowLayoutPanel queueButtons=new FlowLayoutPanel();queueButtons.Dock=DockStyle.Fill;panel.Controls.Add(queueButtons,0,3);
        ButtonAt(queueButtons,"Select Reports",delegate{using(OpenFileDialog d=new OpenFileDialog()){d.Filter="Word reports|*.docx";d.Multiselect=true;if(d.ShowDialog(this)==DialogResult.OK)foreach(string f in d.FileNames)queue.Rows.Add(f,"");}});
        ButtonAt(queueButtons,"Set Selected Title",delegate{if(queue.SelectedRows.Count==0){MessageBox.Show(this,"Select a report row first.","Report title");return;}queue.CurrentCell=queue.SelectedRows[0].Cells[1];queue.BeginEdit(true);});
        ButtonAt(queueButtons,"Remove Selected",delegate{foreach(DataGridViewRow r in queue.SelectedRows)queue.Rows.Remove(r);});ButtonAt(queueButtons,"Clear Queue",delegate{queue.Rows.Clear();});
        Grid(queue);queue.ReadOnly=false;queue.Columns.Add("Report","DOCX report");queue.Columns.Add("Title","Report title — required and editable");queue.Columns[0].ReadOnly=true;panel.Controls.Add(queue,0,4);
        keepGoing.Text="Continue with the next report if one fails";keepGoing.Checked=true;keepGoing.Dock=DockStyle.Fill;panel.Controls.Add(keepGoing,0,5);
        properties.Dock=DockStyle.Fill;properties.PropertySort=PropertySort.Categorized;properties.SelectedObject=layout;layoutPage.Controls.Add(properties);
        FlowLayoutPanel presetBar=new FlowLayoutPanel();presetBar.Dock=DockStyle.Top;presetBar.Height=84;layoutPage.Controls.Add(presetBar);
        presets.DropDownStyle=ComboBoxStyle.DropDownList;presets.Width=185;presets.Items.AddRange(new object[]{"Economic Report","Book Summary","Research Report","Compact Report"});presets.SelectedIndex=0;presetBar.Controls.Add(presets);
        ButtonAt(presetBar,"Apply Preset",delegate{layout=LayoutOptions.Preset(Convert.ToString(presets.SelectedItem));properties.SelectedObject=layout;});ButtonAt(presetBar,"Import Preset",delegate{ImportPreset();});ButtonAt(presetBar,"Export Preset",delegate{ExportPreset();});ButtonAt(presetBar,"Choose Color",delegate{ChooseColor();});
        TableLayoutPanel details=FieldsPanel(detailsPage);cover.Text="Add cover page";cover.Checked=true;cover.AutoSize=true;details.Controls.Add(cover,1,0);
        Field(details,"Subtitle",subtitle,1);Field(details,"Author",author,2);Field(details,"Organization",organization,3);Field(details,"Report date",date,4);foreach(TextBox b in new TextBox[]{subtitle,author,organization,date})b.RightToLeft=RightToLeft.Yes;
        Note(details,"The title comes from each queue row. Cover details apply to all reports.\r\nThe author and title are also written to PDF metadata.",5);
        Grid(results);foreach(string c in new string[]{"Report","Status","Pages","Tables","Headings","Missing Fonts","Warnings","Folder"})results.Columns.Add(c,c);results.Columns[7].Visible=false;resultPage.Controls.Add(results);
        FlowLayoutPanel resultButtons=new FlowLayoutPanel();resultButtons.Dock=DockStyle.Bottom;resultButtons.Height=44;resultPage.Controls.Add(resultButtons);
        ButtonAt(resultButtons,"Open PDF",delegate{OpenResult("Report.pdf");});ButtonAt(resultButtons,"Open INDD",delegate{OpenResult("Report.indd");});ButtonAt(resultButtons,"Open Folder",delegate{OpenResult("");});ButtonAt(resultButtons,"View Details",delegate{OpenResult("result.json");});
        TableLayoutPanel prefs=FieldsPanel(settingsPage);Field(prefs,"GitHub repository",repository,0);repository.Text="marabi766/report-layout";
        updateAtStart.Text="Check for updates at startup (optional network request)";updateAtStart.AutoSize=true;prefs.Controls.Add(updateAtStart,1,1);
        FlowLayoutPanel preferenceButtons=new FlowLayoutPanel();preferenceButtons.AutoSize=true;prefs.Controls.Add(preferenceButtons,1,2);
        update=ButtonAt(preferenceButtons,"Check for Updates",delegate{CheckUpdates(false);});ButtonAt(preferenceButtons,"Save Preferences",delegate{SaveSettings();});ButtonAt(preferenceButtons,"Open Settings Folder",delegate{Directory.CreateDirectory(SettingsStore.Folder);OpenPath(SettingsStore.Folder);});
        Note(prefs,"Settings and queue paths stay on this computer. No report content is uploaded.\r\nUpdate checks read release metadata only. No installer runs automatically.\r\nPrivate repositories require an authenticated GitHub CLI (gh auth login).\r\nMinimize to keep the app in the system tray.",3);
        log.Dock=DockStyle.Fill;log.Multiline=true;log.ReadOnly=true;log.ScrollBars=ScrollBars.Vertical;log.WordWrap=true;logPage.Controls.Add(log);
        ContextMenuStrip menu=new ContextMenuStrip();menu.Items.Add("Open Report Layout",null,delegate{ShowMain();});menu.Items.Add("Open Output Folder",null,delegate{OpenPath(lastOutput??output.Text);});menu.Items.Add("Exit",null,delegate{Close();});
        tray=new NotifyIcon();tray.Icon=new Icon(iconPath,16,16);tray.Text="Report Layout";tray.ContextMenuStrip=menu;tray.Visible=true;tray.DoubleClick+=delegate{ShowMain();};
        Resize+=delegate{if(WindowState==FormWindowState.Minimized)Hide();};
        FormClosing+=delegate(object sender,FormClosingEventArgs e){if(busy){e.Cancel=true;ShowMain();RequestStop();MessageBox.Show(this,"Cancellation was requested. Report Layout will close the working copy and return when InDesign reaches the next safe checkpoint.","Report Layout");}else SaveSettings();};
        FormClosed+=delegate{tray.Visible=false;tray.Icon.Dispose();tray.Dispose();menu.Dispose();logo.Image.Dispose();Icon.Dispose();};
        template.Text=Path.Combine(root,"assets","Template.idml");output.Text=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),"Report Layout");LoadSettings();
        Shown+=delegate{if(updateAtStart.Checked)CheckUpdates(true);};AddLog("Ready. Validate Template loads InDesign fonts and PDF presets. Preview version: Windows/InDesign acceptance testing is still required.");
    }
    TabPage Page(string text){TabPage p=new TabPage(text);p.Padding=new Padding(10);tabs.TabPages.Add(p);return p;}
    static TableLayoutPanel FieldsPanel(Control parent){TableLayoutPanel p=new TableLayoutPanel();p.Dock=DockStyle.Top;p.AutoSize=true;p.ColumnCount=2;p.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,170));p.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));parent.Controls.Add(p);return p;}
    static void Note(TableLayoutPanel p,string text,int row){Label l=new Label();l.Text=text;l.AutoSize=true;l.MaximumSize=new Size(690,0);p.Controls.Add(l,1,row);}
    static Button ButtonAt(Control parent,string text,Action action){Button b=new Button();b.Text=text;b.AutoSize=true;b.Height=34;b.MinimumSize=new Size(125,34);b.Margin=new Padding(4);b.Click+=delegate{action();};parent.Controls.Add(b);return b;}
    static void Field(TableLayoutPanel panel,string caption,TextBox box,int row){Label l=new Label();l.Text=caption;l.AutoSize=true;l.Margin=new Padding(4,10,4,10);box.Dock=DockStyle.Fill;box.Margin=new Padding(4,6,4,6);panel.Controls.Add(l,0,row);panel.Controls.Add(box,1,row);}
    Control PathRow(string label,TextBox box,Action action){TableLayoutPanel p=new TableLayoutPanel();p.Dock=DockStyle.Fill;p.ColumnCount=2;p.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,165));p.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));ButtonAt(p,label,action);box.Dock=DockStyle.Fill;box.Margin=new Padding(4,7,4,4);box.ReadOnly=true;p.Controls.Add(box,1,0);return p;}
    static void Grid(DataGridView g){g.Dock=DockStyle.Fill;g.AllowUserToAddRows=false;g.AllowUserToDeleteRows=false;g.ReadOnly=true;g.RowHeadersVisible=false;g.SelectionMode=DataGridViewSelectionMode.FullRowSelect;g.AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill;g.BackgroundColor=Color.White;}
    void ShowMain(){Show();WindowState=FormWindowState.Normal;Activate();}
    void OpenPath(string path){try{if(String.IsNullOrWhiteSpace(path)||(!Directory.Exists(path)&&!File.Exists(path)))throw new Exception("This output is not available yet.");Process.Start(new ProcessStartInfo(path){UseShellExecute=true});}catch(Exception e){MessageBox.Show(this,e.Message,"Open output");}}
    void OpenResult(string name){if(results.SelectedRows.Count==0){MessageBox.Show(this,"Select a result first.");return;}string folder=Convert.ToString(results.SelectedRows[0].Cells[7].Value);OpenPath(String.IsNullOrEmpty(name)?folder:Path.Combine(folder,name));}
    void UI(Action action){if(IsDisposed||Disposing)return;try{BeginInvoke(action);}catch(InvalidOperationException){}}
    void AddLog(string s){if(InvokeRequired){UI(delegate{AddLog(s);});return;}log.AppendText("["+DateTime.Now.ToString("HH:mm:ss")+"] "+s+Environment.NewLine);}
    void Busy(bool value){busy=value;foreach(Control c in locked)c.Enabled=!value;build.Enabled=validate.Enabled=!value;stop.Enabled=value;progress.Style=value?ProgressBarStyle.Marquee:ProgressBarStyle.Blocks;status.Text=value?"Working. Please do not edit InDesign documents during a job.":"Ready";}
    void RequestStop(){
        if(!busy)return;stopping=true;stop.Enabled=false;status.Text="Cancelling current job...";AddLog("Cancellation requested. Waiting for InDesign to reach a safe checkpoint.");
        string path=cancelFile;if(!String.IsNullOrEmpty(path))try{File.WriteAllText(path,"cancel",Encoding.ASCII);}catch(Exception e){AddLog("Could not signal cancellation: "+e.Message);}
    }
    List<QueueItem> ReadQueue(){queue.EndEdit();List<QueueItem> items=new List<QueueItem>();foreach(DataGridViewRow row in queue.Rows)items.Add(new QueueItem{Report=Convert.ToString(row.Cells[0].Value),Title=Convert.ToString(row.Cells[1].Value)});return items;}
    UserSettings Snapshot(){return new UserSettings{Layout=layout,Template=template.Text,Output=output.Text,Subtitle=subtitle.Text,Author=author.Text,Organization=organization.Text,Date=date.Text,Cover=cover.Checked,ContinueOnError=keepGoing.Checked,Repository=repository.Text.Trim(),CheckUpdatesAtStart=updateAtStart.Checked,Reports=ReadQueue()};}
    void SaveSettings(){try{Validate();SettingsStore.Write(SettingsStore.FileName,Snapshot());}catch(Exception e){AddLog("Settings were not saved: "+e.Message);}}
    void LoadSettings(){if(!File.Exists(SettingsStore.FileName))return;try{UserSettings s=SettingsStore.Read<UserSettings>(SettingsStore.FileName);if(s==null||s.Layout==null)throw new Exception("Settings are incomplete.");s.Layout.Validate();layout=s.Layout;properties.SelectedObject=layout;if(!String.IsNullOrEmpty(s.Template))template.Text=s.Template;if(!String.IsNullOrEmpty(s.Output))output.Text=s.Output;subtitle.Text=s.Subtitle;author.Text=s.Author;organization.Text=s.Organization;date.Text=s.Date;cover.Checked=s.Cover;keepGoing.Checked=s.ContinueOnError;repository.Text=s.Repository;updateAtStart.Checked=s.CheckUpdatesAtStart;if(s.Reports!=null)foreach(QueueItem q in s.Reports)queue.Rows.Add(q.Report,q.Title);}catch(Exception e){AddLog("Could not load saved settings; defaults are active. "+e.Message);}}
    void ImportPreset(){try{using(OpenFileDialog d=new OpenFileDialog()){d.Filter="Layout preset (*.json)|*.json";if(d.ShowDialog(this)!=DialogResult.OK)return;LayoutOptions next=SettingsStore.Read<LayoutOptions>(d.FileName);if(next==null)throw new Exception("Empty preset.");next.Validate();layout=next;properties.SelectedObject=layout;AddLog("Preset imported: "+d.FileName);}}catch(Exception e){MessageBox.Show(this,e.Message,"Import preset");}}
    void ExportPreset(){try{Validate();layout.Validate();using(SaveFileDialog d=new SaveFileDialog()){d.Filter="Layout preset (*.json)|*.json";d.FileName="layout-preset.json";if(d.ShowDialog(this)==DialogResult.OK)SettingsStore.Write(d.FileName,layout);}}catch(Exception e){MessageBox.Show(this,e.Message,"Export preset");}}
    void ChooseColor(){GridItem item=properties.SelectedGridItem;if(item==null||item.PropertyDescriptor==null||(item.PropertyDescriptor.Name!="AccentColor"&&item.PropertyDescriptor.Name!="TableColor")){MessageBox.Show(this,"Select Heading color or Table color first.");return;}using(ColorDialog d=new ColorDialog()){if(d.ShowDialog(this)==DialogResult.OK){item.PropertyDescriptor.SetValue(layout,"#"+d.Color.R.ToString("X2")+d.Color.G.ToString("X2")+d.Color.B.ToString("X2"));properties.Refresh();}}}
    void BuildTemplateFromPdf(){
        string pdfPath;
        using(OpenFileDialog d=new OpenFileDialog()){d.Filter="PDF files|*.pdf";if(d.ShowDialog(this)!=DialogResult.OK)return;pdfPath=d.FileName;}
        int coverPage;
        using(Form dlg=new Form()){
            dlg.Text="Build Template from PDF";dlg.FormBorderStyle=FormBorderStyle.FixedDialog;dlg.StartPosition=FormStartPosition.CenterScreen;
            dlg.ClientSize=new Size(360,170);dlg.MaximizeBox=false;dlg.MinimizeBox=false;dlg.Font=new Font("Segoe UI",10);
            Label l1=new Label();l1.Text="Cover page number:";l1.SetBounds(20,20,190,24);dlg.Controls.Add(l1);
            NumericUpDown coverBox=new NumericUpDown();coverBox.Minimum=1;coverBox.Maximum=100000;coverBox.Value=1;coverBox.SetBounds(220,18,110,28);dlg.Controls.Add(coverBox);
            Label note=new Label();note.Text="Every other page is scanned automatically and grouped into the recurring page styles found (ordinary text, chart/exhibit, full-bleed divider) so the generated template covers all of them, each as its own master spread.";note.SetBounds(20,54,320,80);dlg.Controls.Add(note);
            Button ok=new Button();ok.Text="Build";ok.SetBounds(160,132,90,32);ok.DialogResult=DialogResult.OK;dlg.Controls.Add(ok);
            Button cancel=new Button();cancel.Text="Cancel";cancel.SetBounds(258,132,82,32);cancel.DialogResult=DialogResult.Cancel;dlg.Controls.Add(cancel);
            dlg.AcceptButton=ok;dlg.CancelButton=cancel;
            if(dlg.ShowDialog(this)!=DialogResult.OK)return;
            coverPage=(int)coverBox.Value;
        }
        string engine=Path.Combine(root,"Build-Template-From-PDF.jsx");
        if(!File.Exists(engine)){MessageBox.Show(this,"Build-Template-From-PDF.jsx is missing.");return;}
        string folder;
        try{folder=Path.Combine(SettingsStore.Folder,"Generated Templates","Template-"+DateTime.Now.ToString("yyyyMMdd-HHmmss"));Directory.CreateDirectory(folder);}catch(Exception ex){MessageBox.Show(this,ex.Message);return;}
        Busy(true);AddLog("Scanning all pages of "+Path.GetFileName(pdfPath)+" (cover page "+coverPage+")...");
        Thread worker=new Thread(delegate(){RunTemplateBuild(pdfPath,coverPage,engine,folder);});
        worker.SetApartmentState(ApartmentState.STA);worker.IsBackground=true;worker.Start();
    }
    void RunTemplateBuild(string pdfPath,int coverPage,string engine,string folder){
        object app=null;bool success=false;string message="";string idmlPath=Path.Combine(folder,"Template.idml");
        try{
            Dictionary<string,object> spec=PdfTemplateSpecBuilder.BuildMultiSpec(pdfPath,coverPage);
            object masters;spec.TryGetValue("masters",out masters);int masterCount=masters is System.Collections.ICollection?((System.Collections.ICollection)masters).Count:0;
            AddLog("Found "+masterCount+" recurring page style(s) across "+Convert.ToString(spec.ContainsKey("totalPages")?spec["totalPages"]:"?")+" pages.");
            spec["outputIdml"]=idmlPath;
            spec["outputIndd"]=Path.Combine(folder,"Template.indd");
            spec["outputPreviewPdf"]=Path.Combine(folder,"Template-preview.pdf");
            spec["outputLog"]=Path.Combine(folder,"build-log.txt");
            spec["outputResult"]=Path.Combine(folder,"result.json");
            string json=SettingsStore.Serialize(spec);
            File.WriteAllText(Path.Combine(folder,"template-spec.json"),json,new UTF8Encoding(false));
            if(spec.ContainsKey("warning"))AddLog("Note: "+spec["warning"]);
            string source=Regex.Replace(File.ReadAllText(engine,Encoding.UTF8),@"(?m)^\s*#target[^\r\n]*","");
            app=Connect();
            app.GetType().InvokeMember("DoScript",BindingFlags.InvokeMethod|BindingFlags.OptionalParamBinding,null,app,new object[]{"var TEMPLATE_SPEC = "+json+";\r\n"+source,1246973031});
            string resultPath=Path.Combine(folder,"result.json");
            if(!File.Exists(resultPath))throw new Exception("InDesign did not record a result. Check build-log.txt in "+folder);
            Dictionary<string,object> result=SettingsStore.Read<Dictionary<string,object>>(resultPath);
            success=result.ContainsKey("ok")&&Convert.ToBoolean(result["ok"]);
            message=Convert.ToString(result.ContainsKey("message")?result["message"]:"");
            if(!success)throw new Exception(message);
            if(!File.Exists(idmlPath))throw new Exception("Template.idml was not created.");
        }catch(Exception e){
            success=false;Exception actual=e;while(actual.InnerException!=null)actual=actual.InnerException;message=actual.Message;
            AddLog("Template build failed: "+message);
            string logPath=Path.Combine(folder,"build-log.txt");if(File.Exists(logPath))try{AddLog(File.ReadAllText(logPath));}catch{}
        }finally{
            if(app!=null&&Marshal.IsComObject(app))Marshal.ReleaseComObject(app);
            bool ok=success;
            UI(delegate{
                Busy(false);
                if(ok){
                    template.Text=idmlPath;AddLog("Template built from PDF: "+idmlPath);
                    MessageBox.Show(this,"A new template was built from the PDF and selected as your active template.\r\n\r\n"+idmlPath+"\r\n\r\nReview it in InDesign, then use Build Reports as usual.","Template Built",MessageBoxButtons.OK,MessageBoxIcon.Information);
                }
            });
        }
    }
    void StartWork(bool validationOnly){
        UserSettings s;try{Validate();layout.Validate();s=Snapshot();if(!File.Exists(s.Template))throw new Exception("Select an existing InDesign template.");if(String.IsNullOrWhiteSpace(s.Output))throw new Exception("Select an output folder.");if(!validationOnly){if(s.Reports.Count==0)throw new Exception("Select one or more DOCX reports.");foreach(QueueItem q in s.Reports){if(!File.Exists(q.Report)||!q.Report.EndsWith(".docx",StringComparison.OrdinalIgnoreCase))throw new Exception("Missing DOCX report: "+q.Report);if(String.IsNullOrWhiteSpace(q.Title))throw new Exception("Enter a title for every report.");}}if(!File.Exists(Path.Combine(root,"Layout-Report.jsx")))throw new Exception("Layout-Report.jsx is missing.");}
        catch(Exception e){MessageBox.Show(this,e.Message,"Check settings");return;}
        SaveSettings();stopping=false;Busy(true);results.Rows.Clear();Thread worker=new Thread(delegate(){RunQueue(s,validationOnly);});worker.SetApartmentState(ApartmentState.STA);worker.IsBackground=true;worker.Start();
    }
    object Connect(){
        List<string> ids=new List<string>();ids.Add("InDesign.Application.2026");ids.Add("InDesign.Application");
        try{using(RegistryKey classes=RegistryKey.OpenBaseKey(RegistryHive.ClassesRoot,RegistryView.Registry64)){foreach(string key in classes.GetSubKeyNames())if(key.StartsWith("InDesign.Application.",StringComparison.OrdinalIgnoreCase)&&!ids.Contains(key))ids.Add(key);}}catch(Exception e){AddLog("Registry discovery: "+e.Message);}
        Exception last=null;foreach(string id in ids){try{object running=Marshal.GetActiveObject(id);if(running!=null)return running;}catch{}try{Type t=Type.GetTypeFromProgID(id,false);if(t!=null)return Activator.CreateInstance(t);}catch(Exception ex){last=ex;}}
        throw new Exception("Could not connect to InDesign. Open InDesign and try again.",last);
    }
    static string Get(Dictionary<string,object> r,string key){object value;return r!=null&&r.TryGetValue(key,out value)?Convert.ToString(value):"";}
    static string[] Strings(Dictionary<string,object> r,string key){object v;if(!r.TryGetValue(key,out v))return new string[0];System.Collections.IEnumerable items=v as System.Collections.IEnumerable;List<string> names=new List<string>();if(items!=null)foreach(object item in items)names.Add(Convert.ToString(item));return names.ToArray();}
    void RunQueue(UserSettings s,bool validationOnly){
        object app=null;int passed=0,failed=0;bool warned=false;List<Dictionary<string,object>> summary=new List<Dictionary<string,object>>();string runRoot=null;
        try{
            runRoot=Path.Combine(s.Output,"Run-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+"-"+Guid.NewGuid().ToString("N").Substring(0,8));Directory.CreateDirectory(runRoot);lastOutput=runRoot;
            app=Connect();string source=Regex.Replace(File.ReadAllText(Path.Combine(root,"Layout-Report.jsx"),Encoding.UTF8),@"(?m)^\s*#target[^\r\n]*","");
            List<QueueItem> work=validationOnly?new List<QueueItem>{new QueueItem{Title="Template validation"}}:s.Reports;
            for(int i=0;i<work.Count;i++){
                if(stopping)break;QueueItem q=work[i];string folder=Path.Combine(runRoot,(i+1).ToString("D3"));Directory.CreateDirectory(folder);cancelFile=Path.Combine(folder,"cancel.request");if(File.Exists(cancelFile))File.Delete(cancelFile);
                Dictionary<string,object> r=null;bool ok=false;string error="";
                try{
                    AddLog((i+1)+" / "+work.Count+": "+q.Title);
                    Dictionary<string,object> cfg=new Dictionary<string,object>{{"report",q.Report},{"template",s.Template},{"title",q.Title},{"output",folder},{"cancelFile",cancelFile},{"cover",s.Cover},{"subtitle",s.Subtitle},{"author",s.Author},{"organization",s.Organization},{"date",s.Date},{"layout",s.Layout},{"mode",validationOnly?"validate":"build"},{"closeAfterBuild",work.Count>1}};
                    string json=SettingsStore.Serialize(cfg);File.WriteAllText(Path.Combine(folder,"job-config.json"),json,new UTF8Encoding(false));SettingsStore.Write(Path.Combine(folder,"layout-preset.json"),s.Layout);
                    app.GetType().InvokeMember("DoScript",BindingFlags.InvokeMethod|BindingFlags.OptionalParamBinding,null,app,new object[]{"var REPORT_CONFIG = "+json+";\r\n"+source,1246973031});
                    string path=Path.Combine(folder,"result.json");if(!File.Exists(path))throw new Exception("InDesign did not record result.json.");r=SettingsStore.Read<Dictionary<string,object>>(path);
                    if(Get(r,"ok").ToLowerInvariant()!="true")throw new Exception(Get(r,"message"));
                    if(!validationOnly)foreach(string f in new string[]{"Report.indd","Report.idml","Report.pdf"})if(!File.Exists(Path.Combine(folder,f))||new FileInfo(Path.Combine(folder,f)).Length==0)throw new Exception("Missing or empty output: "+f);
                    ok=true;passed++;if(!String.IsNullOrEmpty(Get(r,"warnings")))warned=true;
                }catch(Exception e){failed++;Exception actual=e;while(actual.InnerException!=null)actual=actual.InnerException;error=actual.Message;AddLog("Failed: "+error);File.WriteAllText(Path.Combine(folder,"windows-error.txt"),e.ToString());if(File.Exists(Path.Combine(folder,"result.json")))try{r=SettingsStore.Read<Dictionary<string,object>>(Path.Combine(folder,"result.json"));}catch{} }
                summary.Add(new Dictionary<string,object>{{"title",q.Title},{"ok",ok},{"folder",folder},{"error",error},{"result",r}});SettingsStore.Write(Path.Combine(runRoot,"batch-summary.json"),summary);
                if(File.Exists(Path.Combine(folder,"report-log.txt")))AddLog(File.ReadAllText(Path.Combine(folder,"report-log.txt")));
                Dictionary<string,object> current=r;string outcome=ok?(validationOnly?"Validated":"Completed"):"Failed";string label=q.Title;string savedFolder=folder;string currentError=error;
                UI(delegate{results.Rows.Add(label,outcome,Get(current,"pages"),Get(current,"tables"),Get(current,"headings"),Get(current,"missingFonts"),Get(current,"warnings")+currentError,savedFolder);if(current!=null){string[] pdfs=Strings(current,"pdfPresets");if(pdfs.Length>0){List<string> names=new List<string>();names.Add("Application settings");names.AddRange(pdfs);PdfNames.Names=names.ToArray();}string[] fonts=Strings(current,"availableFonts");if(fonts.Length>0)FontNames.Names=fonts;properties.Refresh();}});
                if(!ok&&!s.ContinueOnError)break;
            }
        }catch(Exception e){failed++;AddLog("Run stopped: "+e.Message);if(runRoot!=null){try{File.WriteAllText(Path.Combine(runRoot,"windows-error.txt"),e.ToString());}catch(Exception logError){AddLog("Could not save run diagnostics: "+logError.Message);}}}
        finally{
            cancelFile=null;if(app!=null&&Marshal.IsComObject(app))Marshal.ReleaseComObject(app);
            int succeeded=passed,errors=failed;bool hasWarnings=warned;int requested=validationOnly?1:s.Reports.Count;
            UI(delegate{Busy(false);tabs.SelectedIndex=3;ShowMain();string msg="Completed: "+succeeded+" | Failed: "+errors+" | Not processed: "+Math.Max(0,requested-succeeded-errors);status.Text=msg;bool all=succeeded==requested&&errors==0;string caption=all?(validationOnly?"Template Validated":"Reports Created Successfully"):"Run Finished - Review Required";if(hasWarnings)msg+="\r\nWarnings were recorded. Review the Results tab and logs.";tray.ShowBalloonTip(6000,caption,msg,all?ToolTipIcon.Info:ToolTipIcon.Warning);MessageBox.Show(this,msg+"\r\n\r\n"+runRoot,caption,MessageBoxButtons.OK,all?MessageBoxIcon.Information:MessageBoxIcon.Warning);});
        }
    }
    void CheckUpdates(bool automatic){
        if(checkingUpdate)return;string repo=repository.Text.Trim();if(!Regex.IsMatch(repo,@"^[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+$")){if(!automatic)MessageBox.Show(this,"Enter a repository as owner/name.");return;}
        checkingUpdate=true;update.Enabled=false;AddLog("Checking latest published GitHub release...");Thread worker=new Thread(delegate(){
            string message="",url=null;
            try{
                string json;try{ServicePointManager.SecurityProtocol=SecurityProtocolType.Tls12;HttpWebRequest request=(HttpWebRequest)WebRequest.Create("https://api.github.com/repos/"+repo+"/releases/latest");request.UserAgent="ReportLayout/"+VersionLabel;request.Accept="application/vnd.github+json";request.Timeout=15000;request.ReadWriteTimeout=15000;using(WebResponse response=request.GetResponse())using(StreamReader reader=new StreamReader(response.GetResponseStream()))json=reader.ReadToEnd();}catch(WebException){json=ReadReleaseWithGh(repo);}
                Dictionary<string,object> release=new JavaScriptSerializer().Deserialize<Dictionary<string,object>>(json);string tag=Get(release,"tag_name");if(IsNewerRelease(tag,VersionLabel)){url="https://github.com/"+repo+"/releases/latest";message="A newer release is available: "+tag+".\r\nOpen its release page?";}else message="No newer stable release was found. Current version: "+VersionLabel;
            }catch(Exception e){message="Update check could not complete. Check the repository, network, published release and (for private repositories) gh auth login.\r\n"+e.Message;}
            string finalMessage=message,finalUrl=url;UI(delegate{checkingUpdate=false;update.Enabled=true;AddLog(finalMessage);if(finalUrl!=null){if(MessageBox.Show(this,finalMessage,"Update available",MessageBoxButtons.YesNo,MessageBoxIcon.Information)==DialogResult.Yes)Process.Start(new ProcessStartInfo(finalUrl){UseShellExecute=true});}else if(!automatic)MessageBox.Show(this,finalMessage,"Check for Updates");});
        });worker.IsBackground=true;worker.Start();
    }
    public static bool IsNewerRelease(string tag,string current){Match remote=Regex.Match(tag??"",@"^v?(\d+\.\d+\.\d+)(?:-([0-9A-Za-z.-]+))?$");Match local=Regex.Match(current,@"^v?(\d+\.\d+\.\d+)(?:-([0-9A-Za-z.-]+))?$");if(!remote.Success||!local.Success)throw new Exception("Unsupported release version label.");int comparison=new Version(remote.Groups[1].Value).CompareTo(new Version(local.Groups[1].Value));return comparison>0||(comparison==0&&!remote.Groups[2].Success&&local.Groups[2].Success);}
    static string ReadReleaseWithGh(string repo){
        ProcessStartInfo start=new ProcessStartInfo("gh","api repos/"+repo+"/releases/latest");start.UseShellExecute=false;start.CreateNoWindow=true;start.RedirectStandardOutput=true;start.RedirectStandardError=true;start.StandardOutputEncoding=Encoding.UTF8;start.EnvironmentVariables["GH_PROMPT_DISABLED"]="1";
        using(Process p=new Process()){p.StartInfo=start;StringBuilder content=new StringBuilder();p.OutputDataReceived+=delegate(object sender,DataReceivedEventArgs e){if(e.Data!=null)content.AppendLine(e.Data);};p.ErrorDataReceived+=delegate{};p.Start();p.BeginOutputReadLine();p.BeginErrorReadLine();if(!p.WaitForExit(20000)){p.Kill();throw new Exception("GitHub CLI timed out.");}p.WaitForExit();if(p.ExitCode!=0)throw new Exception("GitHub CLI could not read the release.");return content.ToString();}
    }
}
