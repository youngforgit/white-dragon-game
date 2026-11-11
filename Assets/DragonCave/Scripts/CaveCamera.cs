using UnityEngine;
namespace DragonCave
{
    [RequireComponent(typeof(Camera))]
    public class CaveCamera : MonoBehaviour
    {
        Camera view;
        void Awake(){view=GetComponent<Camera>();}
        void LateUpdate()
        {
            // Preserve the authored background and all perch coordinates on other window shapes.
            float aspect=(float)Screen.width/Mathf.Max(1,Screen.height),target=16f/9f;
            if(aspect>target){float width=target/aspect;view.rect=new Rect((1-width)*.5f,0,width,1);}
            else{float height=aspect/target;view.rect=new Rect(0,(1-height)*.5f,1,height);}
        }
    }
}
