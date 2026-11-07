using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace LucyDesktop {
 // Synthetic records for the self-test and layout checks.
 internal static class RequestedRoutineLists {
  static readonly string[] Evening=Enumerable.Range(1,15).Select(i=>"Örnek akşam görevi "+i).ToArray();
  static readonly string[] Shower=Enumerable.Range(1,6).Select(i=>"Örnek duş görevi "+i).ToArray();
  static readonly string[] SupplementNames=Enumerable.Range(1,8).Select(i=>"Örnek takviye "+i).ToArray();
  public static readonly string[] SupplementAmounts=Enumerable.Range(1,8).Select(i=>i+" birim").ToArray();
  static string Key(string title){return (title??"").Trim().ToLower(CultureInfo.GetCultureInfo("tr-TR"));}
  static void Add(Preferences p,string group,string[] titles,string prefix,string[] quantities=null){
   int order=p.Reminders.Where(r=>r.Group==group).Select(r=>r.Order).DefaultIfEmpty(-10).Max()+10;
   for(int i=0;i<titles.Length;i++){
    string id=prefix+"-"+(i+1).ToString("00");
    if(p.Reminders.Any(r=>r.Id==id||r.Group==group&&Key(r.Title)==Key(titles[i])))continue;
    p.Reminders.Add(new Reminder{Id=id,Title=TitleRules.Save(titles[i]),Quantity=quantities==null?"":quantities[i],Message="",Group=group,Icon=group=="supplements"?"capsule":group=="evening"?"skincare":"water",NoNotification=true,Enabled=true,WeekdaysOnly=false,Time=group=="evening"?"22:00":"09:00",Kind="custom",Order=order});order+=10;
   }
  }
  public static void Apply(Preferences p){
   SectionNames.Initialize(p);
   var shower=p.Sections.FirstOrDefault(s=>s.Parent=="care"&&(s.SectionType=="shower"||Key(s.Name)=="duş rutini"));
   if(shower==null){shower=new UserSection{Id="lucy-shower",Parent="care",Name="Duş rutini",Icon="water",Order=p.Sections.Where(s=>s.Parent=="care").Select(s=>s.Order).DefaultIfEmpty(0).Max()+10};p.Sections.Add(shower);}shower.SectionType="shower";
   var market=p.Sections.FirstOrDefault(s=>s.Parent=="shopping"&&Key(s.Name).Contains("market"));
   if(market==null){market=new UserSection{Id="lucy-market",Parent="shopping",Name="Market alışveriş listesi",Icon="food",Order=0};p.Sections.Add(market);}market.Columns=3;
   Add(p,"evening",Evening,"lucy-evening");Add(p,shower.Id,Shower,"lucy-shower-task");Add(p,"supplements",SupplementNames,"lucy-supplement",SupplementAmounts);
   Tracking.SyncPlan(p,DateTime.Today);
  }
 }
}
