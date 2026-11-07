using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace LucyDesktop {
 public partial class SettingsWindow {
  string ritimFilter="all";
  bool InRitim(Reminder r,string group){return group=="daily"?SectionNames.Root(pet.P,r.Group)=="daily":r.Group==group;}
  void Ritim(string group){
   var ink=UI.Ink;
   if(group=="daily"){Heading("Ritim","Bir alt kategori seç; görevlerini kendi yerinde gör.","check");ShowSections("daily");return;}
   var header=new DockPanel{Margin=new Thickness(0,0,0,20)};
   var add=UI.Button("+ Yeni görev",()=>EditTask(null,group),true);add.Name="RitimAdd";add.VerticalAlignment=VerticalAlignment.Center;add.ToolTip=SectionNames.Title(pet.P,group)+" içine görev ekle";DockPanel.SetDock(add,Dock.Right);header.Children.Add(add);
   var titles=new StackPanel();titles.Children.Add(UI.Text("RİTİM / "+ViewDate.ToString("dd MMMM",System.Globalization.CultureInfo.GetCultureInfo("tr-TR")),13,UI.B(UI.ClayColor)));
   titles.Children.Add(UI.Text(SectionNames.Title(pet.P,group),28,ink));titles.Children.Add(UI.Text("Her şeyi aynı anda yapman gerekmiyor.",13,UI.Muted));header.Children.Add(titles);page.Children.Add(header);
   var focus=new DockPanel();var start=UI.Button(pet.FocusActive?"Odak turunu bitir":"25 dakika odaklan",()=>{if(pet.FocusActive)pet.EndFocus();else pet.StartFocus();},true);DockPanel.SetDock(start,Dock.Right);focus.Children.Add(start);focus.Children.Add(UI.Text(pet.FocusActive?"Odak zamanı. Kendine biraz alan aç.":"Şimdi tek bir şey. Küçük bir adımla başla.",16,ink));var fc=UI.Card(focus);fc.Background=UI.B(UI.StoneColor);page.Children.Add(fc);
   var all=pet.P.Reminders.Where(r=>InRitim(r,group)).OrderBy(r=>r.Order).ToList();var today=all.Where(r=>TaskState.ActiveToday(r,ViewDate)).ToList();int done=today.Count(r=>Tracking.Done(pet.P,r,ViewDate));
   var stats=new StackPanel();stats.Children.Add(UI.Text(done+" / "+today.Count+" görev tamamlandı",17,ink));stats.Children.Add(new ProgressBar{Minimum=0,Maximum=Math.Max(1,today.Count),Value=done,Height=5,Foreground=UI.B(UI.OliveColor),Background=UI.B(UI.StoneColor),BorderThickness=new Thickness(0),Margin=new Thickness(0,12,0,0)});page.Children.Add(UI.Card(stats));
   var controls=new WrapPanel{Margin=new Thickness(0,6,0,15)};foreach(string f in new[]{"all","open","done"}){string key=f;string label=f=="all"?"Tüm görevler":f=="open"?"Yapılacaklar":"Tamamlanan";var button=UI.Button(label,()=>{ritimFilter=key;RefreshToday();},ritimFilter==f);button.Name="RitimFilter_"+f;controls.Children.Add(button);}page.Children.Add(controls);
   AddDateAndRoutineRecord(group);
   var shown=all.Where(r=>ritimFilter=="all"||(ritimFilter=="done")==Tracking.Done(pet.P,r,ViewDate)).ToList();
   var grid=new Grid{Name="RitimTasks"};grid.ColumnDefinitions.Add(new ColumnDefinition());grid.ColumnDefinitions.Add(new ColumnDefinition());for(int i=0;i<shown.Count;i++){if(i%2==0)grid.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});var card=TaskCard(shown[i],true);card.Margin=new Thickness(0,0,10,12);Grid.SetColumn(card,i%2);Grid.SetRow(card,i/2);grid.Children.Add(card);}page.Children.Add(grid);
   if(shown.Count==0)page.Children.Add(UI.Card(UI.Text(ritimFilter=="done"?"Tamamladığın görevler burada görünecek.":"Burada biraz alan var. Yeni bir görevle başlayabilirsin.",15,UI.Muted)));
  }
 }
 internal static class RitimQA {
  static IEnumerable<DependencyObject> Walk(DependencyObject root){yield return root;foreach(var o in LogicalTreeHelper.GetChildren(root)){var d=o as DependencyObject;if(d!=null)foreach(var x in Walk(d))yield return x;}}
  public static void Run(PetWindow pet,string folder){
   RequestedRoutineLists.Apply(pet.P);var report=new StringBuilder();int count=0;Action<bool,string> check=(ok,msg)=>{if(!ok)throw new Exception(msg);count++;report.AppendLine(msg);};
   var section=new UserSection{Id="ritim-test-section",Parent="daily",Name="Hastane işleri",Icon="check"};pet.P.Sections.Add(section);
   var other=new UserSection{Id="ritim-other",Parent="daily",Name="Diğer işler",Icon="book"};pet.P.Sections.Add(other);
   var task=new Reminder{Id="ritim-qa-task",Title="Ritim doğrulama görevi",Group=section.Id,NoNotification=true,Enabled=true};pet.P.Reminders.Add(task);
   pet.P.Reminders.Add(new Reminder{Title="Diğer kategorinin görevi",Group=other.Id,NoNotification=true});
   var window=new SettingsWindow(pet);window.Show();window.Navigate("home");window.RenderPage(Path.Combine(folder,"lucy-home.png"));
   check(Walk(window).OfType<Button>().Any(b=>(b.Content as string)=="Ritim"),"Ritim navigation available");
   window.Navigate("daily");window.RenderPage(Path.Combine(folder,"ritim.png"));
   check(UI.AccentColor==UI.OliveColor&&UI.PageBackground==UI.PaperColor,"Ritim uses the requested paper and olive palette");
   check(!Walk(window).OfType<TextBlock>().Any(t=>t.Text==task.Title||t.Text=="Diğer kategorinin görevi"),"Ritim root does not mix task contents from subcategories");
   check(!Walk(window).OfType<Button>().Any(b=>b.Name=="RitimAdd"||b.Name.StartsWith("RitimFilter_")),"Root has neither the global new-task button nor task filters");
   window.Navigate(section.Id);window.RenderPage(Path.Combine(folder,"ritim-category.png"));
   check(Walk(window).OfType<TextBlock>().Any(t=>t.Text==task.Title)&&!Walk(window).OfType<TextBlock>().Any(t=>t.Text=="Diğer kategorinin görevi"),"Opening a category shows only its own tasks");
   check(Walk(window).OfType<Button>().Any(b=>b.Name=="RitimAdd"),"New-task action is available inside its target category");
   Exception editorError=null;var timer=new System.Windows.Threading.DispatcherTimer{Interval=TimeSpan.FromMilliseconds(100)};timer.Tick+=(s,e)=>{timer.Stop();try{var editor=window.OwnedWindows.OfType<ReminderEditor>().Single();Walk(editor).OfType<TextBox>().Single(t=>t.Name=="ReminderTitle").Text="kategoriye yeni görev";Walk(editor).OfType<Button>().Single(b=>(b.Content as string)=="Kaydet").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));}catch(Exception ex){editorError=ex;foreach(Window owned in window.OwnedWindows)owned.Close();}};timer.Start();Walk(window).OfType<Button>().Single(b=>b.Name=="RitimAdd").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));if(editorError!=null)throw editorError;
   check(pet.P.Reminders.Any(r=>r.Title=="Kategoriye yeni görev"&&r.Group==section.Id),"New-task button saves into the currently open category");
   var box=Walk(window).OfType<CheckBox>().First(b=>System.Windows.Automation.AutomationProperties.GetName(b)==task.Title+" tamamlandı");box.IsChecked=true;
   check(Tracking.Done(pet.P,task,DateTime.Today),"Ritim checkbox saves completion through Lucy tracking");
   Walk(window).OfType<Button>().First(b=>b.Name=="RitimFilter_open").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
   check(!Walk(window).OfType<TextBlock>().Any(t=>t.Text==task.Title),"Open filter hides completed tasks");
   Walk(window).OfType<Button>().First(b=>b.Name=="RitimFilter_done").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
   check(Walk(window).OfType<TextBlock>().Any(t=>t.Text==task.Title),"Completed filter shows completed tasks");
   var back=Walk(window).OfType<Button>().Single(b=>b.Name=="CategoryBack");check(back.Visibility==Visibility.Visible,"Category has a persistent visible back button");back.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
   check(!Walk(window).OfType<Button>().Any(b=>b.Name=="RitimAdd")&&back.Visibility==Visibility.Collapsed,"Back returns directly to the Ritim category screen");
   window.Navigate("care");check(UI.AccentColor==UI.OliveColor,"Care uses the same requested palette");window.RenderPage(Path.Combine(folder,"lucy-care.png"));
   foreach(var family in new[]{UI.AppFont,UI.HeadingFont}){GlyphTypeface glyph;var typeface=new Typeface(family,FontStyles.Normal,family==UI.HeadingFont?FontWeights.SemiBold:FontWeights.Normal,FontStretches.Normal);bool found=typeface.TryGetGlyphTypeface(out glyph);File.AppendAllText(Path.Combine(folder,"fonts.txt"),family.Source+" found="+found+" uri="+(found?glyph.FontUri.ToString():"none")+Environment.NewLine);check(found&&glyph.FontUri.IsFile&&glyph.FontUri.LocalPath.StartsWith(Path.Combine(Store.Root,"assets","fonts"),StringComparison.OrdinalIgnoreCase),"Typography loads the bundled font file: "+family.Source);check("ÇĞİÖŞÜçğıöşü".All(c=>glyph.CharacterToGlyphMap.ContainsKey(c)),"Bundled font supports Turkish characters: "+family.Source);}
   using(var trayFont=UI.CreateTrayFont())check(trayFont.FontFamily.Name.StartsWith("DM Sans",StringComparison.Ordinal),"Tray menu loads DM Sans successfully");
   foreach(string group in new[]{"morning","evening","sport","lucy-shower"}){window.Navigate(group);var returnButton=Walk(window).OfType<Button>().Single(b=>b.Name=="CategoryBack");check(returnButton.Visibility==Visibility.Visible,"Back is visible in "+group);returnButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));check(returnButton.Visibility==Visibility.Collapsed,"Back returns to parent in "+group);}
   window.Close();
   File.WriteAllText(Path.Combine(folder,"smoke-test.txt"),"PASS: "+count+Environment.NewLine+report,Encoding.UTF8);
  }
 }
}
