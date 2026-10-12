using System;
using System.IO;
using TMPro;
using TwoDoors.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using static TwoDoors.EditorTools.GameSceneBuilder;
using Object = UnityEngine.Object;

namespace TwoDoors.EditorTools
{
    /// <summary>
    /// 시작 화면 씬 생성. 안개 낀 어두운 복도 끝에 빨간 문과 파란 문이 빛나고,
    /// 문을 고르는 것이 곧 메뉴다(빨강 = 시작, 파랑 = 종료).
    /// </summary>
    static class TitleSceneBuilder
    {
        internal const string ScenePath = "Assets/CJW/Scenes/Title.unity";
        const string VolumePath = "Assets/CJW/Generated/TitleVolume.asset";

        static readonly Color Red = new(0.85f, 0.1f, 0.12f);
        static readonly Color Blue = new(0.12f, 0.3f, 0.95f);

        const float CorridorLength = 15f;
        const float HalfWidth = 3f;
        const float Height = 3.6f;

        [MenuItem("Tools/TwoDoors/Build Title Scene")]
        static void Build()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Play 모드를 먼저 종료하세요.");
            if (File.Exists(ScenePath))
                throw new InvalidOperationException($"{ScenePath}가 이미 있습니다. 다시 만들려면 먼저 삭제하세요.");
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            string previousGuid = BuildListGuid(ScenePath);

            EnsureFolder("Assets/CJW/Scenes");
            EnsureFolder(MaterialFolder);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            SetupAtmosphere();
            var camera = MakeCamera();
            MakeCorridor(out var panels, out var doorLights);
            MakeDust();
            MakeVolume();
            var screen = MakeUi(out var options, out var content, out var fade, out var title);

            Set(screen, so =>
            {
                so.FindProperty("viewCamera").objectReferenceValue = camera;
                SetArray(so.FindProperty("options"), options);
                SetArray(so.FindProperty("doorPanels"), panels);
                SetArray(so.FindProperty("doorLights"), doorLights);
                so.FindProperty("content").objectReferenceValue = content;
                so.FindProperty("fade").objectReferenceValue = fade;
                so.FindProperty("title").objectReferenceValue = title;
            });

            EditorSceneManager.SaveScene(scene, ScenePath);
            KeepGuid(ScenePath, previousGuid);
            ApplyBuildOrder();
            AssetDatabase.SaveAssets();
            Debug.Log($"시작 화면 생성 완료: {ScenePath} (빌드 첫 번째 씬)");
        }

        // ---------- 3D 배경 ----------

