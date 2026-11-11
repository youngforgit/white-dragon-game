using UnityEngine;
using UnityEditor;
namespace DragonCave.Editor
{
    public static class ApprovedClipImporter
    {
        [System.Serializable] class Frame{public string file;public float pivotX,pivotY;}
        [System.Serializable] class Manifest{public Frame[] frames;}
        public static string Import()
        {
            const string root="Assets/DragonCave/Art/Approved/";
            var manifest=JsonUtility.FromJson<Manifest>(System.IO.File.ReadAllText(root+"manifest.json"));
            foreach(var frame in manifest.frames)
            {
                string path=root+frame.file;
                var i=(TextureImporter)AssetImporter.GetAtPath(path);i.textureType=TextureImporterType.Sprite;i.spriteImportMode=SpriteImportMode.Single;i.spritePixelsPerUnit=100;i.alphaIsTransparency=true;i.isReadable=true;i.textureCompression=TextureImporterCompression.Uncompressed;
                var s=new TextureImporterSettings();i.ReadTextureSettings(s);s.spriteAlignment=(int)SpriteAlignment.Custom;s.spritePivot=new Vector2(frame.pivotX,frame.pivotY);s.spriteMeshType=SpriteMeshType.FullRect;i.SetTextureSettings(s);i.SaveAndReimport();
            }
            Sprite[] Load(string prefix,int count){var a=new Sprite[count];for(int i=0;i<count;i++)a[i]=AssetDatabase.LoadAssetAtPath<Sprite>(root+prefix+i.ToString("00")+".png");return a;}
            var g=Object.FindAnyObjectByType<CaveDragon>();var clips=g.GetComponent<ApprovedDragonClips>();if(!clips)clips=g.gameObject.AddComponent<ApprovedDragonClips>();
            clips.jump=Load("Jump",16);clips.cloud=Load("Cloud",16);clips.nasal=Load("Nasal",12);clips.sleep=Load("Sleep",12);clips.sourcePoses=Load("Pose",4);g.approved=clips;
            clips.walk=Load("Walk",8);clips.down=Load("Down",16);clips.wake=Load("Wake",12);clips.sleepResponse=Load("SleepResponse",8);
            Texture2D[] LoadMasks(string prefix,int count){var a=new Texture2D[count];for(int n=0;n<count;n++){string path=root+"Masks/"+prefix+n.ToString("00")+".png";var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.isReadable=true;importer.alphaIsTransparency=true;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.wrapMode=TextureWrapMode.Clamp;importer.SaveAndReimport();a[n]=AssetDatabase.LoadAssetAtPath<Texture2D>(path);}return a;}
            clips.walkMasks=LoadMasks("Walk",8);clips.downMasks=LoadMasks("Down",16);clips.wakeMasks=LoadMasks("Wake",12);clips.sleepResponseMasks=LoadMasks("SleepResponse",8);
            const string sourceMaterialPath="Assets/DragonCave/Art/SourceWithMatte.mat";var sourceMaterial=AssetDatabase.LoadAssetAtPath<Material>(sourceMaterialPath);if(!sourceMaterial){sourceMaterial=new Material(Shader.Find("DragonCave/SourceWithMatte"));AssetDatabase.CreateAsset(sourceMaterial,sourceMaterialPath);}clips.sourceMatteMaterial=sourceMaterial;
            const string matPath="Assets/DragonCave/Art/ApprovedCloud.mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(matPath);if(!material){material=new Material(Shader.Find("DragonCave/ApprovedSprite"));AssetDatabase.CreateAsset(material,matPath);}material.SetFloat("_EdgeFade",.12f);clips.cloudMaterial=material;
            g.dragon.sprite=clips.nasal[0];g.dragon.transform.localScale=Vector3.one*(4.9f*g.depth/clips.nasal[0].bounds.size.x);
            EditorUtility.SetDirty(g);EditorUtility.SetDirty(clips);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());UnityEditor.SceneManagement.EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());AssetDatabase.SaveAssets();
            return "Bound 104 approved frames, including walk8/down16/wake12/sleep-response8 with original source RGB and separate alpha mattes.";
        }
    }
}
