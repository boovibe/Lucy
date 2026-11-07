using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace LucyDesktop {
 internal static class RoutineNotesQA {
  static IEnumerable<DependencyObject> Descendants(DependencyObject root){yield return root;foreach(var item in LogicalTreeHelper.GetChildren(root)){var child=item as DependencyObject;if(child!=null)foreach(var d in Descendants(child))yield return d;}}
  public static void Run(PetWindow pet,string folder){
   var report=new StringBuilder();int count=0;Action<bool,string> check=(ok,text)=>{if(!ok)throw new Exception(text);count++;report.AppendLine(text);};
   var p=pet.P;var today=DateTime.Today;var product=new SkinProduct{Name="Test bakım ürünü"};p.Products.Add(product);
   p.RoutineRecords.Add(new RoutineRecord{Date=Tracking.Key(today),Group="sport",Status="Bugün atladım",Note="Önceki spor notum",Dryness=5,Redness=5,Blemishes=5,ProductIds=new List<string>{product.Id}});
   p.RoutineRecords.Add(new RoutineRecord{Date=Tracking.Key(today),Group="morning",Status="Yaptım",Note="Önceki cilt notum",Dryness=1,Redness=2,Blemishes=3,ProductIds=new List<string>{product.Id}});
   var owner=new SettingsWindow(pet);owner.Show();
   Func<string,bool,RoutineRecord> submit=(group,skin)=>{
    owner.Navigate(group);var editor=new RoutineRecordEditor(owner,p,group,today);Exception failure=null;
    editor.Loaded+=(s,e)=>editor.Dispatcher.BeginInvoke(new Action(()=>{try{
     var controls=Descendants(editor).ToList();var texts=controls.OfType<TextBlock>().Select(t=>t.Text).ToList();
     var note=controls.OfType<TextBox>().Single(b=>b.Name=="RoutineNote");
     check(note.Text==(skin?"Önceki cilt notum":"Önceki spor notum"),group+": existing note is loaded independently.");
     check(texts.Contains("Kuruluk")==skin&&texts.Contains("Kızarıklık")==skin&&texts.Contains("Sivilce görünümü")==skin,group+": skin observation fields appear only in skincare.");
     check(texts.Contains("Bugün kullandığım ürünler")==skin,group+": skincare products appear only in skincare.");
     if(skin)check(controls.OfType<TextBox>().Single(b=>b.Name=="SkinRedness").Text=="2"&&controls.OfType<CheckBox>().Any(c=>c.IsChecked==true),"Existing skin scores and product choices are preserved.");
     note.Text=skin?"Cilt notu doğrulama":"Spor notu doğrulama";
     controls.OfType<ComboBox>().Single(b=>b.Name=="RoutineStatus").SelectedItem="Yaptım";
     editor.UpdateLayout();PetWindow.Snapshot(editor,Path.Combine(folder,group+"-note.png"),(int)editor.ActualWidth,(int)editor.ActualHeight);
     controls.OfType<Button>().Single(b=>b.Name=="SaveRoutineRecord").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
    }catch(Exception ex){failure=ex;editor.Close();}}));
    bool? result=editor.ShowDialog();if(failure!=null)throw failure;check(result==true&&editor.Value.Group==group,group+": real save action submits the correct routine.");return editor.Value;
   };
   var sport=submit("sport",false);Tracking.SaveRoutine(p,sport,DateTime.Now);
   check(sport.ProductIds.Count==0&&!sport.Dryness.HasValue&&!sport.Redness.HasValue&&!sport.Blemishes.HasValue,"Sport submission contains no skincare data.");
   var skinRecord=submit("morning",true);Tracking.SaveRoutine(p,skinRecord,DateTime.Now);
   check(p.RoutineRecords.Single(r=>r.Group=="sport").Note=="Spor notu doğrulama"&&skinRecord.Note=="Cilt notu doğrulama","Saving skincare leaves the separate sport note intact.");
   var careReport=Reports.Build(p,today,today,false);string content=string.Join("\n",careReport.Sections.SelectMany(section=>section.Paragraphs.Concat(section.Rows.Select(row=>string.Join(" ",row)))));
   check(content.Contains("Cilt notu doğrulama")&&!content.Contains("Spor notu doğrulama"),"Skin reports include skincare notes and exclude sport notes.");
   var invalid=new RoutineRecord{Date=Tracking.Key(today),Group="sport",Note="Spor veri kontrolü",ProductIds=new List<string>{product.Id},Dryness=4,Redness=4,Blemishes=4};Tracking.SaveRoutine(p,invalid,DateTime.Now);
   check(invalid.ProductIds.Count==0&&!invalid.Dryness.HasValue&&!invalid.Redness.HasValue&&!invalid.Blemishes.HasValue,"Storage also prevents skincare fields from being saved into sport notes.");
   check(p.RoutineRecords.Single(r=>r.Group=="morning").Redness==2,"Sport save does not alter skincare observations.");owner.Close();
   File.WriteAllText(Path.Combine(folder,"smoke-test.txt"),"PASS: "+count+Environment.NewLine+report,Encoding.UTF8);
  }
 }
}
