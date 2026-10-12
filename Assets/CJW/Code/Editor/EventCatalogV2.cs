using System;
using System.Linq;
using TwoDoors.Characters;
using TwoDoors.Events;
using TwoDoors.Items;
using TwoDoors.Stages;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TwoDoors.EditorTools
{
    public static class EventCatalogV2
    {
        public const string Root = "Assets/CJW/Data/StagesV2";
        public static readonly string[] Order = { "01_Armory", "02_SwarmOrSiege", "03_Blackout", "04_FieldHospital", "05_SupplyToll", "06_AmmoExchange", "07_EmergencySwitch", "08_BloodCache", "09_LastStand" };

        [MenuItem("Tools/TwoDoors/Generate Revised Events")]
        public static void Generate()
        {
            System.IO.Directory.CreateDirectory(Root);AssetDatabase.Refresh();
            Stage(0,"무장할 대가",
                Event<InstantEvent>(0,"Red",hp:-25,entry:new[]{Grant(ItemKind.Pistol,1),Grant(ItemKind.Ammo,70)}),
                Event<SurvivalEvent>(0,"Blue",time:18,mannequins:4,entry:new[]{Grant(ItemKind.Pistol,1),Grant(ItemKind.Ammo,14)},reward:new[]{Grant(ItemKind.Medkit,2)}));
            Stage(1,"떼거리 vs 공성전",
                Event<SurvivalEvent>(1,"Red",time:12,mannequins:8,entry:new[]{Grant(ItemKind.Ammo,35)}),
                Event<SurvivalEvent>(1,"Blue",time:50,golems:3,reward:new[]{Grant(ItemKind.Medkit,2)}));
            Stage(2,"빛을 살 것인가",
                Event<SurvivalEvent>(2,"Red",time:22,mannequins:6,dark:true,entry:new[]{Grant(ItemKind.Flashlight,1)},reward:new[]{Grant(ItemKind.Ammo,70)}),
                Event<InstantEvent>(2,"Blue",hp:-30));
            Stage(3,"야전 병원",
                Event<HealZoneEvent>(3,"Red",time:35,golems:2,healRate:8),
                Event<InstantEvent>(3,"Blue",hp:-20,entry:new[]{Grant(ItemKind.Medkit,4)}));
            Stage(4,"보급품 통행료",
                Event<ItemTradeEvent>(4,"Red",skip:true,payment:ItemKind.Medkit,price:2,penalty:50),
                Event<SurvivalEvent>(4,"Blue",time:45,golems:2,mannequins:4,reward:new[]{Grant(ItemKind.Medkit,3)}));
            Stage(5,"탄약 환전소",
                Event<ItemTradeEvent>(5,"Red",payment:ItemKind.Ammo,price:28,penalty:40,reward:new[]{Grant(ItemKind.Medkit,3)}),
                Event<SurvivalEvent>(5,"Blue",time:30,mannequins:6,zombies:2,entry:new[]{Grant(ItemKind.Ammo,14)},reward:new[]{Grant(ItemKind.Ammo,84)}));
            Stage(6,"달리거나 지불하거나",
                Event<ButtonEvent>(6,"Red",mannequins:5,dark:true,entry:new[]{Grant(ItemKind.Flashlight,1)}),
                Event<InstantEvent>(6,"Blue",hp:-35,entry:new[]{Grant(ItemKind.Ammo,42),Grant(ItemKind.Medkit,1)}));
            Stage(7,"피의 보급 상자",
                Event<InstantEvent>(7,"Red",hp:-55,entry:new[]{Grant(ItemKind.Ammo,105),Grant(ItemKind.Medkit,4)}),
                Event<SurvivalEvent>(7,"Blue",time:55,zombies:3,golems:2,mannequins:4,reward:new[]{Grant(ItemKind.Ammo,35),Grant(ItemKind.Medkit,1)}));
            Stage(8,"최후의 탈출",
                Event<ItemTradeEvent>(8,"Red",payment:ItemKind.Medkit,price:3,penalty:65,escape:true),
                Event<SurvivalEvent>(8,"Blue",time:60,golems:3,mannequins:6,entry:new[]{Grant(ItemKind.Ammo,56)},escape:true));
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Tools/TwoDoors/Apply Revised Events To Game")]
        public static void ApplyToGame()
        {
            if(EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play mode first.");
            if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty) throw new InvalidOperationException("Save the current scene first.");
            Generate();
            var scene=EditorSceneManager.OpenScene("Assets/CJW/Scenes/Game.unity");
            var manager=UnityEngine.Object.FindFirstObjectByType<StageManager>();
            if(!manager) throw new InvalidOperationException("Game has no StageManager.");
            var so=new SerializedObject(manager);var list=so.FindProperty("stages");list.arraySize=Order.Length;
            for(int i=0;i<Order.Length;i++) list.GetArrayElementAtIndex(i).objectReferenceValue=AssetDatabase.LoadAssetAtPath<StageChoice>($"{Root}/{Order[i]}.asset");
            so.ApplyModifiedPropertiesWithoutUndo();
            if (!UnityEngine.Object.FindFirstObjectByType<InventoryView>())
            {
                TwoDoors.Editor.InventorySceneSetup.Install(false);
            }
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        }

        static RoomEvent.ItemGrant Grant(ItemKind kind,int amount)=>new(){kind=kind,amount=amount};
        static T Asset<T>(string path) where T:ScriptableObject
        {
            var asset=AssetDatabase.LoadAssetAtPath<T>(path);
            if(asset) return asset;
            asset=ScriptableObject.CreateInstance<T>();AssetDatabase.CreateAsset(asset,path);return asset;
        }
        static void Stage(int index,string title,RoomEvent red,RoomEvent blue)
        {
            var choice=Asset<StageChoice>($"{Root}/{Order[index]}.asset");var so=new SerializedObject(choice);
            so.FindProperty("title").stringValue=title;so.FindProperty("redDoor").objectReferenceValue=red;so.FindProperty("blueDoor").objectReferenceValue=blue;so.ApplyModifiedPropertiesWithoutUndo();
        }
        static RoomEvent Event<T>(int index,string color,int hp=0,float time=0,int zombies=0,int golems=0,int mannequins=0,bool dark=false,bool skip=false,bool escape=false,float healRate=0,ItemKind payment=ItemKind.Medkit,int price=1,int penalty=35,RoomEvent.ItemGrant[] entry=null,RoomEvent.ItemGrant[] reward=null) where T:RoomEvent
        {
            var ev=Asset<T>($"{Root}/{Order[index]}_{color}.asset");var so=new SerializedObject(ev);
            so.FindProperty("healthChange").intValue=hp;so.FindProperty("lightsOff").boolValue=dark;
            so.FindProperty("zombieCount").intValue=0;so.FindProperty("zombiePrefab").objectReferenceValue=null;
            so.FindProperty("customDescription").stringValue="";so.FindProperty("skipNextStage").boolValue=skip;so.FindProperty("escapeOnClear").boolValue=escape;
            if(ev is SurvivalEvent) so.FindProperty("surviveTime").floatValue=time;
            if(ev is HealZoneEvent) so.FindProperty("healPerSecond").floatValue=healRate;
            if(ev is ButtonEvent) so.FindProperty("buttonCount").intValue=3;
            if(ev is ItemTradeEvent){so.FindProperty("payment").enumValueIndex=(int)payment;so.FindProperty("price").intValue=price;so.FindProperty("missingItemDamage").intValue=penalty;}
            var groups=new[]{("Zombie",zombies),("Golem",golems),("Mannequin",mannequins)}.Where(g=>g.Item2>0).ToArray();
            var array=so.FindProperty("enemyGroups");array.arraySize=groups.Length;
            for(int i=0;i<groups.Length;i++)
            {
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/TwoDoors/Prefabs/{groups[i].Item1}.prefab");
                if(!prefab) throw new InvalidOperationException("Missing enemy prefab: "+groups[i].Item1);
                array.GetArrayElementAtIndex(i).FindPropertyRelative("prefab").objectReferenceValue=prefab.GetComponent<EnemyCharacter>();
                array.GetArrayElementAtIndex(i).FindPropertyRelative("count").intValue=groups[i].Item2;
            }
            void Items(string field,RoomEvent.ItemGrant[] values)
            {
                values ??= Array.Empty<RoomEvent.ItemGrant>();var a=so.FindProperty(field);a.arraySize=values.Length;
                for(int i=0;i<values.Length;i++){a.GetArrayElementAtIndex(i).FindPropertyRelative("kind").enumValueIndex=(int)values[i].kind;a.GetArrayElementAtIndex(i).FindPropertyRelative("amount").intValue=values[i].amount;}
            }
            Items("entryItems",entry);Items("clearItems",reward);so.ApplyModifiedPropertiesWithoutUndo();return ev;
        }
    }
}
