using System.Windows;
using System.Windows.Controls;
namespace LucyDesktop {
 public static partial class UI {
  static ControlTemplate checkTemplate;
  public static ControlTemplate WarmCheckTemplate {get{
   if(checkTemplate==null)checkTemplate=(ControlTemplate)System.Windows.Markup.XamlReader.Parse(
    "<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' TargetType='CheckBox'><DockPanel LastChildFill='True'><Border x:Name='box' Width='16' Height='16' CornerRadius='4' BorderThickness='1.2' BorderBrush='"+StoneColor+"' Background='"+PaperColor+"' VerticalAlignment='Center' Margin='0,0,7,0'><Path x:Name='tick' Data='M 2,7 L 5,10 L 11,3' Stroke='"+PaperColor+"' StrokeThickness='1.8' StrokeStartLineCap='Round' StrokeEndLineCap='Round' Visibility='Collapsed'/></Border><ContentPresenter VerticalAlignment='Center' RecognizesAccessKey='True'/></DockPanel><ControlTemplate.Triggers><Trigger Property='IsChecked' Value='True'><Setter TargetName='box' Property='Background' Value='"+OliveColor+"'/><Setter TargetName='box' Property='BorderBrush' Value='"+OliveColor+"'/><Setter TargetName='tick' Property='Visibility' Value='Visible'/></Trigger><Trigger Property='IsEnabled' Value='False'><Setter Property='Opacity' Value='0.4'/></Trigger><Trigger Property='IsMouseOver' Value='True'><Setter TargetName='box' Property='BorderBrush' Value='"+OliveColor+"'/></Trigger><Trigger Property='IsKeyboardFocused' Value='True'><Setter TargetName='box' Property='BorderBrush' Value='"+InkColor+"'/></Trigger></ControlTemplate.Triggers></ControlTemplate>");
   return checkTemplate;
  }}
 }
}
