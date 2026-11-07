using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using System.Windows;
using System.Windows.Controls;

namespace LucyDesktop {
 [DataContract] public class UserSection {
  [DataMember] public string Id=Guid.NewGuid().ToString("N");
  [DataMember] public string Parent,Name,Icon;
  [DataMember] public int Order;
  [DataMember] public string SectionType;
  [DataMember] public int Columns;
 }
 public static class SectionNames {
  public static void Initialize(Preferences p){
   if(p.Sections==null)p.Sections=new List<UserSection>();
   if(!p.SectionsInitialized){foreach(var s in new[]{new UserSection{Id="morning",Parent="care",Name="Sabah kişisel bakım rutini",Icon="sun",Order=0},new UserSection{Id="evening",Parent="care",Name="Akşam kişisel bakım rutini",Icon="moon",Order=10},new UserSection{Id="sport",Parent="care",Name="Spor rutini",Icon="dumbbell",Order=20}})if(!p.Sections.Any(x=>x.Id==s.Id))p.Sections.Add(s);p.SectionsInitialized=true;}
   if(p.Shopping!=null)foreach(var item in p.Shopping)if(item.SectionId==null)item.SectionId="";
  }
  public static string Parent(Preferences p,string group){var s=p.Sections==null?null:p.Sections.FirstOrDefault(x=>x.Id==group);return s==null?"":s.Parent;}
  public static string Root(Preferences p,string group){string parent=Parent(p,group);if(parent!="")return parent;if(new[]{"morning","evening","sport","products","weekly","monthly","measurements"}.Contains(group))return "care";return group;}
  public static string Title(Preferences p,string group){var s=p.Sections==null?null:p.Sections.FirstOrDefault(x=>x.Id==group);if(s!=null)return s.Name;switch(group){case "care":return "Kişisel bakım rutini";case "shopping":return "Alışveriş listesi";case "daily":return "Ritim";default:return TaskState.GroupName(group);}}
  public static bool IsPersonalRoutine(Preferences p,string group){return new[]{"morning","evening","sport"}.Contains(group)||p!=null&&p.Sections!=null&&p.Sections.Any(s=>s.Id==group&&s.Parent=="care");}
  public static bool IsCare(Preferences p,string group){return group!="sport"&&group!="supplements"&&!(p.Sections??new List<UserSection>()).Any(s=>s.Id==group&&s.SectionType=="shower")&&(Parent(p,group)=="care"||group=="morning"||group=="evening");}
 }
 public static class TitleRules {
  public static string FirstCapital(string text){if(text==null)return "";var tr=CultureInfo.GetCultureInfo("tr-TR");for(int i=0;i<text.Length;i++)if(char.IsLetter(text[i]))return text.Substring(0,i)+text.Substring(i,1).ToUpper(tr)+text.Substring(i+1);return text;}
  public static string Save(string text){return FirstCapital((text??"").Trim());}
  public static void Attach(TextBox box){bool updating=false;box.TextChanged+=(s,e)=>{if(updating)return;string fixedText=FirstCapital(box.Text);if(fixedText==box.Text)return;int caret=box.CaretIndex;updating=true;box.Text=fixedText;box.CaretIndex=Math.Min(caret,box.Text.Length);updating=false;};box.Text=FirstCapital(box.Text);}
 }
 public class ClockPicker:Grid {
  readonly ComboBox hours=new ComboBox{FontSize=15,Padding=new Thickness(3)},minutes=new ComboBox{FontSize=15,Padding=new Thickness(3)};
  public ClockPicker(DateTime now){Margin=new Thickness(0,5,0,12);ColumnDefinitions.Add(new ColumnDefinition());ColumnDefinitions.Add(new ColumnDefinition{Width=GridLength.Auto});ColumnDefinitions.Add(new ColumnDefinition());ColumnDefinitions.Add(new ColumnDefinition{Width=GridLength.Auto});for(int i=0;i<24;i++)hours.Items.Add(i.ToString("00"));for(int i=0;i<60;i++)minutes.Items.Add(i.ToString("00"));hours.IsEditable=false;minutes.IsEditable=false;Children.Add(hours);var colon=UI.Text(":",15);colon.Margin=new Thickness(3,4,3,0);SetColumn(colon,1);Children.Add(colon);SetColumn(minutes,2);Children.Add(minutes);var current=UI.Button("↺",()=>SetTime(DateTime.Now));current.Padding=new Thickness(5);current.Margin=new Thickness(4,0,0,0);current.ToolTip="Şu anki saat";SetColumn(current,3);Children.Add(current);SetTime(now);}
  public string SelectedTime {get{return (string)hours.SelectedItem+":"+(string)minutes.SelectedItem;}}
  public void SetTime(DateTime now){hours.SelectedIndex=now.Hour;minutes.SelectedIndex=now.Minute;}
 }
 public partial class SettingsWindow {
  string shoppingSection="";
  void ShowSections(string parent){
   var add=UI.Button("+ Alt başlık oluştur",()=>EditSection(null,parent),true);add.HorizontalAlignment=HorizontalAlignment.Left;add.Margin=new Thickness(0,0,0,15);page.Children.Add(add);
   var children=pet.P.Sections.Where(s=>s.Parent==parent).OrderBy(s=>s.Order).ToList();if(children.Count==0)return;var grid=new System.Windows.Controls.Primitives.UniformGrid{Columns=parent=="shopping"?3:2};
   foreach(var section in children){string detail=parent=="shopping"?pet.P.Shopping.Count(i=>(i.SectionId??"")==section.Id&&!i.Checked)+" ürün alınmayı bekliyor.":GroupProgress(section.Id);var card=RouteCard(section.Name,detail,section.Icon,section.Id,UI.SurfaceColor);var body=(StackPanel)((DockPanel)card.Child).Children[0];var edit=UI.Button("✎ Başlığı düzenle",()=>EditSection(section,parent));edit.HorizontalAlignment=HorizontalAlignment.Left;edit.Padding=new Thickness(7,4,7,4);edit.FontSize=13;edit.Margin=new Thickness(0,9,0,0);body.Children.Add(edit);grid.Children.Add(card);}page.Children.Add(grid);
  }
  void EditSection(UserSection section,string parent){var editor=new SectionEditor(this,section,parent);if(editor.ShowDialog()!=true)return;if(section==null){editor.Value.Order=pet.P.Sections.Where(s=>s.Parent==parent).Select(s=>s.Order).DefaultIfEmpty(0).Max()+10;pet.P.Sections.Add(editor.Value);}else{section.Name=editor.Value.Name;section.Icon=editor.Value.Icon;}pet.Save();RefreshToday();}
  void ShowSportTableLink(){var content=new DockPanel();var open=UI.Button("Ölçüm tablosunu aç",()=>Navigate("measurements"),true);System.Windows.Controls.DockPanel.SetDock(open,System.Windows.Controls.Dock.Right);content.Children.Add(open);var labels=new StackPanel();labels.Children.Add(UI.Text("Vücut ölçülerim",19));labels.Children.Add(UI.Text("2025'ten itibaren aylar, ölçüm günleri ve düzenlenebilir değerler.",13,UI.Muted));content.Children.Add(labels);page.Children.Add(UI.Card(content));}
 }
 public class SectionEditor:Window {
  public UserSection Value;
  public SectionEditor(Window owner,UserSection existing,string parent){Owner=owner;Title=existing==null?"Alt başlık oluştur":"Alt başlığı düzenle";Width=440;Height=380;ResizeMode=ResizeMode.NoResize;WindowStartupLocation=WindowStartupLocation.CenterOwner;Background=UI.B(UI.PageBackground);FontFamily=UI.AppFont;var panel=new StackPanel{Margin=new Thickness(24)};Content=panel;panel.Children.Add(UI.Text(Title,23));panel.Children.Add(UI.Text("Başlık",13,UI.Muted));var name=UI.Input(existing==null?"":existing.Name,80);TitleRules.Attach(name);panel.Children.Add(name);panel.Children.Add(UI.Text("Sevimli simge",13,UI.Muted));var icon=IconPicker.Create(existing==null?parent=="care"?"heart":parent=="shopping"?"bag":"check":existing.Icon);panel.Children.Add(icon);var error=UI.Text("",13,UI.Muted);panel.Children.Add(error);var row=new StackPanel{Orientation=Orientation.Horizontal};row.Children.Add(UI.Button("Kaydet",()=>{if(string.IsNullOrWhiteSpace(name.Text)){error.Text="Alt başlık için bir ad yaz.";return;}Value=new UserSection{Name=TitleRules.Save(name.Text),Parent=parent,Icon=IconPicker.Key(icon)};DialogResult=true;},true));row.Children.Add(UI.Button("Vazgeç",()=>DialogResult=false));panel.Children.Add(row);}
 }
 public static class SectionTests {
  public static void Run(Action<bool,string> ok){ok(TitleRules.Save("  istanbul işi ")=="İstanbul işi"&&TitleRules.Save("ılık su")=="Ilık su","Task capitalization respects Turkish dotted and dotless letters");ok(TitleRules.FirstCapital("cilt ")=="Cilt "&&TitleRules.FirstCapital("1. diş fırçala")=="1. Diş fırçala","Live capitalization preserves spaces and leading symbols");var p=Preferences.Defaults();var section=new UserSection{Id="custom-care",Name="Haftalık bakım",Parent="care",Icon="heart"};p.Sections.Add(section);var r=new Reminder{Title="Maske",Group=section.Id};p.Reminders.Add(r);Store.Validate(p);ok(r.Group==section.Id&&SectionNames.Root(p,r.Group)=="care","Custom routine groups survive validation and keep their parent");using(var stream=new System.IO.MemoryStream()){var serializer=new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(Preferences));serializer.WriteObject(stream,p);stream.Position=0;var restored=(Preferences)serializer.ReadObject(stream);Store.Validate(restored);ok(restored.Reminders.Last().Group==section.Id&&restored.Sections.Any(s=>s.Id==section.Id),"Custom sections and task links survive reopening");}ok(CaffeineCalculator.Estimate(5,60,1,null,2)==60&&CaffeineCalculator.Estimate(5,60,2,null,2)==120,"Turkish coffee uses the bundled reference serving");var clock=new ClockPicker(new DateTime(2026,10,27,15,37,0));ok(clock.SelectedTime=="15:37","Drink time is selected without typing and defaults to the current minute");ok(p.Measurements.All(m=>!m.MeasurementDay.HasValue),"Blank periods have no invented measurement day");var sample=new Measurement{Date="2031-05-17",Source="Örnek / SPOR",Weight="70"};p.Measurements.Add(sample);MeasurementDates.Initialize(p);ok(sample.MeasurementDay==new DateTime(2031,5,17),"Existing imported measurements preserve their actual day");}
 }
}
