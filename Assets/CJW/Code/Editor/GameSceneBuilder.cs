using System;
using System.IO;
using System.Linq;
using TMPro;
using TwoDoors.Characters;
using TwoDoors.Events;
using TwoDoors.Stages;
using TwoDoors.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TwoDoors.EditorTools
{
    /// <summary>
    /// 김우진의 CharacterTest 씬(방·NavMesh·플레이어·HUD)을 복사해 게임 씬을 만들고,
    /// 문·스테이지 매니저·스폰 위치·버튼·회복 구역을 배치하고 전부 연결한다.
    /// 방 좌표는 CharacterSceneBuilder 기준: 바닥 x ±12, z -11 ~ 15, 플레이어 (0,0,-6).
    /// </summary>
    static class GameSceneBuilder
    {
        const string SourceScene = "Assets/TwoDoors/Scenes/CharacterTest.unity";
        const string ScenePath = "Assets/CJW/Scenes/Game.unity";
        const string MaterialFolder = "Assets/CJW/Generated/Materials";
        const string StageFolder = EventCatalogV2.Root;
        const string ZombiePrefab = "Assets/TwoDoors/Prefabs/Zombie.prefab";

        // 기본 진행 순서. 지름길(단계 건너뛰기)이 마지막 단계를 건너뛰어 바로 탈출하지 않도록 중간에 둔다.
        static readonly string[] StageOrder = EventCatalogV2.Order;

        // 좀비 감지 거리(14) 안에 들어오도록 플레이어 시작 위치 (0,0,-6) 기준 약 13m 이내
        static readonly Vector3[] ZombieSpawns = { new(-7, 0, 5), new(7, 0, 5), new(0, 0, 7) };
        static readonly Vector3[] ButtonSpots = { new(-10, 0, -9), new(10, 0, -9), new(10, 0, 12) };
        static readonly Vector3 HealZoneSpot = new(0, 0, -2);
        const float DoorZ = 14.85f; // 뒤쪽 벽 안쪽 면

        [MenuItem("Tools/TwoDoors/Build Game Scene")]
        static void Build()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Play 모드를 먼저 종료하세요.");
            if (!File.Exists(SourceScene))
                throw new InvalidOperationException($"{SourceScene}가 없습니다. TwoDoors > Create Character Test Scene을 먼저 실행하세요.");
            if (File.Exists(ScenePath))
                throw new InvalidOperationException($"{ScenePath}가 이미 있습니다. 다시 만들려면 먼저 삭제하세요.");
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            StageChoiceAssetGenerator.Generate();
            EnsureFolder("Assets/CJW/Scenes");
            EnsureFolder(MaterialFolder);
            if (!AssetDatabase.CopyAsset(SourceScene, ScenePath))
                throw new InvalidOperationException("CharacterTest 씬 복사에 실패했습니다.");
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            var player = Object.FindFirstObjectByType<PlayerCharacter>();
            if (!player) throw new InvalidOperationException("씬에서 PlayerCharacter를 찾지 못했습니다.");

            // 테스트용으로 미리 배치된 적 제거 (좀비는 이벤트가 스폰한다)
            foreach (var enemy in Object.FindObjectsByType<EnemyCharacter>(FindObjectsSortMode.None))
                Object.DestroyImmediate(enemy.gameObject);

            // 손전등·총구 화염 등 플레이어에 붙은 조명은 제외
            var roomLights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None)
                .Where(l => !l.transform.IsChildOf(player.transform)).ToArray();

            var root = new GameObject("StageSetup").transform;
            var managers = new GameObject("GameManager");
            managers.transform.SetParent(root);
            var room = managers.AddComponent<RoomController>();
            var runner = managers.AddComponent<RoomEventRunner>();
            var stages = managers.AddComponent<StageManager>();
            var hud = managers.AddComponent<StageDebugHud>();

            var playerSpawn = Point("PlayerSpawn", root, player.transform.position, player.transform.rotation);
            var spawnRoot = new GameObject("ZombieSpawns").transform;
            spawnRoot.SetParent(root);
            var zombieSpawns = ZombieSpawns.Select((p, i) => Point($"ZombieSpawn{i + 1}", spawnRoot, p, Quaternion.Euler(0, 180, 0))).ToArray();

            var buttonRoot = new GameObject("Buttons").transform;
            buttonRoot.SetParent(root);
            var buttons = ButtonSpots.Select((p, i) => MakeButton(buttonRoot, i + 1, p)).ToArray();
            var healZone = MakeHealZone(root);

            var red = MakeDoor(root, DoorColor.Red, -4f, new Color(0.75f, 0.08f, 0.08f), stages);
            var blue = MakeDoor(root, DoorColor.Blue, 4f, new Color(0.1f, 0.25f, 0.85f), stages);

            Set(room, so =>
            {
                so.FindProperty("player").objectReferenceValue = player;
                SetArray(so.FindProperty("roomLights"), roomLights);
                so.FindProperty("defaultZombiePrefab").objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<GameObject>(ZombiePrefab).GetComponent<EnemyCharacter>();
                SetArray(so.FindProperty("zombieSpawnPoints"), zombieSpawns);
                SetArray(so.FindProperty("buttons"), buttons);
                so.FindProperty("healZone").objectReferenceValue = healZone;
            });
            Set(runner, so => so.FindProperty("room").objectReferenceValue = room);
            Set(stages, so =>
            {
                var choices = StageOrder
                    .Select(id => AssetDatabase.LoadAssetAtPath<StageChoice>($"{StageFolder}/{id}.asset"))
                    .Where(c => c != null).ToArray();
                SetArray(so.FindProperty("stages"), choices);
                so.FindProperty("room").objectReferenceValue = room;
                so.FindProperty("runner").objectReferenceValue = runner;
                so.FindProperty("redDoor").objectReferenceValue = red;
                so.FindProperty("blueDoor").objectReferenceValue = blue;
                so.FindProperty("playerSpawnPoint").objectReferenceValue = playerSpawn;
            });
            Set(hud, so =>
            {
                so.FindProperty("stages").objectReferenceValue = stages;
                so.FindProperty("runner").objectReferenceValue = runner;
            });

            EditorSceneManager.SaveScene(scene);
            var builds = EditorBuildSettings.scenes.Where(s => s.path != ScenePath).ToList();
            builds.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = builds.ToArray();
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = managers;
            Debug.Log($"게임 씬 생성 완료: {ScenePath} (빌드 첫 번째 씬으로 등록)");
        }

        static StageDoor MakeDoor(Transform parent, DoorColor color, float x, Color tint, StageManager manager)
        {
            var door = new GameObject($"{color}Door");
            door.transform.SetParent(parent);
            door.transform.position = new Vector3(x, 0, DoorZ);

            Box("Frame", door.transform, new Vector3(0, 1.6f, -0.05f), new Vector3(2.4f, 3.2f, 0.1f), Mat("DoorFrame", new Color(0.12f, 0.12f, 0.13f)));
            Box("Panel", door.transform, new Vector3(0, 1.5f, -0.12f), new Vector3(2f, 3f, 0.06f), Mat($"Door{color}", tint, tint * 0.8f));
            var blocker = Box("Lock", door.transform, new Vector3(0, 1.5f, -0.3f), new Vector3(2.1f, 3.05f, 0.2f), Mat("DoorLocked", new Color(0.3f, 0.3f, 0.32f)), collider: true);

            // 문 앞 통과 판정. 잠김 블로커보다 안쪽까지 들어와야 하므로 문 쪽에 붙여 둔다.
            var trigger = door.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = new Vector3(0, 1.5f, -0.7f);
            trigger.size = new Vector3(2f, 3f, 0.9f);

            var labelGo = new GameObject("Label", typeof(TextMeshPro));
            labelGo.transform.SetParent(door.transform, false);
            labelGo.transform.localPosition = new Vector3(0, 4.05f, -0.2f);
            var label = labelGo.GetComponent<TextMeshPro>();
            label.rectTransform.sizeDelta = new Vector2(3.8f, 1.7f);
            label.alignment = TextAlignmentOptions.Center;
            label.enableAutoSizing = true;
            label.fontSizeMin = 1f;
            label.fontSizeMax = 6f;
            label.color = Color.Lerp(tint, Color.white, 0.6f);
            label.text = "";

            var stageDoor = door.AddComponent<StageDoor>();
            Set(stageDoor, so =>
            {
                so.FindProperty("color").enumValueIndex = (int)color;
                so.FindProperty("manager").objectReferenceValue = manager;
                so.FindProperty("label").objectReferenceValue = label;
                so.FindProperty("blocker").objectReferenceValue = blocker;
            });
            return stageDoor;
        }

        static RoomButton MakeButton(Transform parent, int number, Vector3 position)
        {
            var go = new GameObject($"Button{number}");
            go.transform.SetParent(parent);
            go.transform.position = position;
            var pad = Box("Pad", go.transform, new Vector3(0, 0.05f, 0), new Vector3(1.2f, 0.1f, 1.2f), Mat("ButtonPad", new Color(0.8f, 0.6f, 0.1f), new Color(0.6f, 0.4f, 0f)));
            var trigger = go.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = new Vector3(0, 1f, 0);
            trigger.size = new Vector3(1.2f, 2f, 1.2f);
            var button = go.AddComponent<RoomButton>();
            Set(button, so => so.FindProperty("indicator").objectReferenceValue = pad.GetComponent<Renderer>());
            return button;
        }

        static HealZone MakeHealZone(Transform parent)
        {
            var go = new GameObject("HealZone");
            go.transform.SetParent(parent);
            go.transform.position = HealZoneSpot;
            var disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            disc.name = "Visual";
            Object.DestroyImmediate(disc.GetComponent<Collider>());
            disc.transform.SetParent(go.transform, false);
            disc.transform.localPosition = new Vector3(0, 0.01f, 0);
            disc.transform.localScale = new Vector3(3f, 0.01f, 3f);
            disc.GetComponent<Renderer>().sharedMaterial = Mat("HealZone", new Color(0.2f, 0.8f, 0.3f), new Color(0.1f, 0.6f, 0.2f));
            var trigger = go.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = new Vector3(0, 1f, 0);
            trigger.size = new Vector3(3f, 2f, 3f);
            return go.AddComponent<HealZone>();
        }

        static Transform Point(string name, Transform parent, Vector3 position, Quaternion rotation)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent);
            t.SetPositionAndRotation(position, rotation);
            return t;
        }

        static GameObject Box(string name, Transform parent, Vector3 localPosition, Vector3 size, Material material, bool collider = false)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = material;
            return go;
        }

        static Material Mat(string name, Color color, Color? emission = null)
        {
            string path = $"{MaterialFolder}/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m) return m;

            m = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name };
            m.SetColor("_BaseColor", color);
            AssetDatabase.CreateAsset(m, path);
            if (emission.HasValue)
            {
                // 정전 방에서도 문·버튼·회복 구역이 보이도록 발광.
                // URP 머티리얼 후처리가 생성 시점의 키워드를 지우므로 에셋 생성 뒤에 켠다.
                m.SetColor("_EmissionColor", emission.Value);
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                m.EnableKeyword("_EMISSION");
                EditorUtility.SetDirty(m);
            }
            return m;
        }

        static void Set(Object target, Action<SerializedObject> edit)
        {
            var so = new SerializedObject(target);
            edit(so);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetArray(SerializedProperty array, Object[] values)
        {
            array.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int slash = path.LastIndexOf('/');
            EnsureFolder(path[..slash]);
            AssetDatabase.CreateFolder(path[..slash], path[(slash + 1)..]);
        }
    }
}
