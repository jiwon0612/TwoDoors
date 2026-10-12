using TMPro;
using TwoDoors.Characters;
using TwoDoors.Items;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace TwoDoors.Editor
{
    public static class InventorySceneSetup
    {
        private static TMP_FontAsset Font => AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");

        [MenuItem("TwoDoors/Add Inventory To Character Test")]
        public static void Install() => Install(true);

        public static void Install(bool includeDemoPickups)
        {
            if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop Play mode first.");
            var player = Object.FindFirstObjectByType<PlayerCharacter>();
            var hud = Object.FindFirstObjectByType<CharacterDemo>();
            if (!player || !hud) throw new System.InvalidOperationException("Open CharacterTest first.");
            if (hud.GetComponent<InventoryView>()) return;
            var bag = player.GetComponent<PlayerInventory>();
            if (!bag) bag = player.gameObject.AddComponent<PlayerInventory>();
            var settings = new SerializedObject(bag);
            settings.FindProperty("startWithPistol").boolValue = true;
            settings.FindProperty("startWithFlashlight").boolValue = false;
            settings.FindProperty("startingAmmo").intValue = 30;
            settings.FindProperty("startingMedkits").intValue = 0;
            settings.ApplyModifiedPropertiesWithoutUndo();
            var position = player.transform.position;
            player.transform.position = Vector3.zero;
            if (includeDemoPickups)
                PrefabUtility.SaveAsPrefabAsset(player.gameObject,"Assets/TwoDoors/Prefabs/Player.prefab");
            player.transform.position = position;

            if (!Object.FindFirstObjectByType<EventSystem>())
            {
                var events = new GameObject("EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));
                events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            }
            var view = hud.gameObject.AddComponent<InventoryView>();
            var panel = Panel("InventoryPanel",hud.transform,Vector2.zero,new Vector2(1000,460),new Color(0.025f,0.035f,0.045f,0.98f));
            Text("Title",panel.transform,"INVENTORY",new Vector2(0,170),new Vector2(700,60),36);
            Text("Hint",panel.transform,"Select an item to equip or use   |   TAB / ESC to close",new Vector2(0,-175),new Vector2(900,40),22);
            var buttons = new UnityEngine.UI.Button[5];
            var labels = new TMP_Text[5];
            for (int i=0;i<5;i++)
            {
                var cell = Panel("Item"+(ItemKind)i,panel.transform,new Vector2((i-2)*180,0),new Vector2(165,190),new Color(0.12f,0.17f,0.2f));
                buttons[i] = cell.gameObject.AddComponent<UnityEngine.UI.Button>();
                buttons[i].targetGraphic=cell;
                var colors=buttons[i].colors;colors.highlightedColor=new Color(0.5f,0.8f,0.9f);colors.disabledColor=new Color(0.4f,0.4f,0.4f);buttons[i].colors=colors;
                labels[i]=Text("Label",cell.transform,"",Vector2.zero,new Vector2(160,180),24);
            }
            var close = Panel("Close",panel.transform,new Vector2(450,185),new Vector2(50,45),new Color(0.25f,0.3f,0.34f));
            var closeButton = close.gameObject.AddComponent<UnityEngine.UI.Button>();closeButton.targetGraphic=close;
            Text("Label",close.transform,"X",Vector2.zero,new Vector2(45,40),24);
            var prompt=Text("PickupPrompt",hud.transform,"",new Vector2(0,-110),new Vector2(650,40),28);
            var message=Text("InventoryMessage",hud.transform,"",new Vector2(0,-270),new Vector2(900,45),24);
            var so = new SerializedObject(view);
            so.FindProperty("player").objectReferenceValue=player;
            so.FindProperty("panel").objectReferenceValue=panel.gameObject;
            so.FindProperty("prompt").objectReferenceValue=prompt;
            so.FindProperty("message").objectReferenceValue=message;
            so.FindProperty("closeButton").objectReferenceValue=closeButton;
            var bs=so.FindProperty("buttons");bs.arraySize=5;
            var ls=so.FindProperty("labels");ls.arraySize=5;
            for(int i=0;i<5;i++){bs.GetArrayElementAtIndex(i).objectReferenceValue=buttons[i];ls.GetArrayElementAtIndex(i).objectReferenceValue=labels[i];}
            so.ApplyModifiedPropertiesWithoutUndo();panel.gameObject.SetActive(false);
            var controls=hud.transform.Find("Controls").GetComponent<TMP_Text>();
            controls.text="WASD Move   SHIFT Sprint   E Pick up   TAB Inventory   LMB Attack   R Reload   1/2 Weapons   3 Heal   F Light   ESC Pause   F5 Restart";
            CombatContentUpgrade.UpgradeInventory(view);

            var items = includeDemoPickups ? new GameObject("DemoPickups") : null;
            if (includeDemoPickups)
            for(int i=0;i<4;i++)
            {
                var kind=new[]{ItemKind.Pistol,ItemKind.Ammo,ItemKind.Medkit,ItemKind.Flashlight}[i];
                var pickup=CreatePickup(kind,kind==ItemKind.Ammo?21:1,player);
                PrefabUtility.SaveAsPrefabAsset(pickup,"Assets/TwoDoors/Prefabs/Pickup"+kind+".prefab");
                pickup.transform.SetParent(items.transform,false);
                pickup.transform.position=new Vector3((i-1.5f)*0.8f,1.1f,-4.25f);
            }
            EditorSceneManager.MarkSceneDirty(player.gameObject.scene);
            EditorSceneManager.SaveScene(player.gameObject.scene);
            AssetDatabase.SaveAssets();
        }

        private static GameObject CreatePickup(ItemKind kind,int amount,PlayerCharacter player)
        {
            var root=new GameObject("Pickup"+kind,typeof(BoxCollider),typeof(ItemPickup));
            root.GetComponent<BoxCollider>().size=new Vector3(0.55f,0.55f,0.55f);
            var data=new SerializedObject(root.GetComponent<ItemPickup>());data.FindProperty("kind").enumValueIndex=(int)kind;data.FindProperty("amount").intValue=amount;data.ApplyModifiedPropertiesWithoutUndo();
            Color color=kind==ItemKind.Medkit?new Color(0.7f,0.1f,0.12f):kind==ItemKind.Ammo?new Color(0.65f,0.48f,0.12f):new Color(0.18f,0.25f,0.3f);
            var material=new Material(Shader.Find("Universal Render Pipeline/Lit"));material.SetColor("_BaseColor",color);
            AssetDatabase.CreateAsset(material,"Assets/TwoDoors/Generated/Materials/Pickup"+kind+".mat");
            var mesh=GameObject.CreatePrimitive(kind==ItemKind.Flashlight?PrimitiveType.Cylinder:PrimitiveType.Cube);
            mesh.name="Visual";mesh.transform.SetParent(root.transform,false);mesh.transform.localScale=new Vector3(0.4f,0.25f,0.25f);mesh.GetComponent<Renderer>().sharedMaterial=material;Object.DestroyImmediate(mesh.GetComponent<Collider>());
            if(kind==ItemKind.Pistol)
            {
                Object.DestroyImmediate(mesh);
                var gun=Object.Instantiate(player.transform.Find("PlayerCamera/HandsAndWeapon/Pistol").gameObject,root.transform);
                gun.SetActive(true);gun.transform.localPosition=Vector3.zero;gun.transform.localRotation=Quaternion.Euler(0,90,0);gun.transform.localScale=Vector3.one*1.7f;
            }
            var label=new GameObject("Label").AddComponent<TextMeshPro>();label.transform.SetParent(root.transform,false);label.transform.localPosition=Vector3.up*0.45f;label.transform.localRotation=Quaternion.identity;label.transform.localScale=Vector3.one*0.2f;label.font=Font;label.fontSize=2;label.text=PlayerInventory.Label(kind)+" x"+amount;label.alignment=TextAlignmentOptions.Center;label.rectTransform.sizeDelta=new Vector2(1.4f,0.5f);
            return root;
        }

        private static UnityEngine.UI.Image Panel(string name,Transform parent,Vector2 pos,Vector2 size,Color color)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(UnityEngine.UI.Image));go.transform.SetParent(parent,false);
            var rt=(RectTransform)go.transform;rt.anchorMin=rt.anchorMax=new Vector2(0.5f,0.5f);rt.anchoredPosition=pos;rt.sizeDelta=size;
            var image=go.GetComponent<UnityEngine.UI.Image>();image.color=color;return image;
        }
        private static TMP_Text Text(string name,Transform parent,string value,Vector2 pos,Vector2 size,float fontSize)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(TextMeshProUGUI));go.transform.SetParent(parent,false);
            var text=go.GetComponent<TextMeshProUGUI>();text.font=Font;text.text=value;text.fontSize=fontSize;text.alignment=TextAlignmentOptions.Center;text.raycastTarget=false;
            text.rectTransform.anchoredPosition=pos;text.rectTransform.sizeDelta=size;return text;
        }
    }
}

