using TMPro;
using TwoDoors.Characters;
using TwoDoors.Items;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TwoDoors.Editor
{
    public static class QuickAccessUpgrade
    {
        static RectTransform Rect(string name,Transform parent,Vector2 position,Vector2 size)
        {
            var found=parent.Find(name);
            var rect=found ? (RectTransform)found : new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent,false); rect.anchorMin=rect.anchorMax=Vector2.one*.5f;
            rect.anchoredPosition=position;rect.sizeDelta=size;return rect;
        }
        static UnityEngine.UI.Image Image(string name,Transform parent,Vector2 position,Vector2 size,Color color)
        {
            var rect=Rect(name,parent,position,size);var image=rect.GetComponent<UnityEngine.UI.Image>();
            if(!image)image=rect.gameObject.AddComponent<UnityEngine.UI.Image>();image.color=color;image.raycastTarget=false;return image;
        }
        static TMP_Text Text(string name,Transform parent,string value,Vector2 position,Vector2 size,float fontSize,TMP_FontAsset font)
        {
            var rect=Rect(name,parent,position,size);var text=rect.GetComponent<TMP_Text>();
            if(!text)text=rect.gameObject.AddComponent<TextMeshProUGUI>();text.font=font;text.text=value;text.fontSize=fontSize;text.alignment=TextAlignmentOptions.Center;text.raycastTarget=false;return text;
        }
        public static void Setup(CharacterDemo hud)
        {
            var data=new SerializedObject(hud);var player=(PlayerCharacter)data.FindProperty("player").objectReferenceValue;
            var slots=data.FindProperty("slots");for(int i=0;i<slots.arraySize;i++){var image=(UnityEngine.UI.Image)slots.GetArrayElementAtIndex(i).objectReferenceValue;if(image)image.gameObject.SetActive(false);}
            var panel=hud.transform.Find("InventoryPanel");if(panel)panel.gameObject.SetActive(false);
            var controls=hud.transform.Find("Controls").GetComponent<TMP_Text>();
            controls.text="WASD Move   SHIFT Sprint   LMB Attack   R Reload   G Pick up   ESC Pause";
            var root=Rect("QuickAccessBar",hud.transform,new Vector2(0,105),new Vector2(724,102));root.anchorMin=root.anchorMax=new Vector2(.5f,0);
            var bar=root.GetComponent<QuickAccessBar>();if(!bar)bar=root.gameObject.AddComponent<QuickAccessBar>();
            bar.player=player;bar.icons=new ItemIcon[7];bar.cards=new UnityEngine.UI.Image[7];bar.accents=new UnityEngine.UI.Image[7];bar.counts=new TMP_Text[7];
            for(int i=0;i<7;i++)
            {
                var card=Image("Slot"+i,root,new Vector2((i-3)*102,0),new Vector2(94,102),new Color(.035f,.05f,.075f,.94f));bar.cards[i]=card;
                var iconRoot=Rect("Icon",card.transform,new Vector2(0,0),new Vector2(76,48));
                if(!iconRoot.GetComponent<CanvasRenderer>())iconRoot.gameObject.AddComponent<CanvasRenderer>();
                var icon=iconRoot.GetComponent<ItemIcon>();if(!icon)icon=iconRoot.gameObject.AddComponent<ItemIcon>();icon.kind=QuickAccessBar.Kinds[i];icon.color=i==0?Color.white:new Color(.21f,.26f,.3f);icon.raycastTarget=false;icon.SetAllDirty();bar.icons[i]=icon;
                var key=Text("Key",card.transform,QuickAccessBar.Keys[i],new Vector2(-30,35),new Vector2(24,24),20,controls.font);key.color=new Color(.7f,.78f,.84f);
                Text("Name",card.transform,new[]{"KNIFE","PISTOL","AUTO","SHOTGUN","MACHETE","HEAL","LIGHT"}[i],new Vector2(0,-33),new Vector2(90,20),13,controls.font);
                bar.counts[i]=Text("Count",card.transform,"",new Vector2(24,35),new Vector2(40,22),16,controls.font);
                bar.accents[i]=Image("Selected",card.transform,new Vector2(0,-49),new Vector2(94,3),Color.clear);
            }
            EditorUtility.SetDirty(bar);
        }
        [MenuItem("TwoDoors/Apply Quick Access Controls")]
        public static void Apply()
        {
            if(EditorApplication.isPlaying)throw new System.InvalidOperationException("Stop Play mode first");
            foreach(var path in new[]{"Assets/CJW/Scenes/Game.unity","Assets/TwoDoors/Scenes/CharacterTest.unity"})
            {
                var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(path);bool opened=!scene.isLoaded;
                if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
                if(scene.isDirty)throw new System.InvalidOperationException("Unsaved scene: "+path);
                foreach(var root in scene.GetRootGameObjects())
                {
                    foreach(var hud in root.GetComponentsInChildren<CharacterDemo>(true))Setup(hud);
                    foreach(var zombie in root.GetComponentsInChildren<ZombieEnemy>(true))zombie.GetComponent<UnityEngine.AI.NavMeshAgent>().speed=5.76f;
                }
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);if(opened)EditorSceneManager.CloseScene(scene,true);
            }
            const string prefab="Assets/TwoDoors/Prefabs/Zombie.prefab";
            var enemy=PrefabUtility.LoadPrefabContents(prefab);
            try{enemy.GetComponent<UnityEngine.AI.NavMeshAgent>().speed=5.76f;PrefabUtility.SaveAsPrefabAsset(enemy,prefab);}
            finally{PrefabUtility.UnloadPrefabContents(enemy);}
            AssetDatabase.SaveAssets();
        }
    }
}
