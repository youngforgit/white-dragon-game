using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using UnityEngine.InputSystem.UI;
using UnityEngine.EventSystems;

namespace DragonCave.Editor
{
    public static class CaveSceneBuilder
    {
        const string Root="Assets/DragonCave";
        [System.Serializable] class Pivot {public int index;public float pivotX,pivotY;}
        [System.Serializable] class Pivots {public Pivot[] poses;}
        static TMP_FontAsset font;
        static Sprite rounded;
        static Color ColorOf(string hex){ColorUtility.TryParseHtmlString("#"+hex,out var c);return c;}
        [MenuItem("Dragon Cave/Build Cave Scene")]
        public static string Build()
        {
            if(EditorApplication.isPlaying)throw new System.InvalidOperationException("Stop Play mode first.");
            if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new System.InvalidOperationException("Save current scene first.");
            AssetDatabase.Refresh();
            ImportSprite(Root+"/Art/Cave.png",new Vector2(.5f,.5f),false);
            var pivots=JsonUtility.FromJson<Pivots>(System.IO.File.ReadAllText(Root+"/Art/Dragon/pivots.json"));
            var poses=new Sprite[16];
            foreach(var p in pivots.poses){string path=Root+"/Art/Dragon/Pose"+p.index.ToString("00")+".png";ImportSprite(path,new Vector2(p.pivotX,p.pivotY),true);poses[p.index]=AssetDatabase.LoadAssetAtPath<Sprite>(path);}
            font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Root+"/Fonts/CaveFont.asset");
            if(!font){font=TMP_FontAsset.CreateFontAsset(AssetDatabase.LoadAssetAtPath<Font>(Root+"/Fonts/ChenYuluoyan-2.0-Thin.ttf"),72,8,UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA,2048,2048,AtlasPopulationMode.Dynamic,true);font.name="CaveFont";AssetDatabase.CreateAsset(font,Root+"/Fonts/CaveFont.asset");AssetDatabase.AddObjectToAsset(font.material,font);AssetDatabase.AddObjectToAsset(font.atlasTexture,font);}
            rounded=AssetDatabase.LoadAssetAtPath<Sprite>(Root+"/Art/RoundedCard.png");
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var camera=new GameObject("Main Camera").AddComponent<Camera>();camera.tag="MainCamera";camera.orthographic=true;camera.orthographicSize=4.5f;camera.transform.position=new Vector3(0,0,-10);camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=ColorOf("111B23");camera.gameObject.AddComponent<AudioListener>();camera.gameObject.AddComponent<CaveCamera>();
            var backdrop=new GameObject("Approved cave background").AddComponent<SpriteRenderer>();backdrop.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(Root+"/Art/Cave.png");backdrop.sortingOrder=-100;backdrop.transform.localScale=new Vector3(16/backdrop.sprite.bounds.size.x,9/backdrop.sprite.bounds.size.y,1);
            var dragonRoot=new GameObject("White juvenile dragon");var game=dragonRoot.AddComponent<CaveDragon>();game.sceneCamera=camera;game.poses=poses;
            game.dragon=new GameObject("Dragon sprite").AddComponent<SpriteRenderer>();game.dragon.transform.SetParent(dragonRoot.transform,false);game.dragon.sprite=poses[4];game.dragon.sortingOrder=20;game.dragon.color=new Color(.88f,.93f,.99f);
            game.shadow=new GameObject("Contact shadow").AddComponent<SpriteRenderer>();game.shadow.transform.SetParent(dragonRoot.transform,false);game.shadow.sortingOrder=10;game.shadow.transform.localPosition=new Vector3(0,-.015f,0);
            game.perches=new[]{
                P("前景主岩面",.54f,.84f,.92f,false,1,2,3),P("左側乾燥地面",.32f,.78f,.82f,false,0,3,6),P("右側乾燥地面",.75f,.80f,.90f,false,0,4),
                P("後側乾燥地面",.48f,.66f,.60f,false,0,1,4,9),P("石窩入口",.69f,.68f,.65f,false,2,3,5,9),P("避風石窩",.83f,.60f,.52f,false,4),
                P("左側低岩台",.14f,.73f,.61f,true,1,7),P("左側中岩台",.18f,.59f,.47f,true,6,8),P("左側高岩台",.095f,.49f,.38f,true,7),
                P("中央斷岩棚",.50f,.535f,.43f,true,3,4,10,11),P("後方小岩棚",.36f,.46f,.32f,true,9),P("右側上岩棚",.65f,.33f,.33f,true,9)};
            dragonRoot.transform.position=game.perches[0].World;game.dragon.transform.localScale=Vector3.one*1.51f*.92f;
            var zones=new GameObject("Forbidden waterfall and pool");
            var pool=zones.AddComponent<PolygonCollider2D>();pool.isTrigger=true;pool.points=new[]{W(.255f,.54f),W(.44f,.554f),W(.44f,.605f),W(.295f,.622f),W(.247f,.595f)};
            var stream=new GameObject("Waterfall stream - no entry").AddComponent<BoxCollider2D>();stream.transform.SetParent(zones.transform);stream.isTrigger=true;stream.offset=W(.266f,.295f);stream.size=new Vector2(.038f*16,.51f*9);
            BuildUI(game);
            foreach(var p in game.perches)if(CaveDragon.IsWater(p.screenPoint))throw new System.Exception("Perch inside forbidden water: "+p.name);
            for(int i=0;i<game.perches.Length;i++)foreach(var j in game.perches[i].neighbors)if(!game.CanTraverse(i,j))throw new System.Exception("Unsafe edge: "+i+" -> "+j);
            EditorSceneManager.SaveScene(scene,Root+"/Scenes/DragonCave.unity");EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(Root+"/Scenes/DragonCave.unity",true)};
            AssetDatabase.SaveAssets();if(System.IO.File.Exists(Root+"/Art/Approved/manifest.json"))ApprovedClipImporter.Import();Selection.activeGameObject=dragonRoot;
            return "Cave created: 12 authored perches; water exclusions checked; depth-scale route graph active.";
        }
        static Vector2 W(float x,float y)=>new Vector2((x-.5f)*16,(.5f-y)*9);
        static CavePerch P(string name,float x,float y,float depth,bool high,params int[] neighbors)=>new CavePerch{name=name,screenPoint=new Vector2(x,y),depthScale=depth,elevated=high,neighbors=neighbors};
        static void ImportSprite(string path,Vector2 pivot,bool readable)
        {
            var i=(TextureImporter)AssetImporter.GetAtPath(path);i.textureType=TextureImporterType.Sprite;i.spriteImportMode=SpriteImportMode.Single;i.spritePixelsPerUnit=100;i.alphaIsTransparency=true;i.isReadable=readable;i.textureCompression=TextureImporterCompression.Uncompressed;i.maxTextureSize=2048;
            var settings=new TextureImporterSettings();i.ReadTextureSettings(settings);settings.spriteAlignment=(int)SpriteAlignment.Custom;settings.spritePivot=pivot;settings.spriteMeshType=SpriteMeshType.FullRect;i.SetTextureSettings(settings);i.SaveAndReimport();
        }
        static RectTransform Rect(string name,Transform parent,Vector2 anchor,Vector2 pivot,Vector2 pos,Vector2 size)
        {var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=r.anchorMax=anchor;r.pivot=pivot;r.anchoredPosition=pos;r.sizeDelta=size;return r;}
        static TMP_Text Text(Transform parent,string name,string words,int size,Vector2 pos,Vector2 bounds,TextAlignmentOptions align=TextAlignmentOptions.Left)
        {var r=Rect(name,parent,new Vector2(0,1),new Vector2(0,1),pos,bounds);var t=r.gameObject.AddComponent<TextMeshProUGUI>();t.font=font;t.fontSize=size;t.fontStyle=FontStyles.Bold;t.text=words;t.color=ColorOf("DFE9EB");t.alignment=align;t.raycastTarget=false;return t;}
        static UnityEngine.UI.Button Button(Transform parent,string name,string words,Vector2 pos)
        {var r=Rect(name,parent,new Vector2(.5f,1),new Vector2(.5f,1),pos,new Vector2(205,46));var img=r.gameObject.AddComponent<UnityEngine.UI.Image>();img.color=ColorOf("496771");img.sprite=rounded;img.type=UnityEngine.UI.Image.Type.Sliced;var b=r.gameObject.AddComponent<UnityEngine.UI.Button>();b.targetGraphic=img;Text(r,"Label",words,28,Vector2.zero,new Vector2(205,46),TextAlignmentOptions.Center);return b;}
        static void BuildUI(CaveDragon game)
        {
            var canvas=new GameObject("Cave interface",typeof(Canvas),typeof(UnityEngine.UI.CanvasScaler),typeof(UnityEngine.UI.GraphicRaycaster));canvas.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;var scaler=canvas.GetComponent<UnityEngine.UI.CanvasScaler>();scaler.uiScaleMode=UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1600,900);scaler.matchWidthOrHeight=.5f;
            new GameObject("EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));
            var title=Rect("Title",canvas.transform,new Vector2(0,1),new Vector2(0,1),new Vector2(32,-24),new Vector2(400,90));
            Text(title,"Cave name","棲雲洞",38,Vector2.zero,new Vector2(350,46));Text(title,"Subtitle","幼龍 · 自在生息",23,new Vector2(0,-48),new Vector2(350,32));
            var footer=Rect("Activity",canvas.transform,new Vector2(.5f,0),new Vector2(.5f,0),new Vector2(0,24),new Vector2(800,42));
            game.status=Text(footer,"Activity text","讓牠自在探索 · 點擊幼龍互動",27,Vector2.zero,new Vector2(800,42),TextAlignmentOptions.Center);
            var panel=Rect("Dragon choices",canvas.transform,new Vector2(.5f,.5f),new Vector2(.5f,0),Vector2.zero,new Vector2(245,114));var image=panel.gameObject.AddComponent<UnityEngine.UI.Image>();image.color=new Color(.07f,.13f,.17f,.96f);image.sprite=rounded;image.type=UnityEngine.UI.Image.Type.Sliced;
            game.menu=panel.gameObject;game.menuRect=panel;game.menuTitle=Text(panel,"Title","幼龍正看著你",25,new Vector2(10,-10),new Vector2(225,35),TextAlignmentOptions.Center);
            game.cloudButton=Button(panel,"Cloud","吐雲",new Vector2(0,-54));game.wakeButton=Button(panel,"Wake","喚醒",new Vector2(0,-54));panel.gameObject.SetActive(false);
        }
    }
}
