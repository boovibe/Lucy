using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace LucyDesktop {
 internal static class MotionCommandQA {
  const string Motion="Biraz hareket";
  static void Click(PetWindow pet,string title){
   var item=pet.BuildPetMenu().Items.OfType<MenuItem>().Single(i=>(string)i.Header==title);
   item.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
  }

  public static void Run(PetWindow pet,string folder){
   var report=new StringBuilder();int count=0;
   Action<bool,string> check=(ok,message)=>{if(!ok)throw new Exception(message);count++;report.AppendLine(message);};
   pet.P.Adventures=false;
   pet.P.AllowedMotions=new[]{1,2,5};
   pet.OpenSettings();
   var seen=new HashSet<int>();
   int first=0;
   for(int i=0;i<12;i++){
    Click(pet,Motion);
    int scene=pet.ActiveMotionKind*10+pet.ActiveMotionVariant;
    if(i==0)first=scene;
    check(pet.AdventureActive&&seen.Add(scene),"Menu advances to a unique scene while another scene can still be active: "+scene);
   }
   check(Application.Current.Windows.OfType<AdventureStage>().Count(w=>w.IsVisible&&w.Topmost)==1,"Only one stage is open during rapid scene changes.");
   Click(pet,Motion);check(pet.ActiveMotionKind*10+pet.ActiveMotionVariant==first,"The next round begins after all twelve scenes.");
   check(!Application.Current.Windows.OfType<SettingsWindow>().Any(w=>w.IsVisible)&&!pet.P.Adventures,"The manual command closes settings and leaves automatic play off.");
   pet.EndAdventure();
   check(pet.IsVisible&&!pet.Topmost&&pet.WindowState==WindowState.Normal,"Lucy uses ordinary window order after an action.");
   pet.WindowState=WindowState.Minimized;
   check(pet.WindowState==WindowState.Normal,"Ordinary window minimization is blocked.");
   Click(pet,"Simgeye küçült");check(!pet.IsVisible&&!pet.IsPeeking,"Minimize uses the notification area without ears.");
   pet.ShowPet();
   Click(pet,"Saklan");check(!pet.IsVisible&&pet.IsPeeking,"Saklan keeps the ears available.");
   pet.ShowPet();check(pet.IsVisible&&!pet.IsPeeking&&!pet.Topmost,"Restoring Lucy uses ordinary window order.");
   pet.VerifyAdventureLifecycle();
   check(!pet.AdventureActive,"Routes are finite, screen bounds hold and a completed trip rests at home.");
   pet.VerifyDesktopCorner();
   check(pet.IsVisible&&!pet.Topmost,"Lucy stays in the desktop corner and respects an explicit hide.");
   pet.VerifyTemporaryParking();
   check(pet.IsVisible&&!pet.Topmost,"A dragged position survives refresh and the next action ends at home.");
   pet.VerifyWelcome();
   check(pet.IsVisible&&!pet.Topmost,"Welcome is centered and ends at the ordinary home widget.");
   pet.P.Quiet=true;pet.P.QuietStart=DateTime.Now.AddMinutes(-2).ToString("HH:mm");pet.P.QuietEnd=DateTime.Now.AddMinutes(2).ToString("HH:mm");
   Click(pet,Motion);check(!pet.AdventureActive,"Quiet hours block a manual motion.");pet.P.Quiet=false;
   pet.StartFocus();Click(pet,Motion);check(!pet.AdventureActive,"Focus mode blocks a manual motion.");pet.Resume();
   pet.P.MotionReviewPending=true;Click(pet,Motion);check(!pet.AdventureActive,"Pending motion review cannot be bypassed.");pet.P.MotionReviewPending=false;
   pet.P.AllowedMotions=new int[0];Click(pet,Motion);check(!pet.AdventureActive,"No selection leaves motion disabled.");
   pet.RenderAdventureSample(Path.Combine(folder,"groom.png"),2,5,AdventureMove.Groom);
   File.WriteAllText(Path.Combine(folder,"smoke-test.txt"),"PASS: "+count+Environment.NewLine+report,Encoding.UTF8);
  }
 }
}