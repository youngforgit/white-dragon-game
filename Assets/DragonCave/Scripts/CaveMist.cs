using UnityEngine;
namespace DragonCave
{
    public class CaveMist : MonoBehaviour
    {
        SpriteRenderer sprite;float age,life,depth,dir;bool nasal;Vector3 origin;float seed;
        public void Setup(SpriteRenderer renderer,bool isNasal,float scale,float facing,int index)
        {
            sprite=renderer;nasal=isNasal;depth=scale;dir=facing;seed=index*2.399f;life=nasal?.55f:5.1f;
            origin=transform.position;transform.localScale=Vector3.one*.04f;
        }
        void Update()
        {
            age+=Time.deltaTime;float t=age/life;
            if(t>=1){Destroy(gameObject);return;}
            float size=nasal?Mathf.Lerp(.035f,.19f,t)*depth:Mathf.Lerp(.22f,2.8f,Mathf.Clamp01(age/1.8f))*depth;
            transform.localScale=new Vector3(size*(nasal?1.5f:1.3f),size,1);
            float travel=nasal?t*.42f:Mathf.Min(age,2)*.85f;
            Vector3 spread=nasal?Vector3.zero:new Vector3(Mathf.Cos(seed)*Mathf.Min(age,1.6f)*.65f,Mathf.Sin(seed)*Mathf.Min(age,1.6f)*.65f,0)*depth;
            transform.position=origin+new Vector3(dir*travel*depth,age*(nasal?.08f:.26f)*depth,0)+spread;
            float opacity=nasal?Mathf.Sin(t*Mathf.PI)*.24f:Mathf.Clamp01(age/.3f)*Mathf.Clamp01((life-age)/2.6f)*.86f;
            sprite.color=new Color(.9f,.94f,.98f,opacity);
        }
    }
}
