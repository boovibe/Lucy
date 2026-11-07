using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;

namespace LucyDesktop {
 public partial class Measurement:INotifyPropertyChanged {
  [DataMember] public string MeasuredOn {get;set;}
  public event PropertyChangedEventHandler PropertyChanged;
  public bool HasValues {get{return new[]{Shoulder,Chest,Waist,Hip,Weight,Fat,Bmi,Arm,Leg}.Any(s=>!string.IsNullOrWhiteSpace(s));}}
  public DateTime MonthStart {get{DateTime date;if(!DateTime.TryParse(Date,CultureInfo.InvariantCulture,DateTimeStyles.None,out date))date=DateTime.Today;return new DateTime(date.Year,date.Month,1);}}
  public DateTime MonthEnd {get{return MonthStart.AddMonths(1).AddDays(-1);}}
  public string PeriodLabel {get{return MonthStart.ToString("MMMM yyyy",CultureInfo.GetCultureInfo("tr-TR"));}}
  public string MeasurementDateLabel {get{return MeasurementDay.HasValue?MeasurementDay.Value.ToString("dd.MM.yyyy"):"";}}
  public DateTime? MeasurementDay {get{DateTime day;return !string.IsNullOrWhiteSpace(MeasuredOn)&&DateTime.TryParse(MeasuredOn,CultureInfo.InvariantCulture,DateTimeStyles.None,out day)?(DateTime?)day:null;}set{if(value.HasValue){if(value.Value<MonthStart||value.Value>MonthEnd)throw new ArgumentOutOfRangeException("value","Bu satırın ayından bir gün seç.");MeasuredOn=value.Value.ToString("yyyy-MM-dd");Date=MeasuredOn;}else MeasuredOn="";if(PropertyChanged!=null){PropertyChanged(this,new PropertyChangedEventArgs("MeasurementDay"));PropertyChanged(this,new PropertyChangedEventArgs("MeasurementDateLabel"));PropertyChanged(this,new PropertyChangedEventArgs("PeriodLabel"));}}}
 }
 public static class MeasurementDates {
  public static void Initialize(Preferences p){foreach(var m in p.Measurements){if(m.MeasuredOn!=null)continue;m.MeasuredOn=((m.Source??"").EndsWith(" / SPOR",StringComparison.Ordinal)&&m.HasValues||m.Source=="Elle girildi")?m.Date:"";}}
  public static void Add(Preferences p,Measurement record){string month=record.Date.Substring(0,7);p.Measurements.RemoveAll(m=>(m.Date??"").StartsWith(month,StringComparison.Ordinal)&&m.Source=="Elle girilecek"&&!m.HasValues&&!m.MeasurementDay.HasValue&&string.IsNullOrWhiteSpace(m.Notes));if(record.MeasuredOn==null)record.MeasuredOn=record.Date;p.Measurements.Add(record);}
 }
}
