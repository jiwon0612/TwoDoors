using UnityEngine;
using TwoDoors.Items;

namespace TwoDoors.Characters
{
    public sealed class CombatImpact : MonoBehaviour
    {
        static Material sparkMaterial;
        static AudioClip shotClip;
        float age;
        Vector3 velocity;

        public static void Emit(Vector3 point, Vector3 normal, bool kill)
        {
            if (!sparkMaterial)
            {
                sparkMaterial = Resources.Load<Material>("Combat/HitSpark");
                // Visual failures must never interrupt the remaining shotgun pellets.
                if (!sparkMaterial) return;
            }
            for (int i = 0; i < (kill ? 8 : 3); i++)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = "HitSpark";
                go.GetComponent<Collider>().enabled = false;
                go.transform.position = point + normal * 0.03f;
                go.transform.localScale = Vector3.one * (kill ? 0.055f : 0.035f);
                go.GetComponent<Renderer>().sharedMaterial = sparkMaterial;
                go.AddComponent<CombatImpact>().velocity = (normal + Random.insideUnitSphere) * 2f;
            }
        }

        public static void PlayShot(GameObject camera, ItemKind weapon)
        {
            if (!shotClip)
            {
                const int rate = 22050;
                var samples = new float[2205];
                var random = new System.Random(71);
                for (int i = 0; i < samples.Length; i++)
                    samples[i] = ((float)random.NextDouble() * 2 - 1) * Mathf.Exp(-i / 320f) * 0.5f;
                shotClip = AudioClip.Create("WeaponImpact", samples.Length, 1, rate, false);
                shotClip.SetData(samples, 0);
            }
            var audio = camera.GetComponent<AudioSource>();
            if (!audio) { audio = camera.AddComponent<AudioSource>(); audio.playOnAwake = false; audio.spatialBlend = 0; }
            audio.pitch = weapon == ItemKind.Shotgun ? 0.65f : weapon == ItemKind.MachineGun ? 1.2f : 0.95f;
            audio.PlayOneShot(shotClip, weapon == ItemKind.Machete || weapon == ItemKind.Knife ? 0.15f : 0.35f);
        }

        void Update()
        {
            age += Time.deltaTime;
            transform.position += velocity * Time.deltaTime;
            transform.localScale *= Mathf.Exp(-Time.deltaTime * 8);
            if (age > 0.2f) Destroy(gameObject);
        }
    }
}