        static void SetupAtmosphere()
        {
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.07f, 0.07f, 0.085f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.015f, 0.015f, 0.02f);
            RenderSettings.fogDensity = 0.075f;
            RenderSettings.skybox = null;
            // 기본 하늘 반사를 약하게만 남겨 복도 윤곽은 보이되 바닥이 뿌옇지 않게
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;
            RenderSettings.reflectionIntensity = 0.4f;
        }

        static Camera MakeCamera()
        {
            var go = new GameObject("TitleCamera", typeof(Camera), typeof(AudioListener));
            go.tag = "MainCamera";
            go.transform.SetPositionAndRotation(new Vector3(0, 1.55f, 0.6f), Quaternion.Euler(2f, 0, 0));
            var camera = go.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.fieldOfView = 55;
            camera.nearClipPlane = 0.05f;
            var data = go.AddComponent<UniversalAdditionalCameraData>();
            data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.FastApproximateAntialiasing;
            return camera;
        }

        static void MakeCorridor(out Renderer[] panels, out Light[] doorLights)
        {
            var root = new GameObject("Corridor").transform;
            var wall = Mat("TitleWall", new Color(0.09f, 0.09f, 0.1f), smoothness: 0.15f);
            var floor = Mat("TitleFloor", new Color(0.015f, 0.015f, 0.018f), smoothness: 0.6f); // 젖은 바닥 반사
            var trim = Mat("TitleTrim", new Color(0.03f, 0.03f, 0.035f), smoothness: 0.4f);
            float mid = CorridorLength / 2f;

            Box("Floor", root, new Vector3(0, -0.1f, mid), new Vector3(HalfWidth * 2, 0.2f, CorridorLength + 2), floor);
            Box("Ceiling", root, new Vector3(0, Height + 0.1f, mid), new Vector3(HalfWidth * 2, 0.2f, CorridorLength + 2), wall);
            Box("LeftWall", root, new Vector3(-HalfWidth - 0.1f, Height / 2, mid), new Vector3(0.2f, Height, CorridorLength + 2), wall);
            Box("RightWall", root, new Vector3(HalfWidth + 0.1f, Height / 2, mid), new Vector3(0.2f, Height, CorridorLength + 2), wall);
            Box("EndWall", root, new Vector3(0, Height / 2, CorridorLength + 0.1f), new Vector3(HalfWidth * 2, Height, 0.2f), wall);

            // 깊이감을 주는 기둥과 천장 보
            for (float z = 2f; z < CorridorLength; z += 3f)
            {
                Box("PillarL", root, new Vector3(-HalfWidth + 0.15f, Height / 2, z), new Vector3(0.3f, Height, 0.4f), trim);
                Box("PillarR", root, new Vector3(HalfWidth - 0.15f, Height / 2, z), new Vector3(0.3f, Height, 0.4f), trim);
                Box("Beam", root, new Vector3(0, Height - 0.15f, z), new Vector3(HalfWidth * 2, 0.3f, 0.4f), trim);
            }

            // 천장 형광등. 가운데 하나는 고장나서 깜빡인다.
            var bulb = Mat("TitleBulb", new Color(0.9f, 0.9f, 0.8f), new Color(1.6f, 1.55f, 1.3f));
            float[] lamps = { 3.5f, 8f, 12.5f };
            for (int i = 0; i < lamps.Length; i++)
            {
                var lamp = new GameObject($"Lamp{i + 1}").transform;
                lamp.SetParent(root);
                lamp.position = new Vector3(0, Height - 0.05f, lamps[i]);
                var tube = Box("Tube", lamp, Vector3.zero, new Vector3(0.12f, 0.06f, 1.2f), bulb);
                var light = lamp.gameObject.AddComponent<Light>();
                light.type = LightType.Point;
                light.color = new Color(0.85f, 0.88f, 1f);
                light.range = 5.5f;
                light.intensity = i == 1 ? 1.6f : 0.6f;
                // 그림자는 깜빡이는 등 하나만 (포인트 라이트 그림자는 6장씩 들어 비싸다)
                light.shadows = i == 1 ? LightShadows.Soft : LightShadows.None;
                if (i == 1)
                {
                    var flicker = lamp.gameObject.AddComponent<LightFlicker>();
                    Set(flicker, so => so.FindProperty("bulb").objectReferenceValue = tube.GetComponent<Renderer>());
                }
            }

            panels = new Renderer[2];
            doorLights = new Light[2];
            for (int i = 0; i < 2; i++)
            {
                bool red = i == 0;
                var color = red ? Red : Blue;
                var door = new GameObject(red ? "RedDoor" : "BlueDoor").transform;
                door.SetParent(root);
                door.position = new Vector3(red ? -1.35f : 1.35f, 0, CorridorLength);

                Box("Frame", door, new Vector3(0, 1.3f, -0.05f), new Vector3(1.6f, 2.6f, 0.1f), trim);
                panels[i] = Box("Panel", door, new Vector3(0, 1.2f, -0.12f), new Vector3(1.25f, 2.4f, 0.05f),
                    Mat(red ? "DoorRed" : "DoorBlue", color, color * 0.8f)).GetComponent<Renderer>();
                // 문틈으로 새어 나오는 빛
                Box("Leak", door, new Vector3(0, 0.005f, -0.35f), new Vector3(1.25f, 0.01f, 0.5f),
                    Mat(red ? "TitleLeakRed" : "TitleLeakBlue", color * 0.3f, color * 0.6f));

                var light = new GameObject("Glow", typeof(Light)).GetComponent<Light>();
                light.transform.SetParent(door, false);
                light.transform.localPosition = new Vector3(0, 1.2f, -0.8f);
                light.type = LightType.Point;
                light.color = color;
                light.range = 7f;
                light.intensity = 3.5f;
                doorLights[i] = light;
            }
        }

        static void MakeDust()
        {
            var go = new GameObject("Dust", typeof(ParticleSystem));
            go.transform.position = new Vector3(0, Height / 2, CorridorLength / 2);
            var ps = go.GetComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = 10f;
            main.startSpeed = 0.03f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.008f, 0.025f);
            main.startColor = new Color(1f, 1f, 1f, 0.35f);
            main.maxParticles = 400;
            main.prewarm = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = ps.emission;
            emission.rateOverTime = 35f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(HalfWidth * 2, Height, CorridorLength);
            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.05f;
            noise.frequency = 0.2f;

            string path = $"{MaterialFolder}/TitleDust.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!m)
            {
                m = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")) { name = "TitleDust" };
                AssetDatabase.CreateAsset(m, path);
                // 가산 블렌딩 반투명
                m.SetFloat("_Surface", 1f);
                m.SetFloat("_Blend", 2f);
                m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                m.SetFloat("_DstBlend", (float)BlendMode.One);
                m.SetFloat("_ZWrite", 0f);
                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                m.renderQueue = (int)RenderQueue.Transparent;
                EditorUtility.SetDirty(m);
            }
            go.GetComponent<ParticleSystemRenderer>().sharedMaterial = m;
        }

        static void MakeVolume()
        {
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumePath);
            if (!profile)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, VolumePath);

                var bloom = Add<Bloom>(profile);
                bloom.intensity.Override(1.4f);
                bloom.threshold.Override(0.85f);
                bloom.scatter.Override(0.75f);
                var vignette = Add<Vignette>(profile);
                vignette.intensity.Override(0.48f);
                vignette.smoothness.Override(0.45f);
                var grain = Add<FilmGrain>(profile);
                grain.type.Override(FilmGrainLookup.Medium3);
                grain.intensity.Override(0.35f);
                var aberration = Add<ChromaticAberration>(profile);
                aberration.intensity.Override(0.18f);
                var color = Add<ColorAdjustments>(profile);
                color.postExposure.Override(0.25f);
                color.contrast.Override(18f);
                color.saturation.Override(-12f);
            }

            var go = new GameObject("PostProcess", typeof(Volume));
            var volume = go.GetComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = profile;
        }

        static T Add<T>(VolumeProfile profile) where T : VolumeComponent
        {
            var component = profile.Add<T>(true);
            component.name = typeof(T).Name;
            AssetDatabase.AddObjectToAsset(component, profile);
            return component;
        }

        // ---------- UI ----------

        static TitleScreen MakeUi(out TitleOption[] options, out CanvasGroup content, out CanvasGroup fade, out TMP_Text title)
        {
            var events = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            events.transform.SetAsLastSibling();

            var canvasGo = new GameObject("TitleCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            var screen = canvasGo.AddComponent<TitleScreen>();

            var contentRect = Rect("Content", canvasGo.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            content = contentRect.gameObject.AddComponent<CanvasGroup>();

            Text("Overline", contentRect, "TWO  DOORS", 26, new Color(0.6f, 0.6f, 0.65f), new Vector2(0.5f, 1), new Vector2(0, -130), FontStyles.Normal, spacing: 40);
            title = Text("Title", contentRect, "두 개의 문", 128, Color.white, new Vector2(0.5f, 1), new Vector2(0, -235), FontStyles.Bold, spacing: 14);
            Text("Tagline", contentRect, "들어서는 순간, 선택은 되돌릴 수 없다.", 30, new Color(0.7f, 0.7f, 0.74f), new Vector2(0.5f, 1), new Vector2(0, -345), FontStyles.Normal);

            // 빨강/파랑이 맞닿은 짧은 구분선
            var split = Rect("Split", contentRect, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -400), new Vector2(280, 3));
            var left = Rect("Red", split, new Vector2(0, 0), new Vector2(0.5f, 1), Vector2.zero, Vector2.zero);
            Img(left, Red, false);
            var right = Rect("Blue", split, new Vector2(0.5f, 0), new Vector2(1, 1), Vector2.zero, Vector2.zero);
            Img(right, Blue, false);

            options = new[]
            {
                Option(contentRect, "들어간다", "빨간 문  ·  게임 시작", Red, -360),
                Option(contentRect, "돌아선다", "파란 문  ·  게임 종료", Blue, 360),
            };

            Text("Hint", contentRect, "A / D  선택      Enter  결정", 22, new Color(0.5f, 0.5f, 0.55f), new Vector2(0.5f, 0), new Vector2(0, 55), FontStyles.Normal, spacing: 6);
            Text("Rule", contentRect, "체력 100  ·  8단계  ·  살아서 탈출하라", 20, new Color(0.45f, 0.45f, 0.5f), new Vector2(0, 0), new Vector2(60, 55), FontStyles.Normal, TextAlignmentOptions.Left);
            Text("Credits", contentRect, "윤완희  ·  김우진  ·  최지원", 20, new Color(0.45f, 0.45f, 0.5f), new Vector2(1, 0), new Vector2(-60, 55), FontStyles.Normal, TextAlignmentOptions.Right);

            var fadeRect = Rect("Fade", canvasGo.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Img(fadeRect, Color.black, false);
            fade = fadeRect.gameObject.AddComponent<CanvasGroup>();
            fade.blocksRaycasts = false;
            fade.interactable = false;
            return screen;
        }

        static TitleOption Option(RectTransform parent, string label, string caption, Color accent, float x)
        {
            var root = Rect(label, parent, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(x, 210), new Vector2(460, 170));
            Img(root, new Color(0, 0, 0, 0), true); // 마우스 판정 영역

            var labelText = Text("Label", root, label, 64, Color.white, new Vector2(0.5f, 0.5f), new Vector2(0, 28), FontStyles.Bold, spacing: 4);
            var underline = Rect("Underline", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -22), new Vector2(240, 3));
            var line = Img(underline, accent, false);
            var captionText = Text("Caption", root, caption, 24, new Color(0.8f, 0.8f, 0.85f), new Vector2(0.5f, 0.5f), new Vector2(0, -58), FontStyles.Normal, spacing: 2);

            var option = root.gameObject.AddComponent<TitleOption>();
            Set(option, so =>
            {
                so.FindProperty("label").objectReferenceValue = labelText;
                so.FindProperty("caption").objectReferenceValue = captionText;
                so.FindProperty("underline").objectReferenceValue = line;
                so.FindProperty("accent").colorValue = accent;
            });
            return option;
        }

        static RectTransform Rect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        static Image Img(RectTransform rect, Color color, bool raycast)
        {
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = raycast;
            return image;
        }

        static TextMeshProUGUI Text(string name, Transform parent, string text, float size, Color color, Vector2 anchor, Vector2 position,
            FontStyles style, TextAlignmentOptions alignment = TextAlignmentOptions.Center, float spacing = 0)
        {
            var rect = Rect(name, parent, anchor, anchor, position, new Vector2(1400, size * 1.6f));
            rect.pivot = new Vector2(anchor.x, 0.5f);
            var tmp = rect.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.fontStyle = style;
            tmp.alignment = alignment;
            tmp.characterSpacing = spacing;
            tmp.raycastTarget = false;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            return tmp;
        }
    }
}
