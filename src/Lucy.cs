using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using Forms = System.Windows.Forms;

namespace LucyDesktop {
 [DataContract] public class Reminder {
  [DataMember] public string Id = Guid.NewGuid().ToString("N");
  [DataMember] public string Title = "Hatırlatmam";
  [DataMember] public string Message = "Kendine küçük bir an ayır.";
  [DataMember] public string Time = "09:00";
  [DataMember] public int EveryMinutes;
  [DataMember] public bool Enabled = true;
  [DataMember] public bool WeekdaysOnly;
  [DataMember] public string LastTriggered = "";
  [DataMember] public string DoneDate = "";
  [DataMember] public string Kind = "custom";
  [DataMember] public string Group = "daily";
  [DataMember] public string Icon = "check";
  [DataMember] public bool NoNotification;
  [DataMember] public int Order;
  [DataMember] public string Quantity = "";
 }
 [DataContract] public class ShoppingItem {
  [DataMember] public string Id = Guid.NewGuid().ToString("N");
  [DataMember] public string Title = "";
  [DataMember] public string Quantity = "";
  [DataMember] public string SectionId = "";
  [DataMember] public string Icon = "bag";
  [DataMember] public bool Checked;
 }
 [DataContract] public class Preferences {
  [DataMember] public int Version = 3;
  [DataMember] public List<UserSection> Sections = new List<UserSection>();
  [DataMember] public bool SectionsInitialized;
  [DataMember] public string TrackingSince = "";
  [DataMember] public int DefaultWaterMl = 200;
  [DataMember] public List<TaskLog> TaskLogs = new List<TaskLog>();
  [DataMember] public List<RoutineRecord> RoutineRecords = new List<RoutineRecord>();
  [DataMember] public List<PlanItem> Plans = new List<PlanItem>();
  [DataMember] public List<DrinkEntry> Drinks = new List<DrinkEntry>();
  [DataMember] public List<SkinProduct> Products = new List<SkinProduct>();
  [DataMember] public List<Measurement> Measurements = new List<Measurement>();
  [DataMember] public List<string> PromptKeys = new List<string>();
  [DataMember] public bool MeasurementImportDone;
  [DataMember] public bool SoundEnabled = true;
  [DataMember] public bool Adventures = true;
  [DataMember] public bool MotionReviewPending;
  [DataMember] public int[] AllowedMotions = new[]{1,2,5};
  [DataMember] public List<ShoppingItem> Shopping = new List<ShoppingItem>();
  [DataMember] public string Name = "Lucy";
  [DataMember] public string Skin = "lucy";
  [DataMember] public double Size = 205;
  [DataMember] public bool Roam = false;
  [DataMember] public bool OnTop = false;
  [DataMember] public bool FullscreenHide = true;
  [DataMember] public bool Chats = true;
  [DataMember] public bool Quiet = true;
  [DataMember] public string QuietStart = "00:00";
  [DataMember] public string QuietEnd = "07:00";
  [DataMember] public double Left = -99999;
  [DataMember] public double Top = -99999;
  [DataMember] public string WaterDate = "";
  [DataMember] public int WaterCount;
  [DataMember] public List<Reminder> Reminders = new List<Reminder>();
  public static Preferences EmptyProfile() {
   var profile=new Preferences();
   Tracking.Initialize(profile);
   return profile;
  }
  public static Preferences Defaults() {
   var p = new Preferences();
   p.Reminders.Add(new Reminder { Id="morning", Title="Sabah cilt bakımın", Message="Günaydın! Sana iyi gelen sabah cilt bakım rutinine birkaç dakika ayıralım mı?", Time="07:30", Kind="care", WeekdaysOnly=true, Group="morning", Icon="sun" });
   p.Reminders.Add(new Reminder { Id="evening", Title="Akşam cilt bakımın", Message="Günün koşturması bitti. Sana iyi gelen akşam cilt bakım rutinini hatırlatayım dedim. Mır!", Time="22:00", Kind="care", WeekdaysOnly=true, Group="evening", Icon="moon" });
   p.Reminders.Add(new Reminder { Id="water", Title="Su molası", Message="Su bardağın yakınlarda mı? Küçük bir su molasına ne dersin?", EveryMinutes=120, Kind="water", Group="general", Icon="water" });
   p.Reminders.Add(new Reminder { Id="break", Title="Biraz hareket", Message="Pati esnetme zamanı! İstersen ayağa kalk, omuzlarını gevşet ve biraz uzağa bak.", EveryMinutes=60, Kind="break", Group="general", Icon="flower" });
   p.Reminders.Add(new Reminder { Id="history", Title="Tarihte bugün", Message="", Time="10:00", Kind="history", Group="general", Icon="book" });
   p.Reminders.Add(new Reminder { Id="sleep", Title="Geceye yumuşak geçiş", Message="Günü yavaşça kapatalım mı? Yarın için aklındakileri bir yere yazıp kendine dinlenme payı bırakabilirsin.", Time="22:30", Kind="sleep", Group="general", Icon="moon" });
   Tracking.Initialize(p);return p;
  }
 }
 public static class Store {
  public static bool TestMode;
  public static readonly string Root = AppDomain.CurrentDomain.BaseDirectory;
  public static readonly string Data = System.IO.Path.Combine(Root,"data");
  public static readonly string SettingsPath = System.IO.Path.Combine(Data,"settings.json");
  public static void Log(Exception e) { try { Directory.CreateDirectory(Data); File.AppendAllText(System.IO.Path.Combine(Data,"errors.log"),DateTime.Now.ToString("s")+" "+e+Environment.NewLine); } catch {} }
  public static Preferences Load() {
   if (!File.Exists(SettingsPath)) return Preferences.EmptyProfile();
   try { using(var f=File.OpenRead(SettingsPath)) { var p=(Preferences)new DataContractJsonSerializer(typeof(Preferences)).ReadObject(f); Validate(p); return p; } }
   catch(Exception e) { Log(e); try { File.Copy(SettingsPath,SettingsPath+".damaged-"+DateTime.Now.ToString("yyyyMMddHHmmss"),false); } catch {} return Preferences.EmptyProfile(); }
  }
  public static void Validate(Preferences p) {
   if (p==null) throw new InvalidDataException("Ayar dosyası boş.");
   if (string.IsNullOrWhiteSpace(p.Name)) p.Name="Lucy";
   p.Name=p.Name.Trim().Substring(0,Math.Min(24,p.Name.Trim().Length));
   if(double.IsNaN(p.Size)||double.IsInfinity(p.Size)) p.Size=185;
   p.Size=Math.Max(120,Math.Min(240,p.Size));
   if(double.IsNaN(p.Left)||double.IsInfinity(p.Left)) p.Left=-99999;
   if(double.IsNaN(p.Top)||double.IsInfinity(p.Top)) p.Top=-99999;
   if(p.Reminders==null) p.Reminders=new List<Reminder>();
   DataUpgrade.Apply(p); Tracking.Initialize(p);
   if(p.Shopping==null)p.Shopping=new List<ShoppingItem>();
   p.Shopping=p.Shopping.Where(i=>i!=null&&!string.IsNullOrWhiteSpace(i.Title)).Take(500).ToList();
   p.Reminders=p.Reminders.Where(r=>r!=null).ToList();
   foreach(var r in p.Reminders) { if(string.IsNullOrEmpty(r.Id))r.Id=Guid.NewGuid().ToString("N"); r.EveryMinutes=Math.Max(0,Math.Min(1440,r.EveryMinutes)); if(r.EveryMinutes>0)r.EveryMinutes=Math.Max(5,r.EveryMinutes); if(!Scheduler.ValidTime(r.Time))r.Time=r.NoNotification?"":"09:00"; if(string.IsNullOrWhiteSpace(r.Title))r.Title="Hatırlatmam"; if(r.Message==null)r.Message=""; }
   if(!Scheduler.ValidTime(p.QuietStart))p.QuietStart="00:00";
   if(!Scheduler.ValidTime(p.QuietEnd))p.QuietEnd="07:00";
  }
  public static void Save(Preferences p) {
   if(TestMode)return;
   Tracking.SyncPlan(p,DateTime.Today);
   Directory.CreateDirectory(Data);
   var temp=SettingsPath+".tmp";
   using(var f=File.Create(temp))new DataContractJsonSerializer(typeof(Preferences)).WriteObject(f,p);
   if(File.Exists(SettingsPath))File.Replace(temp,SettingsPath,SettingsPath+".bak"); else File.Move(temp,SettingsPath);
  }
 }
 public static class Scheduler {
  public static bool ValidTime(string s) { TimeSpan t; return s!=null&&s.Length==5&&TimeSpan.TryParseExact(s,"hh\\:mm",CultureInfo.InvariantCulture,out t)&&t.TotalHours<24; }
  public static bool QuietAt(Preferences p,DateTime now) {
   if(!p.Quiet)return false;
   var a=TimeSpan.Parse(p.QuietStart); var b=TimeSpan.Parse(p.QuietEnd); var t=now.TimeOfDay;
   return a==b?false:(a<b?t>=a&&t<b:t>=a||t<b);
  }
  public static bool Due(Reminder r,DateTime now,DateTime sessionStart) {
   if(!r.Enabled||r.NoNotification||r.DoneDate==now.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture))return false;
   if(r.WeekdaysOnly&&(now.DayOfWeek==DayOfWeek.Saturday||now.DayOfWeek==DayOfWeek.Sunday))return false;
   DateTime last; bool has=DateTime.TryParse(r.LastTriggered,CultureInfo.InvariantCulture,DateTimeStyles.RoundtripKind,out last);
   if(r.EveryMinutes>0) { var basis=has?last:sessionStart; return now>=basis&&now-basis>=TimeSpan.FromMinutes(r.EveryMinutes); }
   var due=now.Date+TimeSpan.Parse(r.Time);
   return now>=due&&now-due<TimeSpan.FromMinutes(15)&&(!has||last.Date!=now.Date);
  }
 }
 public static class Shortcuts {
  public const string LinkName="Lucy - Masaüstü Dostun.lnk";
  public static string StartupPath {get{return System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup),LinkName);}}
  public static string ExePath {get{return Assembly.GetExecutingAssembly().Location;}}
  public static bool AutoStart { get {return File.Exists(StartupPath);} }
  public static void Create(string path,string args) {
   object shell=null,link=null;
   try {
    var type=Type.GetTypeFromProgID("WScript.Shell"); shell=Activator.CreateInstance(type);
    link=type.InvokeMember("CreateShortcut",BindingFlags.InvokeMethod,null,shell,new object[]{path});
    var lt=link.GetType();
    if(File.Exists(path)){var target=(string)lt.InvokeMember("TargetPath",BindingFlags.GetProperty,null,link,null);if(!string.Equals(target,ExePath,StringComparison.OrdinalIgnoreCase))throw new IOException("Bu isimde başka bir kısayol var: "+path);}
    lt.InvokeMember("TargetPath",BindingFlags.SetProperty,null,link,new object[]{ExePath});
    lt.InvokeMember("Arguments",BindingFlags.SetProperty,null,link,new object[]{args});
    lt.InvokeMember("WorkingDirectory",BindingFlags.SetProperty,null,link,new object[]{Store.Root});
    lt.InvokeMember("Description",BindingFlags.SetProperty,null,link,new object[]{"Lucy • Masaüstündeki küçük dostun"});
    lt.InvokeMember("IconLocation",BindingFlags.SetProperty,null,link,new object[]{ExePath+",0"});
    lt.InvokeMember("Save",BindingFlags.InvokeMethod,null,link,null);
   } finally { if(link!=null)Marshal.FinalReleaseComObject(link); if(shell!=null)Marshal.FinalReleaseComObject(shell); }
  }
  public static void SetAutoStart(bool enabled) { if(enabled)Create(StartupPath,"--startup");else if(File.Exists(StartupPath))File.Delete(StartupPath); }
 }
 public class HistoryCard {
  public string Title,Text,Url;
  public static HistoryCard Calendar(DateTime now) {
   var tr=CultureInfo.GetCultureInfo("tr-TR");
   return new HistoryCard { Title="Bugünün küçük takvimi", Text=now.ToString("d MMMM yyyy, dddd",tr)+". Yılın "+now.DayOfYear+". günü. Bu yılda "+(new DateTime(now.Year,12,31)-now.Date).Days+" gün daha var. Tarihte bu güne ait olaylar için kaynağı açabilirsin.", Url="https://tr.wikipedia.org/wiki/"+Uri.EscapeDataString(now.ToString("d_MMMM",tr)) };
  }
  public static async Task<HistoryCard> Fetch(DateTime now) {
   var result=Calendar(now); string key=now.ToString("yyyy-MM-dd"); string cache=System.IO.Path.Combine(Store.Data,"history-"+key+".json");
   try {
    var json=new JavaScriptSerializer();
    if(File.Exists(cache)) { var c=json.Deserialize<HistoryCard>(File.ReadAllText(cache)); if(c!=null&&!string.IsNullOrEmpty(c.Text))return c; }
    ServicePointManager.SecurityProtocol=SecurityProtocolType.Tls12;
    string title=now.ToString("d MMMM",CultureInfo.GetCultureInfo("tr-TR"));
    string url="https://tr.wikipedia.org/w/api.php?action=query&prop=extracts&explaintext=1&format=json&redirects=1&titles="+Uri.EscapeDataString(title);
    using(var client=new TimedWebClient()) {
     client.Encoding=Encoding.UTF8;client.Headers[HttpRequestHeader.UserAgent]="LucyDesktop/1.0 (personal desktop calendar; Wikipedia source link displayed)";
     var raw=await client.DownloadStringTaskAsync(url);
     var root=(Dictionary<string,object>)json.DeserializeObject(raw);
     var query=(Dictionary<string,object>)root["query"];var pages=(Dictionary<string,object>)query["pages"];
     foreach(var page in pages.Values) {
      var record=(Dictionary<string,object>)page; if(!record.ContainsKey("extract"))continue;
      var extract=(string)record["extract"]; var section=extract.IndexOf("Olaylar",StringComparison.OrdinalIgnoreCase);if(section<0)continue;
      var rest=extract.Substring(section+7); var lines=rest.Split(new[]{'\r','\n'},StringSplitOptions.RemoveEmptyEntries);
      var candidates=lines.Select(s=>s.Trim()).Where(s=>s.Length>20&&s.Length<360&&char.IsDigit(s[0])&&(s.Contains(" - ")||s.Contains(" – ")||s.Contains(" — "))).Take(30).ToList();
      if(candidates.Count==0)continue;
      var selected=candidates[(now.DayOfYear+now.Year)%candidates.Count];
      result=new HistoryCard {Title="Tarihte bugün • "+title,Text=selected,Url=result.Url};
      Directory.CreateDirectory(Store.Data); File.WriteAllText(cache,json.Serialize(result),Encoding.UTF8);return result;
     }
    }
   } catch { /* Offline: keep the honest calendar card; never invent an event. */ }
   return result;
  }
 }
 public class TimedWebClient:WebClient { protected override WebRequest GetWebRequest(Uri address) {var r=base.GetWebRequest(address);r.Timeout=8000;return r;} }
 public static partial class UI {
  public static Brush B(string s){return (Brush)new BrushConverter().ConvertFromString(s);}
  public static readonly FontFamily AppFont=new FontFamily(new Uri(System.IO.Path.Combine(Store.Root,"assets","fonts")+System.IO.Path.DirectorySeparatorChar),"./#DM Sans 14pt");
  public static readonly FontFamily HeadingFont=new FontFamily(new Uri(System.IO.Path.Combine(Store.Root,"assets","fonts")+System.IO.Path.DirectorySeparatorChar),"./#Manrope");
  public static readonly Brush Ink=B("#2C332E"),Muted=B("#656B61"),Mint=B("#DFE1D6"),Cream=B("#F4F1EA");
  public static TextBlock Text(string s,double size,Brush brush=null) {return new TextBlock{Text=s,FontSize=Math.Max(size,13),Foreground=brush??Ink,TextWrapping=TextWrapping.Wrap,FontFamily=size>=17?UI.HeadingFont:UI.AppFont};}
  public static Button Button(string label,Action action,bool accent=false) {
   var b=new Button {Content=label,Tag=accent?"Lucy.Accent":"Lucy.Soft",Padding=new Thickness(14,8,14,8),Margin=new Thickness(0,0,8,0),Background=accent?B(InkColor):B(SoftColor),Foreground=accent?B(PaperColor):Ink,FontFamily=AppFont,BorderThickness=new Thickness(0),Cursor=Cursors.Hand,FontSize=14,MinHeight=34};
   b.Template=(ControlTemplate)System.Windows.Markup.XamlReader.Parse("<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' TargetType='Button'><Border x:Name='bd' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' CornerRadius='10' Background='{TemplateBinding Background}' Padding='{TemplateBinding Padding}'><ContentPresenter HorizontalAlignment='Center' VerticalAlignment='Center'/></Border><ControlTemplate.Triggers><Trigger Property='IsMouseOver' Value='True'><Setter TargetName='bd' Property='Opacity' Value='0.82'/></Trigger><Trigger Property='IsPressed' Value='True'><Setter TargetName='bd' Property='Opacity' Value='0.65'/></Trigger><Trigger Property='IsEnabled' Value='False'><Setter TargetName='bd' Property='Opacity' Value='0.45'/></Trigger></ControlTemplate.Triggers></ControlTemplate>");
   b.Click+=(s,e)=>action();return b;
  }
  public static Border Card(UIElement child) {return new Border{Background=B(SurfaceColor),CornerRadius=new CornerRadius(16),Padding=new Thickness(18),Margin=new Thickness(0,0,0,12),Child=child,BorderBrush=B(BorderColor),BorderThickness=new Thickness(1)};}
  public static TextBox Input(string value,int max=200) {return new TextBox{FontFamily=AppFont,Text=value??"",FontSize=15,Padding=new Thickness(9,7,9,7),BorderBrush=B(BorderColor),BorderThickness=new Thickness(1),Background=B(TintColor(.018)),Foreground=Ink,MaxLength=max,Margin=new Thickness(0,5,0,12)};}
  public static CheckBox Check(string text,bool value) {return new CheckBox{Template=WarmCheckTemplate,FontFamily=AppFont,Content=text,IsChecked=value,FontSize=14,Foreground=Ink,Margin=new Thickness(0,6,0,9),VerticalContentAlignment=VerticalAlignment.Center};}
  public static void OpenUrl(string url) {try { if(url.StartsWith("https://",StringComparison.OrdinalIgnoreCase))Process.Start(new ProcessStartInfo(url){UseShellExecute=true});}catch(Exception e){Store.Log(e);} }
 }
 public class AlphaImage:Image {
  byte[] pixels; int pw,ph;
  public void SetFrame(BitmapSource b) {if(Source==b)return;Source=b;var f=new FormatConvertedBitmap(b,PixelFormats.Bgra32,null,0);pw=f.PixelWidth;ph=f.PixelHeight;pixels=new byte[pw*ph*4];f.CopyPixels(pixels,pw*4,0);}
  protected override HitTestResult HitTestCore(PointHitTestParameters p) {
   if(pixels==null||ActualWidth<=0||ActualHeight<=0)return null;
   var x=(int)(p.HitPoint.X/ActualWidth*pw);var y=(int)(p.HitPoint.Y/ActualHeight*ph);
   if(x<0||y<0||x>=pw||y>=ph||pixels[(y*pw+x)*4+3]<30)return null;
   return new PointHitTestResult(this,p.HitPoint);
  }
 }
 public static class Sprites {
  public static BitmapSource Load(string filename){var b=new BitmapImage();b.BeginInit();b.CacheOption=BitmapCacheOption.OnLoad;b.UriSource=new Uri(System.IO.Path.Combine(Store.Root,"assets",filename));b.EndInit();b.Freeze();return b;}
  public static BitmapSource Frame(BitmapSource source,int index){
   // The generated atlas has a clear row gap at 689..692, rather than its geometric midpoint.
   int x=index%2*627,y=index<2?0:693,w=627,h=index<2?689:561;
   if(source.PixelWidth!=1254||source.PixelHeight!=1254){w=source.PixelWidth/2;h=source.PixelHeight/2;x=index%2*w;y=index/2*h;}
   var crop=new CroppedBitmap(source,new Int32Rect(x,y,w,h));var b=new FormatConvertedBitmap(crop,PixelFormats.Bgra32,null,0);var data=new byte[w*h*4];b.CopyPixels(data,w*4,0);
   int left=w,top=h,right=0,bottom=0;
   for(int py=0;py<h;py++)for(int px=0;px<w;px++)if(data[(py*w+px)*4+3]>30){left=Math.Min(left,px);right=Math.Max(right,px);top=Math.Min(top,py);bottom=Math.Max(bottom,py);}
   if(left>=right||top>=bottom)throw new InvalidDataException("Lucy sprite karesi boş.");
   var trimmed=new CroppedBitmap(crop,new Int32Rect(left,top,right-left+1,bottom-top+1));
   double factor=Math.Min(476.0/trimmed.PixelWidth,472.0/trimmed.PixelHeight);double dw=trimmed.PixelWidth*factor,dh=trimmed.PixelHeight*factor;
   var visual=new DrawingVisual();using(var dc=visual.RenderOpen())dc.DrawImage(trimmed,new Rect((512-dw)/2,492-dh,dw,dh));
   var result=new RenderTargetBitmap(512,512,96,96,PixelFormats.Pbgra32);result.Render(visual);result.Freeze();return result;
  }
  public static void MakeIcon(string path){var sprite=Frame(Load("lucy-sprites.png"),0);var visual=new DrawingVisual();using(var dc=visual.RenderOpen())dc.DrawImage(sprite,new Rect(0,0,256,256));var image=new RenderTargetBitmap(256,256,96,96,PixelFormats.Pbgra32);image.Render(visual);var png=new PngBitmapEncoder();png.Frames.Add(BitmapFrame.Create(image));using(var memory=new MemoryStream()){png.Save(memory);var bytes=memory.ToArray();using(var writer=new BinaryWriter(File.Create(path))){writer.Write((ushort)0);writer.Write((ushort)1);writer.Write((ushort)1);writer.Write((byte)0);writer.Write((byte)0);writer.Write((byte)0);writer.Write((byte)0);writer.Write((ushort)1);writer.Write((ushort)32);writer.Write(bytes.Length);writer.Write(22);writer.Write(bytes);}}}
 }
 public class Notice {public string Title,Text,Url; public Reminder Reminder; public bool AllowDone,PlaySound; public Action OpenAction; public Func<bool> StillRelevant;public string ActionLabel;public DateTime ValidUntil;}
 public partial class PetWindow:Window {
  public Preferences P;
  public readonly Canvas Root=new Canvas();
  AlphaImage pet=new AlphaImage();
  BitmapSource[] lucyFrames;
  BitmapSource cream;
  Border bubble;
  Polygon bubbleTail;
  TextBlock bubbleTitle,bubbleText,activity;
  StackPanel bubbleActions;
  ScaleTransform scale=new ScaleTransform(1,1);
  RotateTransform rotate=new RotateTransform();
  TranslateTransform shift=new TranslateTransform();
  DispatcherTimer animation,scheduler;
  readonly Stopwatch watch=Stopwatch.StartNew();
  readonly Random random=new Random();
  readonly Queue<Notice> pending=new Queue<Notice>();
  readonly DateTime started=DateTime.Now;
  DateTime bubbleUntil=DateTime.MinValue,nextNotice=DateTime.MinValue,snoozeUntil=DateTime.MinValue,nextChat=DateTime.Now.AddMinutes(40),focusUntil=DateTime.MinValue;
  HistoryCard history;
  string historyDay;
  Notice current;
  double happyStart=-100;
  bool dragging,moved,parkedAway,userHidden,quitting;
  Point mouseStart,dragOrigin;
  Forms.NotifyIcon tray;
  SettingsWindow settings;
  int frameIndex=-1;
  public bool FocusActive {get{return focusUntil>DateTime.Now;}}
  public BitmapSource PreviewImage {get{return lucyFrames[0];}}
  public PetWindow(Preferences prefs,bool automatic=false) {
   P=prefs;Title="Lucy • Masaüstü Dostun";Width=340;Height=P.Size+205;WindowStyle=WindowStyle.None;ResizeMode=ResizeMode.NoResize;AllowsTransparency=true;Background=null;ShowInTaskbar=false;ShowActivated=false;
   FontFamily=UI.AppFont;Content=Root;Root.Background=null;
   try {Icon=new BitmapImage(new Uri(System.IO.Path.Combine(Store.Root,"assets","lucy.ico")));}catch{}
   LoadSprites();
   var transforms=new TransformGroup();transforms.Children.Add(scale);transforms.Children.Add(rotate);transforms.Children.Add(shift);pet.RenderTransform=transforms;pet.RenderTransformOrigin=new Point(.5,.9);pet.Cursor=Cursors.Hand;pet.ToolTip="Sevmek için tıkla • Taşımak için sürükle • Menü için sağ tıkla";RenderOptions.SetBitmapScalingMode(pet,BitmapScalingMode.HighQuality);
   Root.Children.Add(pet);BuildBubble();
   activity=UI.Text("",12,UI.B(UI.OliveColor));activity.IsHitTestVisible=false;activity.FontWeight=FontWeights.SemiBold;activity.TextAlignment=TextAlignment.Center;activity.Width=340;Root.Children.Add(activity);
   ApplyPreferences();
   pet.MouseLeftButtonDown+=PetMouseDown;pet.MouseMove+=PetMouseMove;pet.MouseLeftButtonUp+=PetMouseUp;pet.LostMouseCapture+=(s,e)=>dragging=false;
   pet.MouseRightButtonUp+=(s,e)=>{ShowMenu(pet);e.Handled=true;};
   Loaded+=(s,e)=>{
    AnchorToCorner();
    var h=new WindowInteropHelper(this).Handle;var style=Native.GetWindowLong(h,-20);Native.SetWindowLong(h,-20,style|0x08000000|0x00000080);
    var source=HwndSource.FromHwnd(h);if(source!=null)source.AddHook(KeepWidgetVisible);
    if(!Store.TestMode)SetUpTray(); StartTimers();
    if(automatic)BeginStartupWelcome();
    else Say(new Notice{Title="Merhaba, ben "+P.Name+"!",Text="Masaüstündeki küçük dostunum. Sağ tıkla; rutinlerini ve görünümümü birlikte ayarlayalım."},true);
    if(!Store.TestMode)RefreshHistory();InitializeAdventureClock();
   };
   StateChanged+=(s,e)=>{if(!userHidden&&WindowState!=WindowState.Normal)WindowState=WindowState.Normal;};
   Closing+=(s,e)=>{if(!quitting){e.Cancel=true;return;}ClosePeek();if(animation!=null)animation.Stop();if(scheduler!=null)scheduler.Stop();SavePosition();if(tray!=null){tray.Visible=false;tray.Dispose();}};
  }
  IntPtr KeepWidgetVisible(IntPtr hwnd,int message,IntPtr wParam,IntPtr lParam,ref bool handled){
   if(message==0x0112&&(wParam.ToInt64()&0xfff0)==0xf020&&!userHidden)handled=true;
   return IntPtr.Zero;
  }
  void RaiseWidget(){
   if(userHidden)return;
   var hwnd=new WindowInteropHelper(this).Handle;
   if(hwnd!=IntPtr.Zero)Native.SetWindowPos(hwnd,IntPtr.Zero,0,0,0,0,0x13);
  }

  void LoadSprites() {
   var bmp=Sprites.Load("lucy-sprites.png");lucyFrames=new BitmapSource[4];
   for(int i=0;i<4;i++)lucyFrames[i]=Sprites.Frame(bmp,i);
   cream=Sprites.Load("pati.png");
  }
  void BuildBubble() {
   var stack=new StackPanel();
   var header=new DockPanel();var close=UI.Button("×",()=>HideBubble());close.Padding=new Thickness(7,0,7,0);close.MinHeight=22;close.Margin=new Thickness(5,0,0,0);DockPanel.SetDock(close,Dock.Right);header.Children.Add(close);
   bubbleTitle=UI.Text("Lucy",12,UI.B(UI.ClayColor));bubbleTitle.FontFamily=UI.HeadingFont;bubbleTitle.FontWeight=FontWeights.SemiBold;bubbleTitle.Margin=new Thickness(0,3,0,5);header.Children.Add(bubbleTitle);stack.Children.Add(header);
   bubbleText=UI.Text("",13);bubbleText.LineHeight=19;bubbleText.MaxHeight=78;bubbleText.TextTrimming=TextTrimming.CharacterEllipsis;stack.Children.Add(bubbleText);
   bubbleActions=new StackPanel{Orientation=Orientation.Horizontal,Margin=new Thickness(0,12,0,0)};stack.Children.Add(bubbleActions);
   bubble=new Border{Width=316,Background=UI.B(UI.PaperColor),CornerRadius=new CornerRadius(20,20,20,5),Padding=new Thickness(17,12,14,14),Child=stack,Visibility=Visibility.Collapsed,Effect=new DropShadowEffect{BlurRadius=16,ShadowDepth=3,Opacity=.14,Color=Colors.Black}};
   Canvas.SetLeft(bubble,12);Canvas.SetTop(bubble,8);Root.Children.Add(bubble);
   bubbleTail=new Polygon{Fill=UI.B(UI.PaperColor),Points=new PointCollection{new Point(0,0),new Point(20,0),new Point(19,15)},IsHitTestVisible=false,Visibility=Visibility.Collapsed};Canvas.SetLeft(bubbleTail,137);Root.Children.Add(bubbleTail);bubble.SizeChanged+=(s,e)=>PositionBubble(Canvas.GetTop(bubble));bubble.IsVisibleChanged+=(s,e)=>bubbleTail.Visibility=bubble.Visibility;
   bubble.MouseEnter+=(s,e)=>bubbleUntil=DateTime.Now.AddSeconds(25);
  }
  public void ApplyPreferences() {
   EndAdventure();
   Height=P.Size+205;pet.Width=P.Size;pet.Height=P.Size;Canvas.SetLeft(pet,(Width-P.Size)/2);Canvas.SetTop(pet,195);Canvas.SetTop(activity,178);frameIndex=-1;Frame(0);
   if(IsLoaded){PositionMascot();RefreshPeek();}
  }
  void Frame(int index) {if(frameIndex==index)return;frameIndex=index;pet.SetFrame(P.Skin=="cream"?cream:lucyFrames[index]);}
  void StartTimers() {
   animation=new DispatcherTimer(DispatcherPriority.Background){Interval=TimeSpan.FromMilliseconds(33)};animation.Tick+=(s,e)=>Animate();animation.Start();
   scheduler=new DispatcherTimer(DispatcherPriority.Background){Interval=TimeSpan.FromSeconds(2)};scheduler.Tick+=(s,e)=>Tick();scheduler.Start();
  }
  void Animate() {
   double t=watch.Elapsed.TotalSeconds;
   if(adventure!=null)return;
   if(!IsVisible)return;
   bool sleeping=Scheduler.QuietAt(P,DateTime.Now)||DateTime.Now<snoozeUntil||FocusActive;
   if(!sleeping&&!dragging&&t-happyStart>=3&&DrawAdventureRest(t))return;
   var happy=t-happyStart;double hop=happy>=0&&happy<1.2?Math.Sin(happy/1.2*Math.PI)*36:0;
   shift.Y=-hop-(sleeping?Math.Sin(t*1.6)*1.2:Math.Abs(Math.Sin(t*1.9))*3);
   rotate.Angle=Math.Sin(t*1.5)*.8;
   scale.ScaleX=1+(sleeping?Math.Sin(t*1.6)*.009:Math.Sin(t*1.9)*.008);scale.ScaleY=1-(sleeping?Math.Sin(t*1.6)*.009:Math.Sin(t*1.9)*.008);
   Frame(sleeping?2:happy<3?1:0);
   activity.Text=FocusActive?"◌  Odak zamanı • "+Math.Ceiling((focusUntil-DateTime.Now).TotalMinutes)+" dk":DateTime.Now<snoozeUntil?"z z z  • biraz uyukluyorum":Scheduler.QuietAt(P,DateTime.Now)?"☾  sessiz saatler":"";
  }
  void Tick() {
   var now=DateTime.Now; RefreshForDate();
   EnsureDesktopCorner();
   if(adventure!=null&&Area()!=adventure.Stage.Area)EndAdventure();
   if(bubble.Visibility==Visibility.Visible&&now>bubbleUntil)HideBubble();
   if(focusUntil!=DateTime.MinValue&&now>=focusUntil){focusUntil=DateTime.MinValue;pending.Enqueue(new Notice{Title="Odak turu tamamlandı!",Text="25 dakikayı kendine ayırdın. Şimdi kısa bir mola ve kocaman bir pati tebriği. Mır!"});}
   bool quiet=Scheduler.QuietAt(P,now)||now<snoozeUntil||FocusActive||userHidden;
   var weekly=Reports.FinalizeDue(P,now);if(weekly!=null){pending.Enqueue(PromptNotice(weekly));Save();}
   if(!quiet) {
    var prompts=Tracking.Prompts(P,now);foreach(var prompt in prompts)if(pending.Count<20)pending.Enqueue(PromptNotice(prompt));
    bool changed=prompts.Count>0;
    foreach(var r in P.Reminders)if(Scheduler.Due(r,now,started)){
     r.LastTriggered=now.ToString("o",CultureInfo.InvariantCulture);changed=true;
     if(pending.Count<15)pending.Enqueue(MakeNotice(r));
    }
    if(changed)Save();
    if(now>=nextChat&&P.Chats&&pending.Count==0){nextChat=now.AddMinutes(40+random.Next(21));pending.Enqueue(RandomNotice());}
    if(adventure==null&&!userHidden&&now>=nextNotice&&bubble.Visibility!=Visibility.Visible&&pending.Count>0){var n=pending.Dequeue();if((n.ValidUntil==DateTime.MinValue||now<=n.ValidUntil)&&(n.StillRelevant==null||n.StillRelevant())&&(n.Reminder==null||P.Reminders.Contains(n.Reminder)&&n.Reminder.Enabled&&n.Reminder.DoneDate!=now.ToString("yyyy-MM-dd"))&&!TryNoticeActivity(n,now))Say(n,false);}
   }
   if(!Store.TestMode&&historyDay!=now.ToString("yyyy-MM-dd"))RefreshHistory();MaybeStartAdventure(now);
  }
  async void RefreshHistory() {historyDay=DateTime.Now.ToString("yyyy-MM-dd");history=await HistoryCard.Fetch(DateTime.Now);}
  Notice MakeNotice(Reminder r) {
   if(r.Kind=="history"){var h=history??HistoryCard.Calendar(DateTime.Now);return new Notice{Title=h.Title,Text=h.Text,Url=h.Url,Reminder=r};}
   return new Notice{Title=r.Title,Text=r.Message,Reminder=r,AllowDone=true};
  }
  public void Say(Notice n,bool manual) {
   EndAdventure();
   if(userHidden&&manual)ShowPet();if(!manual&&(Scheduler.QuietAt(P,DateTime.Now)||DateTime.Now<snoozeUntil||FocusActive))return;
   PresentNotice(n,manual);
  }
  void PresentNotice(Notice n,bool manual) {
   if((n.Reminder!=null||n.PlaySound)&&!Scheduler.QuietAt(P,DateTime.Now))NotificationSound.Play(P.SoundEnabled);
   current=n;bubbleTitle.Text=n.Title;bubbleText.Text=n.Text;bubbleActions.Children.Clear();
   if(n.AllowDone&&n.Reminder!=null){var done=UI.Button(n.Reminder.Kind=="water"?"İçtim ✓":"Yaptım ✓",()=>{if(n.Reminder.EveryMinutes==0)Tracking.SetDone(P,n.Reminder,true,DateTime.Today,DateTime.Now);if(n.Reminder.Kind=="water")AddWater();Save();HideBubble();happyStart=watch.Elapsed.TotalSeconds;RefreshSettings();},true);bubbleActions.Children.Add(done);
    bubbleActions.Children.Add(UI.Button("10 dk sonra",()=>{HideBubble();var timer=new DispatcherTimer{Interval=TimeSpan.FromMinutes(10)};timer.Tick+=(s,e)=>{timer.Stop();pending.Enqueue(n);};timer.Start();}));
   } else if(n.OpenAction!=null)bubbleActions.Children.Add(UI.Button(n.ActionLabel??"Kaydımı aç",n.OpenAction,true));
   else if(n.Url!=null)bubbleActions.Children.Add(UI.Button("Kaynağı aç ↗",()=>UI.OpenUrl(n.Url),true));
   else bubbleActions.Children.Add(UI.Button("Lucy'nin köşesi",()=>OpenSettings(),true));
   bubble.Visibility=Visibility.Visible;bubbleUntil=DateTime.Now.AddSeconds(manual?22:18);nextNotice=DateTime.Now.AddSeconds(90);happyStart=watch.Elapsed.TotalSeconds;
  }
  void HideBubble(){bubble.Visibility=Visibility.Collapsed;current=null;}
  Notice RandomNotice() {
   string[] messages={"Bugünün her dakikasını verimli geçirmek zorunda değilsin. Biraz nefes, biraz merak, biraz da mırıldanma.","Minik bir adım da adımdır. Bugün kendin için yaptığın küçük bir şeyi fark et.","Buradayım. Sen işine bak; masaüstünün pati kontrolü bende.","Bir pencere açıp gün ışığına bakmak ister misin? Ben de hayali güneş lekeme kıvrılayım.","Bugün kendine nazik bir cümle kur. Mesela: Elimden geleni yapıyorum.","Yapılacaklar listesine küçük bir keyif de yazalım mı? Bir şarkı, bir sayfa, bir fincan mola.","Omuzların kulaklarına doğru tırmandıysa onları usulca bırakabilirsin. Pati kontrolü tamam.","Her şey bir anda çözülmeyebilir. Şimdilik yalnızca sıradaki küçük adım.","Mır mır... Çalışma arkadaşın olarak bir pati selamı bırakmaya geldim.","Bugün hoşuna giden bir şeyi fark ettin mi? Küçük şeyleri biriktirmek de güzel."};
   return new Notice{Title=P.Name+"'den küçük bir not",Text=messages[random.Next(messages.Length)]};
  }
  void PetMouseDown(object s,MouseButtonEventArgs e) {
   if(e.ClickCount==2){OpenSettings();e.Handled=true;return;}
   mouseStart=pet.PointToScreen(e.GetPosition(pet));dragOrigin=new Point(Left,Top);moved=false;dragging=true;pet.CaptureMouse();e.Handled=true;
  }
  void PetMouseMove(object s,MouseEventArgs e) {
   if(!dragging||e.LeftButton!=MouseButtonState.Pressed)return;
   var point=pet.PointToScreen(e.GetPosition(pet));var d=point-mouseStart;
   if(!moved&&d.Length<=5)return;
   moved=true;
   var dpi=Dpi();
   ParkMascot(dragOrigin+new Vector(d.X/dpi.X,d.Y/dpi.Y));
  }
  void PetMouseUp(object s,MouseButtonEventArgs e) {
   if(!dragging)return;dragging=false;pet.ReleaseMouseCapture();if(!moved)Say(RandomNotice(),true);e.Handled=true;
  }
  Point Dpi(){var source=PresentationSource.FromVisual(this);return source==null?new Point(1,1):new Point(source.CompositionTarget.TransformToDevice.M11,source.CompositionTarget.TransformToDevice.M22);}
  public Rect Area(){
   var dpi=Dpi();
   var bounds=Forms.Screen.PrimaryScreen.WorkingArea;
   return new Rect(bounds.Left/dpi.X,bounds.Top/dpi.Y,bounds.Width/dpi.X,bounds.Height/dpi.Y);
  }
  Point CornerPosition(Rect area){
   return new Point(Math.Max(area.Left,area.Right-Width-28),Math.Max(area.Top,area.Bottom-Height-10));
  }
  void AnchorToCorner(){
   var corner=CornerPosition(Area());
   if(double.IsNaN(Left)||Math.Abs(Left-corner.X)>.5)Left=corner.X;
   if(double.IsNaN(Top)||Math.Abs(Top-corner.Y)>.5)Top=corner.Y;
   PositionBubble(8);
  }
  void ParkMascot(Point point){
   parkedAway=true;
   var area=Area();
   double inset=Math.Max(0,(Width-P.Size)*.5);
   Left=AdventureMath.Clamp(point.X,area.Left-inset,Math.Max(area.Left-inset,area.Right-inset-P.Size));
   Top=AdventureMath.Clamp(point.Y,area.Top-195,Math.Max(area.Top-195,area.Bottom-195-P.Size));
   PositionBubble(Math.Max(8,area.Top-Top+8));
  }
  void PositionBubble(double top){
   Canvas.SetTop(bubble,top);
   Canvas.SetTop(bubbleTail,top+bubble.ActualHeight-1);
  }
  void PositionMascot(){
   if(parkedAway)ParkMascot(new Point(Left,Top));
   else AnchorToCorner();
  }
  void EnsureDesktopCorner(){
   if(userHidden||quitting)return;
   if(!dragging)PositionMascot();
   if(!IsVisible)Show();
  }
  internal void VerifyDesktopCorner(){
   var expected=new Point(Left,Top);
   Left-=70;Top-=55;Hide();
   EnsureDesktopCorner();
   if(!IsVisible||Topmost||(new Point(Left,Top)-expected).Length>1)
    throw new InvalidOperationException("Corner expected "+expected+", actual "+new Point(Left,Top)+", visible "+IsVisible+", topmost "+Topmost+", area "+Area());
   MinimizePet();
   EnsureDesktopCorner();
   if(IsVisible)throw new InvalidOperationException("The desktop check overrode a user-requested hide.");
   ShowPet();
  }
  internal void VerifyTemporaryParking(){
   var corner=new Point(Left,Top);
   var area=Area();
   ParkMascot(new Point(area.Left,area.Top));
   var parked=new Point(Left,Top);
   EnsureDesktopCorner();
   if((new Point(Left,Top)-parked).Length>1||parked==corner)
    throw new InvalidOperationException("A drag did not survive the desktop refresh.");
   BeginAdventure(2,5);
   var running=adventure;
   var first=running.Beats.First();
   if(first.Move!=AdventureMove.Walk||(first.From-MascotFloor(parked)).Length>1||first.To!=running.Home)
    throw new InvalidOperationException("The action did not start where Lucy was dropped.");
   AdvanceAdventure(running.Started+running.Duration+.01);
   if(parkedAway||!IsVisible||(new Point(Left,Top)-corner).Length>1)
    throw new InvalidOperationException("Lucy did not return to the usual corner after the action.");
  }
  void SavePosition(){P.Left=Left;P.Top=Top;Save();}
  public void Save(){try{Store.Save(P);}catch(Exception e){Store.Log(e);MessageBox.Show("Ayarlar kaydedilemedi: "+e.Message,"Lucy",MessageBoxButton.OK,MessageBoxImage.Warning);}}
  public void AddWater(){Tracking.AddWater(P,DateTime.Now,P.DefaultWaterMl);Save();RefreshSettings();}
  public int WaterCount {get{return P.Drinks.Where(d=>d.Date==TaskState.Day(DateTime.Now)&&d.Type=="water").Sum(d=>d.Servings);}}
  public void StartFocus(){EndAdventure();focusUntil=DateTime.Now.AddMinutes(25);HideBubble();RefreshSettings();}
  public void EndFocus(){focusUntil=DateTime.MinValue;RefreshSettings();}
  public void Snooze(int minutes){EndAdventure();snoozeUntil=DateTime.Now.AddMinutes(minutes);HideBubble();RefreshSettings();}
  public void Resume(){snoozeUntil=DateTime.MinValue;focusUntil=DateTime.MinValue;ShowPet();Say(new Notice{Title="Yine buradayım!",Text="Kulaklarım açık, patilerim hazır. Rutinlerini zamanı geldiğinde hatırlatacağım."},true);RefreshSettings();}
  public void ShowHistory(){var h=history??HistoryCard.Calendar(DateTime.Now);Say(new Notice{Title=h.Title,Text=h.Text,Url=h.Url},true);}
  public void OpenSettings(){EndAdventure();HideBubble();if(settings==null){settings=new SettingsWindow(this);settings.Closed+=(s,e)=>settings=null;}if(!settings.IsVisible)settings.Show();if(settings.WindowState==WindowState.Minimized)settings.WindowState=WindowState.Normal;settings.Activate();settings.Focus();}
  public void RefreshSettings(){if(settings!=null)settings.RefreshToday();}
  public void ShowPet(){EndAdventure();ClosePeek();userHidden=false;WindowState=WindowState.Normal;Show();PositionMascot();RaiseWidget();}
  public void HidePet(){MinimizePet();EnterPeek();}
  public void MinimizePet(){EndAdventure();userHidden=true;HideBubble();ClosePeek();Hide();}
  public void Quit(){EndAdventure();quitting=true;Close();Application.Current.Shutdown();}
  internal ContextMenu BuildPetMenu(){var m=new ContextMenu{FontFamily=UI.AppFont,FontSize=16,StaysOpen=false};Action<string,Action> add=(text,action)=>{var i=new MenuItem{Header=text};i.Click+=(s,e)=>action();m.Items.Add(i);};add("Lucy'nin köşesi / Ayarlar",OpenSettings);add("Tarihte bugün",ShowHistory);add("Bir şey söyle",()=>Say(RandomNotice(),true));add("25 dakika odaklan",StartFocus);m.Items.Add(new Separator());add("1 saat uyukla",()=>Snooze(60));add("Hatırlatmalara devam",Resume);add("Biraz hareket",PreviewAdventure);add("Simgeye küçült",MinimizePet);add("Saklan",HidePet);m.Items.Add(new Separator());add("Tamamen çık",Quit);return m;}
  ContextMenu activePetMenu;
  void ShowMenu(FrameworkElement target){
   if(activePetMenu!=null)activePetMenu.IsOpen=false;
   var menu=BuildPetMenu();
   activePetMenu=menu;
   menu.PlacementTarget=target;
   menu.Placement=System.Windows.Controls.Primitives.PlacementMode.MousePoint;
   var outside=new DispatcherTimer(DispatcherPriority.Input){Interval=TimeSpan.FromMilliseconds(15)};
   outside.Tick+=(s,e)=>{
    var popup=PresentationSource.FromVisual(menu) as HwndSource;
    if(popup!=null&&Native.OutsideMousePress(popup.Handle))menu.IsOpen=false;
   };
   menu.Opened+=(s,e)=>{Native.ReadMouseButtons();outside.Start();};
   menu.Closed+=(s,e)=>{outside.Stop();if(activePetMenu==menu)activePetMenu=null;};
   menu.IsOpen=true;
  }
  void SetUpTray(){tray=new Forms.NotifyIcon();var ico=System.IO.Path.Combine(Store.Root,"assets","lucy.ico");tray.Icon=File.Exists(ico)?new System.Drawing.Icon(ico):System.Drawing.SystemIcons.Information;tray.Text="Lucy • Masaüstü Dostun";tray.Visible=true;tray.DoubleClick+=(s,e)=>Dispatcher.BeginInvoke(new Action(Wake));var m=new Forms.ContextMenuStrip();m.Font=UI.CreateTrayFont();m.BackColor=System.Drawing.ColorTranslator.FromHtml(UI.PaperColor);m.ForeColor=System.Drawing.ColorTranslator.FromHtml(UI.InkColor);m.Items.Add("Lucy'yi göster",null,(s,e)=>Dispatcher.BeginInvoke(new Action(ShowPet)));m.Items.Add("Rutinlerim ve listelerim",null,(s,e)=>Dispatcher.BeginInvoke(new Action(OpenSettings)));m.Items.Add("Simgeye küçült",null,(s,e)=>Dispatcher.BeginInvoke(new Action(MinimizePet)));m.Items.Add("Saklan",null,(s,e)=>Dispatcher.BeginInvoke(new Action(HidePet)));m.Items.Add("1 saat uyukla",null,(s,e)=>Dispatcher.BeginInvoke(new Action(()=>Snooze(60))));m.Items.Add("Devam et",null,(s,e)=>Dispatcher.BeginInvoke(new Action(Resume)));m.Items.Add(new Forms.ToolStripSeparator());m.Items.Add("Tamamen çık",null,(s,e)=>Dispatcher.BeginInvoke(new Action(Quit)));tray.ContextMenuStrip=m;}
  public void Wake(){ShowPet();OpenSettings();}
  public void RenderPreview(string path){Say(new Notice{Title="Merhaba, ben "+P.Name+"!",Text="Biraz mola, biraz bakım, bolca pati. Gününü küçük hatırlatmalarla güzelleştirmeye geldim."},true);Root.UpdateLayout();Snapshot(Root,path,(int)Width,(int)Height);}
  public static void Snapshot(Visual visual,string path,int width,int height){var image=new RenderTargetBitmap(width,height,96,96,PixelFormats.Pbgra32);image.Render(visual);var enc=new PngBitmapEncoder();enc.Frames.Add(BitmapFrame.Create(image));using(var f=File.Create(path))enc.Save(f);}
 }
 public static class Native {
  [DllImport("user32.dll")]public static extern bool AllowSetForegroundWindow(uint processId);
  [DllImport("user32.dll")]public static extern bool SetWindowPos(IntPtr h,IntPtr after,int x,int y,int w,int height,uint flags);
  [DllImport("user32.dll")]public static extern int GetWindowLong(IntPtr h,int n);
  [DllImport("user32.dll")]public static extern int SetWindowLong(IntPtr h,int n,int v);
  [DllImport("user32.dll")]static extern IntPtr GetForegroundWindow();
  [DllImport("user32.dll")]static extern bool IsZoomed(IntPtr h);
  [DllImport("user32.dll")]static extern bool GetWindowRect(IntPtr h,out RECT rect);
  [DllImport("user32.dll")]static extern bool GetCursorPos(out CursorPoint point);
  [DllImport("user32.dll")]static extern short GetAsyncKeyState(int key);
  [StructLayout(LayoutKind.Sequential)]struct CursorPoint {public int X,Y;}
  public static int ReadMouseButtons(){return (ushort)GetAsyncKeyState(1)|(ushort)GetAsyncKeyState(2)|(ushort)GetAsyncKeyState(4);}
  public static bool OutsideMousePress(IntPtr popup){
   CursorPoint p;RECT r;
   if((ReadMouseButtons()&0x8001)==0
    ||!GetCursorPos(out p)||!GetWindowRect(popup,out r))return false;
   return p.X<r.Left||p.X>=r.Right||p.Y<r.Top||p.Y>=r.Bottom;
  }
  [DllImport("user32.dll",CharSet=CharSet.Unicode)]static extern int GetClassName(IntPtr h,StringBuilder b,int max);
  [StructLayout(LayoutKind.Sequential)]struct RECT {public int Left,Top,Right,Bottom;}
  public static bool Fullscreen(IntPtr own){var h=GetForegroundWindow();if(h==IntPtr.Zero||h==own||IsZoomed(h))return false;var b=new StringBuilder(256);GetClassName(h,b,256);if(new[]{"Progman","WorkerW","Shell_TrayWnd"}.Contains(b.ToString()))return false;RECT r;if(!GetWindowRect(h,out r))return false;var screen=Forms.Screen.FromHandle(h).Bounds;return r.Left<=screen.Left&&r.Top<=screen.Top&&r.Right>=screen.Right&&r.Bottom>=screen.Bottom;}
 }
 public static class Tests {
  public static void Run(string output){var passed=new List<string>();Action<bool,string> ok=(value,label)=>{if(!value)throw new Exception(label);passed.Add(label);};try{
   var p=Preferences.Defaults();var day=new DateTime(2026,10,26,9,0,0);var r=new Reminder{Time="09:00"};
   ok(Scheduler.Due(r,day,day.AddHours(-1)),"Daily reminder fires at its exact time");r.LastTriggered=day.ToString("o");ok(!Scheduler.Due(r,day.AddMinutes(1),day),"Restart does not duplicate daily reminder");ok(Scheduler.Due(r,day.AddDays(1),day),"Next day reminder returns");r.LastTriggered="";ok(!Scheduler.Due(r,day.AddMinutes(16),day),"Old reminders do not pile up after sleep");r.DoneDate="2026-10-26";ok(!Scheduler.Due(r,day,day),"Completed daily task stays quiet");r.DoneDate="";r.WeekdaysOnly=true;ok(!Scheduler.Due(r,new DateTime(2026,10,31,9,0,0),day),"Weekday reminder skips Saturday");r.WeekdaysOnly=false;r.Enabled=false;ok(!Scheduler.Due(r,day,day),"Disabled reminders stay quiet");
   var repeat=new Reminder{EveryMinutes=60};ok(!Scheduler.Due(repeat,day.AddMinutes(59),day),"Interval does not fire early");ok(Scheduler.Due(repeat,day.AddMinutes(60),day),"Interval fires on boundary");repeat.LastTriggered=day.AddMinutes(60).ToString("o");ok(!Scheduler.Due(repeat,day.AddMinutes(61),day),"Interval remembers last delivery");
   ok(Scheduler.QuietAt(p,day.Date),"Quiet hours start inclusive");ok(Scheduler.QuietAt(p,day.Date.AddHours(2)),"Quiet hours cross midnight");ok(!Scheduler.QuietAt(p,day.Date.AddHours(7)),"Quiet hours end exclusive");p.QuietStart="12:00";p.QuietEnd="14:00";ok(Scheduler.QuietAt(p,day.Date.AddHours(13)),"Daytime quiet window works");p.QuietEnd="12:00";ok(!Scheduler.QuietAt(p,day),"Equal quiet endpoints disable restriction");ok(!Scheduler.ValidTime("24:00")&&!Scheduler.ValidTime("9:00")&&Scheduler.ValidTime("09:00"),"Time validation rejects invalid hours");
   using(var ms=new MemoryStream()){var ser=new DataContractJsonSerializer(typeof(Preferences));ser.WriteObject(ms,p);ms.Position=0;var copy=(Preferences)ser.ReadObject(ms);ok(copy.Name=="Lucy"&&copy.Reminders.Count==6,"Preferences survive a JSON round trip");}
   var cal=HistoryCard.Calendar(new DateTime(2028,2,29));ok(cal.Text.Contains("60.")&&cal.Text.Contains("306"),"Offline calendar handles leap years");
   FeatureTests.Run(ok); TrackingTests.Run(ok); SectionTests.Run(ok);RequestedListsTests.Run(ok);
   File.WriteAllText(output,"PASS: "+passed.Count+Environment.NewLine+string.Join(Environment.NewLine,passed),Encoding.UTF8);
  }catch(Exception e){File.WriteAllText(output,"FAIL: "+e+Environment.NewLine+string.Join(Environment.NewLine,passed),Encoding.UTF8);Environment.ExitCode=1;}}
 }
 public static class Program {
  static Mutex mutex;static EventWaitHandle wake,exitSignal;static RegisteredWaitHandle registration,exitRegistration;
  [STAThread] public static void Main(string[] args) {
   if(args.Length>1&&args[0]=="--motion-options"){MotionPreviews.Generate(args[1]);return;}
   if(args.Length>1&&args[0]=="--make-icon"){Sprites.MakeIcon(args[1]);return;}
   if(args.Length>1&&args[0]=="--self-test"){Tests.Run(args[1]);return;}
   if(args.Contains("--exit")){try{using(var signal=EventWaitHandle.OpenExisting("Local\\LucyDesktopMascotExit_v1"))signal.Set();}catch(WaitHandleCannotBeOpenedException){}return;}
   Store.TestMode=args.Length>1&&(args[0]=="--qa-ritim"||args[0]=="--qa-care-editor"||args[0]=="--qa-wheel"||args[0]=="--qa-requested-lists"||args[0]=="--qa"||args[0]=="--qa-motion-command"||args[0]=="--qa-routine-notes");
   string suffix=Store.TestMode?"_qa":"";
   bool first;
   try{mutex=new Mutex(true,(Store.TestMode?"Local\\":"Global\\")+"LucyDesktopMascot_v1"+suffix,out first);}
   catch(UnauthorizedAccessException){return;}
   if(!first){
    if(args.Contains("--settings")){
     using(var current=Process.GetCurrentProcess())foreach(var other in Process.GetProcessesByName(current.ProcessName)){
      using(other){try{if(other.Id!=current.Id&&other.SessionId==current.SessionId)Native.AllowSetForegroundWindow((uint)other.Id);}
      catch(System.ComponentModel.Win32Exception){}catch(InvalidOperationException){}}
     }
     using(var signal=new EventWaitHandle(false,EventResetMode.AutoReset,"Local\\LucyDesktopMascotWake_v1"+suffix))signal.Set();
    }
    mutex.Dispose();return;
   }
   wake=new EventWaitHandle(false,EventResetMode.AutoReset,"Local\\LucyDesktopMascotWake_v1"+suffix);
   try{
    WheelNavigation.Install();var app=new Application{ShutdownMode=ShutdownMode.OnExplicitShutdown};
    app.DispatcherUnhandledException+=(s,e)=>{Store.Log(e.Exception);e.Handled=true;};
    var prefs=Store.TestMode?Preferences.Defaults():Store.Load();if(!Store.TestMode)Store.Save(prefs);if(Store.TestMode){prefs.Quiet=false;prefs.Roam=false;prefs.FullscreenHide=false;prefs.OnTop=false;}
    var pet=new PetWindow(prefs,args.Contains("--startup"));app.MainWindow=pet;
    exitSignal=new EventWaitHandle(false,EventResetMode.AutoReset,"Local\\LucyDesktopMascotExit_v1"+suffix);exitRegistration=ThreadPool.RegisterWaitForSingleObject(exitSignal,(s,t)=>app.Dispatcher.BeginInvoke(new Action(pet.Quit)),null,Timeout.Infinite,false);
    registration=ThreadPool.RegisterWaitForSingleObject(wake,(s,t)=>app.Dispatcher.BeginInvoke(new Action(pet.Wake)),null,Timeout.Infinite,false);
    if(args.Contains("--settings")||(!args.Contains("--startup")&&!args.Contains("--preview")&&!Store.TestMode))pet.Loaded+=(s,e)=>pet.OpenSettings();
    if(args.Length>1&&args[0]=="--preview"){var path=args[1];pet.Loaded+=(s,e)=>{var timer=new DispatcherTimer{Interval=TimeSpan.FromSeconds(1)};timer.Tick+=(ss,ee)=>{timer.Stop();pet.RenderPreview(path);pet.Quit();};timer.Start();};}
    if(Store.TestMode){string folder=args[1];Directory.CreateDirectory(folder);pet.Loaded+=(s,e)=>{var timer=new DispatcherTimer{Interval=TimeSpan.FromSeconds(1)};timer.Tick+=(ss,ee)=>{timer.Stop();try{if(args[0]=="--qa-ritim")RitimQA.Run(pet,folder);else if(args[0]=="--qa-motion-command")MotionCommandQA.Run(pet,folder);else if(args[0]=="--qa-routine-notes")RoutineNotesQA.Run(pet,folder);else if(args[0]=="--qa-requested-lists")RequestedListsQA.Run(pet,folder);else if(args[0]=="--qa-wheel")WheelNavigationQA.Run(pet,folder);else if(args[0]=="--qa-care-editor")CareEditorQA.Run(pet,folder);else FeatureQA.Run(pet,folder);}catch(Exception ex){File.WriteAllText(System.IO.Path.Combine(folder,"smoke-test.txt"),"FAIL: "+ex,Encoding.UTF8);}pet.Quit();};timer.Start();};}
    pet.Show();app.Run();
   }catch(Exception e){Store.Log(e);MessageBox.Show("Lucy açılamadı: "+e.Message,"Lucy",MessageBoxButton.OK,MessageBoxImage.Error);}
   finally{if(registration!=null)registration.Unregister(null);if(exitRegistration!=null)exitRegistration.Unregister(null);if(exitSignal!=null)exitSignal.Dispose();wake.Dispose();mutex.ReleaseMutex();mutex.Dispose();}
  }
 }
}
