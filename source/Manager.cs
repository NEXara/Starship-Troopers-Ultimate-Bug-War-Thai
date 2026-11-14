using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Reflection;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

public sealed class Policy {
 public string Build="22832378", PakHash="a031d3d1e0845dcac1e336a7221cd37024349e112381cbc37bf1997827613765", TocHash="24a0e7e736f6709a4c9112616ea62b4879fdebd0aba012c7f09d1fa8816d843e";
 public Dictionary<string,byte[]> Payload=new Dictionary<string,byte[]>();
 internal string FixtureRoot;
 public string LegacyHash="a52f62be1f289f5452388d77fef2a25d264702347d87e42e139ef7e0be75ad2d";
 public static Policy Public(){var p=new Policy();foreach(var ext in new[]{"pak","utoc","ucas"})p.Payload["UBW-Thai-UI-pilot_P."+ext]=Resources.Bytes("payload."+ext);return p;}
}
public sealed class Receipt {public string Root,Version; public Dictionary<string,string> Files=new Dictionary<string,string>();}
public sealed class Journal {public string Root,Phase;public Dictionary<string,string> Before=new Dictionary<string,string>();public string ReceiptBefore;}
public static class Resources {
 public static byte[] Bytes(string name){using(var s=Assembly.GetExecutingAssembly().GetManifestResourceStream(name)){if(s==null)throw new Exception("Missing resource: "+name);using(var m=new MemoryStream()){s.CopyTo(m);return m.ToArray();}}}
 public static Image AppImage(){using(var stream=new MemoryStream(Bytes("TH.png")))using(var image=Image.FromStream(stream))return new Bitmap(image);}
 public static Icon AppIcon(){using(var stream=new MemoryStream(Bytes("TH.ico")))using(var icon=new Icon(stream))return (Icon)icon.Clone();}
 public static string Text(string name){return Encoding.UTF8.GetString(Bytes(name)).TrimStart('\uFEFF');}
}
public sealed class Engine {
 const string ReceiptName="UBWThai-install.json",JournalName="UBWThai-pending.json";
 readonly Policy policy;readonly string root,mods,paks;readonly JavaScriptSerializer json=new JavaScriptSerializer();
 internal Action<int> Fault;
 public Engine(string game,Policy p){policy=p;root=Path.GetFullPath(game).TrimEnd(Path.DirectorySeparatorChar);paks=Path.Combine(root,@"StarshipTroopers_UBW\Content\Paks");mods=Path.Combine(paks,"~mods");SafePath(root);SafePath(paks);SafePath(mods);}
 public static string Hash(byte[] data){using(var h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(data)).Replace("-","").ToLowerInvariant();}
 public static string HashFile(string p){using(var h=SHA256.Create())using(var s=File.OpenRead(p))return BitConverter.ToString(h.ComputeHash(s)).Replace("-","").ToLowerInvariant();}
 static void SafePath(string path){for(var d=new DirectoryInfo(path);d!=null;d=d.Parent)if(d.Exists&&(d.Attributes&FileAttributes.ReparsePoint)!=0)throw new Exception("โฟลเดอร์นี้เชื่อมไปยังตำแหน่งอื่น กรุณาเลือกโฟลเดอร์เกมจริง");if(File.Exists(path)&&(File.GetAttributes(path)&FileAttributes.ReparsePoint)!=0)throw new Exception("ไฟล์เชื่อมไปยังตำแหน่งอื่น: "+path);}
 string At(string name){if(Path.GetFileName(name)!=name)throw new Exception("Invalid filename");var p=Path.Combine(mods,name);SafePath(p);return p;}
 bool Trusted(string name,string hash){byte[] bytes;return policy.Payload.TryGetValue(name,out bytes)&&(hash==Hash(bytes)||(name.EndsWith(".pak")&&(hash=="0ab12632b99426be01e60519aaba264c37f28ccf5bb633708099dceac3141561"||hash=="2c360ae0d04a391f0bb4a2ec660fe5659ac1165b804baa738d69b5628f115a60"||hash=="ad037cdbf281909f1276d86c9dbb8b4eb95f839a98aa3434c9d6df7ab4de32d1"||hash=="c68255d0a0c7632d0ee3d44d37bcfc705f5997a6234eca2096fae72a5a8f742d"||hash=="8bdd16808e95063d156c4083a739420c5f8eec977ba6c7afa8f10e499affe898"||hash=="8010e90511d7a3a515a34024138cbb69753084c4b8cdc7f076807fb658708167"||hash==policy.LegacyHash||hash=="ab02b517296203aa3ad3f95875a09a6262baa85285bbb2e51cde91fd6de5e493")));}
 void Closed(){if(policy.FixtureRoot==root&&root.StartsWith(Path.GetFullPath(Path.GetTempPath()),StringComparison.OrdinalIgnoreCase)&&root.Contains("UBWThai-fixture-"))return;foreach(var p in Process.GetProcesses()){using(p){try{var n=p.ProcessName;if(n.Equals("StarshipTroopers_UBW",StringComparison.OrdinalIgnoreCase)||n.Equals("StarshipTroopers_UBW-Win64-Shipping",StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("กรุณาปิดเกมก่อนติดตั้งหรือคืนภาษาเดิม");}catch(System.ComponentModel.Win32Exception){}}}}
 public string Check(bool build){
  if(!Directory.Exists(paks)||!File.Exists(Path.Combine(root,"StarshipTroopers_UBW.exe")))throw new Exception("ไม่พบเกมในโฟลเดอร์ที่เลือก");
  if(build){var manifest=Path.GetFullPath(Path.Combine(root,@"..\..\appmanifest_2321780.acf"));if(!File.Exists(manifest))throw new Exception("ไม่พบข้อมูลติดตั้ง Steam ของเกมนี้");var text=File.ReadAllText(manifest);if(!System.Text.RegularExpressions.Regex.IsMatch(text,"\"appid\"\\s+\"2321780\"")||!System.Text.RegularExpressions.Regex.IsMatch(text,"\"buildid\"\\s+\""+policy.Build+"\""))throw new Exception("เวอร์ชันเกมเปลี่ยนไป กรุณารอชุดแปลที่รองรับเวอร์ชันนี้");foreach(var pair in new[]{new[]{"pakchunk0-Windows.pak",policy.PakHash},new[]{"pakchunk0-Windows.utoc",policy.TocHash}}){var f=Path.Combine(paks,pair[0]);SafePath(f);if(!File.Exists(f)||HashFile(f)!=pair[1])throw new Exception("ไฟล์เกมไม่ตรงกับรุ่นที่รองรับ: "+pair[0]);}}
  foreach(var name in policy.Payload.Keys){if(File.Exists(Path.Combine(paks,name)))throw new Exception("พบชุดแปลเก่าอยู่นอกโฟลเดอร์ ~mods กรุณาตรวจสอบก่อน");var f=At(name);if(File.Exists(f)&&!Trusted(name,HashFile(f)))throw new Exception("พบไฟล์ชื่อเดียวกันที่ไม่ตรงกับชุดแปลนี้ จึงไม่เขียนทับ: "+name);}
  var receipt=At(ReceiptName);if(File.Exists(receipt)){var r=json.Deserialize<Receipt>(File.ReadAllText(receipt));if(r==null||r.Root!=root||r.Files==null||r.Files.Count!=3||r.Files.Any(x=>!Trusted(x.Key,x.Value)))throw new Exception("ข้อมูลการติดตั้งไม่ตรงกับเกมนี้");}
  return File.Exists(At("UBW-Thai-UI-pilot_P.pak"))?"พบม็อดภาษาไทย — พร้อมอัปเดตหรือคืนภาษาเดิม":"พร้อมติดตั้งภาษาไทย";
 }
 void Save(string name,object value){var target=At(name);var stage=At(name+".writing");if(File.Exists(stage))throw new Exception("พบงานเขียนค้าง: "+name);File.WriteAllText(stage,json.Serialize(value),new UTF8Encoding(false));if(File.Exists(target))File.Replace(stage,target,null);else File.Move(stage,target);}
 void Clean(string name,string expected){var f=At(name);if(!File.Exists(f))return;if(HashFile(f)!=expected)throw new Exception("ไฟล์เปลี่ยนแปลงระหว่างดำเนินการ: "+name);File.Delete(f);}
 public void Recover(){
  var jp=At(JournalName);if(!File.Exists(jp))return;var j=json.Deserialize<Journal>(File.ReadAllText(jp));if(j==null||j.Root!=root||j.Before==null||j.Before.Count!=policy.Payload.Count||j.Before.Any(x=>!policy.Payload.ContainsKey(x.Key)||(x.Value!=null&&!Trusted(x.Key,x.Value)))||!new[]{"pending","committed"}.Contains(j.Phase))throw new Exception("ข้อมูลกู้งานไม่ถูกต้อง กรุณาเก็บไฟล์ไว้เพื่อตรวจสอบ");
  if(j.ReceiptBefore!=null){var prior=json.Deserialize<Receipt>(j.ReceiptBefore);if(prior==null||prior.Root!=root||prior.Files==null||prior.Files.Count!=3||prior.Files.Any(x=>!Trusted(x.Key,x.Value)))throw new Exception("ข้อมูลคืนการติดตั้งไม่ถูกต้อง");}
  // Validate every file before touching any file in recovery.
  foreach(var pair in j.Before){var f=At(pair.Key);var bak=At(pair.Key+".ubwthai-backup");var stage=At(pair.Key+".ubwthai-stage");if(File.Exists(stage)&&HashFile(stage)!=Hash(policy.Payload[pair.Key]))throw new Exception("ไฟล์เตรียมติดตั้งเปลี่ยนแปลง");if(File.Exists(f)&&!Trusted(pair.Key,HashFile(f)))throw new Exception("ไฟล์ติดตั้งถูกแก้ไข ไม่สามารถกู้งานอัตโนมัติได้");if(File.Exists(bak)&&(pair.Value==null||HashFile(bak)!=pair.Value))throw new Exception("สำเนากู้งานไม่ตรงกัน");if(j.Phase=="pending"&&pair.Value!=null&&!File.Exists(bak)&&(!File.Exists(f)||HashFile(f)!=pair.Value))throw new Exception("ไม่พบไฟล์เดิมสำหรับกู้งาน");}
  foreach(var pair in j.Before){var f=At(pair.Key);var bak=At(pair.Key+".ubwthai-backup");var stage=At(pair.Key+".ubwthai-stage");if(File.Exists(stage)){if(HashFile(stage)!=Hash(policy.Payload[pair.Key]))throw new Exception("ไฟล์เตรียมติดตั้งเปลี่ยนแปลง");File.Delete(stage);}if(j.Phase=="pending"){if(File.Exists(bak)){if(File.Exists(f))File.Delete(f);File.Move(bak,f);}else if(pair.Value==null&&File.Exists(f))File.Delete(f);}else if(File.Exists(bak))File.Delete(bak);}
  if(j.Phase=="pending"){var rp=At(ReceiptName);if(j.ReceiptBefore==null){if(File.Exists(rp))File.Delete(rp);}else {var previous=json.Deserialize<Receipt>(j.ReceiptBefore);if(previous==null||previous.Root!=root||previous.Files.Any(x=>!Trusted(x.Key,x.Value)))throw new Exception("ข้อมูลคืนการติดตั้งไม่ถูกต้อง");Save(ReceiptName,previous);}}
  File.Delete(jp);
 }
 public string Change(bool remove){using(var mutex=new System.Threading.Mutex(false,"Local\\UBWThai_"+Hash(Encoding.UTF8.GetBytes(root.ToUpperInvariant())))){bool acquired=false;try{try{acquired=mutex.WaitOne(0);}catch(System.Threading.AbandonedMutexException){acquired=true;}if(!acquired)throw new Exception("มีตัวติดตั้งอื่นกำลังทำงานกับเกมนี้อยู่");return ChangeCore(remove);}finally{if(acquired)mutex.ReleaseMutex();}}}
 string ChangeCore(bool remove){
  Closed();Recover();Check(!remove);Directory.CreateDirectory(mods);
  var before=new Dictionary<string,string>();foreach(var name in policy.Payload.Keys){before[name]=File.Exists(At(name))?HashFile(At(name)):null;if(File.Exists(At(name+".ubwthai-backup"))||File.Exists(At(name+".ubwthai-stage")))throw new Exception("พบไฟล์ทำงานค้าง กรุณาตรวจสอบก่อน");}
  if(remove&&before.Values.All(x=>x==null))return "ไม่มีม็อดภาษาไทยให้ถอนออก";
  // An exact pinned 0.7.0 package is an explicitly supported migration baseline.
  if(remove&&!File.Exists(At(ReceiptName))&&before["UBW-Thai-UI-pilot_P.pak"]!=policy.LegacyHash)throw new Exception("ไม่พบข้อมูลยืนยันการติดตั้ง จึงไม่ลบไฟล์");
  var j=new Journal{Root=root,Phase="pending",Before=before,ReceiptBefore=File.Exists(At(ReceiptName))?File.ReadAllText(At(ReceiptName)):null};Save(JournalName,j);
  try{
   if(!remove)foreach(var pair in policy.Payload){var stage=At(pair.Key+".ubwthai-stage");File.WriteAllBytes(stage,pair.Value);if(HashFile(stage)!=Hash(pair.Value))throw new Exception("ตรวจไฟล์เตรียมติดตั้งไม่ผ่าน");}
   int count=0;foreach(var pair in policy.Payload){var f=At(pair.Key);if(File.Exists(f))File.Move(f,At(pair.Key+".ubwthai-backup"));if(!remove)File.Move(At(pair.Key+".ubwthai-stage"),f);if(Fault!=null)Fault(++count);}
   if(remove){if(File.Exists(At(ReceiptName)))File.Delete(At(ReceiptName));}else Save(ReceiptName,new Receipt{Root=root,Version="1.0.2",Files=policy.Payload.ToDictionary(x=>x.Key,x=>Hash(x.Value))});
   j.Phase="committed";Save(JournalName,j);Recover();
  }catch{Recover();throw;}
  return remove?"คืนภาษาเดิมแล้ว — เปิดเกมใหม่ได้เลย":"ติดตั้งภาษาไทยแล้ว — อย่าลืมตั้งภาษาในเกมเป็น English (ENG) แล้วเปิดเกมใหม่";
 }
}
public sealed class MainForm:Form {
 TextBox game,details;Label status;Button install,restore,browse;Policy policy=Policy.Public();bool busy;
 public MainForm(){Text="Starship Troopers: Ultimate Bug War! - Mod แปลไทย • Setup 1.0.2";ClientSize=new Size(670,590);MinimumSize=new Size(690,630);StartPosition=FormStartPosition.CenterScreen;Font=new Font("Segoe UI",10);BackColor=Color.FromArgb(246,247,249);AutoScaleMode=AutoScaleMode.Dpi;Icon=Resources.AppIcon();
  var layout=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(24),ColumnCount=1,RowCount=10};layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));Controls.Add(layout);
  var header=new FlowLayoutPanel{AutoSize=true,Dock=DockStyle.Top,WrapContents=false};header.Controls.Add(new PictureBox{Image=Resources.AppImage(),Size=new Size(34,34),SizeMode=PictureBoxSizeMode.Zoom,Margin=new Padding(0,0,10,0)});header.Controls.Add(new Label{Text="Starship Troopers - Mod แปลไทย",AutoSize=true,Font=new Font("Segoe UI",18,FontStyle.Bold),Margin=new Padding(0,3,0,0)});layout.Controls.Add(header);
  layout.Controls.Add(new Label{Text="Ultimate Bug War! • Setup 1.0.2 • คงชื่อเฉพาะเป็นอังกฤษ",AutoSize=true,Margin=new Padding(3,8,3,16)});
  var path=new TableLayoutPanel{Dock=DockStyle.Top,ColumnCount=2,Height=36};path.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));path.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,94));game=new TextBox{Dock=DockStyle.Fill,Text=Detect()};browse=new Button{Text="เลือกเกม…",Dock=DockStyle.Fill};browse.Click+=(s,e)=>{using(var d=new FolderBrowserDialog{Description="เลือกโฟลเดอร์ StarshipTroopers_UltimateBugWar",SelectedPath=game.Text})if(d.ShowDialog(this)==DialogResult.OK){game.Text=d.SelectedPath;Inspect();}};game.Leave+=(s,e)=>{if(!busy)Inspect();};path.Controls.Add(game);path.Controls.Add(browse);layout.Controls.Add(path);
  layout.Controls.Add(new Label{Text="ตัวอย่างโฟลเดอร์เกม:\r\nN:\\SteamLibrary\\steamapps\\common\\StarshipTroopers_UltimateBugWar\r\nเลือกโฟลเดอร์นี้ ไม่ต้องเลือกไฟล์ .exe (ไดรฟ์และตำแหน่งอาจต่างกัน)",AutoSize=true,ForeColor=Color.DimGray,Font=new Font("Segoe UI",8.5f),Margin=new Padding(3,3,3,8)});
  status=new Label{AutoSize=true,MaximumSize=new Size(610,0),Padding=new Padding(0,8,0,8)};layout.Controls.Add(status);
  layout.Controls.Add(new Label{Text="✓ เนื้อเรื่อง คำอธิบายภารกิจ และคำเตือน\r\n✓ เป้าหมายภารกิจและข้อความช่วยเล่น\r\n✓ บทพูดและซับไตเติล\r\n\r\n6,642 คีย์ • ใช้กับ Steam build 22832378\r\nอย่าลืมตั้งภาษาในเกมเป็น English (ENG)\r\nRelease 1.0.2 • 15 พฤศจิกายน 2569 (15/11/2026)",AutoSize=true,MaximumSize=new Size(610,0),Margin=new Padding(3,4,3,18)});
  install=new Button{Text="ติดตั้ง / อัปเดตภาษาไทย",Dock=DockStyle.Top,Height=44,BackColor=Color.FromArgb(28,97,192),ForeColor=Color.White,FlatStyle=FlatStyle.Flat};install.Click+=async(s,e)=>await Run(false);layout.Controls.Add(install);
  var footer=new FlowLayoutPanel{AutoSize=true,Dock=DockStyle.Top};var toggle=new Button{Text="รายละเอียด ▾",AutoSize=true,FlatStyle=FlatStyle.Flat};toggle.FlatAppearance.BorderSize=0;var info=new Button{Text="ⓘ Info / เครดิต",AutoSize=true,FlatStyle=FlatStyle.Flat,ForeColor=Color.FromArgb(28,97,192)};info.FlatAppearance.BorderSize=0;info.Click+=(s,e)=>{using(var f=new InfoForm())f.ShowDialog(this);};restore=new Button{Text="คืนภาษาเดิม…",AutoSize=true,FlatStyle=FlatStyle.Flat,ForeColor=Color.FromArgb(28,97,192)};restore.FlatAppearance.BorderSize=0;restore.Click+=async(s,e)=>await Run(true);toggle.Click+=(s,e)=>{details.Visible=!details.Visible;toggle.Text=details.Visible?"รายละเอียด ▴":"รายละเอียด ▾";};footer.Controls.AddRange(new Control[]{toggle,info,restore});layout.Controls.Add(footer);
  details=new TextBox{Multiline=true,ReadOnly=true,ScrollBars=ScrollBars.Vertical,Dock=DockStyle.Fill,BackColor=Color.White,Visible=false};layout.Controls.Add(details);
  layout.Controls.Add(new Label{Text="Made with AI by NEXara (ใช้ AI แปลทั้งหมดเพราะงั้นไม่รับประกันคุณภาพการแปล)",AutoSize=true,MaximumSize=new Size(610,0),ForeColor=Color.DimGray,Font=new Font("Segoe UI",8),Margin=new Padding(3,8,3,0)});
  for(int i=0;i<8;i++)layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));layout.RowStyles.Add(new RowStyle(SizeType.Percent,100));layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));Shown+=(s,e)=>Inspect();
 }
 static string Detect(){var candidates=new List<string>();foreach(var d in DriveInfo.GetDrives().Where(x=>x.IsReady))foreach(var folder in new[]{@"SteamLibrary\steamapps\common\StarshipTroopers_UltimateBugWar",@"Program Files (x86)\Steam\steamapps\common\StarshipTroopers_UltimateBugWar",@"Steam\steamapps\common\StarshipTroopers_UltimateBugWar"})candidates.Add(Path.Combine(d.RootDirectory.FullName,folder));return candidates.FirstOrDefault(x=>File.Exists(Path.Combine(x,"StarshipTroopers_UBW.exe")))??"";}
 void Inspect(){try{status.Text=new Engine(game.Text,policy).Check(true)+"\r\nBuild เกม: 22832378 • รองรับ: 22832378";status.ForeColor=Color.FromArgb(35,115,70);details.Text="ตรวจเวอร์ชันและไฟล์เกมผ่านแล้ว\r\nเลือก English ในเกม\r\nปิดเกมก่อนติดตั้งหรือคืนภาษาเดิม";}catch(Exception e){status.Text="เลือกโฟลเดอร์เกมหรือตรวจสอบรายละเอียด";status.ForeColor=Color.DimGray;details.Text=e.Message;details.Visible=true;}}
 async Task Run(bool remove){busy=true;install.Enabled=restore.Enabled=game.Enabled=browse.Enabled=false;try{var root=game.Text;status.Text=remove?"กำลังคืนภาษาเดิม…":"กำลังติดตั้งภาษาไทย…";var result=await Task.Run(()=>new Engine(root,policy).Change(remove));status.Text=result;details.Text=result;MessageBox.Show(this,result,"เสร็จแล้ว",MessageBoxButtons.OK,MessageBoxIcon.Information);}catch(Exception e){status.Text="ดำเนินการไม่สำเร็จ";details.Text=e.Message;details.Visible=true;MessageBox.Show(this,e.Message,"ตรวจสอบก่อนดำเนินการ",MessageBoxButtons.OK,MessageBoxIcon.Information);}finally{busy=false;install.Enabled=restore.Enabled=game.Enabled=browse.Enabled=true;}}
}
public sealed class InfoForm:Form {
 public InfoForm(){Text="Information • เครดิตและคำขอบคุณ";ClientSize=new Size(700,640);MinimumSize=new Size(716,500);StartPosition=FormStartPosition.CenterParent;Font=new Font("Segoe UI",10);BackColor=Color.FromArgb(246,247,249);Icon=Resources.AppIcon();
 var top=new Label{Text="เครดิตและคำขอบคุณ\r\nขอบคุณผู้สร้างเกม ฟอนต์ และเครื่องมือที่ช่วยให้งานนี้เกิดขึ้นได้",Dock=DockStyle.Top,Height=66,Padding=new Padding(18,12,12,0),BackColor=Color.White};var bottom=new Panel{Dock=DockStyle.Bottom,Height=45,BackColor=Color.White};var close=new Button{Text="ปิด",Anchor=AnchorStyles.Right|AnchorStyles.Bottom,Location=new Point(600,8),Size=new Size(82,29)};bottom.Resize+=(s,e)=>close.Left=bottom.Width-100;close.Click+=(s,e)=>Close();bottom.Controls.Add(close);
 var cards=new FlowLayoutPanel{Dock=DockStyle.Fill,AutoScroll=true,FlowDirection=FlowDirection.TopDown,WrapContents=false,Padding=new Padding(14,10,14,10)};Controls.Add(cards);Controls.Add(bottom);Controls.Add(top);
 Add(cards,"Starship Troopers: Ultimate Bug War!","Auroch Digital • Dotemu / Game Source Entertainment","ผู้พัฒนาและผู้จัดจำหน่ายเกมต้นฉบับ เนื้อเรื่อง ตัวละคร และเครื่องหมายการค้าเป็นของเจ้าของสิทธิ์เดิม ม็อดชุมชนนี้ไม่ได้รับรองโดยผู้สร้างเกม","https://store.steampowered.com/app/2321780/");
 Add(cards,"Mod แปลไทย • Setup 1.0.2","Made with AI by NEXara","ใช้ AI แปลทั้งหมดเพราะงั้นไม่รับประกันคุณภาพการแปล\r\nใช้ AI ช่วยแปลและพัฒนา ผ่านการตรวจโครงสร้างข้อความและทดสอบติดตั้งจำลอง ผู้ใช้ทดสอบใช้งานในเกมแล้ว แต่ไม่ได้ตรวจครบทุกฉากและทุกความละเอียด\r\nแนวทางหน้าตาโปรแกรมต่อเนื่องจากงาน Darktide ของผู้ใช้",null);
 Add(cards,"Orbitron","The Orbitron Project Authors • The League of Moveable Type","ฟอนต์ต้นทางของชุด UBWThai Display เพิ่มอักษรไทยและใช้ชื่อใหม่สำหรับฟอนต์ดัดแปลง • SIL OFL 1.1","https://github.com/theleagueof/orbitron");
 Add(cards,"Oxanium","The Oxanium Project Authors • Severin Meyer","ฟอนต์ต้นทางของชุด UBWThai Tech รักษารูปอักษรและความกว้างภาษาอังกฤษ • SIL OFL 1.1","https://github.com/sevmeyer/oxanium");
 Add(cards,"Noto Sans / Noto Sans Thai","The Noto Project Authors • Monotype Imaging Inc.","ฟอนต์ภาษาอังกฤษและอักษรไทยที่ใช้ผสานใน UBWThai Sans / Display / Tech • SIL OFL 1.1","https://github.com/notofonts/noto-fonts");
 Add(cards,"Noto Thai","The Noto Project Authors","ที่มาฟอนต์ไทยและใบอนุญาต • ประกาศลิขสิทธิ์ของฟอนต์แต่ละรุ่นแนบไว้ครบ","https://github.com/notofonts/thai");
 Add(cards,"repak 0.2.2 / retoc 0.1.5","trumank และผู้ร่วมพัฒนา","เครื่องมือแพ็กและตรวจไฟล์ระหว่างพัฒนา ไม่ได้รวมตัวเครื่องมือในโปรแกรมติดตั้ง • repak: MIT / Apache-2.0 • retoc: MIT","https://github.com/trumank/repak");
 Add(cards,"FontTools 4.55.3","FontTools contributors","ใช้ผสานฟอนต์และตรวจอักษรไทยระหว่างพัฒนา ไม่ต้องติดตั้งเพื่อใช้ม็อด • MIT","https://github.com/fonttools/fonttools");
 Add(cards,"ooz-wasm 2.0.0","Alexander Drozdov • SnosMe/ooz-wasm","ใช้ตรวจข้อมูลระหว่างพัฒนา ไม่ได้แจกตัวเครื่องมือพร้อมม็อด • GPL-3.0-or-later","https://github.com/SnosMe/ooz-wasm");
 var legal=new Button{Text="อ่านข้อมูลฉบับเต็ม / ใบอนุญาตและประกาศลิขสิทธิ์",AutoSize=true,Margin=new Padding(0,5,0,10)};legal.Click+=(s,e)=>ShowLicenses();cards.Controls.Add(legal);
 cards.Resize+=(s,e)=>{foreach(Control c in cards.Controls)if(c is Panel)c.Width=Math.Max(580,cards.ClientSize.Width-48);};
 }
 static void Add(FlowLayoutPanel parent,string title,string author,string description,string url){var card=new Panel{Width=640,AutoSize=false,BackColor=Color.White,Padding=new Padding(14,12,14,12),Margin=new Padding(0,0,0,10)};var content=new TableLayoutPanel{AutoSize=true,Dock=DockStyle.Top,ColumnCount=1};content.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,596));card.Controls.Add(content);content.Controls.Add(new Label{Text=title,AutoSize=true,Font=new Font("Segoe UI",10,FontStyle.Bold),Margin=new Padding(0,0,0,6)});content.Controls.Add(new Label{Text=author,AutoSize=true,MaximumSize=new Size(596,0),ForeColor=Color.FromArgb(28,97,192),Font=new Font("Segoe UI",9),Margin=new Padding(0,0,0,8)});content.Controls.Add(new Label{Text=description,AutoSize=true,MaximumSize=new Size(596,0),Font=new Font("Segoe UI",9),ForeColor=Color.FromArgb(65,70,80),Margin=new Padding(0,0,0,8)});if(url!=null){var link=new LinkLabel{Text="เปิดแหล่งต้นฉบับ",AutoSize=true,Margin=new Padding(0)};link.LinkClicked+=(s,e)=>{try{Process.Start(new ProcessStartInfo(url){UseShellExecute=true});}catch(Exception ex){MessageBox.Show(ex.Message);}};content.Controls.Add(link);}card.Height=content.GetPreferredSize(new Size(612,0)).Height+24;parent.Controls.Add(card);}
 void ShowLicenses(){using(var f=new Form{Text="ข้อมูลและใบอนุญาตฉบับเต็ม",Size=new Size(740,650),StartPosition=FormStartPosition.CenterParent,Icon=Resources.AppIcon()}){var tabs=new TabControl{Dock=DockStyle.Fill,Multiline=true};f.Controls.Add(tabs);foreach(var name in new[]{"Credits","FONT-NOTICES","Orbitron-OFL","Oxanium-OFL","Noto-OFL","NotoThai-OFL"}){var page=new TabPage(name);page.Controls.Add(new TextBox{Multiline=true,ReadOnly=true,ScrollBars=ScrollBars.Both,Dock=DockStyle.Fill,WordWrap=true,Font=new Font("Segoe UI",10),Text=Resources.Text(name).Replace("\r\n","\n").Replace("\n","\r\n"),BackColor=Color.White});tabs.TabPages.Add(page);}f.ShowDialog(this);}}
}
static class Program {
 [STAThread] static int Main(string[] args){if(args.SequenceEqual(new[]{"--self-test"}))return SelfTests.Run();Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);if(args.Length==2&&(args[0]=="--preview"||args[0]=="--preview-info")){using(Form f=args[0]=="--preview-info"?(Form)new InfoForm():new MainForm()){f.Opacity=0;f.ShowInTaskbar=false;f.Show();Application.DoEvents();using(var b=new Bitmap(f.Width,f.Height)){f.DrawToBitmap(b,new Rectangle(Point.Empty,f.Size));b.Save(args[1]);}}return 0;}Application.Run(new MainForm());return 0;}
}
