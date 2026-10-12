using UnityEngine;

namespace TwoDoors.UI
{
    /// <summary>고장난 형광등처럼 불규칙하게 깜빡이는 조명. 같은 오브젝트의 발광 렌더러도 함께 끈다.</summary>
    [RequireComponent(typeof(Light))]
    public class LightFlicker : MonoBehaviour
    {
        [SerializeField] Renderer bulb;
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
