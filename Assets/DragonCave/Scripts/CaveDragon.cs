using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using TMPro;

namespace DragonCave
{
    [System.Serializable] public class CavePerch
    {
        public string name;
        public Vector2 screenPoint;
        [Range(.2f,1.2f)] public float depthScale;
        public int[] neighbors;
        public bool elevated;
        public Vector3 World => new Vector3((screenPoint.x-.5f)*16f,(.5f-screenPoint.y)*9f,0);
    }
    public enum DragonState { Idle, Walking, Jumping, Responding, Sleeping, SleepResponse, Waking, Charging, Exhaling }

    public class CaveDragon : MonoBehaviour
    {
        public SpriteRenderer dragon, shadow;
        public Sprite[] poses;
        public CavePerch[] perches;
        public Camera sceneCamera;
        public GameObject menu;
        public TMP_Text status, menuTitle;
        public UnityEngine.UI.Button cloudButton, wakeButton;
        public RectTransform menuRect;
        public DragonState state = DragonState.Idle;
        public int currentPerch;
        public int sleepingPerch = 5;
        public float depth = 1;
        public bool autoActivity = true;
        public bool pendingClick;
        public int completedJumps;
        public int completedClouds;
        public bool noseEyesClosed;
        public ApprovedDragonClips approved;
        public float currentBellyScale = 1;
        public float FrameTime => Time.time;
        float nextMove, breathAt, phaseStart, walkPhase;
        Coroutine motion, interaction;
        Coroutine sleepResponseMotion;
        int facing = -1;
        bool initialized;
        Sprite mistSprite;
        readonly List<CaveMist> mist = new List<CaveMist>();
        // Separate geometry for the waterfall stream and pool. No navigation edge crosses either.
        static readonly Vector2[] Water = { new Vector2(.255f,.54f),new Vector2(.44f,.554f),new Vector2(.44f,.605f),new Vector2(.295f,.622f),new Vector2(.247f,.595f) };

