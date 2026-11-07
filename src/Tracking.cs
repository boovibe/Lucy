using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Windows;

namespace LucyDesktop {
 [DataContract] public class TaskLog {
  [DataMember] public string TaskId;
  [DataMember] public string Title;
  [DataMember] public string Group;
  [DataMember] public string Date;
  [DataMember] public string RecordedAt;
  [DataMember] public bool Done;
 }
 [DataContract] public class PlanItem {
  [DataMember] public string TaskId,Title,Group,Date;
 }
 [DataContract] public class RoutineRecord {
  [DataMember] public string Date,Group,Note,Status,UpdatedAt;
  [DataMember] public List<string> ProductIds=new List<string>();
  [DataMember] public int? Dryness,Redness,Blemishes;
 }
 [DataContract] public class DrinkEntry {
  [DataMember] public string Id=Guid.NewGuid().ToString("N");
  [DataMember] public string Date,Time,Type,Name,Note;
  [DataMember] public double? Ml,CaffeineMg,DryGrams;
  [DataMember] public int Servings=1;
 }
 [DataContract] public class SkinProduct {
  [DataMember] public string Id=Guid.NewGuid().ToString("N");
  [DataMember] public string Name,Category,Ingredients,Started;
  [DataMember] public bool Active=true;
 }
 [DataContract] public partial class Measurement {
  [DataMember] public string Id {get;set;}
  [DataMember] public string Date {get;set;}
  [DataMember] public string Shoulder {get;set;}
  [DataMember] public string Chest {get;set;}
  [DataMember] public string Waist {get;set;}
  [DataMember] public string Hip {get;set;}
  [DataMember] public string Weight {get;set;}
  [DataMember] public string Fat {get;set;}
  [DataMember] public string Bmi {get;set;}
  [DataMember] public string Arm {get;set;}
  [DataMember] public string Leg {get;set;}
  [DataMember] public string Notes {get;set;}
  [DataMember] public string Source {get;set;}
 }
 public class TrackingPrompt {public string Title,Text,Page;public DateTime Date,Expires;}
 public static class Tracking {
  public static string Key(DateTime day){return TaskState.Day(day);}
  static void Lists(Preferences p){
   if(p.TaskLogs==null)p.TaskLogs=new List<TaskLog>();if(p.RoutineRecords==null)p.RoutineRecords=new List<RoutineRecord>();if(p.Plans==null)p.Plans=new List<PlanItem>();if(p.Drinks==null)p.Drinks=new List<DrinkEntry>();if(p.Products==null)p.Products=new List<SkinProduct>();if(p.Measurements==null)p.Measurements=new List<Measurement>();if(p.PromptKeys==null)p.PromptKeys=new List<string>();if(p.DefaultWaterMl<20||p.DefaultWaterMl>2000)p.DefaultWaterMl=200;
  }
  public static void Upgrade(Preferences p){
   Lists(p);p.Adventures=true;p.OnTop=false;foreach(var group in DataUpgrade.Groups){int n=0;foreach(var r in p.Reminders.Where(r=>r!=null&&r.Group==group)){r.Order=n++*10;if(r.Message==r.Title)r.Message="";DateTime done;if(r.Group!="general"&&DateTime.TryParse(r.DoneDate,out done)){p.TaskLogs.Add(new TaskLog{TaskId=r.Id,Title=r.Title,Group=r.Group,Date=Key(done),Done=true,RecordedAt=DateTime.Now.ToString("o")});if(!p.Plans.Any(i=>i.Date==Key(done)&&i.TaskId==r.Id))p.Plans.Add(new PlanItem{Date=Key(done),Group=r.Group,TaskId=r.Id,Title=r.Title});}}}
   if(p.WaterCount>0&&!string.IsNullOrWhiteSpace(p.WaterDate))p.Drinks.Add(new DrinkEntry{Date=p.WaterDate,Time="",Type="water",Name="Önceki bardak sayacı",Ml=null,CaffeineMg=0,Servings=p.WaterCount,Note="Bardak hacmi belirtilmediği için ml toplamına eklenmedi."});
   p.Version=3;
  }
  public static void Initialize(Preferences p){
   Lists(p);if(string.IsNullOrWhiteSpace(p.TrackingSince))p.TrackingSince=Key(DateTime.Today);
   EnsureMeasurementYear(p,DateTime.Today.Year);
   SectionNames.Initialize(p);MeasurementDates.Initialize(p);SyncPlan(p,DateTime.Today);
  }
  public static void EnsureMeasurementYear(Preferences p,int year){for(int month=1;month<=12;month++){string prefix=year.ToString("0000")+"-"+month.ToString("00");if(!p.Measurements.Any(m=>(m.Date??"").StartsWith(prefix,StringComparison.Ordinal)))p.Measurements.Add(new Measurement{Id=Guid.NewGuid().ToString("N"),Date=prefix+"-01",Source="Elle girilecek",Notes=""});}}
  public static void SyncPlan(Preferences p,DateTime date){if(p.Plans==null||p.Reminders==null)return;string day=Key(date);foreach(var r in p.Reminders.Where(r=>r!=null&&r.Group!="general"&&TaskState.ActiveToday(r,date)))if(!p.Plans.Any(i=>i.Date==day&&i.TaskId==r.Id))p.Plans.Add(new PlanItem{TaskId=r.Id,Title=r.Title,Group=r.Group,Date=day});}
  public static bool CanEdit(DateTime day,DateTime now){return day.Date<=now.Date&&now<=day.Date.AddDays(1).AddHours(12);}
  public static bool Done(Preferences p,Reminder r,DateTime day){var log=p.TaskLogs.LastOrDefault(l=>l.TaskId==r.Id&&l.Date==Key(day));return log!=null?log.Done:r.DoneDate==Key(day);}
  public static void SetDone(Preferences p,Reminder r,bool done,DateTime day,DateTime now){
   if(!CanEdit(day,now))throw new InvalidOperationException("Bu günün kayıt penceresi kapandı.");
   SyncPlan(p,day);string date=Key(day);var log=p.TaskLogs.LastOrDefault(l=>l.TaskId==r.Id&&l.Date==date);if(log==null){log=new TaskLog{TaskId=r.Id,Date=date};p.TaskLogs.Add(log);}log.Title=r.Title;log.Group=r.Group;log.Done=done;log.RecordedAt=now.ToString("o");if(day.Date==now.Date)r.DoneDate=done?date:"";
  }
  public static bool HasRecord(Preferences p,string group,DateTime date){string day=Key(date);return p.RoutineRecords.Any(r=>r.Date==day&&r.Group==group)||p.TaskLogs.Any(r=>r.Date==day&&r.Group==group);}
  public static void SaveRoutine(Preferences p,RoutineRecord record,DateTime now){DateTime day=DateTime.ParseExact(record.Date,"yyyy-MM-dd",CultureInfo.InvariantCulture);if(!CanEdit(day,now))throw new InvalidOperationException("Kayıtlar ertesi gün saat 12.00'ye kadar değiştirilebilir.");if(!SectionNames.IsCare(p,record.Group)){record.ProductIds=new List<string>();record.Dryness=record.Redness=record.Blemishes=null;}p.RoutineRecords.RemoveAll(r=>r.Date==record.Date&&r.Group==record.Group);record.UpdatedAt=now.ToString("o");p.RoutineRecords.Add(record);SyncPlan(p,day);}
  public static void AddWater(Preferences p,DateTime now,int ml){p.Drinks.Add(new DrinkEntry{Date=Key(now),Time=now.ToString("HH:mm"),Type="water",Name="Su",Ml=ml,CaffeineMg=0});}
  public static double Water(Preferences p,DateTime day){return p.Drinks.Where(d=>d.Date==Key(day)&&d.Type=="water").Sum(d=>d.Ml??0);}
  public static double Caffeine(Preferences p,DateTime day){return p.Drinks.Where(d=>d.Date==Key(day)).Sum(d=>d.CaffeineMg??0);}
  public static int UnknownCaffeine(Preferences p,DateTime day){return p.Drinks.Count(d=>d.Date==Key(day)&&d.Type!="water"&&!d.CaffeineMg.HasValue);}
  public static string DrinkSummary(Preferences p,DateTime day){int unknown=UnknownCaffeine(p,day);int unknownWater=p.Drinks.Where(d=>d.Date==Key(day)&&d.Type=="water"&&!d.Ml.HasValue).Sum(d=>d.Servings);return "Su: "+Water(p,day).ToString("0",CultureInfo.InvariantCulture)+" ml · Tahmini kafein: "+Caffeine(p,day).ToString("0.#",CultureInfo.InvariantCulture)+" mg"+(unknown>0?" · "+unknown+" içeceğin kafeini belirtilmedi":"")+(unknownWater>0?" · "+unknownWater+" eski bardağın hacmi bilinmiyor":"");}
  public static List<TrackingPrompt> Prompts(Preferences p,DateTime now){
   SyncPlan(p,now.Date);var list=new List<TrackingPrompt>();
   Action<string,DateTime,DateTime,string,string,string> add=(key,due,until,title,text,page)=>{if(now<due||now>until||p.PromptKeys.Contains(key))return;p.PromptKeys.Add(key);list.Add(new TrackingPrompt{Title=title,Text=text,Page=page,Date=due.Date,Expires=until});};
   var today=now.Date;string day=Key(today);
   if(p.Plans.Any(i=>i.Date==day&&i.Group=="morning")&&!HasRecord(p,"morning",today))add("morning-log:"+day,today.AddHours(12),today.AddDays(1).AddHours(12),"Sabahına küçük bir not", "Sabah bakımın nasıl geçti? Bugünkü rutinini kaydedelim; küçük adımların günlüğünde yerini alsın.","morning");
   var yesterday=today.AddDays(-1);string prev=Key(yesterday);
   if(p.Plans.Any(i=>i.Date==prev&&i.Group=="evening")&&!HasRecord(p,"evening",yesterday)){int count=list.Count;add("evening-log:"+prev,today.AddHours(10),today.AddHours(12),"Dünkü akşama bir pati notu","Dün akşamki bakımını henüz not etmemişsin. Öğlene kadar kaydedip gününü tamamlayabilirsin.","evening");if(list.Count>count)list.Last().Date=yesterday;}
   foreach(int hour in new[]{11,15,19})add("coffee:"+day+":"+hour,today.AddHours(hour),today.AddHours(hour+1),"Kahve molan oldu mu?","İçtiğin kahveyi türü ve miktarıyla not edelim mi? Günün yaklaşık kafein toplamını birlikte takip edelim.","drinks");
   add("day-end:"+day,today.AddHours(23),today.AddDays(1).AddTicks(-1),"Günün küçük içecek özeti",DrinkSummary(p,today),"drinks");
   if(p.PromptKeys.Count>3000)p.PromptKeys.RemoveRange(0,p.PromptKeys.Count-2000);
   return list;
  }
 }
 public static class TaskOrder {
  public static void Move(Preferences p,string source,string target,bool after){var a=p.Reminders.FirstOrDefault(r=>r.Id==source);var b=p.Reminders.FirstOrDefault(r=>r.Id==target);if(a==null||b==null||a==b||a.Group!=b.Group)return;var rows=p.Reminders.Where(r=>r.Group==a.Group).OrderBy(r=>r.Order).ToList();rows.Remove(a);rows.Insert(rows.IndexOf(b)+(after?1:0),a);for(int i=0;i<rows.Count;i++)rows[i].Order=i*10;}
 }
 public partial class PetWindow {
  Notice PromptNotice(TrackingPrompt prompt){return new Notice{Title=prompt.Title,Text=prompt.Text,PlaySound=true,ValidUntil=prompt.Expires,StillRelevant=()=>!new[]{"morning","evening"}.Contains(prompt.Page)||!Tracking.HasRecord(P,prompt.Page,prompt.Date),ActionLabel="Kaydımı aç",OpenAction=()=>{OpenSettings();settings.NavigateAt(prompt.Page,prompt.Date);}};}
 }
 public static class TrackingTests {
  public static void Run(Action<bool,string> ok){
   ok(CaffeineCalculator.Estimate(4,100,1,null,2)==70&&CaffeineCalculator.Estimate(4,400,1,null,2)==70,"Instant coffee caffeine depends on powder rather than added water");ok(CaffeineCalculator.Estimate(4,200,2,null,3)==210,"Instant powder and serving counts scale caffeine correctly");ok(CaffeineCalculator.Estimate(4,200,2,55,2)==110,"User supplied caffeine overrides the instant estimate");var p=Preferences.Defaults();var day=new DateTime(2026,10,27);var next=day.AddDays(1);var r=p.Reminders.First(x=>x.Id=="morning");
   ok(Tracking.CanEdit(day,next.AddHours(11).AddMinutes(59))&&Tracking.CanEdit(day,next.AddHours(12))&&!Tracking.CanEdit(day,next.AddHours(12).AddTicks(1)),"Previous-day edits close exactly after noon");
   Tracking.SetDone(p,r,true,day,next.AddHours(11));ok(Tracking.Done(p,r,day)&&!Tracking.Done(p,r,next),"Backdated completion belongs to the selected day");
   Tracking.SetDone(p,r,false,day,next.AddHours(11));ok(!Tracking.Done(p,r,day)&&p.TaskLogs.Count==1,"Unchecking updates the same historical record");
   bool blocked=false;try{Tracking.SetDone(p,r,true,day,next.AddHours(13));}catch(InvalidOperationException){blocked=true;}ok(blocked,"Late historical edits are rejected in the data layer");
   p.Plans.Clear();p.TaskLogs.Clear();Tracking.SyncPlan(p,day);var prompts=Tracking.Prompts(p,day.AddHours(12));ok(prompts.Count(x=>x.Page=="morning")==1&&!Tracking.Prompts(p,day.AddHours(13)).Any(x=>x.Page=="morning"),"Missing morning entry prompts once after noon");
   ok(Tracking.Prompts(p,next.AddHours(10)).Any(x=>x.Page=="evening"&&x.Date==day),"Missing evening entry prompts next day at ten");
   Tracking.AddWater(p,day.AddHours(9),250);Tracking.AddWater(p,day.AddHours(10),300);p.Drinks.Add(new DrinkEntry{Date=Tracking.Key(day),Type="coffee",Name="Filtre",Ml=200,CaffeineMg=90});p.Drinks.Add(new DrinkEntry{Date=Tracking.Key(day),Type="coffee",Name="Bilinmeyen",Ml=60,CaffeineMg=null});ok(Tracking.Water(p,day)==550&&Tracking.Caffeine(p,day)==90&&Tracking.UnknownCaffeine(p,day)==1,"Water and caffeine totals preserve unknown amounts");
   var a=new Reminder{Id="a",Group="sport",Order=0};var b=new Reminder{Id="b",Group="sport",Order=10};var c=new Reminder{Id="c",Group="sport",Order=20};p.Reminders.AddRange(new[]{a,b,c});TaskOrder.Move(p,"a","c",true);ok(p.Reminders.Where(x=>x.Group=="sport").OrderBy(x=>x.Order).Last().Id=="a","Dragging down persists the new task order");
   var clean=Preferences.EmptyProfile();
   ok(clean.Reminders.Count==0&&clean.Plans.Count==0&&clean.TaskLogs.Count==0&&clean.RoutineRecords.Count==0&&clean.Shopping.Count==0&&clean.Drinks.Count==0&&clean.Products.Count==0&&clean.Measurements.All(m=>!m.HasValues&&!m.MeasurementDay.HasValue&&string.IsNullOrEmpty(m.Notes)),"A fresh profile has no personal tasks or recorded data");
  }
 }
}
