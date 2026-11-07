using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace LucyDesktop {
 public static class RequestedListsTests {
  public static void Run(Action<bool,string> ok){
   var p=Preferences.Defaults();var market=new UserSection{Id="existing-market",Parent="shopping",Name="Örnek market listesi"};p.Sections.Add(market);var oldItem=new ShoppingItem{Id="existing-item",SectionId=market.Id,Title="Süt",Checked=true,Quantity="2 litre"};p.Shopping.Add(oldItem);
   RequestedRoutineLists.Apply(p);int count=p.Reminders.Count;RequestedRoutineLists.Apply(p);
   ok(count==35&&p.Reminders.Count==count,"Requested list import adds exactly 29 tasks and is idempotent");
   ok(p.Reminders.Count(r=>r.Group=="supplements")==8&&p.Reminders.Where(r=>r.Group=="supplements").OrderBy(r=>r.Order).Select(r=>r.Quantity).SequenceEqual(RequestedRoutineLists.SupplementAmounts),"Eight supplements keep separate requested quantity fields");
   ok(p.Reminders.Where(r=>r.Id.StartsWith("lucy-")).All(r=>r.NoNotification&&r.Message==""),"Imported checklist items do not invent notification times or duplicate notes");
   ok(market.Columns==3&&p.Shopping.Single()==oldItem&&oldItem.Checked&&oldItem.Quantity=="2 litre","Existing market products and completion survive the three-column layout change");
   var shower=p.Sections.Single(s=>s.SectionType=="shower");ok(p.Reminders.Count(r=>r.Group==shower.Id)==6&&!SectionNames.IsCare(p,shower.Id)&&!SectionNames.IsCare(p,"supplements"),"Shower and supplement logs stay separate from skincare observations");
   var task=p.Reminders.First(r=>r.Group=="supplements");var day=new DateTime(2026,10,27);Tracking.SetDone(p,task,true,day,day.AddHours(10));ok(Tracking.Done(p,task,day)&&!Tracking.Done(p,task,day.AddDays(1)),"Weekly supplement checks belong to their selected date");
   using(var stream=new MemoryStream()){var serializer=new DataContractJsonSerializer(typeof(Preferences));serializer.WriteObject(stream,p);stream.Position=0;var copy=(Preferences)serializer.ReadObject(stream);Store.Validate(copy);ok(copy.Reminders.Count(r=>r.Group=="supplements")==8&&copy.Reminders.First(r=>r.Id==task.Id).Quantity==task.Quantity&&Tracking.Done(copy,copy.Reminders.First(r=>r.Id==task.Id),day)&&copy.Sections.Single(s=>s.Id==market.Id).Columns==3,"Supplement quantities, weekly checks, custom shower and market columns survive reopening");}
  }
 }
 internal static class RequestedListsQA {
  static IEnumerable<DependencyObject> Descendants(DependencyObject root){yield return root;foreach(var item in LogicalTreeHelper.GetChildren(root)){var child=item as DependencyObject;if(child!=null)foreach(var d in Descendants(child))yield return d;}}
  static List<Button> OpenButtons(Window window){return Descendants(window).OfType<Button>().Where(b=>b.Name=="RouteOpen").ToList();}
  public static void Run(PetWindow pet,string folder){
   var report=new StringBuilder();int count=0;Action<bool,string> check=(ok,text)=>{if(!ok)throw new Exception(text);count++;report.AppendLine(text);};var p=pet.P;
   RequestedRoutineLists.Apply(p);var market=p.Sections.First(s=>s.Parent=="shopping"&&s.Columns==3);foreach(string name in Enumerable.Range(1,12).Select(i=>"Örnek ürün "+i))p.Shopping.Add(new ShoppingItem{Title=name,SectionId=market.Id,Icon="food",Quantity="1 adet"});
   var window=new SettingsWindow(pet);window.Show();window.Navigate("home");window.UpdateLayout();
   var opens=OpenButtons(window);check(opens.Count==4&&opens.Max(b=>b.TranslatePoint(new Point(),window).Y)-opens.Min(b=>b.TranslatePoint(new Point(),window).Y)<.5,"All four home entry buttons share the same baseline.");window.RenderPage(Path.Combine(folder,"home-aligned.png"));
   window.Navigate("care");window.UpdateLayout();opens=OpenButtons(window);var firstRow=opens.Take(2).ToList();check(firstRow.Count==2&&firstRow.Max(b=>b.TranslatePoint(new Point(),window).Y)-firstRow.Min(b=>b.TranslatePoint(new Point(),window).Y)<.5,"Routine entry buttons in the same row share the same baseline.");window.RenderPage(Path.Combine(folder,"care-aligned.png"));
   window.Navigate(market.Id);window.UpdateLayout();var itemGrid=Descendants(window).OfType<UniformGrid>().Single(g=>g.Name=="ShoppingItemsGrid");check(itemGrid.Columns==3&&itemGrid.Children.Count==12,"The market renders twelve existing items in three columns.");window.RenderPage(Path.Combine(folder,"market-three-columns.png"));
   var shower=p.Sections.Single(s=>s.SectionType=="shower");window.Navigate(shower.Id);window.RenderPage(Path.Combine(folder,"shower.png"));window.Navigate("evening");window.RenderPage(Path.Combine(folder,"evening.png"));
   window.Navigate("supplements");window.UpdateLayout();var grid=Descendants(window).OfType<Grid>().Single(g=>g.Name=="SupplementWeekTable");check(grid.ColumnDefinitions.Count==10&&grid.RowDefinitions.Count==9,"Supplement table has separate name and quantity columns, seven days and eight entries.");
   var checks=Descendants(window).OfType<CheckBox>().Where(c=>c.Name=="SupplementDay").ToList();check(checks.Count==56,"Each of eight supplements has seven dated checkboxes.");var task=p.Reminders.First(r=>r.Group=="supplements");string key=task.Id+"|"+Tracking.Key(DateTime.Today);checks.Single(c=>(string)c.Tag==key).IsChecked=true;check(Tracking.Done(p,task,DateTime.Today)&&!Tracking.Done(p,task,DateTime.Today.AddDays(1)),"Toggling the real table checkbox records only the selected day.");
   check(Descendants(window).OfType<Button>().Any(b=>(b.Content as string)=="Takviyeler"),"Supplements is a main navigation tab.");window.RenderPage(Path.Combine(folder,"supplements-week.png"));
   window.Navigate("morning");var recordEditor=new RoutineRecordEditor(window,p,shower.Id,DateTime.Today);recordEditor.Show();recordEditor.UpdateLayout();check(!Descendants(recordEditor).OfType<TextBlock>().Any(t=>new[]{"Kuruluk","Kızarıklık","Sivilce görünümü"}.Contains(t.Text)),"Shower note form does not show skincare scores.");recordEditor.Close();
   var editor=new ReminderEditor(window,task,"supplements");editor.Show();editor.UpdateLayout();check(Descendants(editor).OfType<TextBox>().Single(t=>t.Name=="SupplementQuantity").Text==RequestedRoutineLists.SupplementAmounts[0],"Supplement editor exposes the saved amount in a separate field.");PetWindow.Snapshot(editor,Path.Combine(folder,"supplement-editor.png"),(int)editor.ActualWidth,(int)editor.ActualHeight);editor.Close();window.Close();
   File.WriteAllText(Path.Combine(folder,"smoke-test.txt"),"PASS: "+count+Environment.NewLine+report,Encoding.UTF8);
  }
 }
}