        void Start()
        {
            cloudButton.onClick.AddListener(ChargeCloud); wakeButton.onClick.AddListener(Wake);
            menu.SetActive(false); currentPerch=0; transform.position=perches[0].World; depth=perches[0].depthScale;
            mistSprite=CreateMist();shadow.sprite=mistSprite;phaseStart=Time.time;nextMove=Time.time+7;breathAt=Time.time+4;
            initialized=true; SetPose(4); UpdateVisual();
        }
        void Update()
        {
            if(!initialized)return;
            if(Mouse.current!=null && Mouse.current.leftButton.wasPressedThisFrame && !(EventSystem.current && EventSystem.current.IsPointerOverGameObject()))
            {
                Vector3 p=sceneCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());
                if(HitDragon(p)) ClickDragon(); else if(menu.activeSelf)CloseMenu();
            }
            bool resting=state==DragonState.Idle||state==DragonState.Sleeping;
            if(resting && Time.time>=breathAt) { StartCoroutine(NasalBreath());breathAt=Time.time+(state==DragonState.Sleeping?5.5f:7.5f); }
            if(autoActivity && !noseEyesClosed && state==DragonState.Idle && !menu.activeSelf && Time.time>=nextMove)
            {
                var neighbors=perches[currentPerch].neighbors;
                if(neighbors.Length>0)TravelTo(neighbors[Random.Range(0,neighbors.Length)]);
                nextMove=Time.time+Random.Range(6f,11f);
            }
            UpdateVisual();
            for(int i=mist.Count-1;i>=0;i--)if(!mist[i])mist.RemoveAt(i);
        }
        public static bool IsWater(Vector2 p)
        {
            if(p.x>.247f && p.x<.285f && p.y>.04f && p.y<.55f)return true;
            bool inside=false;
            for(int i=0,j=Water.Length-1;i<Water.Length;j=i++)
                if((Water[i].y>p.y)!=(Water[j].y>p.y) && p.x<(Water[j].x-Water[i].x)*(p.y-Water[i].y)/(Water[j].y-Water[i].y)+Water[i].x)inside=!inside;
            return inside;
        }
        public static Vector2 ScreenPoint(Vector3 p)=>new Vector2(p.x/16f+.5f,.5f-p.y/9f);
        public bool CanTraverse(int from,int to)
        {
            if(from<0||to<0||from>=perches.Length||to>=perches.Length)return false;
            if(System.Array.IndexOf(perches[from].neighbors,to)<0)return false;
            for(int i=0;i<=80;i++)if(IsWater(Vector2.Lerp(perches[from].screenPoint,perches[to].screenPoint,i/80f)))return false;
            return true;
        }
        public bool TravelTo(int target)
        {
            if(state!=DragonState.Idle||!CanTraverse(currentPerch,target))return false;
            motion=StartCoroutine(Travel(target));return true;
        }
        IEnumerator Travel(int target)
        {
            Vector3 start=transform.position,end=perches[target].World;float fromDepth=depth,toDepth=perches[target].depthScale;
            bool jump=perches[currentPerch].elevated||perches[target].elevated;
            if(jump&&approved){yield return ApprovedJump(target,start,end,fromDepth,toDepth);yield break;}
            facing=end.x<start.x?-1:1;float duration=jump?1.5f:Mathf.Max(1.2f,Vector3.Distance(start,end)/(1.1f*depth));
            state=jump?DragonState.Jumping:DragonState.Walking;float t=0;
            if(jump){SetPose(11);yield return new WaitForSeconds(.3f);}
            while(t<1)
            {
                t=Mathf.Min(1,t+Time.deltaTime/duration);
                Vector3 position=Vector3.Lerp(start,end,t);
                // Depth is interpolated between authored physical planes, independently from jump height.
                depth=Mathf.Lerp(fromDepth,toDepth,t);
                if(jump)position.y+=Mathf.Sin(t*Mathf.PI)*.55f*Mathf.Lerp(fromDepth,toDepth,t);
                if(IsWater(ScreenPoint(position))) { transform.position=start;depth=fromDepth;state=DragonState.Idle;motion=null;yield break; }
                transform.position=position;
                if(jump)SetPose(t>.8f?11:end.y>=start.y?(t<.58f?8:9):10);
                else {walkPhase+=Time.deltaTime/.15f;if(approved)approved.Show("Walk",(int)walkPhase%8);else SetPose((int)walkPhase%4);}
                yield return null;
            }
            transform.position=end;depth=toDepth;currentPerch=target;
            if(jump){completedJumps++;SetPose(11);yield return new WaitForSeconds(.3f);}
            state=DragonState.Idle;SetPose(4);motion=null;nextMove=Time.time+Random.Range(5f,10f);
            if(pendingClick){pendingClick=false;ClickDragon();}
            else if(target==sleepingPerch)EnterSleep();
        }
        IEnumerator ApprovedJump(int target,Vector3 start,Vector3 end,float fromDepth,float toDepth)
        {
            state=DragonState.Jumping;facing=end.x<start.x?-1:1;bool upward=end.y>=start.y;
            float[] progress=upward?new float[]{0,0,0,0,0,.08f,.22f,.38f,.58f,.76f,.88f,.94f,.98f,1,1,1}:new float[]{0,0,0,.04f,.20f,.45f,.72f,1,1,1,1,1,1,1,1,1};
            float[] downTiming={.6f,.14f,.14f,.1f,.07f,.07f,.07f,.07f,.07f,.09f,.1f,.11f,.12f,.13f,.18f,.9f};
            // The approved up-jump has sixteen distinct source frames; do not replace them with four poses.
            for(int i=0;i<16;i++)
            {
                float p=progress[i];depth=Mathf.Lerp(fromDepth,toDepth,p);
                Vector3 position=Vector3.Lerp(start,end,p);position.y+=Mathf.Sin(p*Mathf.PI)*.28f*depth;
                if(IsWater(ScreenPoint(position))){transform.position=start;depth=fromDepth;state=DragonState.Idle;SetPose(4);motion=null;yield break;}
                transform.position=position;
                approved.Show(upward?"Jump":"Down",i);
                approved.Fit(depth,facing);
                yield return new WaitForSeconds(upward?(i==0?.25f:i==15?.3f:.09f):downTiming[i]);
            }
            transform.position=end;depth=toDepth;currentPerch=target;completedJumps++;
            state=DragonState.Idle;SetPose(4);motion=null;nextMove=Time.time+Random.Range(5f,10f);
            if(pendingClick){pendingClick=false;ClickDragon();}else if(target==sleepingPerch)EnterSleep();
        }
        public void ClickDragon()
        {
            if(noseEyesClosed){pendingClick=true;return;}
            if(state==DragonState.Jumping||state==DragonState.Waking){pendingClick=true;return;}
            if(state==DragonState.Charging||state==DragonState.Exhaling)return;
            if(motion!=null){StopCoroutine(motion);motion=null;}
            if(state==DragonState.SleepResponse)return;
            if(state==DragonState.Sleeping){state=DragonState.SleepResponse;if(approved)sleepResponseMotion=StartCoroutine(SleepClickRoutine());else{SetPose(13);StartCoroutine(RevealMenu(true));}return;}
            state=DragonState.Responding;SetPose(14);StartCoroutine(RevealMenu(false));
        }
        IEnumerator RevealMenu(bool sleep){yield return new WaitForSeconds(.45f);if(state==(sleep?DragonState.SleepResponse:DragonState.Responding))ShowMenu(sleep);}
        IEnumerator SleepClickRoutine()
        {
            float[] timing={.9f,.13f,.16f,.35f,.35f,.16f,.16f,1f};
            for(int i=0;i<8;i++){approved.Show("SleepResponse",i);approved.Fit(depth,facing);if(i==3)ShowMenu(true);yield return new WaitForSeconds(timing[i]);}
            sleepResponseMotion=null;
        }
        void ShowMenu(bool sleep)
        {
            menu.SetActive(true);cloudButton.gameObject.SetActive(!sleep);wakeButton.gameObject.SetActive(sleep);
            menuTitle.text=sleep?"牠輕輕睜開了眼睛…":"幼龍正看著你";
            // Anchor in viewport space; it follows depth-scaled dragon instead of a fixed screen corner.
            Vector3 world=transform.position+Vector3.up*1.9f*depth;
            Vector3 v=sceneCamera.WorldToViewportPoint(world);
            menuRect.anchorMin=menuRect.anchorMax=new Vector2(Mathf.Clamp(v.x,.17f,.83f),Mathf.Clamp(v.y,.23f,.78f));menuRect.anchoredPosition=Vector2.zero;
        }
        public void CloseMenu()
        {
            menu.SetActive(false);
            if(state==DragonState.SleepResponse){if(sleepResponseMotion!=null){StopCoroutine(sleepResponseMotion);sleepResponseMotion=null;}state=DragonState.Sleeping;SetPose(12);breathAt=Time.time+2;}
            else if(state==DragonState.Responding){state=DragonState.Idle;SetPose(4);nextMove=Time.time+4;}
        }
        public void EnterSleep()
        {
            if(state!=DragonState.Idle)return;
            state=DragonState.Sleeping;SetPose(12);breathAt=Time.time+1.5f;menu.SetActive(false);
        }
        public void Wake()
        {
            if(state!=DragonState.Sleeping&&state!=DragonState.SleepResponse)return;
            if(sleepResponseMotion!=null){StopCoroutine(sleepResponseMotion);sleepResponseMotion=null;}
            menu.SetActive(false);if(interaction!=null)StopCoroutine(interaction);interaction=StartCoroutine(WakeRoutine());
        }
        IEnumerator WakeRoutine()
        {
            state=DragonState.Waking;
            if(approved){float[] timing={.6f,.16f,.16f,.18f,.18f,.22f,.22f,.22f,.22f,.22f,.25f,.9f};for(int i=0;i<12;i++){approved.Show("Wake",i);approved.Fit(depth,facing);yield return new WaitForSeconds(timing[i]);}}
            else{SetPose(15);yield return new WaitForSeconds(1.4f);}
            state=DragonState.Idle;SetPose(4);nextMove=Time.time+6;interaction=null;
            if(pendingClick){pendingClick=false;ClickDragon();}
        }
        public void ChargeCloud()
        {
            if(state!=DragonState.Responding&&state!=DragonState.Idle)return;
            menu.SetActive(false);interaction=StartCoroutine(CloudRoutine());
        }
        IEnumerator CloudRoutine()
        {
            if(approved)
            {
                float[] timing={.5f,.16f,.22f,.25f,.22f,.09f,.09f,.12f,.16f,.7f,.7f,.6f,.5f,.4f,.25f,.65f};
                for(int i=0;i<16;i++)
                {
                    state=i<5?DragonState.Charging:DragonState.Exhaling;
                    currentBellyScale=i<5?1+i*.125f:1;
                    approved.Show("Cloud",i);approved.Fit(depth,facing);
                    yield return new WaitForSeconds(timing[i]);
                }
                currentBellyScale=1;completedClouds++;state=DragonState.Idle;SetPose(4);nextMove=Time.time+6;interaction=null;yield break;
            }
            state=DragonState.Charging;
            SetPose(4);yield return new WaitForSeconds(.25f);
            SetPose(6);float t=0;
            while(t<1){t=Mathf.Min(1,t+Time.deltaTime/.9f);currentBellyScale=Mathf.Lerp(1,1.15f,t);yield return null;}
            yield return new WaitForSeconds(.45f);
            state=DragonState.Exhaling;SetPose(7);currentBellyScale=1;
            for(int i=0;i<24;i++)
            {
                EmitMist(false,i);yield return new WaitForSeconds(.035f);
            }
            SetPose(4);yield return new WaitForSeconds(4.8f);
            completedClouds++;state=DragonState.Responding;SetPose(14);yield return new WaitForSeconds(.8f);
            state=DragonState.Idle;SetPose(4);nextMove=Time.time+6;interaction=null;
        }
        IEnumerator NasalBreath()
        {
            bool sleep=state==DragonState.Sleeping;
            if(!sleep && state!=DragonState.Idle)yield break;
            if(approved)
            {
                for(int i=0;i<12;i++)
                {
                    if(state!=(sleep?DragonState.Sleeping:DragonState.Idle))break;
                    noseEyesClosed=sleep||(i>=3&&i<=8);
                    approved.Show(sleep?"Sleep":"Nasal",i);approved.Fit(depth,facing);
                    yield return new WaitForSeconds(i>=4&&i<=7?.1f:sleep?.35f:.22f);
                }
                noseEyesClosed=false;
                if(state==(sleep?DragonState.Sleeping:DragonState.Idle))SetPose(sleep?12:4);
                if(pendingClick&&(state==DragonState.Sleeping||state==DragonState.Idle)){pendingClick=false;ClickDragon();}
                yield break;
            }
            noseEyesClosed=true;SetPose(sleep?12:5);
            yield return new WaitForSeconds(.15f);
            // Cancel if an interaction changed the pose during the short inhale.
            if(state!=(sleep?DragonState.Sleeping:DragonState.Idle)){noseEyesClosed=false;yield break;}
            for(int i=0;i<2;i++)EmitMist(true,i);
            yield return new WaitForSeconds(.65f);
            noseEyesClosed=false;
            if(state==(sleep?DragonState.Sleeping:DragonState.Idle))SetPose(sleep?12:4);
            if(pendingClick && (state==DragonState.Sleeping||state==DragonState.Idle)){pendingClick=false;ClickDragon();}
        }
        void SetPose(int i){if(approved){approved.Pose(i);approved.Fit(depth,facing);}else if(poses!=null&&i>=0&&i<poses.Length)dragon.sprite=poses[i];}
        void UpdateVisual()
        {
            float breathe=(state==DragonState.Sleeping||state==DragonState.Idle)?Mathf.Sin((Time.time-phaseStart)*1.5f)*.008f:0;
            if(approved)approved.Fit(depth,facing);
            else dragon.transform.localScale=new Vector3((facing<0?1:-1)*1.51f*depth,1.51f*depth*(currentBellyScale+breathe),1);
            shadow.transform.localScale=new Vector3(depth*1.7f,depth*.22f,1);shadow.color=new Color(0,0,0,state==DragonState.Jumping?.13f:.27f);
            string label=state==DragonState.Sleeping?"安靜熟睡 · 點擊選擇喚醒":state==DragonState.SleepResponse?"選擇喚醒，牠才會起身":state==DragonState.Charging?"深吸一口氣…":state==DragonState.Exhaling?"雲霧正在慢慢散開":state==DragonState.Responding?"幼龍正在回應你":state==DragonState.Jumping?"輕巧地躍過岩棚":"讓牠自在探索 · 點擊幼龍互動";
            status.text=label;
        }
        public bool HitDragon(Vector3 point)
        {
            Vector3 p=dragon.transform.InverseTransformPoint(new Vector3(point.x,point.y,dragon.transform.position.z));
            var s=dragon.sprite;if(!s)return false;var b=s.bounds;
            if(p.x<b.min.x||p.x>b.max.x||p.y<b.min.y||p.y>b.max.y)return false;
            if(approved&&approved.activeMask)return approved.activeMask.GetPixelBilinear((p.x-b.min.x)/b.size.x,(p.y-b.min.y)/b.size.y).a>.2f;
            int x=Mathf.Clamp(Mathf.RoundToInt((p.x-b.min.x)*s.pixelsPerUnit+s.textureRect.x),0,s.texture.width-1);
            int y=Mathf.Clamp(Mathf.RoundToInt((p.y-b.min.y)*s.pixelsPerUnit+s.textureRect.y),0,s.texture.height-1);
            return s.texture.GetPixel(x,y).a>.2f;
        }
        void EmitMist(bool nasal,int index)
        {
            var go=new GameObject(nasal?"Nostril breath":"Cloud veil");var sr=go.AddComponent<SpriteRenderer>();sr.sprite=mistSprite;sr.sortingOrder=nasal?25:40;
            bool sleep=state==DragonState.Sleeping;float dir=facing<0?-1:1;
            Vector3 mouth=transform.position+new Vector3(dir*.94f*depth,(sleep?.26f:1.65f)*depth,0);
            if(!nasal)mouth.y+=.12f*depth;
            go.transform.position=mouth;
            var m=go.AddComponent<CaveMist>();m.Setup(sr,nasal,depth,dir,index);mist.Add(m);
        }
        static Sprite CreateMist()
        {
            int size=128;var t=new Texture2D(size,size,TextureFormat.RGBA32,false);t.name="Soft volume density";
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                float dx=(x-63.5f)/63.5f,dy=(y-63.5f)/63.5f;
                float r=Mathf.Sqrt(dx*dx+dy*dy);float n=Mathf.PerlinNoise(x*.055f,y*.055f);
                float a=Mathf.Pow(Mathf.Clamp01(1-r),1.1f)*Mathf.Lerp(.65f,1,n);
                t.SetPixel(x,y,new Color(.9f,.94f,.98f,a));
            }
            t.Apply();return Sprite.Create(t,new Rect(0,0,size,size),Vector2.one*.5f,64);
        }
        void OnDestroy(){if(mistSprite){Destroy(mistSprite.texture);Destroy(mistSprite);}foreach(var m in mist)if(m)Destroy(m.gameObject);}
        void OnDrawGizmosSelected()
        {
            if(perches==null)return;
            for(int i=0;i<perches.Length;i++){Gizmos.color=Color.cyan;Gizmos.DrawWireSphere(perches[i].World,.08f);foreach(int j in perches[i].neighbors)if(j<perches.Length){Gizmos.color=CanTraverse(i,j)?Color.green:Color.red;Gizmos.DrawLine(perches[i].World,perches[j].World);}}
        }
    }
}
