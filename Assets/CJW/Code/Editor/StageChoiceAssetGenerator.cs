using System;
using TwoDoors.Events;
using UnityEditor;
using UnityEngine;

namespace TwoDoors.EditorTools
{
    /// <summary>
    /// 기획된 11개 선택지를 Assets/CJW/Data/Stages 에 에셋으로 생성한다.
    /// 이미 있는 에셋은 덮어쓰지 않으므로, 생성 후 인스펙터에서 수치를 조절해도 안전하다.
    /// </summary>
    static class StageChoiceAssetGenerator
    {
        const string Root = "Assets/CJW/Data/Stages";

        [MenuItem("Tools/TwoDoors/Generate Stage Choices")]
        internal static void Generate()
        {
            EnsureFolder(Root);

            Stage("01_Toll", "통행료",
                Instant("Red", hp: -10),
                Survival("Blue", zombies: 1, time: 10));

            Stage("02_RiskyHeal", "위험한 치료",
                Survival("Red", hp: 30, zombies: 1, time: 15),
                Instant("Blue", hp: -10));

            Stage("03_Blackout", "정전",
                Survival("Red", lightsOff: true, zombies: 1, time: 10),
                Survival("Blue", zombies: 1, time: 20, description: "밝은 방\n좀비 1마리 등장\n20초 동안 생존"));

            Stage("04_ShortAndHard", "짧고 굵게",
                Survival("Red", zombies: 3, time: 8),
                Survival("Blue", zombies: 1, time: 25));

            Stage("05_HealStrength", "치료 강도",
                Survival("Red", hp: 15, zombies: 1, time: 10),
                Survival("Blue", hp: 50, zombies: 2, time: 20));

            Stage("06_DarkReward", "어둠 속 보상",
                Survival("Red", hp: 30, lightsOff: true, zombies: 1, time: 15),
                Survival("Blue", zombies: 1, time: 15, description: "밝은 방\n체력 회복 없음\n좀비 1마리 등장\n15초 동안 생존"));

            Stage("07_Shortcut", "지름길",
                Instant("Red", hp: -30, skip: true),
                Survival("Blue", hp: 20, zombies: 2, time: 15));

            Stage("08_FinalDeal", "마지막 거래",
                Instant("Red", hp: -25, escape: true, description: "체력 25 감소\n즉시 탈출 성공"),
                Survival("Blue", zombies: 2, time: 20, escape: true, description: "좀비 2마리 등장\n20초 동안 생존하면 탈출 성공"));

            Stage("09_EmergencySwitch", "비상 스위치",
                Create<ButtonEvent>("Red", zombies: 1, configure: so => so.FindProperty("buttonCount").intValue = 3),
                Survival("Blue", zombies: 1, time: 20));

            Stage("10_HealZone", "회복 구역",
                Create<HealZoneEvent>("Red", zombies: 2, configure: so =>
                {
                    so.FindProperty("surviveTime").floatValue = 20;
                    so.FindProperty("healPerSecond").floatValue = 3;
                }),
                Survival("Blue", hp: 10, zombies: 1, time: 15));

            // 특수 좀비 프리팹이 준비되면 Red 에셋의 zombiePrefab 에 지정
            Stage("11_Gaze", "시선",
                Survival("Red", zombies: 1, time: 20, description: "바라보면 멈추는 특수 좀비 1마리 등장\n20초 동안 생존"),
                Survival("Blue", zombies: 1, time: 15, description: "일반 좀비 1마리 등장\n15초 동안 생존"));

            AssetDatabase.SaveAssets();
            Debug.Log($"Stage choices generated in {Root}");
        }

        // ---- 아래는 생성 헬퍼 ----

        static string currentFolder;

        static void Stage(string id, string title, Func<RoomEvent> red, Func<RoomEvent> blue)
        {
            currentFolder = $"{Root}/{id}";
            EnsureFolder(currentFolder);

            string path = $"{currentFolder}/{id}.asset";
            if (AssetDatabase.LoadAssetAtPath<StageChoice>(path) != null) return;

            var choice = ScriptableObject.CreateInstance<StageChoice>();
            AssetDatabase.CreateAsset(choice, path);
            var so = new SerializedObject(choice);
            so.FindProperty("title").stringValue = title;
            so.FindProperty("redDoor").objectReferenceValue = red();
            so.FindProperty("blueDoor").objectReferenceValue = blue();
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static Func<RoomEvent> Instant(string door, int hp = 0, bool skip = false, bool escape = false, string description = null) =>
            Create<InstantEvent>(door, hp, skip: skip, escape: escape, description: description);

        static Func<RoomEvent> Survival(string door, int hp = 0, bool lightsOff = false, int zombies = 0, float time = 15,
            bool escape = false, string description = null) =>
            Create<SurvivalEvent>(door, hp, lightsOff, zombies, escape: escape, description: description,
                configure: so => so.FindProperty("surviveTime").floatValue = time);

        static Func<RoomEvent> Create<T>(string door, int hp = 0, bool lightsOff = false, int zombies = 0,
            bool skip = false, bool escape = false, string description = null, Action<SerializedObject> configure = null)
            where T : RoomEvent => () =>
        {
            string path = $"{currentFolder}/{door}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<RoomEvent>(path);
            if (existing != null) return existing;

            var ev = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(ev, path);
            var so = new SerializedObject(ev);
            so.FindProperty("healthChange").intValue = hp;
            so.FindProperty("lightsOff").boolValue = lightsOff;
            so.FindProperty("zombieCount").intValue = zombies;
            so.FindProperty("skipNextStage").boolValue = skip;
            so.FindProperty("escapeOnClear").boolValue = escape;
            so.FindProperty("customDescription").stringValue = description ?? "";
            configure?.Invoke(so);
            so.ApplyModifiedPropertiesWithoutUndo();
            return ev;
        };

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int slash = path.LastIndexOf('/');
            string parent = path[..slash];
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, path[(slash + 1)..]);
        }
    }
}
