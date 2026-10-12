using UnityEngine;

namespace TwoDoors.UI
{
    /// <summary>
    /// 형광등. 발광 막대(bulb)를 조명 켜짐 상태와 맞추고, flicker가 켜져 있으면 고장난 듯 불규칙하게 깜빡인다.
    /// 정전 이벤트가 Light를 끄면 막대도 함께 꺼진다.
    /// </summary>
    [RequireComponent(typeof(Light))]
    public class LightFlicker : MonoBehaviour
    {
        [SerializeField] Renderer bulb;
        [SerializeField] bool flicker = true;
        [SerializeField, Range(0f, 1f)] float flickerChance = 0.08f;

        Light target;
        float baseIntensity;
        float nextChange;
        bool burst;

        void Awake()
        {
            target = GetComponent<Light>();
            baseIntensity = target.intensity;
        }

        void Update()
        {
            if (!target.enabled || !flicker)
            {
                if (bulb) bulb.enabled = target.enabled;
                return;
            }
            if (Time.time < nextChange) return;

            // 대부분은 켜져 있다가, 가끔 짧게 여러 번 끊긴다
            if (!burst && Random.value < flickerChance) burst = true;
            if (burst)
            {
                bool on = Random.value > 0.5f;
                Set(on ? baseIntensity * Random.Range(0.3f, 1f) : 0f);
                nextChange = Time.time + Random.Range(0.03f, 0.12f);
                if (Random.value < 0.15f) burst = false;
            }
            else
            {
                Set(baseIntensity * Random.Range(0.92f, 1f));
                nextChange = Time.time + Random.Range(0.1f, 0.4f);
            }
        }

        void Set(float intensity)
        {
            target.intensity = intensity;
            if (bulb) bulb.enabled = intensity > baseIntensity * 0.2f;
        }
    }
}
