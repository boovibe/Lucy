using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace LucyDesktop {
 internal static class CareEditorQA {
  static IEnumerable<DependencyObject> Tree(DependencyObject node){yield return node;foreach(var obj in LogicalTreeHelper.GetChildren(node)){var child=obj as DependencyObject;if(child!=null)foreach(var item in Tree(child))yield return item;}}
  public static void Run(PetWindow pet,string folder){
   var report=new StringBuilder();int count=0;Action<bool,string> check=(ok,text)=>{if(!ok)throw new Exception(text);count++;report.AppendLine(text);};var p=pet.P;RequestedRoutineLists.Apply(p);p.Sections.Add(new UserSection{Id="care-custom",Parent="care",Name="Haftalık bakım",Icon="flower"});p.Reminders.Add(new Reminder{Title="Örnek spor görevi",Group="sport"});p.Reminders.Add(new Reminder{Title="Örnek özel bakım görevi",Group="care-custom"});p.Reminders.Add(new Reminder{Title="Örnek günlük görev",Group="daily"});
   p.Sections.Add(new UserSection{Id="daily-custom",Parent="daily",Name="Günlük alt başlık"});p.Reminders.Add(new Reminder{Title="Alt başlık görevi",Group="daily-custom"});var window=new SettingsWindow(pet);window.Show();
   foreach(var group in new[]{"morning","evening","sport","lucy-shower","care-custom","daily-custom"}){window.Navigate(group);window.UpdateLayout();var grid=Tree(window).OfType<Grid>().Single(g=>g.Name=="RoutineTasksGrid"||g.Name=="RitimTasks");check(grid.ColumnDefinitions.Count==2,group+": task grid has the correct column count.");if(group=="evening")window.RenderPage(Path.Combine(folder,"evening-two-columns.png"));}
   Func<string,bool,Reminder> saveNew=(group,enable)=>{
    window.Navigate(group);var editor=new ReminderEditor(window,null,group);Exception error=null;
    editor.Loaded+=(s,e)=>editor.Dispatcher.BeginInvoke(new Action(()=>{try{
     var fields=Tree(editor).ToList();var title=fields.OfType<TextBox>().Single(t=>t.Name=="ReminderTitle");var time=fields.OfType<TextBox>().Single(t=>t.Name=="ReminderTime");var notify=fields.OfType<CheckBox>().Single(t=>t.Name=="ReminderNotification");var save=fields.OfType<Button>().Single(b=>(b.Content as string)=="Kaydet");
     check(notify.IsChecked==false&&time.Text==""&&!time.IsEnabled,group+": new reminder starts unchecked with an empty disabled time field.");title.Text="örnek yeni görev";
     if(enable){notify.IsChecked=true;check(time.IsEnabled&&time.Text=="","Enabling a reminder does not insert a suggested time.");save.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));check(editor.Value==null&&editor.IsVisible,"An enabled reminder cannot save without a manually entered valid time.");time.Text="21:45";}
     editor.UpdateLayout();PetWindow.Snapshot(editor,Path.Combine(folder,enable?"manual-time-editor.png":"blank-time-editor.png"),(int)editor.ActualWidth,(int)editor.ActualHeight);save.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
    }catch(Exception ex){error=ex;editor.Close();}}));
    bool? accepted=editor.ShowDialog();if(error!=null)throw error;check(accepted==true,"New care task saved through the editor.");return editor.Value;
   };
   var untimed=saveNew("morning",false);check(untimed.NoNotification&&untimed.Time=="","Saving without a reminder retains an empty time.");p.Reminders.Add(untimed);
   using(var stream=new MemoryStream()){var serializer=new DataContractJsonSerializer(typeof(Preferences));serializer.WriteObject(stream,p);stream.Position=0;var copy=(Preferences)serializer.ReadObject(stream);Store.Validate(copy);var restored=copy.Reminders.Single(r=>r.Id==untimed.Id);check(restored.NoNotification&&restored.Time=="","Empty untimed care task time survives serialization and startup validation.");}
   var timed=saveNew("evening",true);check(!timed.NoNotification&&timed.Time=="21:45","Manually entered reminder and time are saved exactly.");
   var existing=p.Reminders.First(r=>r.Id=="morning");var edit=new ReminderEditor(window,existing,"morning");edit.Show();edit.UpdateLayout();check(Tree(edit).OfType<TextBox>().Single(t=>t.Name=="ReminderTime").Text=="07:30"&&Tree(edit).OfType<CheckBox>().Single(c=>c.Name=="ReminderNotification").IsChecked==true,"Existing configured reminders retain their enabled state and time.");edit.Close();window.Close();
   File.WriteAllText(Path.Combine(folder,"smoke-test.txt"),"PASS: "+count+Environment.NewLine+report,Encoding.UTF8);
  }
 }
}
