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
using System.Security.Cryptography;

[assembly: AssemblyTitle("Report Layout")]
[assembly: AssemblyVersion("1.1.2.0")]
public sealed class ReportLayout : Form {
    public const string VersionLabel="1.1.2";
    public const string CopyrightNotice="Copyright © 2026 Mohammad Arabi. All rights reserved.";
    const string AccessPasswordHash="08329d8ed0053d1ef450dfecf0b945b09a8c464bbab50c553bf122fe543fe238";
    static string Sha256(string s){using(SHA256 sha=SHA256.Create()){byte[] hash=sha.ComputeHash(Encoding.UTF8.GetBytes(s));StringBuilder sb=new StringBuilder();foreach(byte b in hash)sb.Append(b.ToString("x2"));return sb.ToString();}}
    readonly string root=AppDomain.CurrentDomain.BaseDirectory;
    TextBox report=new TextBox(), template=new TextBox(), title=new TextBox(), output=new TextBox(), log=new TextBox();
    Button chooseReport=new Button(),chooseTemplate=new Button(),chooseOutput=new Button(),build=new Button(),openOutput=new Button(),threadPages=new Button();
    CheckBox cover=new CheckBox(); ProgressBar progress=new ProgressBar();
    bool busy; string lastOutput; NotifyIcon tray; PictureBox logo;
    [STAThread] public static void Main(){
        Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
        if(!SignIn())return;
        Application.Run(new ReportLayout());
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
        Text="Report Layout "+VersionLabel; ClientSize=new Size(840,650);MinimumSize=Size;MaximumSize=Size;StartPosition=FormStartPosition.CenterScreen;
        AutoScaleMode=AutoScaleMode.Dpi;BackColor=Color.FromArgb(245,247,250);Font=new Font("Segoe UI",10);RightToLeft=RightToLeft.No;
        string iconPath=Path.Combine(root,"assets","ReportLayout.ico");
        Icon=new Icon(iconPath,32,32);ShowIcon=true;
        logo=new PictureBox();logo.SetBounds(30,16,100,100);logo.SizeMode=PictureBoxSizeMode.Zoom;logo.Image=Image.FromFile(Path.Combine(root,"assets","ReportLayout.png"));Controls.Add(logo);
        Label h=new Label();h.Text="Report Layout";h.Font=new Font("Segoe UI",23,FontStyle.Bold);h.ForeColor=Color.FromArgb(20,65,120);h.SetBounds(145,23,650,44);Controls.Add(h);
        Label sub=new Label();sub.Text="Turn Word reports into editable InDesign documents.";sub.SetBounds(148,75,650,28);Controls.Add(sub);
        MakeRow(report,chooseReport,"Select Report",133);MakeRow(template,chooseTemplate,"Select Template",184);
        Label tl=new Label();tl.Text="Report Title";tl.SetBounds(30,239,140,30);Controls.Add(tl);title.SetBounds(185,235,625,32);title.RightToLeft=RightToLeft.Yes;Controls.Add(title);
        MakeRow(output,chooseOutput,"Output Folder",286);output.Text=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),"Report Layout");
        template.Text=Path.Combine(root,"assets","Template.idml");
        cover.Text="Add a cover page";cover.Checked=true;cover.SetBounds(30,334,270,28);Controls.Add(cover);
        build.Text="Build Report";build.SetBounds(30,377,220,45);build.BackColor=Color.FromArgb(21,79,158);build.ForeColor=Color.White;build.FlatStyle=FlatStyle.Flat;Controls.Add(build);
        openOutput.Text="Open Output Folder";openOutput.SetBounds(268,377,220,45);openOutput.Enabled=false;Controls.Add(openOutput);
        threadPages.Text="Thread Body Pages...";threadPages.SetBounds(506,377,220,45);Controls.Add(threadPages);threadPages.Click+=delegate{ThreadBodyPages();};
        Label tip=new Label();tip.Text="Minimize to keep the app in the system tray.";tip.SetBounds(30,435,760,24);Controls.Add(tip);
        progress.SetBounds(30,467,780,8);Controls.Add(progress);
        log.SetBounds(30,490,780,134);log.Multiline=true;log.ReadOnly=true;log.ScrollBars=ScrollBars.Vertical;log.BackColor=Color.White;Controls.Add(log);
        Label footer=new Label();footer.Text="Report Layout "+VersionLabel+"   |   "+CopyrightNotice;footer.Font=new Font("Segoe UI",8);footer.ForeColor=Color.Gray;footer.TextAlign=ContentAlignment.MiddleCenter;footer.SetBounds(30,628,780,18);Controls.Add(footer);
        chooseReport.Click+=delegate {using(OpenFileDialog d=new OpenFileDialog()){d.Title="Select Word Report";d.Filter="Word report (*.docx)|*.docx";if(d.ShowDialog()==DialogResult.OK){report.Text=d.FileName;title.Text=Path.GetFileNameWithoutExtension(d.FileName);}}};
        chooseTemplate.Click+=delegate {using(OpenFileDialog d=new OpenFileDialog()){d.Title="Select InDesign Template";d.Filter="InDesign template|*.idml;*.indd;*.indt";if(d.ShowDialog()==DialogResult.OK)template.Text=d.FileName;}};
        chooseOutput.Click+=delegate {using(FolderBrowserDialog d=new FolderBrowserDialog()){d.Description="Select Output Folder";if(d.ShowDialog()==DialogResult.OK)output.Text=d.SelectedPath;}};
        build.Click+=delegate {StartBuild();};openOutput.Click+=delegate {OpenOutput();};
        ContextMenuStrip menu=new ContextMenuStrip();menu.Items.Add("Open Report Layout",null,delegate {ShowMain();});menu.Items.Add("Open Output Folder",null,delegate {OpenOutput();});menu.Items.Add(new ToolStripSeparator());menu.Items.Add("Exit",null,delegate {Close();});
        tray=new NotifyIcon();tray.Icon=new Icon(iconPath,16,16);tray.Text="Report Layout";tray.ContextMenuStrip=menu;tray.Visible=true;tray.DoubleClick+=delegate {ShowMain();};
        Resize+=delegate {if(WindowState==FormWindowState.Minimized)Hide();};
        FormClosing+=delegate(object sender,FormClosingEventArgs e){if(busy){e.Cancel=true;ShowMain();MessageBox.Show(this,"A report is being built. Please wait until the operation finishes.","Report Layout",MessageBoxButtons.OK,MessageBoxIcon.Information);}};
        FormClosed+=delegate {tray.Visible=false;tray.Icon.Dispose();tray.Dispose();menu.Dispose();logo.Image.Dispose();Icon.Dispose();};
        AddLog("Ready. The included template is selected. InDesign must be installed.");
    }
    void ShowMain(){Show();WindowState=FormWindowState.Normal;Activate();}
    void OpenOutput(){if(!String.IsNullOrEmpty(lastOutput)&&Directory.Exists(lastOutput))Process.Start("explorer.exe",QuoteArg(lastOutput));}
    void MakeRow(TextBox box,Button button,string text,int y){button.Text=text;button.SetBounds(30,y-2,140,36);Controls.Add(button);box.SetBounds(185,y,625,32);box.ReadOnly=true;box.RightToLeft=RightToLeft.No;Controls.Add(box);}
    void AddLog(string s){if(InvokeRequired){BeginInvoke(new Action<string>(AddLog),s);return;}log.AppendText("["+DateTime.Now.ToString("HH:mm:ss")+"] "+s+Environment.NewLine);}
    void Busy(bool value){busy=value;foreach(Control c in new Control[]{chooseReport,chooseTemplate,chooseOutput,build,title,cover,threadPages})c.Enabled=!value;progress.Style=value?ProgressBarStyle.Marquee:ProgressBarStyle.Blocks;progress.MarqueeAnimationSpeed=value?30:0;}
    static string QuoteArg(string s){return "\""+s+"\"";}
    void StartBuild(){
        if(!File.Exists(report.Text)||!File.Exists(template.Text)){MessageBox.Show(this,"Select a report and a template.");return;}
        if(String.IsNullOrWhiteSpace(title.Text)){MessageBox.Show(this,"Enter a report title.");return;}
        string engine=Path.Combine(root,"Layout-Report.jsx");if(!File.Exists(engine)){MessageBox.Show(this,"Layout-Report.jsx is missing. Run Start.vbs from the complete extracted package.");return;}
        string folder;
        try{folder=Path.Combine(output.Text,"Report-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+"-"+Guid.NewGuid().ToString("N").Substring(0,6));Directory.CreateDirectory(folder);}catch(Exception ex){MessageBox.Show(this,ex.Message);return;}
        Dictionary<string,object> cfg=new Dictionary<string,object>();cfg["report"]=report.Text;cfg["template"]=template.Text;cfg["title"]=title.Text;cfg["output"]=folder;cfg["cover"]=cover.Checked;
        string config=new JavaScriptSerializer().Serialize(cfg);
        try{File.WriteAllText(Path.Combine(folder,"job-config.json"),config,new UTF8Encoding(false));}catch(Exception ex){MessageBox.Show(this,ex.Message);return;}
        lastOutput=folder;openOutput.Enabled=false;Busy(true);AddLog("Connecting to InDesign. Please leave its documents unchanged until the build finishes.");
        Thread worker=new Thread(delegate(){RunJob(engine,config,folder);});worker.SetApartmentState(ApartmentState.STA);worker.IsBackground=true;worker.Start();
    }
    void ThreadBodyPages(){
        string inddPath;
        using(OpenFileDialog d=new OpenFileDialog()){d.Filter="InDesign document|*.indd";if(d.ShowDialog()!=DialogResult.OK)return;inddPath=d.FileName;}
        int startPage,endPage;
        using(Form dlg=new Form()){
            dlg.Text="Thread Body Pages";dlg.FormBorderStyle=FormBorderStyle.FixedDialog;dlg.StartPosition=FormStartPosition.CenterScreen;
            dlg.ClientSize=new Size(360,190);dlg.MaximizeBox=false;dlg.MinimizeBox=false;dlg.Font=new Font("Segoe UI",10);
            Label l1=new Label();l1.Text="First page to thread:";l1.SetBounds(20,20,190,24);dlg.Controls.Add(l1);
            NumericUpDown startBox=new NumericUpDown();startBox.Minimum=1;startBox.Maximum=100000;startBox.Value=2;startBox.SetBounds(220,18,110,28);dlg.Controls.Add(startBox);
            Label l2=new Label();l2.Text="Last page to thread:";l2.SetBounds(20,56,190,24);dlg.Controls.Add(l2);
            NumericUpDown endBox=new NumericUpDown();endBox.Minimum=1;endBox.Maximum=100000;endBox.Value=100000;endBox.SetBounds(220,54,110,28);dlg.Controls.Add(endBox);
            Label note=new Label();note.Text="Links the first text frame found on each page, in page order. Exclude a cover or a glossary-style page grid from the range. A value above the actual last page is fine -- it is clamped automatically.";note.SetBounds(20,90,320,70);dlg.Controls.Add(note);
            Button ok=new Button();ok.Text="Thread";ok.SetBounds(160,164,90,32);ok.DialogResult=DialogResult.OK;dlg.Controls.Add(ok);
            Button cancel=new Button();cancel.Text="Cancel";cancel.SetBounds(258,164,82,32);cancel.DialogResult=DialogResult.Cancel;dlg.Controls.Add(cancel);
            dlg.AcceptButton=ok;dlg.CancelButton=cancel;
            if(dlg.ShowDialog(this)!=DialogResult.OK)return;
            startPage=(int)startBox.Value;endPage=(int)endBox.Value;
        }
        string engine=Path.Combine(root,"Thread-Body-Pages.jsx");
        if(!File.Exists(engine)){MessageBox.Show(this,"Thread-Body-Pages.jsx is missing.");return;}
        string folder;
        try{folder=Path.Combine(Path.GetTempPath(),"ReportLayout-Thread-"+DateTime.Now.ToString("yyyyMMdd-HHmmss"));Directory.CreateDirectory(folder);}catch(Exception ex){MessageBox.Show(this,ex.Message);return;}
        Busy(true);AddLog("Threading "+Path.GetFileName(inddPath)+" from page "+startPage+" to "+endPage+"...");
        Thread worker=new Thread(delegate(){RunThreadBodyPages(inddPath,startPage,endPage,engine,folder);});worker.SetApartmentState(ApartmentState.STA);worker.IsBackground=true;worker.Start();
    }
    void RunThreadBodyPages(string inddPath,int startPage,int endPage,string engine,string folder){
        object app=null;bool success=false;string message="";
        try{
            Dictionary<string,object> spec=new Dictionary<string,object>();
            spec["inddPath"]=inddPath;spec["startPage"]=startPage;spec["endPage"]=endPage;
            spec["outputResult"]=Path.Combine(folder,"result.json");spec["outputLog"]=Path.Combine(folder,"build-log.txt");
            string json=new JavaScriptSerializer().Serialize(spec);
            string source=File.ReadAllText(engine,Encoding.UTF8);
            source=System.Text.RegularExpressions.Regex.Replace(source,@"(?m)^\s*#target[^\r\n]*","");
            app=Connect();
            app.GetType().InvokeMember("DoScript",BindingFlags.InvokeMethod|BindingFlags.OptionalParamBinding,null,app,new object[]{"var THREAD_SPEC = "+json+";\r\n"+source,1246973031});
            string resultPath=Path.Combine(folder,"result.json");
            if(!File.Exists(resultPath))throw new Exception("InDesign did not record a result. Check build-log.txt in "+folder);
            var result=new JavaScriptSerializer().Deserialize<Dictionary<string,object>>(File.ReadAllText(resultPath,Encoding.UTF8));
            success=result.ContainsKey("ok")&&Convert.ToBoolean(result["ok"]);
            message=Convert.ToString(result.ContainsKey("message")?result["message"]:"");
            if(!success)throw new Exception(message);
        }catch(Exception e){
            success=false;Exception actual=e;while(actual.InnerException!=null)actual=actual.InnerException;message=actual.Message;
            AddLog("Threading failed: "+message);
            string logPath=Path.Combine(folder,"build-log.txt");if(File.Exists(logPath))try{AddLog(File.ReadAllText(logPath));}catch{}
        }finally{
            if(app!=null&&Marshal.IsComObject(app))Marshal.ReleaseComObject(app);
            bool ok=success;string finalMessage=message;
            BeginInvoke(new Action(delegate{
                Busy(false);
                if(ok){AddLog("Threading done: "+finalMessage);MessageBox.Show(this,finalMessage+"\r\n\r\nThe document was saved.","Threading Complete",MessageBoxButtons.OK,MessageBoxIcon.Information);}
            }));
        }
    }
    object Connect(){
        List<string> ids=new List<string>();ids.Add("InDesign.Application.2026");ids.Add("InDesign.Application");
        using(RegistryKey classes=RegistryKey.OpenBaseKey(RegistryHive.ClassesRoot,RegistryView.Registry64)){
            foreach(string key in classes.GetSubKeyNames())if(key.StartsWith("InDesign.Application.",StringComparison.OrdinalIgnoreCase)&&!ids.Contains(key))ids.Add(key);
        }
        Exception last=null;
        foreach(string id in ids){
            try{object running=Marshal.GetActiveObject(id);if(running!=null){AddLog("Connected to "+id);return running;}}catch{}
            try{Type t=Type.GetTypeFromProgID(id,false);if(t!=null){object a=Activator.CreateInstance(t);AddLog("Starting "+id);return a;}}catch(Exception ex){last=ex;}
        }
        throw new Exception("Could not connect to InDesign. Open InDesign and try again, or run Layout-Report.jsx directly from its Scripts panel."+(last==null?"":"\r\n"+last.Message));
    }
    void RunJob(string engine,string config,string folder){
        object app=null;bool success=false;string completedWarnings="";
        try{
            app=Connect();string source=File.ReadAllText(engine,Encoding.UTF8);
            source=System.Text.RegularExpressions.Regex.Replace(source,@"(?m)^\s*#target[^\r\n]*","");
            string code="var REPORT_CONFIG = "+config+";\r\n"+source;
            app.GetType().InvokeMember("DoScript",BindingFlags.InvokeMethod|BindingFlags.OptionalParamBinding,null,app,new object[]{code,1246973031});
            string status=Path.Combine(folder,"result.json");if(!File.Exists(status))throw new Exception("InDesign did not record a result. Check its window and report-log.txt.");
            var result=new JavaScriptSerializer().Deserialize<Dictionary<string,object>>(File.ReadAllText(status,Encoding.UTF8));
            success=result.ContainsKey("ok")&&Convert.ToBoolean(result["ok"]);
            if(!success)throw new Exception(Convert.ToString(result["message"]));
            foreach(string f in new string[]{"Report.indd","Report.idml","Report.pdf"})if(!File.Exists(Path.Combine(folder,f))||new FileInfo(Path.Combine(folder,f)).Length==0)throw new Exception("Missing or empty output: "+f);
            AddLog("Report built. Pages: "+result["pages"]);
            string warnings=Convert.ToString(result["warnings"]);completedWarnings=warnings;if(!String.IsNullOrEmpty(warnings))AddLog("Warnings: "+warnings);
            AddLog("INDD, IDML and PDF have been saved to the output folder.");
        }catch(Exception ex){
            success=false;Exception actual=ex;while(actual.InnerException!=null)actual=actual.InnerException;
            AddLog("Report build failed: "+actual.Message);
            try{File.WriteAllText(Path.Combine(folder,"windows-error.txt"),ex.ToString(),Encoding.UTF8);}catch{}
            string details=Path.Combine(folder,"report-log.txt");if(File.Exists(details))AddLog(File.ReadAllText(details,Encoding.UTF8));
            AddLog("Send report-log.txt and windows-error.txt for troubleshooting.");
        }finally{
            if(app!=null&&Marshal.IsComObject(app))Marshal.ReleaseComObject(app);
            BeginInvoke(new Action(delegate{
                Busy(false);openOutput.Enabled=true;
                if(success){
                    string message="Your report was created successfully.\r\nINDD, IDML and PDF are ready.\r\n\r\n"+folder;
                    if(!String.IsNullOrEmpty(completedWarnings))message+="\r\n\r\nWarnings were recorded. Please review report-log.txt.";
                    tray.ShowBalloonTip(6000,"Report created successfully","Your INDD, IDML and PDF files are ready.",ToolTipIcon.Info);
                    ShowMain();MessageBox.Show(this,message,"Report Created Successfully",MessageBoxButtons.OK,MessageBoxIcon.Information);
                }
            }));
        }
    }
}
