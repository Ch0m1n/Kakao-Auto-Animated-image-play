using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;

internal class SoftPanel : Panel
{
    public Color Fill = Color.White;
    public SoftPanel() { DoubleBuffered=true; BackColor=Color.Transparent; }
    internal static GraphicsPath Rounded(Rectangle r, int radius) {
        var p=new GraphicsPath(); int d=radius*2;
        p.AddArc(r.X,r.Y,d,d,180,90);p.AddArc(r.Right-d,r.Y,d,d,270,90);
        p.AddArc(r.Right-d,r.Bottom-d,d,d,0,90);p.AddArc(r.X,r.Bottom-d,d,d,90,90);p.CloseFigure();return p;
    }
    protected override void OnPaint(PaintEventArgs e) {
        e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;
        using(var p=Rounded(new Rectangle(0,0,Width-1,Height-1),18))
        using(var brush=new SolidBrush(Fill)) e.Graphics.FillPath(brush,p);
        base.OnPaint(e);
    }
}

internal class SoftButton : Button
{
    public bool Primary;
    bool hover;
    public SoftButton() { FlatStyle=FlatStyle.Flat;FlatAppearance.BorderSize=0;Cursor=Cursors.Hand;
        SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true); }
    protected override void OnMouseEnter(EventArgs e) {hover=true;Invalidate();base.OnMouseEnter(e);}
    protected override void OnMouseLeave(EventArgs e) {hover=false;Invalidate();base.OnMouseLeave(e);}
    protected override void OnPaint(PaintEventArgs e) {
        e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;
        var surface=Parent as SoftPanel;
        e.Graphics.Clear(surface!=null?surface.Fill:(Parent!=null?Parent.BackColor:Color.White));
        var color=Primary?(hover?Color.FromArgb(246,208,48):Color.FromArgb(255,223,76)):
            (hover?Color.FromArgb(226,231,234):Color.FromArgb(240,243,245));
        if(!Enabled)color=Color.FromArgb(233,236,238);
        using(var p=SoftPanel.Rounded(new Rectangle(0,0,Width-1,Height-1),10))
        using(var b=new SolidBrush(color))e.Graphics.FillPath(b,p);
        TextRenderer.DrawText(e.Graphics,Text,Font,ClientRectangle,Enabled?Color.FromArgb(30,40,47):Color.Gray,
            TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);
        if(Focused&&ShowFocusCues)ControlPaint.DrawFocusRectangle(e.Graphics,new Rectangle(5,5,Width-10,Height-10));
    }
}

