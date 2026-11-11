var g=UnityEngine.Object.FindAnyObjectByType<DragonCave.CaveDragon>();g.autoActivity=false;g.CloseMenu();
async System.Threading.Tasks.Task Wait(System.Func<bool> condition,int limit=15000){var timer=System.Diagnostics.Stopwatch.StartNew();while(!condition()&&timer.ElapsedMilliseconds<limit)await System.Threading.Tasks.Task.Delay(20);if(!condition())throw new System.Exception("Timeout: "+g.state);}
if(g.state==DragonCave.DragonState.Sleeping||g.state==DragonCave.DragonState.SleepResponse)g.Wake();await Wait(()=>g.state==DragonCave.DragonState.Idle);
typeof(DragonCave.CaveDragon).GetField("breathAt",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).SetValue(g,UnityEngine.Time.time+1000);
var report=new System.Collections.Generic.List<string>();
void Check(bool valid,string name){if(!valid)throw new System.Exception(name);report.Add(name);}
float previousScale=UnityEngine.Time.timeScale;UnityEngine.Time.timeScale=.5f;
var camera=g.sceneCamera;var rt=UnityEngine.RenderTexture.GetTemporary(800,450,24);var texture=new UnityEngine.Texture2D(800,450,UnityEngine.TextureFormat.RGB24,false);
void Capture(string clip,int index){string dir="Screenshots/LatestMotions/"+clip;System.IO.Directory.CreateDirectory(dir);var target=camera.targetTexture;var active=UnityEngine.RenderTexture.active;try{camera.targetTexture=rt;camera.Render();UnityEngine.RenderTexture.active=rt;texture.ReadPixels(new UnityEngine.Rect(0,0,800,450),0,0);texture.Apply();System.IO.File.WriteAllBytes(dir+"/frame"+index.ToString("00")+".png",texture.EncodeToPNG());}finally{camera.targetTexture=target;UnityEngine.RenderTexture.active=active;}}
async System.Threading.Tasks.Task Record(string clip,int count,System.Func<bool> ongoing){var seen=new System.Collections.Generic.HashSet<int>();var timer=System.Diagnostics.Stopwatch.StartNew();while(ongoing()&&timer.ElapsedMilliseconds<20000){if(g.approved.playingClip==clip&&seen.Add(g.approved.playingFrame))Capture(clip,g.approved.playingFrame);if(clip=="SleepResponse"&&seen.Count==count)break;await System.Threading.Tasks.Task.Delay(20);}Check(seen.Count==count,clip+" plays all "+count+" approved frames");}
try{
 Check(g.approved.walk.Length==8&&g.approved.down.Length==16&&g.approved.wake.Length==12&&g.approved.sleepResponse.Length==8,"Scene references all four complete approved clips");
 g.currentPerch=0;g.transform.position=g.perches[0].World;g.depth=g.perches[0].depthScale;Check(g.TravelTo(2),"Walk route starts");await Record("Walk",8,()=>g.state==DragonCave.DragonState.Walking);
 Check(g.currentPerch==2,"Walk reaches dry ground destination");
 g.currentPerch=9;g.transform.position=g.perches[9].World;g.depth=g.perches[9].depthScale;Check(g.TravelTo(3),"Downward jump starts");g.ClickDragon();Check(g.pendingClick,"Downward jump queues click until landing");await Record("Down",16,()=>g.state==DragonCave.DragonState.Jumping);
 Check(g.currentPerch==3&&UnityEngine.Vector3.Distance(g.transform.position,g.perches[3].World)<.01f,"Downward jump finishes at authored four-paw landing anchor");Check(UnityEngine.Mathf.Abs(g.depth-g.perches[3].depthScale)<.001f,"Downward jump ends at correct distance scale");
 await Wait(()=>g.state==DragonCave.DragonState.Responding);g.CloseMenu();
 g.currentPerch=5;g.transform.position=g.perches[5].World;g.depth=g.perches[5].depthScale;g.EnterSleep();g.ClickDragon();
 await Record("SleepResponse",8,()=>g.state==DragonCave.DragonState.SleepResponse);
 Check(g.state==DragonCave.DragonState.SleepResponse,"Sleep click does not wake dragon");
 await Wait(()=>g.menu.activeSelf);g.wakeButton.onClick.Invoke();await Record("Wake",12,()=>g.state==DragonCave.DragonState.Waking);
 Check(g.state==DragonCave.DragonState.Idle,"Wake sequence completes before autonomous idle");
 for(int i=0;i<g.perches.Length;i++)foreach(int to in g.perches[i].neighbors)if(!g.CanTraverse(i,to))throw new System.Exception("Unsafe route");report.Add("All waterfall/pool exclusions still valid");
 System.IO.File.WriteAllText("Screenshots/LatestMotions/verification.txt",string.Join("\n",report));
}finally{UnityEngine.Time.timeScale=previousScale;UnityEngine.RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.DestroyImmediate(texture);g.autoActivity=true;}
return report;
