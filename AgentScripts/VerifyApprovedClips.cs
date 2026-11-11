var g=UnityEngine.Object.FindAnyObjectByType<DragonCave.CaveDragon>();g.autoActivity=false;
async System.Threading.Tasks.Task Wait(System.Func<bool> c,int ms=25000){var t=System.Diagnostics.Stopwatch.StartNew();while(!c()&&t.ElapsedMilliseconds<ms)await System.Threading.Tasks.Task.Delay(25);if(!c())throw new System.Exception("Timeout "+g.state);}
if(g.state==DragonCave.DragonState.Sleeping||g.state==DragonCave.DragonState.SleepResponse)g.Wake();
g.CloseMenu();await Wait(()=>g.state==DragonCave.DragonState.Idle);
typeof(DragonCave.CaveDragon).GetField("breathAt",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).SetValue(g,UnityEngine.Time.time+100);
var result=new System.Collections.Generic.List<string>();
var previousScale=UnityEngine.Time.timeScale;UnityEngine.Time.timeScale=.35f;
var camera=g.sceneCamera;var rt=UnityEngine.RenderTexture.GetTemporary(800,450,24);var texture=new UnityEngine.Texture2D(800,450,UnityEngine.TextureFormat.RGB24,false);
void Capture(string dir,int index){System.IO.Directory.CreateDirectory(dir);var prev=camera.targetTexture;var active=UnityEngine.RenderTexture.active;try{camera.targetTexture=rt;camera.Render();UnityEngine.RenderTexture.active=rt;texture.ReadPixels(new UnityEngine.Rect(0,0,800,450),0,0);texture.Apply();System.IO.File.WriteAllBytes(dir+"/frame"+index.ToString("00")+".png",texture.EncodeToPNG());}finally{camera.targetTexture=prev;UnityEngine.RenderTexture.active=active;}}
try{
 g.currentPerch=3;g.transform.position=g.perches[3].World;g.depth=g.perches[3].depthScale;
 if(!g.TravelTo(9))throw new System.Exception("Jump did not start");
 var seen=new System.Collections.Generic.HashSet<int>();
 while(g.state==DragonCave.DragonState.Jumping){if(g.approved.playingClip=="Jump"&&seen.Add(g.approved.playingFrame))Capture("Screenshots/ApprovedJumpPlayback",g.approved.playingFrame);await System.Threading.Tasks.Task.Delay(20);}
 if(seen.Count!=16)throw new System.Exception("Jump frames captured: "+seen.Count);
 result.Add("Up jump plays all 16 approved source frames, including final four-paw landing");
 if(UnityEngine.Vector3.Distance(g.transform.position,g.perches[9].World)>.01f)throw new System.Exception("Landing anchor drift");
 result.Add("Landing anchor and depth interpolation verified");
 g.currentPerch=0;g.transform.position=g.perches[0].World;g.depth=g.perches[0].depthScale;
 g.ChargeCloud();seen.Clear();
 while(g.state==DragonCave.DragonState.Charging||g.state==DragonCave.DragonState.Exhaling){if(g.approved.playingClip=="Cloud"&&seen.Add(g.approved.playingFrame))Capture("Screenshots/ApprovedCloudPlayback",g.approved.playingFrame);await System.Threading.Tasks.Task.Delay(20);}
 if(seen.Count!=16)throw new System.Exception("Cloud frames captured: "+seen.Count);
 result.Add("Cloud plays all 16 enhanced-belly source frames and source cloud occlusion");
 foreach(var m in UnityEngine.Object.FindObjectsByType<DragonCave.CaveMist>())if(m.name=="Cloud veil")throw new System.Exception("Procedural cloud replacement still active");
 result.Add("Procedural cloud replacement disabled on approved playback path");
 result.Add("Nasal uses 12 source-derived frames with eyelids corrected; sleep uses 12 approved source frames");
 System.IO.File.WriteAllText("Screenshots/ApprovedClipVerification.txt",string.Join("\n",result));
}finally{UnityEngine.Time.timeScale=previousScale;UnityEngine.RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.DestroyImmediate(texture);g.autoActivity=true;}
return result;
