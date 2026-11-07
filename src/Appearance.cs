using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace LucyDesktop {
 public static partial class UI {
  public const string PaperColor="#F4F1EA",InkColor="#2C332E",OliveColor="#68745C",ClayColor="#AE6248",StoneColor="#DFE1D6";
  public static string PageBackground=PaperColor;
  public static string SurfaceColor {get{return "#FBF9F5";}}
  public static string BorderColor {get{return StoneColor;}}
  public static string Mix(string from,string to,double strength){var a=(Color)ColorConverter.ConvertFromString(from);var b=(Color)ColorConverter.ConvertFromString(to);strength=Math.Max(0,Math.Min(1,strength));return "#"+((byte)Math.Round(a.R+(b.R-a.R)*strength)).ToString("X2")+((byte)Math.Round(a.G+(b.G-a.G)*strength)).ToString("X2")+((byte)Math.Round(a.B+(b.B-a.B)*strength)).ToString("X2");}
  public static string TintColor(double strength){return Mix(PaperColor,OliveColor,strength);}
  public static Brush IconBrush(string original){var c=(Color)ColorConverter.ConvertFromString(original);double light=(.2126*c.R+.7152*c.G+.0722*c.B)/255.0;if(original=="#655174")return Ink;if(light<.50)return B(OliveColor);bool warm=new[]{"#EFAFCA","#E6B77E","#D4AA65","#E8AFCB","#EBAEB0"}.Contains(original);return B(Mix(PaperColor,warm?ClayColor:OliveColor,Math.Max(.08,Math.Min(.65,.04+(1-light)*1.6))));}
  static System.Drawing.Text.PrivateFontCollection trayFonts;
  public static System.Drawing.Font CreateTrayFont(){if(trayFonts==null){trayFonts=new System.Drawing.Text.PrivateFontCollection();trayFonts.AddFontFile(System.IO.Path.Combine(Store.Root,"assets","fonts","DMSans.ttf"));}return new System.Drawing.Font(trayFonts.Families[0],10);}

 }
 public partial class SettingsWindow {
  void RecolorChrome(){RecolorButtons(Content as DependencyObject);}
  public void RenderInstantCoffee(string path){Navigate("drinks");var type=FindDrinkType(page);if(type==null)throw new Exception("Drink selector missing");type.SelectedIndex=4;UpdateLayout();RenderPage(path);}
  ComboBox FindDrinkType(DependencyObject root){var combo=root as ComboBox;if(combo!=null&&combo.Name=="DrinkType")return combo;foreach(var child in LogicalTreeHelper.GetChildren(root)){var item=child as DependencyObject;if(item==null)continue;var result=FindDrinkType(item);if(result!=null)return result;}return null;}
  void RecolorButtons(DependencyObject root){if(root==null)return;var button=root as Button;if(button!=null){if((button.Tag as string)=="Lucy.Accent"){button.Background=UI.B(UI.InkColor);button.Foreground=UI.B(UI.PaperColor);}else if((button.Tag as string)=="Lucy.Soft"){button.Background=UI.B(UI.SoftColor);button.Foreground=UI.Ink;}}foreach(var child in LogicalTreeHelper.GetChildren(root)){var item=child as DependencyObject;if(item!=null)RecolorButtons(item);}}
  void PaletteControlStyles(){var combo=new Style(typeof(ComboBox));combo.Setters.Add(new Setter(Control.BackgroundProperty,UI.B(UI.SurfaceColor)));combo.Setters.Add(new Setter(Control.BorderBrushProperty,UI.B(UI.BorderColor)));combo.Setters.Add(new Setter(Control.ForegroundProperty,UI.Ink));Resources[typeof(ComboBox)]=combo;}
 }
 public static class CaffeineCalculator {
  public const double InstantReferenceGrams=2,InstantEstimateMg=70;
  public static double? Estimate(int type,double ml,int servings,double? overrideMg,double grams){if(type==0)return 0;if(overrideMg.HasValue)return overrideMg.Value*servings;if(type>=1&&type<=3){double[] volumes={0,200,60,220};double[] caffeine={0,90,80,50};return ml/volumes[type]*caffeine[type]*servings;}if(type==4)return grams/InstantReferenceGrams*InstantEstimateMg*servings;if(type==5)return ml/60.0*60.0*servings;return null;}
 }
}
