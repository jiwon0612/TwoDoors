using System;
using System.IO;
using System.Linq;
using TMPro;
using TwoDoors.Characters;
using TwoDoors.Events;
using TwoDoors.Stages;
using TwoDoors.UI;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
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
        internal const string ScenePath = "Assets/CJW/Scenes/Game.unity";
        internal const string MaterialFolder = "Assets/CJW/Generated/Materials";
        const string GameVolumePath = "Assets/CJW/Generated/GameVolume.asset";
        const string NavMeshPath = "Assets/CJW/Generated/GameNavMesh.asset";
        const string StageFolder = EventCatalogV2.Root;
        const string ZombiePrefab = "Assets/TwoDoors/Prefabs/Zombie.prefab";

        // 단계 구성과 순서는 EventCatalogV2(김우진 개편)가 관리한다.
        static readonly string[] StageOrder = EventCatalogV2.Order;

        // 좀비 감지 거리(14) 안에 들어오도록 플레이어 시작 위치 (0,0,-6) 기준 약 13m 이내
        static readonly Vector3[] ZombieSpawns = { new(-7, 0, 5), new(7, 0, 5), new(0, 0, 7) };
        static readonly Vector3[] ButtonSpots = { new(-10, 0, -9), new(10, 0, -9), new(10, 0, 12) };
        static readonly Vector3 HealZoneSpot = new(0, 0, -2);
        const float DoorZ = 14.85f; // 뒤쪽 벽 안쪽 면
        const float DoorHalfWidth = 1.1f;
        const float DoorHeight = 3.2f;
        static readonly float[] DoorXs = { -4f, 4f };

        [MenuItem("Tools/TwoDoors/Build Game Scene")]
        static void Build()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Play 모드를 먼저 종료하세요.");
            if (!File.Exists(SourceScene))
                throw new InvalidOperationException($"{SourceScene}가 없습니다. TwoDoors > Create Character Test Scene을 먼저 실행하세요.");
            if (File.Exists(ScenePath))
                throw new InvalidOperationException($"{ScenePath}가 이미 있습니다. 다시 만들려면 먼저 삭제하세요.");
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            string previousGuid = BuildListGuid(ScenePath);

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
            // 테스트 씬의 데모 줍기 아이템도 제거 (아이템은 이벤트 보급/보상으로만 얻는다)
            var demoPickups = GameObject.Find("DemoPickups");
            if (demoPickups) Object.DestroyImmediate(demoPickups);
            foreach (var pickup in Object.FindObjectsByType<TwoDoors.Items.ItemPickup>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (pickup) Object.DestroyImmediate(pickup.gameObject);

            // 손전등·총구 화염 등 플레이어에 붙은 조명은 제외
            var roomLights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None)
                .Where(l => !l.transform.IsChildOf(player.transform)).ToArray();
            roomLights = roomLights.Concat(DressRoom(roomLights)).ToArray();
            HideTestHeader();

            var root = new GameObject("StageSetup").transform;
            var managers = new GameObject("GameManager");
            managers.transform.SetParent(root);
            var room = managers.AddComponent<RoomController>();
            var runner = managers.AddComponent<RoomEventRunner>();
            var stages = managers.AddComponent<StageManager>();
            var hud = managers.AddComponent<StageDebugHud>();
            var fade = managers.AddComponent<ScreenFade>();
            managers.AddComponent<DamageHealFeedback>();
            var banner = managers.AddComponent<StageBanner>();

            var playerSpawn = Point("PlayerSpawn", root, player.transform.position, player.transform.rotation);
            var spawnRoot = new GameObject("ZombieSpawns").transform;
            spawnRoot.SetParent(root);
            var zombieSpawns = ZombieSpawns.Select((p, i) => Point($"ZombieSpawn{i + 1}", spawnRoot, p, Quaternion.Euler(0, 180, 0))).ToArray();

            var buttonRoot = new GameObject("Buttons").transform;
            buttonRoot.SetParent(root);
            var buttons = ButtonSpots.Select((p, i) => MakeButton(buttonRoot, i + 1, p)).ToArray();
            var healZone = MakeHealZone(root);

            CutBackWall(root, DoorXs);
            var red = MakeDoor(root, DoorColor.Red, DoorXs[0], new Color(0.75f, 0.08f, 0.08f), stages);
            var blue = MakeDoor(root, DoorColor.Blue, DoorXs[1], new Color(0.1f, 0.25f, 0.85f), stages);

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
                so.FindProperty("fade").objectReferenceValue = fade;
            });
            Set(hud, so =>
            {
                so.FindProperty("stages").objectReferenceValue = stages;
                so.FindProperty("runner").objectReferenceValue = runner;
            });
            Set(banner, so =>
            {
                so.FindProperty("stages").objectReferenceValue = stages;
                so.FindProperty("runner").objectReferenceValue = runner;
            });

            BakeNavMesh(player);
            EditorSceneManager.SaveScene(scene);
            KeepGuid(ScenePath, previousGuid);
            ApplyBuildOrder();
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = managers;
            Debug.Log($"게임 씬 생성 완료: {ScenePath} (빌드 씬 목록에 등록)");
        }

        // 시작 화면과 같은 재료·조명으로 방을 꾸민다.
        // 복사본 씬의 렌더러 머티리얼 연결과 조명 값만 바꾸고, 김우진의 머티리얼/프리팹 에셋은 그대로 둔다.
        // 반환값은 새로 추가한 천장 등(정전 이벤트가 같이 꺼야 하는 조명).
        static Light[] DressRoom(Light[] roomLights)
        {
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.085f, 0.09f, 0.11f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.015f, 0.016f, 0.022f);
            RenderSettings.fogDensity = 0.05f;
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;
            RenderSettings.reflectionIntensity = 0.4f;

            var wall = Mat("TitleWall", new Color(0.09f, 0.09f, 0.1f), smoothness: 0.15f);
            var floor = Mat("TitleFloor", new Color(0.015f, 0.015f, 0.018f), smoothness: 0.6f);
            var trim = Mat("TitleTrim", new Color(0.03f, 0.03f, 0.035f), smoothness: 0.4f);
            var bulb = Mat("TitleBulb", new Color(0.9f, 0.9f, 0.8f), new Color(1.6f, 1.55f, 1.3f));

            var room = GameObject.Find("TestRoom");
            if (room)
                foreach (var r in room.GetComponentsInChildren<Renderer>(true))
                {
                    if (r.name == "Floor") r.sharedMaterial = floor;
                    else if (r.name.EndsWith("Wall")) r.sharedMaterial = wall; // CutBackWall이 이 재질을 이어받는다
                    else if (r.name == "Obstacle") r.sharedMaterial = trim;
                }

            // 천장과 벽 기둥·보로 시작 화면 복도 같은 깊이감
            var dressing = new GameObject("Dressing").transform;
            Box("Ceiling", dressing, new Vector3(0, 5.1f, 2), new Vector3(24, 0.2f, 26), wall, collider: true);
            for (float z = -8.5f; z <= 13f; z += 4.3f)
            {
                Box("PillarL", dressing, new Vector3(-11.7f, 2.5f, z), new Vector3(0.3f, 5f, 0.5f), trim, collider: true);
                Box("PillarR", dressing, new Vector3(11.7f, 2.5f, z), new Vector3(0.3f, 5f, 0.5f), trim, collider: true);
                Box("Beam", dressing, new Vector3(0, 4.85f, z), new Vector3(24, 0.3f, 0.4f), trim);
            }

            // 해 조명은 약한 보조광으로만 남긴다 (천장이 생겨 그림자는 끈다)
            foreach (var light in roomLights.Where(l => l.type == LightType.Directional))
            {
                light.intensity = 0.15f;
                light.shadows = LightShadows.None;
            }

            // 기존 천장 등은 형광등으로, 시작 지점 쪽에 두 개 추가. 가운데 하나는 고장나서 깜빡인다.
            var lamps = roomLights.Where(l => l.type == LightType.Point).ToList();
            var added = new System.Collections.Generic.List<Light>();
            foreach (var spot in new[] { new Vector3(-6f, 0, -5f), new Vector3(6f, 0, -5f) })
            {
                var light = new GameObject("CeilingLight", typeof(Light)).GetComponent<Light>();
                light.transform.SetParent(dressing);
                light.transform.position = spot;
                light.type = LightType.Point;
                lamps.Add(light);
                added.Add(light);
            }
            for (int i = 0; i < lamps.Count; i++)
            {
                var light = lamps[i];
                bool broken = i == 1;
                var p = light.transform.position;
                light.transform.position = new Vector3(p.x, 4.6f, p.z);
                light.color = new Color(0.85f, 0.88f, 1f);
                light.intensity = broken ? 3f : 2.2f;
                light.range = 10f;
                light.shadows = broken ? LightShadows.Soft : LightShadows.None;
                var tube = Box("Tube", light.transform, new Vector3(0, 0.32f, 0), new Vector3(0.14f, 0.06f, 1.6f), bulb);
                var lamp = light.gameObject.AddComponent<LightFlicker>();
                Set(lamp, so =>
                {
                    so.FindProperty("bulb").objectReferenceValue = tube.GetComponent<Renderer>();
                    so.FindProperty("flicker").boolValue = broken;
                });
            }

            // 문 앞 색 조명: 젖은 바닥에 빨강·파랑이 번진다. 정전에도 꺼지지 않도록 roomLights에는 넣지 않는다.
            DoorSpill(dressing, DoorXs[0], new Color(0.85f, 0.1f, 0.12f));
            DoorSpill(dressing, DoorXs[1], new Color(0.12f, 0.3f, 0.95f));

            TitleSceneBuilder.MakeDust(new Vector3(0, 2.5f, 2f), new Vector3(24, 5, 26), 60f);

            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(GameVolumePath);
            if (!profile)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, GameVolumePath);
            }
            // 시작 화면 값에 맞춘다. 이미 있는 프로필도 매번 같은 값으로 덮어쓴다.
            var bloom = Override<Bloom>(profile);
            bloom.intensity.Override(1.2f);
            bloom.threshold.Override(0.6f);
            bloom.scatter.Override(0.75f);
            var vignette = Override<Vignette>(profile);
            vignette.intensity.Override(0.45f);
            vignette.smoothness.Override(0.45f);
            var grain = Override<FilmGrain>(profile);
            grain.type.Override(FilmGrainLookup.Medium3);
            grain.intensity.Override(0.3f);
            var aberration = Override<ChromaticAberration>(profile);
            aberration.intensity.Override(0.12f);
            var color = Override<ColorAdjustments>(profile);
            color.postExposure.Override(0.2f);
            color.contrast.Override(16f);
            color.saturation.Override(-12f);
            EditorUtility.SetDirty(profile);

            var volume = new GameObject("PostProcess", typeof(Volume)).GetComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = profile;
            return added.ToArray();
        }

        // CharacterTest의 NavMeshSurface는 데이터 연결이 끊겨 있어(복사본도 마찬가지) 좀비가 움직이지 못한다.
        // 꾸민 방(기둥·문 포함) 기준으로 새로 구워 우리 폴더에 저장한다. 원본 씬/에셋은 그대로 둔다.
        static void BakeNavMesh(PlayerCharacter player)
        {
            var surface = Object.FindFirstObjectByType<NavMeshSurface>();
            if (!surface) throw new InvalidOperationException("씬에서 NavMeshSurface를 찾지 못했습니다.");

            surface.collectObjects = CollectObjects.All; // 방 밖에 추가한 기둥·벽 조각도 반영
            surface.useGeometry = UnityEngine.AI.NavMeshCollectGeometry.PhysicsColliders;
            player.gameObject.SetActive(false); // 플레이어 캡슐이 바닥에 구멍을 내지 않도록
            try { surface.BuildNavMesh(); }
            finally { player.gameObject.SetActive(true); }

            if (AssetDatabase.LoadAssetAtPath<Object>(NavMeshPath)) AssetDatabase.DeleteAsset(NavMeshPath);
            AssetDatabase.CreateAsset(surface.navMeshData, NavMeshPath);
            EditorUtility.SetDirty(surface);
        }

        static void DoorSpill(Transform parent, float x, Color color)
        {
            var light = new GameObject("DoorSpill", typeof(Light)).GetComponent<Light>();
            light.transform.SetParent(parent);
            light.transform.position = new Vector3(x, 1.6f, DoorZ - 1.2f);
            light.type = LightType.Point;
            light.color = color;
            light.range = 6.5f;
            light.intensity = 2.5f;
        }

        static T Override<T>(VolumeProfile profile) where T : VolumeComponent
        {
            if (profile.TryGet<T>(out var existing)) return existing;
            return AddOverride<T>(profile);
        }

        internal static T AddOverride<T>(VolumeProfile profile) where T : VolumeComponent
        {
            var component = profile.Add<T>(true);
            component.name = typeof(T).Name;
            AssetDatabase.AddObjectToAsset(component, profile);
            return component;
        }

        // 캐릭터 테스트용 상단 제목 문구는 복사본 씬에서만 끈다
        static void HideTestHeader()
        {
            foreach (var text in Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None))
                if (text.text.Contains("CHARACTER TEST")) text.gameObject.SetActive(false);
        }

        /// <summary>빌드 씬 목록에 남아 있는 이 경로의 GUID (씬을 지우고 다시 만들기 전 값).</summary>
        internal static string BuildListGuid(string path) =>
            EditorBuildSettings.scenes.FirstOrDefault(s => s.path == path)?.guid.ToString();

        /// <summary>
        /// 다시 만든 씬의 GUID를 예전 값으로 되돌린다. Unity가 빌드 목록에서 경로별 예전 GUID를 붙들고 있어서,
        /// 새 GUID로 두면 빌드 목록과 어긋난다.
        /// </summary>
        internal static void KeepGuid(string path, string previousGuid)
        {
            string current = AssetDatabase.AssetPathToGUID(path);
            if (string.IsNullOrEmpty(previousGuid) || previousGuid == current) return;

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            string meta = path + ".meta";
            File.WriteAllText(meta, File.ReadAllText(meta).Replace(current, previousGuid));
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            EditorSceneManager.OpenScene(path);
        }

        /// <summary>빌드 씬 순서를 시작 화면 → 게임 → 나머지로 맞춘다. 없는 씬은 건너뛴다.</summary>
        internal static void ApplyBuildOrder()
        {
            var first = new[] { TitleSceneBuilder.ScenePath, ScenePath }.Where(File.Exists).ToArray();
            var builds = first.Select(p => new EditorBuildSettingsScene(p, true)).ToList();
            builds.AddRange(EditorBuildSettings.scenes.Where(s => !first.Contains(s.path)));
            EditorBuildSettings.scenes = builds.ToArray();
        }

        // 뒤쪽 벽(CharacterSceneBuilder 기준 z=15, 두께 0.3, 높이 5, x ±12)에 문 구멍을 낸다.
        static void CutBackWall(Transform parent, float[] doorXs)
        {
            var original = GameObject.Find("BackWall");
            if (!original) throw new InvalidOperationException("씬에서 BackWall을 찾지 못했습니다.");
            var material = original.GetComponent<Renderer>().sharedMaterial;
            original.SetActive(false);

            var root = new GameObject("BackWallWithDoors").transform;
            root.SetParent(parent);
            float left = -12f;
            foreach (float x in doorXs.OrderBy(v => v))
            {
                WallPiece(root, left, x - DoorHalfWidth, 0f, 5f, material);
                WallPiece(root, x - DoorHalfWidth, x + DoorHalfWidth, DoorHeight, 5f, material); // 문 위 인방
                left = x + DoorHalfWidth;
            }
            WallPiece(root, left, 12f, 0f, 5f, material);
        }

        static void WallPiece(Transform parent, float x0, float x1, float y0, float y1, Material material)
        {
            if (x1 - x0 < 0.01f) return;
            var wall = Box("Wall", parent, new Vector3((x0 + x1) / 2f, (y0 + y1) / 2f, 15f), new Vector3(x1 - x0, y1 - y0, 0.3f), material, collider: true);
            // 김우진의 벽 충돌체처럼 두께 1.2로 늘려 밀림으로 벽을 뚫지 않게 한다. 방 쪽 면은 그대로 두고 바깥으로만 늘린다.
            var box = wall.GetComponent<BoxCollider>();
            box.size = new Vector3(1f, 1f, 4f);
            box.center = new Vector3(0f, 0f, 1.5f);
        }

        static StageDoor MakeDoor(Transform parent, DoorColor color, float x, Color tint, StageManager manager)
        {
            var door = new GameObject($"{color}Door");
            door.transform.SetParent(parent);
            door.transform.position = new Vector3(x, 0, DoorZ);
            var trim = Mat("DoorFrame", new Color(0.12f, 0.12f, 0.13f));

            // 방 쪽 문틀
            Box("JambL", door.transform, new Vector3(-DoorHalfWidth - 0.1f, DoorHeight / 2f + 0.05f, -0.04f), new Vector3(0.2f, DoorHeight + 0.1f, 0.12f), trim, collider: true);
            Box("JambR", door.transform, new Vector3(DoorHalfWidth + 0.1f, DoorHeight / 2f + 0.05f, -0.04f), new Vector3(0.2f, DoorHeight + 0.1f, 0.12f), trim, collider: true);
            Box("Head", door.transform, new Vector3(0, DoorHeight + 0.1f, -0.04f), new Vector3(DoorHalfWidth * 2 + 0.4f, 0.2f, 0.12f), trim, collider: true);

            // 경첩(왼쪽 끝, 벽 두께 가운데)과 문짝. openAngle -90이면 통로 쪽으로 열린다.
            var hinge = new GameObject("Hinge").transform;
            hinge.SetParent(door.transform, false);
            hinge.localPosition = new Vector3(-DoorHalfWidth + 0.1f, 0, 0.15f);
            float leafWidth = DoorHalfWidth * 2 - 0.2f;
            var leaf = Box("Leaf", hinge, new Vector3(leafWidth / 2f, (DoorHeight - 0.1f) / 2f, 0), new Vector3(leafWidth, DoorHeight - 0.1f, 0.08f),
                Mat($"Door{color}", tint, tint * 0.8f), collider: true);

            // 문 뒤 짧은 통로. 문 색으로 빛나서 열리면 빛이 새어 나온다.
            const float depth = 2.9f;
            float mid = 0.3f + depth / 2f - 0.15f;
            var passage = new GameObject("Passage").transform;
            passage.SetParent(door.transform, false);
            var passageMat = Mat("PassageWall", new Color(0.06f, 0.06f, 0.07f));
            Box("Floor", passage, new Vector3(0, -0.15f, mid), new Vector3(DoorHalfWidth * 2 + 0.3f, 0.3f, depth), passageMat, collider: true);
            Box("Ceiling", passage, new Vector3(0, DoorHeight + 0.15f, mid), new Vector3(DoorHalfWidth * 2 + 0.3f, 0.3f, depth), passageMat, collider: true);
            Box("SideL", passage, new Vector3(-DoorHalfWidth - 0.15f, DoorHeight / 2f, mid), new Vector3(0.3f, DoorHeight, depth), passageMat, collider: true);
            Box("SideR", passage, new Vector3(DoorHalfWidth + 0.15f, DoorHeight / 2f, mid), new Vector3(0.3f, DoorHeight, depth), passageMat, collider: true);
            Box("End", passage, new Vector3(0, DoorHeight / 2f, 0.3f + depth - 0.1f), new Vector3(DoorHalfWidth * 2 + 0.3f, DoorHeight, 0.2f),
                Mat($"Passage{color}", tint * 0.2f, tint * 0.35f), collider: true);
            var glow = new GameObject("Glow", typeof(Light)).GetComponent<Light>();
            glow.transform.SetParent(passage, false);
            glow.transform.localPosition = new Vector3(0, DoorHeight - 0.8f, mid + 0.3f);
            glow.type = LightType.Point;
            glow.color = tint;
            glow.range = 4.5f;
            glow.intensity = 3f;

            // 통과 판정: 문지방을 넘어 통로 안쪽까지 들어와야 확정
            var trigger = door.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = new Vector3(0, 1.5f, 1.9f);
            trigger.size = new Vector3(DoorHalfWidth * 2 - 0.2f, 3f, 1.4f);

            var labelGo = new GameObject("Label", typeof(TextMeshPro));
            labelGo.transform.SetParent(door.transform, false);
            labelGo.transform.localPosition = new Vector3(0, DoorHeight + 0.95f, -0.12f);
            var label = labelGo.GetComponent<TextMeshPro>();
            label.rectTransform.sizeDelta = new Vector2(3.8f, 1.4f);
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
                so.FindProperty("hinge").objectReferenceValue = hinge;
                so.FindProperty("leaf").objectReferenceValue = leaf.GetComponent<Renderer>();
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

        internal static GameObject Box(string name, Transform parent, Vector3 localPosition, Vector3 size, Material material, bool collider = false)
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

        internal static Material Mat(string name, Color color, Color? emission = null, float? smoothness = null)
        {
            string path = $"{MaterialFolder}/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m) return m;

            m = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name };
            m.SetColor("_BaseColor", color);
            if (smoothness.HasValue) m.SetFloat("_Smoothness", smoothness.Value);
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

        internal static void Set(Object target, Action<SerializedObject> edit)
        {
            var so = new SerializedObject(target);
            edit(so);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        internal static void SetArray(SerializedProperty array, Object[] values)
        {
            array.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }

        internal static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int slash = path.LastIndexOf('/');
            EnsureFolder(path[..slash]);
            AssetDatabase.CreateFolder(path[..slash], path[(slash + 1)..]);
        }
    }
}
