using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace LucyDesktop {
 public static class AdventureMath {
  public static int[] AllowedChoices(int[] configured){return (configured??new[]{1,2,5}).Where(k=>k==1||k==2||k==5).Distinct().ToArray();}
  public static double Smooth(double t){t=Clamp(t,0,1);return t*t*(3-2*t);}
  public static Point Lerp(Point a,Point b,double t){return a+(b-a)*t;}
  public static double Clamp(double x,double min,double max){return Math.Max(min,Math.Min(max,x));}
 }

 enum AdventureMove { Walk, Aim, Hop, Peek, EdgeLeap, Bump, Dazed, Roll, Groom, Butterfly, Stalk, Pounce, Turn, IceCream, Pizza, Doner, Retreat, Charge, Settle, Nap, Wake, Welcome, Idle }

 sealed class AdventureBeat {
  public AdventureMove Move;
  public Point From, To;
  public double Duration, Facing, TargetSlope;
 }

 sealed class AdventureSession {
  public int Kind,Variant;
  public bool Speak;
  public double Started, Duration, Size;
  public Point Home, Cursor;
  public AdventureStage Stage;
  public readonly List<AdventureBeat> Beats=new List<AdventureBeat>();

  public void Add(AdventureMove move,Point to,double duration,double facing,double targetSlope=0){
   Beats.Add(new AdventureBeat{Move=move,From=Cursor,To=to,Duration=duration,Facing=facing,TargetSlope=targetSlope});
   Duration+=duration;
   Cursor=to;
  }
 }

 public partial class PetWindow {
  const int LookUp=8, LookDown=9, Wiggle=10, RollSide=12, RollBelly=13, RollOtherSide=14, Stunned=15;
  const int IceCreamPose=16, IceCreamAfter=17, PizzaPose=18, PizzaAfter=19, RearRun=20, FrontRun=22, GroomStart=24, Cook=28;
  const int Asleep=-1;
  AdventureSession adventure;
  AdventureFrame[] motionFrames;
  AdventureFrame creamMotion,idleMotion,sleepMotion;
  int nextWall;
  readonly HashSet<int> playedScenes=new HashSet<int>();
  static readonly int[] SceneOrder={11,20,50,25,10,23,12,21,51,24,13,22};
  int lastScene=-1;
  static readonly int[] CookingFrames={0,1,2,0,1,2,3};
  static readonly int[] GroomFrames={0,1,2,1,2,0,1,2,3,2,0};
  DateTime nextAdventure,nextNoticeActivity=DateTime.Now.AddMinutes(12);
  double adventureRestUntil,adventureRestStarted;


  public bool AdventureActive {get{return adventure!=null;}}
  internal int ActiveMotionKind {get{return adventure==null?-1:adventure.Kind;}}
  internal int ActiveMotionVariant {get{return adventure==null?-1:adventure.Variant;}}
  void InitializeAdventureClock(){nextAdventure=DateTime.Now.AddMinutes(random.Next(25,56));}

  void LoadMotionFrames(){
   if(motionFrames!=null)return;
   var frames=LoadAdventureAtlas(Sprites.Load("lucy-motion-portrait-v7.png"));
   creamMotion=new AdventureFrame(cream,.96);
   idleMotion=new AdventureFrame(lucyFrames[0],.96);
   sleepMotion=new AdventureFrame(lucyFrames[2],.96);
   nextWall=random.Next(2)==0?-1:1;
   motionFrames=frames;
  }

  static AdventureFrame[] LoadAdventureAtlas(BitmapSource atlas){
   const int columns=4, rows=8;
   int w=atlas.PixelWidth/columns,h=atlas.PixelHeight/rows;
   var frames=new AdventureFrame[columns*rows];
   for(int i=0;i<frames.Length;i++){
    var frame=new CroppedBitmap(atlas,new Int32Rect(i%columns*w,i/columns*h,w,h));
    frame.Freeze();
    frames[i]=new AdventureFrame(frame,.86);
   }
   return frames;
  }

  void MaybeStartAdventure(DateTime now){
   if(Store.TestMode||adventure!=null||now<nextAdventure)return;
   if(P.MotionReviewPending||!P.Adventures){nextAdventure=now.AddMinutes(10);return;}
   if(userHidden||!IsVisible||FocusActive||now<snoozeUntil||Scheduler.QuietAt(P,now)||settings!=null||dragging||pet.IsMouseOver||bubble.Visibility==Visibility.Visible||pending.Count>0||AdventureInput.IdleSeconds()>180||Native.Fullscreen(new WindowInteropHelper(this).Handle)){
    nextAdventure=now.AddMinutes(5);return;
   }
   var allowed=AdventureMath.AllowedChoices(P.AllowedMotions);
   if(allowed.Length==0){nextAdventure=now.AddMinutes(10);return;}
   StartNextAdventure(allowed);
  }

  bool StartNextAdventure(int[] allowed,bool cornerOnly=false){
   var eligible=SceneOrder.Where(id=>allowed.Contains(id/10)&&(P.Skin!="cream"||id/10!=2||id==20)).ToArray();
   if(eligible.Length==0)return false;
   bool newRound=eligible.All(playedScenes.Contains);
   int idToPlay=eligible.FirstOrDefault(id=>(newRound||!playedScenes.Contains(id))
    &&(!cornerOnly||id/10==2&&id!=20)&&(eligible.Length==1||!newRound||id!=lastScene));
   if(idToPlay==0)return false;
   BeginAdventure(idToPlay/10,idToPlay%10);
   if(newRound)playedScenes.Clear();
   playedScenes.Add(idToPlay);
   lastScene=idToPlay;
   return true;
  }

  public void PreviewAdventure(){
   var now=DateTime.Now;
   string blocked=P.MotionReviewPending?"Hareket seçimini tamamladığında oyunumu deneyebilirsin.":
    Scheduler.QuietAt(P,now)?"Sessiz saatler bittikten sonra yeniden oynayabiliriz.":
    FocusActive?"Odak turu bittiğinde yeniden oynayabiliriz.":
    now<snoozeUntil?"Uyuklama modu açık. Hatırlatmalara devam seçeneğiyle uyanabilirim.":null;
   if(blocked!=null){Say(new Notice{Title="Birazdan oynarız",Text=blocked},true);return;}
   var allowed=AdventureMath.AllowedChoices(P.AllowedMotions);
   if(allowed.Length==0){Say(new Notice{Title="Bir hareket seçelim",Text="Şu anda etkin bir hareket seçilmemiş."},true);return;}
   if(settings!=null)settings.Close();
   if(userHidden)ShowPet();
   StartNextAdventure(allowed);
  }

  Point MascotFloor(Point windowPosition){return windowPosition+new Vector(Width/2,195+P.Size*.96);}

  void BeginAdventure(int kind,int variant,int wall=0){
   EndAdventure();
   adventureRestUntil=0;
   HideBubble();
   LoadMotionFrames();
   var area=Area();
   double size=Math.Min(P.Size,Math.Min(area.Width,area.Height)*.49);
   var home=MascotFloor(CornerPosition(area));
   var start=parkedAway?MascotFloor(new Point(Left,Top)):home;
   var a=new AdventureSession{Kind=kind,Variant=variant,Size=size,Home=home,Cursor=start,Speak=P.Chats&&random.Next(3)==0};

   PlanAdventure(a,area,variant,wall);
   ShowAdventure(a,area);
  }

  void BeginStartupWelcome(){
   LoadMotionFrames();
   var area=Area();
   double size=Math.Min(P.Size,Math.Min(area.Width,area.Height)*.49);
   var home=new Point(Left+Width/2,Top+195+P.Size*.96);
   var center=SafeFloor(new Point(area.Left+area.Width*.5,area.Top+area.Height*.5+size*.70+82),area,size);
   var a=new AdventureSession{Kind=-1,Size=size,Home=home,Cursor=center};
   a.Add(AdventureMove.Welcome,center,3.05,1);
   Walk(a,home);
   a.Add(AdventureMove.Idle,home,.30,1);
   ShowAdventure(a,area);
  }

  void ShowAdventure(AdventureSession a,Rect area){
   a.Stage=new AdventureStage(area,a);
   a.Stage.Actor.MouseLeftButtonDown+=(s,e)=>{ReturnFromAdventure();e.Handled=true;};
   a.Stage.Actor.MouseRightButtonUp+=(s,e)=>{ShowMenu(a.Stage.Actor);e.Handled=true;};
   a.Started=watch.Elapsed.TotalSeconds;
   adventure=a;

   activity.Text="";
   AdvanceAdventure(a.Started);
   try{a.Stage.Show();}
   catch{EndAdventure();throw;}
   pet.Visibility=Visibility.Hidden;
   parkedAway=false;
   AnchorToCorner();
   CompositionTarget.Rendering+=RenderAdventure;
  }

  static int VariantCount(int kind){return kind==5?2:kind==2?6:4;}

  void PlanAdventure(AdventureSession a,Rect area,int variant,int wall=0){
   double s=a.Size;
   var home=a.Home;
   double inward=home.X>area.Left+area.Width*.5?-1:1;
   double center=area.Left+area.Width*(.39+random.NextDouble()*.22);
   double high=area.Top+area.Height*(.30+random.NextDouble()*.14);
   double low=area.Top+area.Height*(.72+random.NextDouble()*.17);
   double level=home.Y>area.Top+area.Height*.66?high:low;
   if(a.Kind==1&&variant==1){
    if(wall==0){wall=nextWall;nextWall=-nextWall;}
    double edgeX=wall>0?area.Right:area.Left;
    var look=SafeFloor(new Point(edgeX-wall*s*.68,level),area,s);
    Walk(a,SafeFloor(new Point(look.X-wall*s,home.Y),area,s));
    Hop(a,look,.86);
    a.Add(AdventureMove.Peek,look,.82,wall);
    var lip=new Point(edgeX-wall*s*.31,look.Y-s*.035);
    Aim(a,lip,wall,.58);
    a.Add(AdventureMove.EdgeLeap,lip,.48,wall);
    var fallen=SafeFloor(new Point(edgeX-wall*s*.96,look.Y+s*.37),area,s);
    a.Add(AdventureMove.Bump,fallen,.81,wall);
    a.Add(AdventureMove.Dazed,fallen,1.15,-wall);
   }else if(a.Kind==1){
    double across=area.Left+area.Width*(inward<0?.21+random.NextDouble()*.13:.66+random.NextDouble()*.13);
    if(variant==3){across=center;level=area.Top+area.Height*(.53+random.NextDouble()*.18);}
    var landing=SafeFloor(new Point(across,level),area,s);
    var start=SafeFloor(new Point(AdventureMath.Lerp(home,landing,variant==0?.68:.77).X,home.Y),area,s);
    Walk(a,start);
    Hop(a,landing,variant==0?.84:1.03);
    var along=SafeFloor(new Point(variant==3?center:across+inward*s*.72,landing.Y),area,s);
    Walk(a,along);
    if(variant==2)Roll(a,SafeFloor(new Point(along.X-inward*s*.38,along.Y),area,s));
    if(variant>=2){
     a.Add(AdventureMove.Settle,a.Cursor,.44,inward);
     a.Add(AdventureMove.Nap,a.Cursor,variant==2?4.4:3.2,inward);
     a.Add(AdventureMove.Wake,a.Cursor,.60,inward);
    }
   }else if(a.Kind==2&&variant==0){
    var run=SafeFloor(new Point(center,a.Cursor.Y),area,s);
    double chaseDirection=Direction(a.Cursor,run);
    var play=SafeFloor(new Point(run.X+chaseDirection*s*.80,level),area,s);
    a.Add(AdventureMove.Butterfly,run,1.65,chaseDirection);
    a.Add(AdventureMove.Stalk,run,.82,chaseDirection,play.Y-run.Y);
    a.Add(AdventureMove.Pounce,play,.91,chaseDirection);
    Roll(a,SafeFloor(new Point(play.X+chaseDirection*s*.30,play.Y),area,s));
   }else if(a.Kind==2){
    Walk(a,home);
    double facing=home.X>area.Left+area.Width*.5?-1:1;
    if(variant==4){
     var side=SafeFloor(new Point(home.X+facing*s*.21,home.Y),area,s);
     Roll(a,side);
     Roll(a,home);
    }else{
     var skit=variant==1?AdventureMove.IceCream:variant==2?AdventureMove.Pizza:variant==3?AdventureMove.Doner:AdventureMove.Groom;
     a.Add(skit,home,variant==5?3.45:variant==3?4.45:3.10,facing);
    }
    a.Add(AdventureMove.Idle,home,.30,1);
    return;
   }else{
    var start=SafeFloor(new Point(variant==0?center:area.Left+area.Width*(inward<0?.27:.73),home.Y),area,s);
    Walk(a,start);
    var far=SafeFloor(new Point(center,high),area,s);
    a.Add(AdventureMove.Retreat,far,2.45,inward);
    a.Add(AdventureMove.Charge,start,2.10,-inward);
   }

   if(Math.Abs(a.Cursor.Y-home.Y)>4){
    Walk(a,SafeFloor(new Point(home.X+inward*s*1.55,a.Cursor.Y),area,s));
    var lower=SafeFloor(new Point(home.X+inward*s*.44,home.Y),area,s);
    Hop(a,lower,.91);
   }
   Walk(a,home);
   if(a.Beats.Any(b=>b.Move==AdventureMove.Nap)){
    a.Add(AdventureMove.Idle,home,.30,1);
   }else{
    a.Add(AdventureMove.Settle,home,.55,a.Beats.Count==0?inward:a.Beats[a.Beats.Count-1].Facing);
    a.Add(AdventureMove.Nap,home,.36,a.Beats[a.Beats.Count-1].Facing);
   }
  }

  static Point SafeFloor(Point point,Rect area,double size){
   return new Point(AdventureMath.Clamp(point.X,area.Left+size*.61,area.Right-size*.61),
    AdventureMath.Clamp(point.Y,area.Top+size*.93,area.Bottom-size*.09));
  }

  static double Direction(Point from,Point to){return to.X>=from.X?1:-1;}

  static void Walk(AdventureSession a,Point to){
   double distance=(to-a.Cursor).Length;
   if(distance<1)return;
   double seconds=AdventureMath.Clamp(distance/(a.Size*1.02),.30,3.15);
   double facing=Direction(a.Cursor,to);
   var previous=a.Beats.LastOrDefault();
   if(previous!=null&&previous.Facing!=facing&&previous.Move!=AdventureMove.Nap&&previous.Move!=AdventureMove.Wake)
    a.Add(AdventureMove.Turn,a.Cursor,.22,facing);
   a.Add(AdventureMove.Walk,to,seconds,facing);
  }

  static void Hop(AdventureSession a,Point to,double duration){
   if((to-a.Cursor).Length<1)return;
   double facing=Direction(a.Cursor,to);
   Aim(a,to,facing,.76);
   a.Add(AdventureMove.Hop,to,Math.Max(duration,Math.Min(1.40,(to-a.Cursor).Length/(a.Size*2.4))),facing);
  }

  static void Aim(AdventureSession a,Point target,double facing,double duration){
   a.Add(AdventureMove.Aim,a.Cursor,duration,facing,target.Y-a.Cursor.Y);
  }

  static void Roll(AdventureSession a,Point to){
   a.Add(AdventureMove.Roll,to,1.50,Direction(a.Cursor,to));
  }

  void RenderAdventure(object sender,EventArgs e){AdvanceAdventure(watch.Elapsed.TotalSeconds);}

  void AdvanceAdventure(double time){
   var a=adventure;
   if(a==null)return;
   double elapsed=Math.Max(0,time-a.Started);
   if(elapsed>=a.Duration){
    FinishAdventure(a,time);
    return;
   }
   foreach(var beat in a.Beats){
    if(elapsed<beat.Duration){DrawAdventure(a,beat,elapsed);return;}
    elapsed-=beat.Duration;
   }
  }

  void DrawAdventure(AdventureSession a,AdventureBeat beat,double elapsed){
   double u=AdventureMath.Clamp(elapsed/beat.Duration,0,1);
   double pace=u-.88*Math.Sin(2*Math.PI*u)/(2*Math.PI);
   var floor=AdventureMath.Lerp(beat.From,beat.To,pace);
   double s=a.Size,lift=0,sx=beat.Facing,sy=1,angle=0,shadow=1;
   int frame=0, adjacentFrame=-1;
   double poseBlend=0;
   switch(beat.Move){
    case AdventureMove.Walk:
    case AdventureMove.Butterfly:
     double distance=(beat.To-beat.From).Length;
     double stride=distance*pace/Math.Max(18,s*.45);
     frame=u>.95?0:(int)(stride*4)%4;
     lift=Math.Abs(Math.Sin(stride*Math.PI*2))*s*.015*Math.Sin(Math.PI*u);
     break;
    case AdventureMove.Hop:
    case AdventureMove.Pounce:
    case AdventureMove.EdgeLeap:
     bool edgeLeap=beat.Move==AdventureMove.EdgeLeap;
     double flightEnd=edgeLeap?1:.83;
     double air=AdventureMath.Clamp((u-.05)/(flightEnd-.05),0,1);
     double heightChange=beat.To.Y-beat.From.Y;
     double arch=Math.Max(s*(beat.Move==AdventureMove.Pounce?.27:.31),Math.Abs(heightChange)*.28);
     floor=AdventureMath.Lerp(beat.From,beat.To,air);
     lift=4*air*(1-air)*arch+(edgeLeap?s*.13*air:0);
     frame=u<.05?4:air<.26?5:air<(edgeLeap?1.01:.78)?6:7;
     // Keep the visible center continuous while switching the airborne drawings.
     const double blendRadius=.07;
     double swap=air<.52?.26:.78;
     if((!edgeLeap||swap==.26)&&Math.Abs(air-swap)<blendRadius){
      int early=swap==.26?5:6;
      double blend=AdventureMath.Smooth((air-swap+blendRadius)/(2*blendRadius));
      adjacentFrame=frame==early?early+1:early;
      poseBlend=frame==early?blend:1-blend;
     }
     double speedY=heightChange-4*arch*(1-2*air);
     angle=beat.Facing*AdventureMath.Clamp(speedY/s*9,-24,26)*Math.Sin(air*Math.PI);
     double landing=AdventureMath.Clamp((u-flightEnd)/(1-flightEnd+.001),0,1);
     sy=1-(edgeLeap?0:.16*Math.Sin(Math.PI*landing));
     break;
    case AdventureMove.Aim:
    case AdventureMove.Stalk:
     bool looking=u<.47;
     int gaze=beat.TargetSlope<0?LookUp:beat.TargetSlope>0?LookDown:4;
     double wiggle=Math.Sin((u-.47)*Math.PI*11);
     frame=u<.09&&beat.Move==AdventureMove.Aim?0:looking?gaze:u>.88?gaze:Wiggle+(wiggle>0?1:0);
     angle=looking?beat.Facing*AdventureMath.Clamp(beat.TargetSlope/s*3,-6,6):beat.Facing*wiggle*2.4;
     sy=1-.075*AdventureMath.Smooth(u/.48);
     break;
    case AdventureMove.Peek:
     frame=u<.23?0:LookDown;
     angle=beat.Facing*7*Math.Sin(Math.PI*u);
     sy=1-.065*Math.Sin(Math.PI*u);
     break;
    case AdventureMove.Bump:
     double recoil=AdventureMath.Clamp((u-.115)/.76,0,1);
     double drag=1-(1-recoil)*(1-recoil);
     floor=AdventureMath.Lerp(beat.From,beat.To,drag);
     lift=s*(.13*(1-recoil)+.20*Math.Sin(Math.PI*recoil));
     angle=-beat.Facing*57*Math.Sin(Math.PI*recoil);
     sx*=1-.31*Math.Sin(Math.PI*AdventureMath.Clamp(u/.24,0,1));
     sy=1+.10*Math.Sin(Math.PI*AdventureMath.Clamp(u/.24,0,1));
     frame=u<.18?6:recoil<.76?RollBelly:RollOtherSide;
     break;
    case AdventureMove.Dazed:
     frame=Stunned;
     angle=beat.Facing*9*Math.Sin(elapsed*12)*Math.Pow(1-u,1.4);
     sy=1+.04*Math.Sin(elapsed*13)*(1-u);
     break;
    case AdventureMove.Roll:
     frame=u<.24?RollSide:u<.62?RollBelly:u<.89?RollOtherSide:RollSide;
     angle=beat.Facing*19*Math.Sin(u*Math.PI*2);
     sy=1+.04*Math.Sin(u*Math.PI*3);
     break;
    case AdventureMove.Groom:
     frame=GroomStart+GroomFrames[Math.Min(GroomFrames.Length-1,(int)(u*GroomFrames.Length))];
     sx=-beat.Facing;
     angle=sx*.9*Math.Sin(elapsed*10);
     sy=1+.012*Math.Sin(elapsed*14);
     break;

    case AdventureMove.IceCream:
    case AdventureMove.Pizza:
     frame=beat.Move==AdventureMove.IceCream?(elapsed%.87<.39?IceCreamPose:IceCreamAfter):(elapsed<.60?PizzaPose:PizzaAfter);
     double bite=Math.Pow(Math.Max(0,Math.Sin(elapsed*8.6)),2);
     sx*=1+.023*bite;
     sy=1-.065*bite;
     angle=beat.Facing*(2+5*bite);
     break;
    case AdventureMove.Doner:
     frame=Cook+CookingFrames[Math.Min(CookingFrames.Length-1,(int)(u*CookingFrames.Length))];
     break;
    case AdventureMove.Retreat:
    case AdventureMove.Charge:
     double depth=beat.Move==AdventureMove.Retreat?u:1-u;
     double perspective=1/(1+2.15*AdventureMath.Smooth(depth));
     int step=(int)(elapsed*10)%4;
     sy=perspective;
     sx=step>=2?-perspective:perspective;
     frame=(beat.Move==AdventureMove.Retreat?RearRun:FrontRun)+step%2;
     lift=Math.Abs(Math.Sin(elapsed*15))*s*.047*perspective*Math.Sin(Math.PI*u);
     break;
    case AdventureMove.Turn:
     frame=4;
     sx=(u<.5?-beat.Facing:beat.Facing)*(.20+.80*Math.Abs(Math.Cos(Math.PI*u)));
     break;
    case AdventureMove.Settle:
     frame=u<.30?4:u<.69?RollSide:Asleep;
     sx=beat.Facing;
     sy=1-.065*Math.Sin(Math.PI*AdventureMath.Clamp(u/.69,0,1));
     break;
    case AdventureMove.Nap:
     frame=Asleep;
     sx=beat.Facing;
     sy=beat.To==a.Home?1:1+.016*Math.Sin(elapsed*3);
     if(beat.To==a.Home)shadow=1-AdventureMath.Smooth(u);
     break;
    case AdventureMove.Wake:
     frame=u<.32?Asleep:4;
     sx=beat.Facing;
     sy=1+.12*Math.Sin(Math.PI*u);
     break;
    case AdventureMove.Welcome:
    case AdventureMove.Idle:
     sx=1;
     shadow=beat.Move==AdventureMove.Welcome?0:1-AdventureMath.Smooth(u);
     break;
   }
   bool sleeping=frame==Asleep;
   if(sleeping)sx=1;
   var source=P.Skin=="cream"?creamMotion:sleeping?sleepMotion:beat.Move==AdventureMove.Idle||beat.Move==AdventureMove.Welcome?idleMotion:motionFrames[frame];
   a.Stage.Draw(source,beat,floor,lift,sx,sy,angle,elapsed,u,shadow,a.Speak&&beat.Move==AdventureMove.Nap&&beat.To!=a.Home,sleeping,
    adjacentFrame>=0&&P.Skin!="cream"?motionFrames[adjacentFrame]:null,poseBlend);
  }

  void FinishAdventure(AdventureSession a,double time){
   var last=a.Beats[a.Beats.Count-1];
   DrawAdventure(a,last,last.Duration);
   bool resting=last.Move==AdventureMove.Nap;
   if(resting){
    happyStart=-100;
    adventureRestUntil=time+random.Next(10,19);
    adventureRestStarted=time;
    DrawAdventureRest(time);
   }else{
    rotate.Angle=shift.Y=0;
    scale.ScaleX=scale.ScaleY=1;
    happyStart=-100;
    Frame(0);
   }
   CloseAdventure(a);
  }

  bool TryNoticeActivity(Notice notice,DateTime now){
   if(!P.Adventures||P.Skin=="cream"||P.MotionReviewPending||!AdventureMath.AllowedChoices(P.AllowedMotions).Contains(2)
    ||settings!=null||dragging||pet.IsMouseOver||!IsVisible||Native.Fullscreen(new WindowInteropHelper(this).Handle)
    ||now<nextNoticeActivity||random.Next(4)!=0)return false;
   if(!StartNextAdventure(AdventureMath.AllowedChoices(P.AllowedMotions),true))return false;
   nextNoticeActivity=now.AddMinutes(45);
   PresentNotice(notice,false);
   return true;
  }

  void ReturnFromAdventure(){
   var a=adventure;
   if(a==null)return;
   a.Cursor=a.Stage.Floor;
   a.Beats.Clear();
   a.Duration=0;
   a.Started=watch.Elapsed.TotalSeconds;
   Walk(a,a.Home);
   a.Add(AdventureMove.Idle,a.Home,.30,1);
  }

  bool DrawAdventureRest(double time){
   if(time>=adventureRestUntil)return false;
   rotate.Angle=0;
   shift.Y=0;
   scale.ScaleX=1;
   scale.ScaleY=1+.016*Math.Sin((time-adventureRestStarted)*3);
   Frame(2);
   activity.Text="";
   return true;
  }

  public void EndAdventure(){
   adventureRestUntil=0;
   var a=adventure;
   if(a==null)return;
   rotate.Angle=shift.Y=0;
   scale.ScaleX=scale.ScaleY=1;
   Frame(0);
   CloseAdventure(a);
  }

  void CloseAdventure(AdventureSession a){
   CompositionTarget.Rendering-=RenderAdventure;
   adventure=null;
   pet.Visibility=Visibility.Visible;
   a.Stage.Close();
   InitializeAdventureClock();
  }

  internal void VerifyWelcome(){
   var origin=new Point(Left,Top);
   BeginStartupWelcome();
   var a=adventure;
   AdvanceAdventure(a.Started+.75);
   var caption=a.Stage.Surface.Children.OfType<Border>().Single(b=>b.Child is TextBlock&&((TextBlock)b.Child).Text=="Hoş geldin sahip, seni çok özledim");
   if(caption.Visibility!=Visibility.Visible||Math.Abs(a.Stage.Top+Canvas.GetTop(caption)+46-(a.Stage.Area.Top+a.Stage.Area.Height/2))>1)
    throw new InvalidOperationException("Welcome did not render in the center.");
   AdvanceAdventure(a.Started+a.Duration+.01);
   if(AdventureActive||(new Point(Left,Top)-origin).Length>1||Topmost)throw new InvalidOperationException("Welcome did not return home.");
  }

  internal void VerifyAdventureLifecycle(){
   var area=Area();
   var home=new Point(Left+Width/2,Top+195+P.Size*.96);
   var originalLeft=Left;var originalTop=Top;
   foreach(int kind in new[]{1,2,5})for(int variant=0;variant<VariantCount(kind);variant++){
    var a=new AdventureSession{Kind=kind,Size=P.Size,Home=home,Cursor=home};
    PlanAdventure(a,area,variant);
    if(a.Duration<2||a.Duration>28||a.Cursor!=home)throw new InvalidOperationException("Trip duration or final location is invalid.");
    if(a.Beats.Where(b=>b.Move==AdventureMove.Walk).Any(b=>b.From==b.To))throw new InvalidOperationException("A walking leg must travel.");
    if(kind==5&&(!a.Beats.Any(b=>b.Move==AdventureMove.Retreat)||!a.Beats.Any(b=>b.Move==AdventureMove.Charge)))throw new InvalidOperationException("Depth needs distinct directions.");
   }
   foreach(int wall in new[]{-1,1}){
    BeginAdventure(1,1,wall);
    var running=adventure;
    bool reachedLip=false;
    var down=running.Beats.First(b=>b.Move==AdventureMove.Aim&&b.TargetSlope>0);
    DrawAdventure(running,down,down.Duration*.25);
    if(P.Skin!="cream"&&running.Stage.Actor.Source!=motionFrames[LookDown].Bitmap)
     throw new InvalidOperationException("A lower target must use the downward gaze.");
    var stageOrigin=new Point(running.Stage.Left,running.Stage.Top);
    foreach(var jump in running.Beats.Where(b=>b.Move==AdventureMove.Hop||b.Move==AdventureMove.EdgeLeap)){
     double flight=jump.Move==AdventureMove.EdgeLeap?.95:.78;
     foreach(double swap in jump.Move==AdventureMove.EdgeLeap?new[]{.26}:new[]{.26,.78}){
      double when=(.05+flight*swap)*jump.Duration;
      DrawAdventure(running,jump,when-.0001);
      var before=running.Stage.ActorBounds;
      DrawAdventure(running,jump,when+.0001);
      var after=running.Stage.ActorBounds;
      var displacement=new Vector((after.Left+after.Right-before.Left-before.Right)*.5,
       (after.Top+after.Bottom-before.Top-before.Bottom)*.5);
      if(displacement.Length>1)throw new InvalidOperationException("The visible center jumped between airborne poses.");
     }
    }
    foreach(var beat in running.Beats)for(int i=0;i<=20;i++){
     DrawAdventure(running,beat,beat.Duration*i/20.0);
     if(new Point(running.Stage.Left,running.Stage.Top)!=stageOrigin)throw new InvalidOperationException("The native stage moved during rendering.");
     var bounds=running.Stage.ActorBounds;
     if(bounds.Top<running.Stage.Top)throw new InvalidOperationException("The stage clipped a frame at the top.");
     if(beat.Move==AdventureMove.EdgeLeap||beat.Move==AdventureMove.Bump){
      var onScreen=Rect.Intersect(area,bounds);
      if(onScreen.IsEmpty||onScreen.Width<bounds.Width*.65)throw new InvalidOperationException("The edge jump hid most of Lucy.");
      reachedLip|=wall<0?bounds.Left<area.Left:bounds.Right>area.Right;
     }else if(!area.Contains(bounds))throw new InvalidOperationException("An actor frame exceeded the work area.");
    }
    if(!reachedLip)throw new InvalidOperationException("The jump never reached the screen edge.");
    var nap=running.Beats.Last();
    DrawAdventure(running,nap,nap.Duration);
    if(running.Stage.Actor.Source!=lucyFrames[2])throw new InvalidOperationException("The scene did not use the original sleeping portrait.");
    AdvanceAdventure(running.Started+running.Duration+.01);
    if(pet.Source!=lucyFrames[2]||shift.Y!=0)throw new InvalidOperationException("The corner did not retain the original sleeping portrait.");
    if(AdventureActive||adventureRestUntil<=running.Started+running.Duration||Left!=originalLeft||Top!=originalTop)
     throw new InvalidOperationException("A completed trip must stop at home and enter rest.");
   }
  }

  public void RenderAdventureSample(string path,int kind){RenderAdventureSample(path,kind,0,kind==1?AdventureMove.Hop:kind==2?AdventureMove.Butterfly:AdventureMove.Charge);}
  internal void RenderAdventureSample(string path,int kind,int variant,AdventureMove move){
   double originalLeft=Left,originalTop=Top;bool originalTopmost=Topmost;
   BeginAdventure(kind,variant);
   var a=adventure;
   try{
    var beat=a.Beats.First(b=>b.Move==move);
    DrawAdventure(a,beat,beat.Duration*(move==AdventureMove.Bump?.43:.46));
    a.Stage.UpdateLayout();
    var area=a.Stage.Area;
    var visual=new DrawingVisual();
    var brush=new VisualBrush(a.Stage.Surface){ViewboxUnits=BrushMappingMode.Absolute,Viewbox=new Rect(0,0,a.Stage.Width,a.Stage.Height),Stretch=Stretch.Fill};
    using(var drawing=visual.RenderOpen()){
     drawing.DrawRectangle(UI.B("#EDF0F8"),null,new Rect(0,0,area.Width,area.Height));
     drawing.DrawRectangle(brush,null,new Rect(a.Stage.Left-area.Left,a.Stage.Top-area.Top,a.Stage.Width,a.Stage.Height));
    }
    Snapshot(visual,path,(int)area.Width,(int)area.Height);
   }finally{EndAdventure();}
   if(Left!=originalLeft||Top!=originalTop||Topmost!=originalTopmost)throw new InvalidOperationException("Adventure moved the home window.");
  }
 }

 sealed class AdventureFrame {
  public readonly BitmapSource Bitmap;
  public readonly double Anchor;
  public readonly Rect Bounds;
  readonly Point[] footLine;

  public AdventureFrame(BitmapSource bitmap,double anchor){
   Bitmap=bitmap;
   Anchor=anchor;
   var converted=new FormatConvertedBitmap(bitmap,PixelFormats.Bgra32,null,0);
   int w=bitmap.PixelWidth,h=bitmap.PixelHeight;
   var pixels=new byte[w*h*4];
   converted.CopyPixels(pixels,w*4,0);
   int left=w,top=h,right=-1,bottom=-1;
   var footByColumn=new int[w];
   for(int y=0;y<h;y++)for(int x=0;x<w;x++){
    if(pixels[(y*w+x)*4+3]<30)continue;
    left=Math.Min(left,x);right=Math.Max(right,x);
    top=Math.Min(top,y);bottom=Math.Max(bottom,y);
    footByColumn[x]=y+1;
   }
   if(right<left)throw new InvalidDataException("Lucy motion atlas contains an empty frame.");
   Bounds=new Rect((double)left/w-.5,(double)top/h-anchor,(double)(right-left+1)/w,(double)(bottom-top+1)/h);
   var feet=new List<Point>();
   for(int x=left;x<=right;x++){
    if(footByColumn[x]>0)feet.Add(new Point((x+.5)/w-.5,(double)footByColumn[x]/h-anchor));
   }
   footLine=feet.ToArray();
  }

  public double GroundContact(Matrix transform){
   double bottom=double.NegativeInfinity;
   foreach(var point in footLine)bottom=Math.Max(bottom,transform.Transform(point).Y);
   return bottom;
  }
 }

 sealed class AdventureStage:Window {
  public readonly Rect Area;
  public readonly Canvas Surface=new Canvas();
  public readonly Image Actor=new Image{Stretch=Stretch.Fill,Cursor=System.Windows.Input.Cursors.Hand,ToolTip="Tıkla: köşeye dön · Sağ tıkla: menü"};
  readonly double size;
  readonly Image previousPose=new Image{Stretch=Stretch.Fill,IsHitTestVisible=false,Opacity=0};
  readonly Border welcomeCaption;
  readonly Border napCaption=new Border{Width=200,CornerRadius=new CornerRadius(10),Padding=new Thickness(9,5,9,5),
   Background=UI.B("#FAF5EB"),Child=new TextBlock{Text="Çok yoruldum… Biraz da burada uyuyalım.",TextWrapping=TextWrapping.Wrap,TextAlignment=TextAlignment.Center,FontSize=12},IsHitTestVisible=false};
  readonly Ellipse shadow=new Ellipse{Fill=Brushes.Black,Effect=new BlurEffect{Radius=5},IsHitTestVisible=false};
  readonly Canvas butterfly=MakeButterfly();
  readonly ScaleTransform stretch=new ScaleTransform();
  readonly RotateTransform tilt=new RotateTransform();
  readonly ScaleTransform wings=new ScaleTransform();
  readonly Ellipse[] particles=new Ellipse[5];
  readonly Line impact=new Line{Stroke=UI.B("#F5B74D"),StrokeThickness=3,StrokeStartLineCap=PenLineCap.Round,StrokeEndLineCap=PenLineCap.Round,IsHitTestVisible=false};
  static readonly Brush Crumb=UI.B("#E9AF61"), Spark=UI.B("#F6D373"), Steam=UI.B("#E5DFD3");

  public AdventureStage(Rect area,AdventureSession scene){
   Area=area;size=scene.Size;
   bool welcome=scene.Kind==-1;
   // Keep the layered window fixed while WPF composes the moving sprite.
   var bounds=new Rect(scene.Home,scene.Home);
   foreach(var beat in scene.Beats){bounds.Union(beat.From);bounds.Union(beat.To);}
   double side=Math.Max(welcome?Math.Min(370,area.Width*.49):120,size*1.25);
   double above=size*1.22+Math.Max(size*.56,area.Height*.20);
   bounds=new Rect(bounds.Left-side,bounds.Top-above,bounds.Width+2*side,bounds.Height+above+size*.35);
   bounds.Intersect(area);
   Title="Lucy • Dolaşıyor";
   Left=bounds.Left;Top=bounds.Top;Width=bounds.Width;Height=bounds.Height;
   WindowStyle=WindowStyle.None;ResizeMode=ResizeMode.NoResize;
   AllowsTransparency=true;Background=null;ShowActivated=false;ShowInTaskbar=false;Topmost=true;
   Content=Surface;
   Surface.ClipToBounds=true;
   Surface.Children.Add(shadow);
   Actor.Width=Actor.Height=size;
   Actor.RenderTransformOrigin=new Point(.5,.86);
   var transforms=new TransformGroup();
   transforms.Children.Add(stretch);
   transforms.Children.Add(tilt);
   Actor.RenderTransform=transforms;
   RenderOptions.SetBitmapScalingMode(Actor,BitmapScalingMode.HighQuality);
   previousPose.Width=previousPose.Height=size;
   RenderOptions.SetBitmapScalingMode(previousPose,BitmapScalingMode.HighQuality);
   Surface.Children.Add(previousPose);
   Surface.Children.Add(Actor);
   napCaption.Visibility=Visibility.Collapsed;
   Surface.Children.Add(napCaption);
   if(welcome){
    welcomeCaption=new Border{Width=side*2-16,Padding=new Thickness(12,15,12,17),Background=UI.B("#253044"),
     CornerRadius=new CornerRadius(24),IsHitTestVisible=false,Child=new TextBlock{
      Text="Hoş geldin sahip, seni çok özledim",FontFamily=UI.HeadingFont,FontSize=Math.Min(38,area.Width*.051),FontWeight=FontWeights.SemiBold,
      TextAlignment=TextAlignment.Center,TextWrapping=TextWrapping.Wrap,Foreground=Brushes.White}};
    Canvas.SetLeft(welcomeCaption,Area.Left+Area.Width*.5-Left-welcomeCaption.Width*.5);
    Surface.Children.Add(welcomeCaption);
   }
   butterfly.IsHitTestVisible=false;
   butterfly.RenderTransform=wings;
   Surface.Children.Add(butterfly);
   Surface.Children.Add(impact);
   for(int i=0;i<particles.Length;i++){
    particles[i]=new Ellipse{IsHitTestVisible=false};
    Surface.Children.Add(particles[i]);
   }
   SourceInitialized+=(s,e)=>{
    var handle=new WindowInteropHelper(this).Handle;
    Native.SetWindowLong(handle,-20,Native.GetWindowLong(handle,-20)|0x08000000|0x00000080);
   };
  }

  public Rect ActorBounds {get;private set;}
  public Point Floor {get;private set;}
  AdventureBeat currentBeat;

  public void Draw(AdventureFrame frame,AdventureBeat beat,Point floor,double lift,double sx,double sy,double angle,double time,double progress,double shadowWeight,bool speak,bool sleeping,AdventureFrame adjacentPose,double poseBlend){
   var move=beat.Move;
   var transform=Matrix.Identity;
   transform.Scale(sx*size,sy*size);
   transform.Rotate(angle);
   Rect visible=Rect.Transform(frame.Bounds,transform);
   double air=lift;
   double contact=sleeping||move==AdventureMove.Idle||move==AdventureMove.Welcome?0:frame.GroundContact(transform);
   lift+=contact;
   var poseOffset=new Vector();
   if(adjacentPose!=null){
    Rect adjacent=Rect.Transform(adjacentPose.Bounds,transform);
    poseOffset=new Vector(
     (adjacent.Left+adjacent.Width*.5-visible.Left-visible.Width*.5)*poseBlend,
     (adjacent.Top+adjacent.Height*.5-visible.Top-visible.Height*.5+contact-adjacentPose.GroundContact(transform))*poseBlend);
    visible.Offset(poseOffset);
   }
   bool atLip=move==AdventureMove.EdgeLeap||move==AdventureMove.Bump;
   if(!atLip)floor.X=AdventureMath.Clamp(floor.X,Area.Left-visible.Left+2,Area.Right-visible.Right-2);
   if(air>0){
    double clearance=Math.Max(0,floor.Y+visible.Top-(lift-air)-Area.Top-2);
    lift-=Math.Max(0,air-clearance);
   }
   floor.Y=AdventureMath.Clamp(floor.Y,Area.Top-visible.Top+lift+2,Area.Bottom-visible.Bottom+lift-2);
   Floor=floor;
   ActorBounds=new Rect(floor.X+visible.X,floor.Y+visible.Y-lift,visible.Width,visible.Height);
   double anchorX=floor.X-Left,anchorY=floor.Y-Top;

   if(beat!=currentBeat){
    previousPose.Source=Actor.Source;
    previousPose.RenderTransformOrigin=Actor.RenderTransformOrigin;
    previousPose.RenderTransform=Actor.RenderTransform.CloneCurrentValue();
    Canvas.SetLeft(previousPose,anchorX-size*.5);
    Canvas.SetTop(previousPose,anchorY-size*previousPose.RenderTransformOrigin.Y-lift);
    currentBeat=beat;
   }
   // Movement keyframes are discrete poses. Only the return to the home portrait dissolves.
   double transition=move!=AdventureMove.Idle||previousPose.Source==null||previousPose.Source==frame.Bitmap?1:AdventureMath.Smooth(time/.09);
   previousPose.Opacity=1-transition;
   Actor.Opacity=transition;
   Actor.Source=frame.Bitmap;
   Actor.RenderTransformOrigin=new Point(.5,frame.Anchor);
   stretch.ScaleX=sx;
   stretch.ScaleY=sy;
   tilt.Angle=angle;
   Canvas.SetLeft(Actor,anchorX-size*.5+poseOffset.X);
   Canvas.SetTop(Actor,anchorY-size*frame.Anchor-lift+poseOffset.Y);

   double footprint=Math.Min(1,Math.Abs(sx));
   double width=size*.52*footprint/(1+Math.Max(0,air)/size);
   shadow.Width=width;
   shadow.Height=Math.Max(3,width*.13);
   shadow.Opacity=.17*footprint*shadowWeight/(1+Math.Max(0,air)/size*3);
   Canvas.SetLeft(shadow,anchorX-width*.5);
   Canvas.SetTop(shadow,anchorY-shadow.Height*.45);

   if(welcomeCaption!=null){
    welcomeCaption.Visibility=move==AdventureMove.Welcome?Visibility.Visible:Visibility.Collapsed;
    welcomeCaption.Opacity=AdventureMath.Smooth(time/.24)*(1-AdventureMath.Smooth((progress-.76)/.24));
    Canvas.SetTop(welcomeCaption,Area.Top+Area.Height*.5-Top-46);
   }
   DrawButterfly(beat,floor,time,progress);
   DrawParticles(move,beat.Facing,time,progress,anchorX,anchorY);
   impact.Opacity=move==AdventureMove.Bump?1-AdventureMath.Smooth(progress/.36):0;
   if(impact.Opacity>0){
    impact.X1=impact.X2=(beat.Facing>0?Area.Right-4:Area.Left+4)-Left;
    impact.Y1=anchorY-size*.68;
    impact.Y2=anchorY-size*.30;
   }
   napCaption.Visibility=speak?Visibility.Visible:Visibility.Collapsed;
   if(speak){
    napCaption.Opacity=AdventureMath.Smooth(time/.30)*AdventureMath.Smooth((1-progress)/.28);
    Canvas.SetLeft(napCaption,AdventureMath.Clamp(anchorX-100,Area.Left-Left+3,Area.Right-Left-203));
    Canvas.SetTop(napCaption,AdventureMath.Clamp(anchorY-size*.69-42,Area.Top-Top+3,Area.Bottom-Top-45));
   }
  }

  void DrawButterfly(AdventureBeat beat,Point floor,double time,double progress){
   var move=beat.Move;
   bool playing=move==AdventureMove.Butterfly||move==AdventureMove.Stalk||move==AdventureMove.Pounce;
   butterfly.Visibility=playing?Visibility.Visible:Visibility.Collapsed;
   if(!playing)return;
   double dart=move==AdventureMove.Pounce?AdventureMath.Smooth(progress):0;
   double flyTime=time+(move==AdventureMove.Stalk?1.65:move==AdventureMove.Pounce?2.47:0);
   double flutter=Math.Sin(flyTime*9)*size*.07;
   double hover=move==AdventureMove.Stalk?Math.Sin(progress*Math.PI):0;
   double x=AdventureMath.Clamp(floor.X+beat.Facing*size*(.58+.36*dart)-beat.Facing*size*.12*hover+flutter,Area.Left+20,Area.Right-20);
   double y=AdventureMath.Clamp(floor.Y-size*(.78+.29*dart-.21*hover)+Math.Sin(flyTime*13)*size*.06,Area.Top+20,Area.Bottom-20);
   Canvas.SetLeft(butterfly,x-Left-20);
   Canvas.SetTop(butterfly,y-Top-17);
   butterfly.Opacity=move==AdventureMove.Butterfly?AdventureMath.Smooth(time/.22):1-AdventureMath.Smooth((dart-.68)/.32);
   wings.ScaleX=.32+.68*Math.Abs(Math.Sin(flyTime*23));
  }

  void DrawParticles(AdventureMove move,double facing,double time,double progress,double anchorX,double anchorY){
   bool bump=move==AdventureMove.Bump,steam=move==AdventureMove.Doner,dazed=move==AdventureMove.Dazed;
   bool eating=move==AdventureMove.IceCream||move==AdventureMove.Pizza;
   for(int i=0;i<particles.Length;i++){
    var p=particles[i];
    p.Visibility=bump||steam||eating||dazed?Visibility.Visible:Visibility.Collapsed;
    if(p.Visibility!=Visibility.Visible)continue;
    double phase=bump?Math.Min(1,progress*1.25):(time*(steam?.87:1.5)+i*.21)%1;
    double x=bump?facing*size*(.29-phase*(.16+i*.06)):
     steam?facing*size*.22+Math.Sin(phase*5+i)*size*.038: facing*size*(.10+phase*(.12+i*.025));
    double y=bump?size*(-.47-phase*(.1+i*.07)):
     steam?size*(-.50-phase*.35):size*(-.46-.11*Math.Sin(phase*Math.PI)+phase*.08);
    if(dazed){
     double orbit=time*5+i*2*Math.PI/particles.Length;
     x=Math.Cos(orbit)*size*.27;
     y=size*(-.80+Math.Sin(orbit)*.075);
    }
    p.Width=p.Height=steam?5+8*phase:bump||dazed?5+i%3:3+i%2;
    p.Fill=bump||dazed?Spark:steam?Steam:Crumb;
    p.Opacity=dazed?.88*(1-AdventureMath.Smooth((progress-.55)/.45)):(1-phase)*(steam?.47:.88);
    Canvas.SetLeft(p,AdventureMath.Clamp(anchorX+x,Area.Left-Left+1,Area.Right-Left-p.Width-1));
    Canvas.SetTop(p,AdventureMath.Clamp(anchorY+y,Area.Top-Top+1,Area.Bottom-Top-p.Height-1));
   }
  }

  public static Canvas MakeButterfly(){
   var c=new Canvas{Width=40,Height=34,RenderTransformOrigin=new Point(.5,.5)};
   for(int i=0;i<2;i++){
    var wing=new Ellipse{Width=16,Height=22,Fill=UI.B("#D6B7EA"),Stroke=UI.B("#9C7BB7"),StrokeThickness=1};
    Canvas.SetLeft(wing,4+i*16);Canvas.SetTop(wing,4);c.Children.Add(wing);
   }
   var body=new Ellipse{Width=5,Height=20,Fill=UI.B("#81658E")};
   Canvas.SetLeft(body,18);Canvas.SetTop(body,9);c.Children.Add(body);
   return c;
  }
 }

 static class AdventureInput {
  [StructLayout(LayoutKind.Sequential)]struct LastInput {public uint Size,Time;}
  [DllImport("user32.dll")]static extern bool GetLastInputInfo(ref LastInput data);
  public static double IdleSeconds(){var data=new LastInput{Size=(uint)Marshal.SizeOf(typeof(LastInput))};return GetLastInputInfo(ref data)?unchecked((uint)Environment.TickCount-data.Time)/1000.0:0;}
 }
}
