using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace LucyDesktop {
 internal static class WheelNavigation {
  static bool installed;
  public static void Install(){if(installed)return;installed=true;EventManager.RegisterClassHandler(typeof(Window),Mouse.PreviewMouseWheelEvent,new MouseWheelEventHandler(OnWheel),true);}
  static DependencyObject Parent(DependencyObject node){
   if(node is Visual||node is System.Windows.Media.Media3D.Visual3D)return VisualTreeHelper.GetParent(node)??LogicalTreeHelper.GetParent(node);
   var content=node as FrameworkContentElement;return content==null?LogicalTreeHelper.GetParent(node):content.Parent;
  }
  static void OnWheel(object sender,MouseWheelEventArgs e){
   if(e.Handled||e.Delta==0)return;var viewers=new List<ScrollViewer>();
   for(var node=e.OriginalSource as DependencyObject;node!=null;node=Parent(node)){
    var combo=node as ComboBox;if(combo!=null&&combo.IsDropDownOpen)return;
    var date=node as DatePicker;if(date!=null&&date.IsDropDownOpen)return;
    var viewer=node as ScrollViewer;if(viewer!=null&&viewer.IsEnabled&&viewer.VerticalScrollBarVisibility!=ScrollBarVisibility.Disabled)viewers.Add(viewer);
   }
   // A horizontal-only table viewer must not swallow the surrounding page's wheel.
   foreach(var viewer in viewers){
    bool canMove=viewer.ScrollableHeight>.1&&(e.Delta<0?viewer.VerticalOffset<viewer.ScrollableHeight-.1:viewer.VerticalOffset>.1);if(!canMove)continue;
    double notches=Math.Abs(e.Delta)/120.0;int lines=SystemParameters.WheelScrollLines;
    if(lines<0){if(e.Delta<0)viewer.PageDown();else viewer.PageUp();}
    else if(viewer.CanContentScroll){int steps=Math.Max(1,(int)Math.Ceiling(notches*Math.Max(1,lines)));for(int i=0;i<steps;i++){if(e.Delta<0)viewer.LineDown();else viewer.LineUp();}}
    else viewer.ScrollToVerticalOffset(Math.Max(0,Math.Min(viewer.ScrollableHeight,viewer.VerticalOffset-Math.Sign(e.Delta)*notches*Math.Max(1,lines)*16)));
    e.Handled=true;return;
   }
   if(viewers.Count>0)e.Handled=true;
  }
 }
 internal static class WheelNavigationQA {
  static IEnumerable<DependencyObject> Visuals(DependencyObject root){yield return root;for(int i=0;i<VisualTreeHelper.GetChildrenCount(root);i++)foreach(var child in Visuals(VisualTreeHelper.GetChild(root,i)))yield return child;}
  static void Settle(Window window){window.UpdateLayout();var frame=new DispatcherFrame();window.Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle,new Action(()=>frame.Continue=false));Dispatcher.PushFrame(frame);window.UpdateLayout();}
  static void Wheel(UIElement target,int delta,Window window){target.RaiseEvent(new MouseWheelEventArgs(Mouse.PrimaryDevice,Environment.TickCount,delta){RoutedEvent=Mouse.PreviewMouseWheelEvent});Settle(window);}
  public static void Run(PetWindow pet,string folder){
   var report=new StringBuilder();int count=0;Action<bool,string> check=(ok,text)=>{if(!ok)throw new Exception(text);count++;report.AppendLine(text);};RequestedRoutineLists.Apply(pet.P);
   var window=new SettingsWindow(pet){Height=650};window.Show();window.Navigate("supplements");Settle(window);
   var viewers=Visuals(window).OfType<ScrollViewer>().ToList();var page=viewers.First(v=>v.Content is StackPanel);var table=Visuals(window).OfType<Grid>().Single(g=>g.Name=="SupplementWeekTable");
   check(page.ScrollableHeight>0,"Supplement page has overflowing content for the wheel regression test.");
   Wheel(table,-120,window);check(page.VerticalOffset>0,"Wheel over the nested weekly table scrolls the page downward.");double offset=page.VerticalOffset;
   Wheel(table,120,window);check(page.VerticalOffset<offset,"Wheel over the weekly table scrolls the page upward.");
   window.Navigate("evening");Settle(window);page=Visuals(window).OfType<ScrollViewer>().First(v=>v.Content is StackPanel);var cardText=Visuals(window).OfType<TextBlock>().First(t=>t.Text=="Örnek akşam görevi 1");Wheel(cardText,-120,window);check(page.VerticalOffset>0,"Wheel over a routine task card scrolls its page.");
   window.Navigate("measurements");Settle(window);page=Visuals(window).OfType<ScrollViewer>().First(v=>v.Content is StackPanel);var grid=Visuals(window).OfType<DataGrid>().Single();var inner=Visuals(grid).OfType<ScrollViewer>().First(v=>v.ScrollableHeight>0);
   inner.ScrollToTop();page.ScrollToTop();Settle(window);var cell=Visuals(grid).OfType<DataGridCell>().First();Wheel(cell,-120,window);check(inner.VerticalOffset>0&&page.VerticalOffset==0,"Measurement rows scroll first while the inner table can move.");
   inner.ScrollToBottom();Settle(window);cell=Visuals(grid).OfType<DataGridCell>().First();Wheel(cell,-120,window);check(page.VerticalOffset>0,"At the bottom of the measurement grid, the wheel continues scrolling the surrounding page.");
   page.ScrollToBottom();Settle(window);double before=page.VerticalOffset;var year=Visuals(window).OfType<ComboBox>().First();object selected=year.SelectedItem;Wheel(year,120,window);check(page.VerticalOffset<before&&Equals(year.SelectedItem,selected),"Wheel over a closed selector scrolls the page without changing the selected year.");
   var editor=new ReminderEditor(window,null,"supplements"){Height=530};editor.Show();Settle(editor);var editPage=Visuals(editor).OfType<ScrollViewer>().First(v=>v.Content is StackPanel);var input=Visuals(editor).OfType<TextBox>().First();Wheel(input,-120,editor);check(editPage.VerticalOffset>0,"Wheel over a single-line input scrolls a task editor.");editor.Close();window.Close();
   File.WriteAllText(Path.Combine(folder,"smoke-test.txt"),"PASS: "+count+Environment.NewLine+report,Encoding.UTF8);
  }
 }
}
