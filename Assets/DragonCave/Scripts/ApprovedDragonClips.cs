using UnityEngine;
namespace DragonCave
{
    public class ApprovedDragonClips : MonoBehaviour
    {
        public Sprite[] jump,cloud,nasal,sleep,sourcePoses;
        public Sprite[] walk,down,wake,sleepResponse;
        public Texture2D[] walkMasks,downMasks,wakeMasks,sleepResponseMasks;
        public Material sourceMatteMaterial;
        public Texture2D activeMask;
        public Material cloudMaterial;
        public string playingClip;
        public int playingFrame;
        public int framesShown;
        SpriteRenderer image;
        Material originalMaterial;
        float width;
        int nativeFacing=-1;
        MaterialPropertyBlock maskProperties;
        void Awake(){image=GetComponent<CaveDragon>().dragon;originalMaterial=image.sharedMaterial;}
        public void Show(string clip,int frame)
        {
            if(!image)image=GetComponent<CaveDragon>().dragon;
            Sprite[] sprites=clip=="Walk"?walk:clip=="Down"?down:clip=="Wake"?wake:clip=="SleepResponse"?sleepResponse:clip=="Jump"?jump:clip=="Cloud"?cloud:clip=="Nasal"?nasal:clip=="Sleep"?sleep:sourcePoses;
            frame=Mathf.Clamp(frame,0,sprites.Length-1);
            playingClip=clip;playingFrame=frame;framesShown++;image.sprite=sprites[frame];
            if(originalMaterial)image.sharedMaterial=clip=="Cloud"&&cloudMaterial?cloudMaterial:originalMaterial;
            Texture2D[] masks=clip=="Walk"?walkMasks:clip=="Down"?downMasks:clip=="Wake"?wakeMasks:clip=="SleepResponse"?sleepResponseMasks:null;
            activeMask=masks!=null&&frame<masks.Length?masks[frame]:null;
            if(activeMask&&sourceMatteMaterial){image.sharedMaterial=sourceMatteMaterial;if(maskProperties==null)maskProperties=new MaterialPropertyBlock();image.GetPropertyBlock(maskProperties);maskProperties.SetTexture("_AlphaMask",activeMask);image.SetPropertyBlock(maskProperties);}
            width=clip=="Down"?6.3f:clip=="Cloud"?9.6f:clip=="Jump"?8f:clip=="Sleep"||clip=="SleepResponse"?4.6f:4.9f;
            nativeFacing=clip=="Jump"||clip=="Down"||(clip=="Pose"&&frame==2)?1:-1;
        }
        public void Pose(int pose)
        {
            if(pose<=3)Show("Pose",0);
            else if(pose==4)Show("Nasal",0);
            else if(pose==5)Show("Nasal",5);
            else if(pose==6)Show("Cloud",4);
            else if(pose==7)Show("Cloud",5);
            else if(pose==8)Show("Jump",6);
            else if(pose==9)Show("Jump",9);
            else if(pose==10)Show("Pose",2);
            else if(pose==11)Show("Jump",13);
            else if(pose==12||pose==13)Show("Sleep",0);
            else Show("Pose",1);
        }
        public void Fit(float depth,int facing)
        {
            if(!image||!image.sprite)return;
            float scale=width*depth/image.sprite.bounds.size.x;
            image.transform.localScale=new Vector3(scale*(facing==nativeFacing?1:-1),scale,1);
        }
    }
}
