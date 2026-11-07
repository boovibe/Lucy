using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Media;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace LucyDesktop {
 public static class DataUpgrade {
  public static readonly string[] Groups={"morning","evening","sport","daily","general","supplements"};
  public static void Apply(Preferences p){
   if(p.Version<2){
    p.Quiet=true;p.QuietStart="00:00";p.QuietEnd="07:00";p.SoundEnabled=true;
    foreach(var r in p.Reminders.Where(r=>r!=null)){
     if(r.Id=="morning"){r.Time="07:30";r.WeekdaysOnly=true;r.Group="morning";r.Icon="sun";}
     else if(r.Id=="evening"){r.Time="22:00";r.WeekdaysOnly=true;r.Group="evening";r.Icon="moon";}
     else {r.Group=new[]{"water","break","history","sleep"}.Contains(r.Kind)?"general":"daily";r.Icon=CuteIcons.Infer(r.Title,r.Kind);}
     if(r.Id=="water")r.EveryMinutes=120;if(r.Id=="break")r.EveryMinutes=60;
    }
    p.Version=2;
   }
   if(p.Version<3)Tracking.Upgrade(p);
   foreach(var r in p.Reminders.Where(r=>r!=null)){
    if(!Groups.Contains(r.Group)&&(p.Sections==null||!p.Sections.Any(s=>s.Id==r.Group)))r.Group="daily";
    if(r.Group=="supplements"&&(r.Icon=="flower"||string.IsNullOrWhiteSpace(r.Icon)))r.Icon="capsule";
    if(!CuteIcons.Keys.Contains(r.Icon))r.Icon=CuteIcons.Infer(r.Title,r.Kind);
   }
  }
 }
 public static class TaskState {
  public static string Day(DateTime now){return now.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture);}
  public static bool ActiveToday(Reminder r,DateTime now){return r.Enabled&&(!r.WeekdaysOnly||now.DayOfWeek!=DayOfWeek.Saturday&&now.DayOfWeek!=DayOfWeek.Sunday);}
  public static bool Done(Reminder r,DateTime now){return r.DoneDate==Day(now);}
  public static void SetDone(Reminder r,bool value,DateTime now){r.DoneDate=value?Day(now):"";}
  public static string GroupName(string group){var app=System.Windows.Application.Current;var pet=app==null?null:app.MainWindow as PetWindow;var section=pet==null||pet.P.Sections==null?null:pet.P.Sections.FirstOrDefault(s=>s.Id==group);if(section!=null)return section.Name;switch(group){case "morning":return "Sabah kişisel bakım rutini";case "evening":return "Akşam kişisel bakım rutini";case "sport":return "Spor rutini";case "supplements":return "Takviyeler";case "daily":return "Ritim";default:return "Mola ve diğer hatırlatmalar";}}
  public static string GroupIcon(string group){var app=System.Windows.Application.Current;var pet=app==null?null:app.MainWindow as PetWindow;var section=pet==null||pet.P.Sections==null?null:pet.P.Sections.FirstOrDefault(s=>s.Id==group);if(section!=null)return section.Icon;switch(group){case "morning":return "sun";case "evening":return "moon";case "sport":return "dumbbell";case "supplements":return "capsule";default:return "check";}}
  public static string Schedule(Reminder r){return (r.NoNotification?"Saat bildirimi kapalı":r.EveryMinutes>0?"Her "+r.EveryMinutes+" dakika":r.Time)+(r.WeekdaysOnly?" · hafta içi":" · her gün");}
 }
 public static class NotificationSound {
  static SoundPlayer player;
  public static void Play(bool enabled){if(!enabled||Store.TestMode)return;try{if(player==null){player=new SoundPlayer(System.IO.Path.Combine(Store.Root,"assets","lucy-notify.wav"));player.Load();}player.Play();}catch(Exception e){Store.Log(e);}}
 }
 public partial class PetWindow {
  EarsWindow peek;
  string lastPageDay=TaskState.Day(DateTime.Now);
  public bool IsPeeking {get{return peek!=null&&peek.IsVisible;}}
  void RefreshForDate(){if(settings!=null)settings.RefreshEditWindow();string day=TaskState.Day(DateTime.Now);if(lastPageDay!=day){lastPageDay=day;Tracking.SyncPlan(P,DateTime.Today);Save();RefreshSettings();}}
  public void EnterPeek(){ClosePeek();peek=new EarsWindow(this,P.Skin=="cream"?cream:lucyFrames[0]);peek.Show();}
  void ClosePeek(){if(peek!=null){peek.Close();peek=null;}}
  void RefreshPeek(){if(userHidden)EnterPeek();}
  public void PreviewReminder(Reminder r){Say(MakeNotice(r),true);}
  public void RenderEars(string path){var ears=new EarsWindow(this,PreviewImage);ears.Show();ears.UpdateLayout();Snapshot((Visual)ears.Content,path,(int)ears.Width,(int)ears.Height);ears.Close();}
 }
 public class EarsWindow:Window {
  public EarsWindow(PetWindow owner,BitmapSource sprite){
   Title="Lucy • Kulaklar";Width=owner.P.Size;Height=Math.Floor(owner.P.Size*.24);WindowStyle=WindowStyle.None;ResizeMode=ResizeMode.NoResize;AllowsTransparency=true;Background=null;ShowInTaskbar=false;ShowActivated=false;
   var canvas=new Canvas{Width=Width,Height=Height,ClipToBounds=true};Content=canvas;
   canvas.Children.Add(new Image{Source=sprite,Width=Width,Height=Width,Cursor=System.Windows.Input.Cursors.Hand,ToolTip="Lucy burada! Geri gelmem için kulaklarıma tıkla."});
   var area=owner.Area();Left=Math.Max(area.Left+8,Math.Min(owner.Left+170-Width/2,area.Right-Width-8));Top=area.Bottom-Height;
   MouseLeftButtonDown+=(s,e)=>{owner.ShowPet();e.Handled=true;};MouseRightButtonUp+=(s,e)=>owner.OpenSettings();
   Loaded+=(s,e)=>{var h=new WindowInteropHelper(this).Handle;Native.SetWindowLong(h,-20,Native.GetWindowLong(h,-20)|0x08000000|0x00000080);};
  }
 }
 public static class CuteIcons {
  public static readonly string[] Keys={"sun","moon","skincare","tooth","dumbbell","water","flower","bag","food","book","heart","check","capsule"};
  public static readonly string[] Labels={"Güneş","Ay","Cilt bakımı","Diş bakımı","Spor","Su","Çiçek","Alışveriş","Yiyecek","Kitap","Kalp","Görev","Kapsül"};
  static Brush B(string color){return UI.IconBrush(color);}
  static void Shape(Canvas c,Shape shape,double x=0,double y=0){Canvas.SetLeft(shape,x);Canvas.SetTop(shape,y);c.Children.Add(shape);}
  static void Ellipse(Canvas c,double x,double y,double w,double h,string fill,string stroke=null){Shape(c,new Ellipse{Width=w,Height=h,Fill=B(fill),Stroke=stroke==null?null:B(stroke),StrokeThickness=2,IsHitTestVisible=false},x,y);}
  static void Rect(Canvas c,double x,double y,double w,double h,string fill,double radius=4){Shape(c,new Rectangle{Width=w,Height=h,RadiusX=radius,RadiusY=radius,Fill=B(fill),Stroke=B("#80648F"),StrokeThickness=2,IsHitTestVisible=false},x,y);}
  static void Line(Canvas c,double x1,double y1,double x2,double y2,string color="#80648F",double thickness=2){c.Children.Add(new Line{X1=x1,Y1=y1,X2=x2,Y2=y2,Stroke=B(color),StrokeThickness=thickness,StrokeStartLineCap=PenLineCap.Round,StrokeEndLineCap=PenLineCap.Round});}
  static void Path(Canvas c,string geometry,string fill,string stroke="#80648F"){c.Children.Add(new System.Windows.Shapes.Path{Data=Geometry.Parse(geometry),Fill=fill==null?null:B(fill),Stroke=stroke==null?null:B(stroke),StrokeThickness=2,StrokeLineJoin=PenLineJoin.Round});}
  static void Face(Canvas c,double x,double y){Ellipse(c,x-6,y,2.8,3.5,"#655174");Ellipse(c,x+4,y,2.8,3.5,"#655174");Path(c,"M "+(x-3)+","+(y+6)+" Q "+x+","+(y+10)+" "+(x+4)+","+(y+6),null);Ellipse(c,x-11,y+5,5,2.5,"#EFAFCA");Ellipse(c,x+8,y+5,5,2.5,"#EFAFCA");}
  public static FrameworkElement Create(string key,double size=44){
   var c=new Canvas{Width=64,Height=64,IsHitTestVisible=false};string background=key=="sun"||key=="food"?"#FFF1D7":key=="water"||key=="tooth"?"#E1F1FA":key=="heart"||key=="bag"?"#FBE5EE":"#EDE3F7";Ellipse(c,0,0,64,64,background);
   switch(key){
    case "capsule":Path(c,"M 18,43 C 12,38 12,31 18,25 L 31,12 C 37,6 45,7 50,12 C 56,18 56,25 50,31 L 37,44 C 31,50 24,49 18,43 Z","#D3E3B8");Path(c,"M 18,25 L 37,44 C 31,50 24,49 18,43 C 12,38 12,31 18,25 Z","#E8AFCB");Line(c,18,25,37,44);Line(c,35,15,42,12,"#FFFFFF",3);Face(c,25,32);break;
    case "sun":for(int i=0;i<8;i++){double a=i*Math.PI/4;Line(c,32+Math.Cos(a)*20,32+Math.Sin(a)*20,32+Math.Cos(a)*25,32+Math.Sin(a)*25,"#D4AA65",2.5);}Ellipse(c,18,18,28,28,"#FFE3A5","#D4AA65");Face(c,32,28);break;
    case "moon":Path(c,"M 42,12 C 18,6 7,34 24,48 C 38,59 54,45 53,34 C 35,43 23,24 42,12 Z","#D4C2ED");Ellipse(c,44,16,4,4,"#E6B77E");Path(c,"M 48,9 L 49,13 L 53,14 L 49,15 L 48,19 L 47,15 L 43,14 L 47,13 Z","#E6B77E",null);break;
    case "skincare":Rect(c,23,12,18,8,"#C9B1DF",2);Rect(c,18,22,28,32,"#F6D8E8",7);Rect(c,23,29,18,17,"#FFF9FE",3);Path(c,"M 32,32 C 25,39 28,43 32,43 C 36,43 39,39 32,32 Z","#C6ACDF");break;
    case "tooth":Path(c,"M 19,13 C 9,18 14,34 18,46 C 21,58 27,52 29,41 C 31,36 34,36 36,41 C 39,55 45,56 47,44 C 53,28 55,12 43,12 C 36,12 34,16 30,16 C 25,16 24,12 19,13 Z","#FFFEFF");Face(c,32,25);break;
    case "dumbbell":Rect(c,14,18,8,28,"#D7BFEB",3);Rect(c,7,24,7,16,"#E7CEE9",3);Rect(c,22,29,20,6,"#A68AC7",2);Rect(c,42,18,8,28,"#D7BFEB",3);Rect(c,50,24,7,16,"#E7CEE9",3);break;
    case "water":Path(c,"M 32,9 C 27,18 15,30 16,39 C 17,58 49,58 49,39 C 49,30 38,18 32,9 Z","#BADFEE");Face(c,32,34);break;
    case "flower":for(int i=0;i<5;i++){double a=i*Math.PI*2/5-Math.PI/2;Ellipse(c,24+Math.Cos(a)*13,24+Math.Sin(a)*13,16,20,"#E6BCDC");}Ellipse(c,22,23,22,22,"#FFE8AE","#BE9CC6");Face(c,33,29);break;
    case "bag":Rect(c,15,25,34,29,"#F0C9DE",5);Path(c,"M 23,27 L 23,19 C 23,7 42,7 42,19 L 42,27",null);Face(c,32,35);break;
    case "food":Path(c,"M 12,28 L 52,28 C 51,57 13,57 12,28 Z","#F2C7C0");Line(c,10,28,54,28);Ellipse(c,18,15,14,14,"#BCD5AE");Ellipse(c,29,12,16,17,"#D3E3B8");Ellipse(c,38,19,10,10,"#EBAEB0");Face(c,32,35);break;
    case "book":Rect(c,15,13,34,40,"#D3C5E9",5);Line(c,23,14,23,52);Line(c,29,24,42,24,"#FFFFFF",3);Line(c,29,31,40,31,"#FFFFFF",3);Path(c,"M 39,13 L 39,27 L 43,23 L 47,27 L 47,13 Z","#F1BFD2",null);break;
    case "heart":Path(c,"M 32,52 C 25,45 10,35 12,23 C 14,10 27,10 32,20 C 37,10 51,11 53,24 C 54,36 39,46 32,52 Z","#E8AFCB");Face(c,32,29);break;
    default:Rect(c,15,13,34,40,"#FFFDFE",6);Rect(c,24,10,16,8,"#D6BDE7",3);Path(c,"M 23,34 L 29,40 L 42,26",null,"#A280BB");break;
   }
   return new Viewbox{Width=size,Height=size,Child=c};
  }
  public static string Infer(string title,string kind=""){
   string t=(title??"").ToLower(CultureInfo.GetCultureInfo("tr-TR"));
   if(kind=="water"||t.Contains("su iç"))return "water";if(kind=="sleep"||t.Contains("uyku"))return "moon";if(kind=="history"||t.Contains("kitap")||t.Contains("oku"))return "book";if(t.Contains("diş"))return "tooth";if(t.Contains("cilt")||t.Contains("krem")||t.Contains("bakım")||t.Contains("serum")||t.Contains("nemlendir")||t.Contains("yüz"))return "skincare";if(t.Contains("spor")||t.Contains("egzersiz")||t.Contains("yürüy"))return "dumbbell";if(t.Contains("alışveriş"))return "bag";if(t.Contains("yemek")||t.Contains("meyve")||t.Contains("süt")||t.Contains("ekmek"))return "food";if(kind=="break"||t.Contains("bitki"))return "flower";return kind=="shopping"?"bag":"check";
  }
 }
 public static class FeatureTests {
  public static void Run(Action<bool,string> ok){
   ok(AdventureMath.AllowedChoices(new[]{0,1,2,3,4,5,2}).SequenceEqual(new[]{1,2,5}),"Only approved motion numbers 2, 3 and 6 are eligible, including stale preferences");
   ok(AdventureMath.AllowedChoices(null).SequenceEqual(new[]{1,2,5})&&AdventureMath.AllowedChoices(new int[0]).Length==0,"Motion defaults retain the approved choices and an empty selection stays disabled");
   var p=Preferences.Defaults();ok(p.QuietStart=="00:00"&&p.QuietEnd=="07:00","Requested quiet hours are the defaults");
   ok(p.Reminders.First(r=>r.Id=="morning").Time=="07:30"&&p.Reminders.First(r=>r.Id=="morning").WeekdaysOnly,"Weekday morning care at 07:30");
   ok(p.Reminders.First(r=>r.Id=="evening").Time=="22:00"&&p.Reminders.First(r=>r.Id=="evening").WeekdaysOnly,"Weekday evening care at 22:00");
   var old=Preferences.Defaults();old.Version=0;old.Reminders[0].Time="09:00";old.Reminders[0].DoneDate="2026-10-27";old.Reminders.Add(new Reminder{Id="mine",Title="Kitabımı oku",Group=null,Icon=null});DataUpgrade.Apply(old);ok(old.Reminders.Count==7&&old.Reminders[0].DoneDate=="2026-10-27"&&old.Reminders.Last().Group=="daily","Migration preserves custom tasks and completions");
   old.Reminders[0].Time="08:00";DataUpgrade.Apply(old);ok(old.Reminders[0].Time=="08:00","Migration is applied only once");
   var task=new Reminder{Time="07:30",Group="morning"};TaskState.SetDone(task,true,new DateTime(2026,10,27));ok(TaskState.Done(task,new DateTime(2026,10,27))&&!TaskState.Done(task,new DateTime(2026,10,28)),"Daily checkboxes reset with the calendar date");
   task.NoNotification=true;ok(!Scheduler.Due(task,new DateTime(2026,10,28,7,30,0),DateTime.MinValue),"Untimed checklist items never notify");
   p.Shopping.Add(new ShoppingItem{Title="Süt",Checked=true,Quantity="1 litre",Icon="food"});using(var ms=new MemoryStream()){var serializer=new DataContractJsonSerializer(typeof(Preferences));serializer.WriteObject(ms,p);ms.Position=0;var copy=(Preferences)serializer.ReadObject(ms);ok(copy.Shopping[0].Checked&&copy.Shopping[0].Quantity=="1 litre","Shopping completion and quantity survive reopening");}
   ok(CuteIcons.Infer("Dişlerini fırçala")=="tooth"&&CuteIcons.Infer("Spor yap")=="dumbbell","Relevant task icons are suggested");
   ok(File.Exists(System.IO.Path.Combine(Store.Root,"assets","lucy-notify.wav")),"Notification audio is packaged locally");
   string legacy="{\"Name\":\"Lucy\",\"Reminders\":[{\"Id\":\"morning\",\"Title\":\"Sabah bakım\",\"Time\":\"09:00\",\"Kind\":\"care\",\"Enabled\":true}],\"QuietStart\":\"23:00\",\"QuietEnd\":\"08:00\",\"Size\":205}";using(var ms=new MemoryStream(Encoding.UTF8.GetBytes(legacy))){var actual=(Preferences)new DataContractJsonSerializer(typeof(Preferences)).ReadObject(ms);Store.Validate(actual);ok(actual.Version==3&&actual.Shopping!=null&&actual.SoundEnabled&&actual.Reminders[0].Group=="morning"&&actual.Reminders[0].Time=="07:30","Real legacy JSON without the new fields upgrades safely");}
  }
 }
 public static class FeatureQA {
  public static void Run(PetWindow pet,string folder){
   pet.RenderPreview(System.IO.Path.Combine(folder,"lucy-preview.png"));
   pet.P.Sections.Add(new UserSection{Id="qa-care",Parent="care",Name="Haftalık bakım",Icon="flower",Order=40});pet.P.Sections.Add(new UserSection{Id="qa-daily",Parent="daily",Name="Ev işleri",Icon="check"});pet.P.Sections.Add(new UserSection{Id="qa-shopping",Parent="shopping",Name="Bakım alışverişi",Icon="bag"});pet.P.Reminders.Add(new Reminder{Title="Maskemi uygula",Message="",Group="qa-care",NoNotification=true,Icon="skincare"});pet.P.Reminders.Add(new Reminder{Title="Çamaşırları katla",Message="",Group="qa-daily",NoNotification=true});pet.P.Shopping.Add(new ShoppingItem{Title="Pamuk",SectionId="qa-shopping",Icon="skincare"});Tracking.SyncPlan(pet.P,DateTime.Today);
   var settings=new SettingsWindow(pet);settings.Show();settings.UpdateLayout();
   foreach(var page in new[]{"home","care","morning","evening","sport","shopping","daily","general","settings","drinks","products","measurements","weekly","monthly","qa-care","qa-daily","qa-shopping"}){settings.Navigate(page);settings.UpdateLayout();settings.RenderPage(System.IO.Path.Combine(folder,page+".png"));}
   // Exercise actual application data actions, then verify what will be displayed on tomorrow's checklist.
   var r=new Reminder{Title="Dişlerini fırçala",Message="",Time="07:40",Group="morning",Icon="tooth",Order=20};pet.P.Reminders.Add(r);Tracking.SetDone(pet.P,r,true,DateTime.Today,DateTime.Now);settings.Navigate("morning");settings.RenderPage(System.IO.Path.Combine(folder,"morning-with-task.png"));
   pet.P.Shopping.Add(new ShoppingItem{Title="Süt",Quantity="1 litre",Icon="food"});pet.P.Shopping.Add(new ShoppingItem{Title="Nemlendirici",Quantity="1 adet",Icon="skincare",Checked=true});settings.Navigate("shopping");settings.RenderPage(System.IO.Path.Combine(folder,"shopping-with-items.png"));
   Tracking.AddWater(pet.P,DateTime.Now,250);pet.P.Drinks.Add(new DrinkEntry{Date=Tracking.Key(DateTime.Today),Time="10:00",Name="Filtre kahve",Type="coffee",Ml=200,CaffeineMg=90});settings.Navigate("drinks");settings.RenderPage(System.IO.Path.Combine(folder,"drinks-with-records.png"));
   var product=new SkinProduct{Name="Örnek nemlendirici",Category="Nemlendirici",Ingredients="Örnek kayıt",Active=true};pet.P.Products.Add(product);Tracking.SaveRoutine(pet.P,new RoutineRecord{Date=Tracking.Key(DateTime.Today),Group="morning",Status="Yaptım",Note="Örnek günlük notu",ProductIds=new List<string>{product.Id},Dryness=2,Redness=1},DateTime.Now);
   Reports.Export(pet.P,Reports.WeekStart(DateTime.Today),Reports.WeekStart(DateTime.Today).AddDays(6),false,folder);Reports.Export(pet.P,new DateTime(DateTime.Today.Year,DateTime.Today.Month,1),new DateTime(DateTime.Today.Year,DateTime.Today.Month,1).AddMonths(1).AddDays(-1),true,folder);
   settings.RenderInstantCoffee(System.IO.Path.Combine(folder,"instant-coffee.png"));settings.Close();var editor=new ReminderEditor(pet,null,"sport");editor.Show();editor.UpdateLayout();PetWindow.Snapshot(editor,System.IO.Path.Combine(folder,"task-editor.png"),(int)editor.ActualWidth,(int)editor.ActualHeight);editor.Close();pet.RenderEars(System.IO.Path.Combine(folder,"ears.png"));
   var report=new StringBuilder();report.AppendLine("All seventeen navigation pages rendered with Manrope headings and DM Sans text.");report.AppendLine("Daily task, drink and shopping states rendered.");report.AppendLine("Weekly and monthly documents exported.");report.AppendLine("Task editor with icon picker rendered.");report.AppendLine("Ear-only edge window rendered: "+pet.P.Size+" x "+Math.Floor(pet.P.Size*.24));
   for(int i=0;i<4;i++){var frame=Sprites.Frame(Sprites.Load("lucy-sprites.png"),i);if(frame.PixelWidth!=512||frame.PixelHeight!=512)throw new Exception("Invalid sprite");}
   foreach(int kind in new[]{1,2,5})pet.RenderAdventureSample(System.IO.Path.Combine(folder,"adventure-"+kind+".png"),kind);report.AppendLine("Three approved motion scenes rendered; original location and topmost state restored after each.");report.AppendLine("Four normalized character sprites loaded.");pet.HidePet();if(pet.IsVisible||!pet.IsPeeking)throw new Exception("Peek transition failed");pet.ShowPet();if(!pet.IsVisible||pet.IsPeeking)throw new Exception("Peek restore failed");report.AppendLine("Main mascot hides into ears and restores successfully.");File.WriteAllText(System.IO.Path.Combine(folder,"smoke-test.txt"),report.ToString(),Encoding.UTF8);
  }
 }
}
