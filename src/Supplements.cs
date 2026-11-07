using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;

namespace LucyDesktop {
 public partial class SettingsWindow {
  DateTime supplementWeek=Reports.WeekStart(DateTime.Today);
  void SupplementsPage(){
   Heading("Takviyeler","Miktarları ayrı düzenle; aldığın takviyeyi ilgili günün sütununda işaretle.","capsule");
   var controls=new WrapPanel{Margin=new Thickness(0,0,0,12)};
   controls.Children.Add(UI.Button("← Önceki hafta",()=>{supplementWeek=supplementWeek.AddDays(-7);RefreshToday();}));
   controls.Children.Add(UI.Button("Bu hafta",()=>{supplementWeek=Reports.WeekStart(DateTime.Today);RefreshToday();}));
   controls.Children.Add(UI.Button("Sonraki hafta →",()=>{supplementWeek=supplementWeek.AddDays(7);RefreshToday();}));
   controls.Children.Add(UI.Button("+ Takviye ekle",()=>EditTask(null,"supplements"),true));page.Children.Add(controls);
   page.Children.Add(UI.Text(supplementWeek.ToString("dd MMMM",CultureInfo.GetCultureInfo("tr-TR"))+" – "+supplementWeek.AddDays(6).ToString("dd MMMM yyyy",CultureInfo.GetCultureInfo("tr-TR")),17));
   var tasks=pet.P.Reminders.Where(r=>r.Group=="supplements").OrderBy(r=>r.Order).ToList();
   if(tasks.Count==0){Empty("Takviye listen burada.","Adını ve miktarını ayrı alanlara yazarak ilk kaydı ekleyebilirsin.","capsule");return;}
   var table=new Grid{Name="SupplementWeekTable",MinWidth=770,Margin=new Thickness(0,12,0,8)};
   table.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(220)});table.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(100)});
   for(int d=0;d<7;d++)table.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(58)});
   table.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(44)});
   for(int row=0;row<=tasks.Count;row++)table.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});
   Action<UIElement,int,int,bool> cell=(child,row,col,header)=>{var box=new Border{Child=child,Padding=new Thickness(7,6,7,6),Background=UI.B(header?UI.SoftColor:row%2==0?UI.SurfaceColor:UI.PaperColor),BorderBrush=UI.B(UI.BorderColor),BorderThickness=new Thickness(0,0,1,1)};Grid.SetRow(box,row);Grid.SetColumn(box,col);table.Children.Add(box);};
   cell(UI.Text("Takviye",15),0,0,true);cell(UI.Text("Miktar",15),0,1,true);
   string[] days={"Pzt","Sal","Çar","Per","Cum","Cmt","Paz"};for(int d=0;d<7;d++){var label=UI.Text(days[d]+"\n"+supplementWeek.AddDays(d).ToString("dd.MM"),13);label.TextAlignment=TextAlignment.Center;cell(label,0,d+2,true);}cell(UI.Text("",12),0,9,true);
   for(int i=0;i<tasks.Count;i++){
    var task=tasks[i];int row=i+1;var name=UI.Text(task.Title,14);name.VerticalAlignment=VerticalAlignment.Center;cell(name,row,0,false);var quantity=UI.Text(task.Quantity??"",14);quantity.VerticalAlignment=VerticalAlignment.Center;cell(quantity,row,1,false);
    for(int d=0;d<7;d++){
     DateTime day=supplementWeek.AddDays(d);var check=UI.Check("",Tracking.Done(pet.P,task,day));check.Name="SupplementDay";check.Tag=task.Id+"|"+Tracking.Key(day);check.Margin=new Thickness(0);check.HorizontalAlignment=HorizontalAlignment.Center;check.VerticalAlignment=VerticalAlignment.Center;check.IsEnabled=Tracking.CanEdit(day,DateTime.Now)&&TaskState.ActiveToday(task,day);check.ToolTip=task.Title+" · "+day.ToString("dd MMMM",CultureInfo.GetCultureInfo("tr-TR"));AutomationProperties.SetName(check,check.ToolTip.ToString());check.Checked+=(s,e)=>SetTaskCheckAt(task,true,day);check.Unchecked+=(s,e)=>SetTaskCheckAt(task,false,day);cell(check,row,d+2,false);
    }
    var edit=UI.Button("✎",()=>EditTask(task,"supplements"));edit.Padding=new Thickness(3);edit.Margin=new Thickness(0);edit.VerticalAlignment=VerticalAlignment.Center;edit.ToolTip="Takviyeyi ve miktarını düzenle";AutomationProperties.SetName(edit,task.Title+" miktarını düzenle");cell(edit,row,9,false);
   }
   var viewport=new ScrollViewer{Content=table,HorizontalScrollBarVisibility=ScrollBarVisibility.Auto,VerticalScrollBarVisibility=ScrollBarVisibility.Disabled};page.Children.Add(viewport);
   page.Children.Add(UI.Text("Her tik kendi tarihine kaydolur. Bugünün kayıtlarını ve dünkü kayıtları ertesi gün 12.00 sınırına kadar düzenleyebilirsin. Geçmiş haftalar görüntülenir; gelecek günler henüz işaretlenmez.",12,UI.Muted));
  }
  void SetTaskCheckAt(Reminder task,bool done,DateTime day){if(!Tracking.CanEdit(day,DateTime.Now)){status.Text="Bu günün kayıt penceresi kapandı.";RefreshToday();return;}Tracking.SetDone(pet.P,task,done,day,DateTime.Now);pet.Save();RefreshToday();}
 }
}
