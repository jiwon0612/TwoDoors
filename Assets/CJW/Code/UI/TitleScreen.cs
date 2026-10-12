using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace TwoDoors.UI
{
    /// <summary>
    /// 시작 화면. 복도 끝 두 문 중 하나를 고르는 것 자체가 메뉴다.
    /// 빨간 문 = 게임 시작, 파란 문 = 종료. A/D·←/→로 고르고 Enter/Space, 또는 마우스로 클릭.
    /// </summary>
    public class TitleScreen : MonoBehaviour
    {
        public const string TitleScene = "Title";
        public const string GameScene = "Game";

        [Header("카메라")]
        [SerializeField] Camera viewCamera;
        [SerializeField] float driftSpeed = 0.35f;
        [SerializeField] float leanAngle = 3.5f;

        [Header("문 (0 = 빨강/시작, 1 = 파랑/종료)")]
        [SerializeField] TitleOption[] options;
        [SerializeField] Renderer[] doorPanels;
        [SerializeField] Light[] doorLights;

        [Header("화면")]
        [SerializeField] CanvasGroup content;
        [SerializeField] CanvasGroup fade;
        [SerializeField] TMP_Text title;

        int selected;
        bool leaving;
        float introTime;
        Vector3 cameraHome;
        Quaternion cameraHomeRotation;
        Color[] panelEmission;
        float[] lightIntensity;
        MaterialPropertyBlock block;

        static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

        void Awake()
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            cameraHome = viewCamera.transform.position;
            cameraHomeRotation = viewCamera.transform.rotation;
            block = new MaterialPropertyBlock();
            panelEmission = new Color[doorPanels.Length];
            for (int i = 0; i < doorPanels.Length; i++) panelEmission[i] = doorPanels[i].sharedMaterial.GetColor(EmissionColor);
            lightIntensity = new float[doorLights.Length];
            for (int i = 0; i < doorLights.Length; i++) lightIntensity[i] = doorLights[i].intensity;

            for (int i = 0; i < options.Length; i++)
            {
                int index = i;
                options[i].Hovered += () => Select(index);
                options[i].Clicked += () => Confirm(index);
            }

            content.alpha = 0f;
            fade.alpha = 1f;
            Select(0);
        }

        void Update()
        {
            introTime += Time.deltaTime;
            if (!leaving)
            {
                fade.alpha = 1f - Mathf.Clamp01(introTime / 1.5f);
                content.alpha = Mathf.Clamp01((introTime - 0.6f) / 1.2f);
                // 제목 글자 간격이 서서히 좁혀지며 등장
                title.characterSpacing = Mathf.Lerp(60f, 14f, Mathf.SmoothStep(0f, 1f, introTime / 2.5f));
                HandleKeyboard();
                Drift();
            }

            for (int i = 0; i < doorPanels.Length; i++)
            {
                bool on = i == selected;
                float pulse = 1f + Mathf.Sin(Time.time * (on ? 3.2f : 1.3f) + i) * (on ? 0.25f : 0.08f);
                float strength = (on ? 1.6f : 0.55f) * pulse * (leaving && on ? 1f + introTime * 2f : 1f);
                block.SetColor(EmissionColor, panelEmission[i] * strength);
                doorPanels[i].SetPropertyBlock(block);
                if (i < doorLights.Length)
                    doorLights[i].intensity = Mathf.Lerp(doorLights[i].intensity, lightIntensity[i] * strength, Time.deltaTime * 6f);
            }
        }

        void HandleKeyboard()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;
            if (keyboard.aKey.wasPressedThisFrame || keyboard.leftArrowKey.wasPressedThisFrame) Select(0);
            if (keyboard.dKey.wasPressedThisFrame || keyboard.rightArrowKey.wasPressedThisFrame) Select(1);
            if (keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame) Confirm(selected);
        }

        void Drift()
        {
            // 숨 쉬듯 흔들리며, 고른 문 쪽으로 살짝 고개를 돌린다
            float t = Time.time * driftSpeed;
            var sway = new Vector3(Mathf.Sin(t) * 0.06f, Mathf.Sin(t * 1.7f) * 0.03f, 0f);
            viewCamera.transform.position = Vector3.Lerp(viewCamera.transform.position, cameraHome + sway, Time.deltaTime * 2f);
            var lean = Quaternion.Euler(Mathf.Sin(t * 1.3f) * 0.4f, (selected == 0 ? -1f : 1f) * leanAngle, 0f);
            viewCamera.transform.rotation = Quaternion.Slerp(viewCamera.transform.rotation, cameraHomeRotation * lean, Time.deltaTime * 2.5f);
        }

        void Select(int index)
        {
            if (leaving) return;
            selected = index;
            for (int i = 0; i < options.Length; i++) options[i].SetSelected(i == index);
        }

        void Confirm(int index)
        {
            if (leaving || introTime < 0.8f) return;
            Select(index);
            leaving = true;
            introTime = 0f;
            StartCoroutine(EnterDoor(index));
        }

        IEnumerator EnterDoor(int index)
        {
            var target = doorPanels[index].transform.position;
            var start = viewCamera.transform.position;
            var startRotation = viewCamera.transform.rotation;
            var lookRotation = Quaternion.LookRotation(target - start);

            for (float t = 0f; t < 1f; t += Time.deltaTime / 1.4f)
            {
                float e = t * t * (3f - 2f * t);
                viewCamera.transform.SetPositionAndRotation(
                    Vector3.Lerp(start, target - (target - start).normalized * 0.4f, e * e),
                    Quaternion.Slerp(startRotation, lookRotation, e));
                content.alpha = 1f - e * 2f;
                fade.alpha = Mathf.Clamp01((t - 0.45f) / 0.55f);
                yield return null;
            }
            fade.alpha = 1f;

            if (index == 0)
            {
                SceneManager.LoadScene(GameScene);
            }
            else
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            }
        }

        /// <summary>게임 중 시작 화면으로 돌아갈 때 사용.</summary>
        public static void LoadTitle()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(TitleScene);
        }
    }
}