internal class AutoPlayForm : Form
{
    readonly string root,state;
    readonly int ownerPid;
    readonly long ownerCreated;
    readonly JavaScriptSerializer json=new JavaScriptSerializer();
    readonly Color ink=Color.FromArgb(29,43,51),muted=Color.FromArgb(98,112,120);
    readonly Label phase,detail,playing,downloads,notice;
    readonly SoftButton start,stop,startup,diagnostics,logs;
    readonly Timer timer=new Timer();
    bool busy;
    public AutoPlayForm(string bundle) {
        root=bundle;state=Path.Combine(root,"artifacts","kakao-internals");
        using(var self=Process.GetCurrentProcess()){ownerPid=self.Id;ownerCreated=self.StartTime.ToUniversalTime().ToFileTimeUtc();}
        BindOwner();
        Text="Kakao AutoPlay";ClientSize=new Size(584,694);MinimumSize=Size;MaximumSize=Size;
        AutoScaleDimensions=new SizeF(96,96);AutoScaleMode=AutoScaleMode.Dpi;
        FormBorderStyle=FormBorderStyle.FixedSingle;MaximizeBox=false;StartPosition=FormStartPosition.CenterScreen;
        BackColor=Color.FromArgb(245,247,248);Font=new Font("맑은 고딕",10);ForeColor=ink;DoubleBuffered=true;
        AddLabel(this,"AUTOPLAY / PERSONAL",28,24,400,22,9,muted,true);
        AddLabel(this,"움짤은, 움직이게.",28,53,510,45,25,ink,true);
        AddLabel(this,"GIF와 WebP를 채팅방에서 자동으로, 계속.",30,105,520,26,11,muted,false);
        var card=new SoftPanel {Left=28,Top=153,Width=528,Height=240};Controls.Add(card);
        phase=AddLabel(card,"시작할 준비가 됐어요",22,20,480,29,15,ink,true);
        detail=AddLabel(card,"시작을 누르고 카톡 채팅방을 열어 주세요.",22,57,480,26,10,muted,false);
        playing=AddLabel(card,"—",22,101,220,35,22,ink,true);
        downloads=AddLabel(card,"—",278,101,220,35,22,ink,true);
        playing.AutoSize=true;downloads.AutoSize=true;playing.Top=88;downloads.Top=88;
        AddLabel(card,"재생 중인 원본",24,141,220,22,9,muted,false);
        AddLabel(card,"원본 다운로드 완료",280,141,220,22,9,muted,false);
        start=Button(card,"자동재생 시작",22,183,298,42,true);
        stop=Button(card,"중지",332,183,174,42,false);
        start.Click+=async delegate {await Action("start",null);};
        stop.Click+=async delegate {await Action("stop",null);};
        AddLabel(this,"로그인할 때도 자동으로",30,415,260,24,11,ink,true);
        AddLabel(this,"한 번 등록하면 다음 로그인부터 이어져요.",30,445,315,23,9,muted,false);
        startup=Button(this,"자동 시작 등록",367,418,189,42,false);
        startup.Click+=async delegate {await Action("startup",null);};
        AddLabel(this,"문제가 생겼나요?",30,497,280,25,11,ink,true);
        AddLabel(this,"실행·오류 기록을 진단 ZIP으로 저장해 주세요.",30,528,520,23,9,muted,false);
        diagnostics=Button(this,"진단 ZIP 저장",28,565,259,42,false);
        logs=Button(this,"로그 폴더 열기",299,565,257,42,false);
        diagnostics.Click+=async delegate {await Action("diagnostics",null);};
        logs.Click+=delegate {Directory.CreateDirectory(state);Process.Start("explorer.exe","\""+state+"\"");};
        notice=AddLabel(this,"창을 닫거나 도우미를 종료하면 재생 작업도 정리돼요.",30,628,520,48,9,muted,false);
        timer.Interval=2000;timer.Tick+=delegate {RefreshStatus();};timer.Start();
        FormClosed+=delegate {timer.Stop();timer.Dispose();StopOwnedPlayback();};RefreshStatus();
    }
    void BindOwner(){
        Directory.CreateDirectory(state);string path=Path.Combine(state,"autoplay-owner.json"),temporary=Path.Combine(state,"owner-"+Guid.NewGuid().ToString("N")+".tmp");
        try{File.WriteAllText(temporary,json.Serialize(new Dictionary<string,object>{{"pid",ownerPid},{"created",ownerCreated}}),Encoding.UTF8);
            for(int i=0;;i++){try{if(File.Exists(path))File.Replace(temporary,path,null);else File.Move(temporary,path);break;}catch(IOException){if(i>=3)throw;System.Threading.Thread.Sleep(20);}}
        }finally{if(File.Exists(temporary))File.Delete(temporary);}
    }
    void StopOwnedPlayback(){try{var lease=Read("autoplay-owner.json");if(Convert.ToInt64(lease["pid"])==ownerPid&&Convert.ToInt64(lease["created"])==ownerCreated)File.WriteAllText(Path.Combine(state,"autoplay-stop.signal"),"owner-closed",Encoding.ASCII);}catch{}}
    Label AddLabel(Control parent,string text,int x,int y,int w,int h,float size,Color color,bool bold) {
        var surface=parent as SoftPanel;
        var l=new Label {Text=text,Left=x,Top=y,Width=w,Height=h,BackColor=surface!=null?surface.Fill:parent.BackColor,ForeColor=color,
            Font=new Font("맑은 고딕",size,bold?FontStyle.Bold:FontStyle.Regular),AutoEllipsis=true};parent.Controls.Add(l);return l;
    }
    SoftButton Button(Control parent,string text,int x,int y,int w,int h,bool primary) {
        var b=new SoftButton {Text=text,AccessibleName=text,Left=x,Top=y,Width=w,Height=h,Primary=primary,
            Font=new Font("맑은 고딕",10,FontStyle.Bold)};parent.Controls.Add(b);return b;
    }
    Dictionary<string,object> Read(string name) {
        try {return json.Deserialize<Dictionary<string,object>>(File.ReadAllText(Path.Combine(state,name)));}
        catch {return new Dictionary<string,object>();}
    }
    static string Value(Dictionary<string,object> obj,string key,string fallback) {return obj.ContainsKey(key)?Convert.ToString(obj[key]):fallback;}
    void RefreshStatus() {
        if(busy)return;
        var s=Read("autoplay-status.json");double updated;
        bool fresh=double.TryParse(Value(s,"updated","0"),out updated)&&DateTimeOffset.UtcNow.ToUnixTimeSeconds()-updated<10;
        string status=fresh?Value(s,"phase","waiting"):"idle";
        bool running=status=="running";
        phase.Text=running?"자동재생이 켜져 있어요":status=="waiting"||status=="waiting-for-chat"?"채팅방을 기다리고 있어요":
            status=="starting"||status=="reconnecting"?"카톡에 연결하고 있어요":status=="stopping"?"안전하게 종료하고 있어요":"시작할 준비가 됐어요";
        phase.ForeColor=running?Color.FromArgb(29,115,83):ink;
        detail.Text=running?"새 GIF · WebP를 자동으로 받고 반복재생해요.":"시작을 누르고 GIF가 있는 채팅방을 열어 주세요.";
        string lastExit=Value(s,"lastWorkerExit","");
        if(fresh&&!running&&lastExit!=""&&lastExit!="0"&&status!="stopped"&&status!="stopping") {
            phase.Text="재생 작업이 종료돼 다시 연결 중이에요";phase.ForeColor=Color.FromArgb(175,62,44);
            detail.Text="진단 ZIP에 종료 이유를 기록해요. 진단 저장을 눌러 주세요.";
        }
        var p=Read("bitmap-bridge-progress.json");
        var d=p.ContainsKey("originalDownloads")?p["originalDownloads"] as Dictionary<string,object>:null;
        if(running&&d!=null&&Value(d,"available","")=="False")detail.Text="저장된 움짤 재생 중 · 원본 자동 받기는 중단됐어요.";
        playing.Text=running?Value(p,"active","0")+"개":"—";
        downloads.Text=running&&d!=null?Value(d,"saved","0")+"개":"—";
        start.Enabled=!fresh||status=="stopped";stop.Enabled=fresh&&status!="stopped";
        if(Value(s,"connectionError","")!="") {
            phase.Text="카톡 연결을 확인해주세요";phase.ForeColor=Color.FromArgb(175,62,44);
            detail.Text=Value(s,"connectionError","");
        }
    }
    async Task Action(string action,string output) {
        if(busy)return;busy=true;
        foreach(var b in new[]{start,stop,startup,diagnostics,logs})b.Enabled=false;
        notice.ForeColor=muted;notice.Text=action=="diagnostics"?"진단 기록을 모으고 있어요…":"요청을 처리하고 있어요…";
        try {
            if(action=="start"||action=="startup")BindOwner();
            string result=await Task.Run(()=>{
                var info=new ProcessStartInfo(Path.Combine(root,"runtime","python.exe"),"\""+Path.Combine(root,"tools","kakao-portable-launcher.py")+"\" "+action+" --json"+(output==null?"":" --output \""+output+"\""));
                info.UseShellExecute=false;info.CreateNoWindow=true;info.RedirectStandardOutput=true;info.RedirectStandardError=true;
                info.StandardOutputEncoding=Encoding.UTF8;info.StandardErrorEncoding=Encoding.UTF8;info.EnvironmentVariables["PYTHONIOENCODING"]="utf-8";
                using(var process=Process.Start(info)) {
                    var stderr=process.StandardError.ReadToEndAsync();string stdout=process.StandardOutput.ReadToEnd();process.WaitForExit();
                    if(stdout.Trim().Length==0)throw new Exception(stderr.Result.Length>0?stderr.Result:"실행 결과를 받지 못했어요. 로그 폴더를 확인해 주세요.");
                    return stdout.Trim();
                }
            });
            var response=json.Deserialize<Dictionary<string,object>>(result);
            bool ok=Value(response,"ok","False")=="True";
            notice.ForeColor=ok?Color.FromArgb(29,115,83):Color.FromArgb(175,62,44);
            notice.Text=ok&&action=="diagnostics"?"진단 ZIP을 저장했어요. ‘로그 폴더 열기’에서 찾을 수 있어요.":Value(response,"message","처리를 마쳤어요.");
        } catch(Exception error) {notice.ForeColor=Color.FromArgb(175,62,44);notice.Text=error.Message;}
        finally {busy=false;foreach(var b in new[]{startup,diagnostics,logs})b.Enabled=true;RefreshStatus();}
    }
}
