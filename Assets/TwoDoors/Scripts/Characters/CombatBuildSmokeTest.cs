using System;
using System.Collections;
using TwoDoors.Items;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TwoDoors.Characters
{
    // Explicit command-line regression check for the actual Windows player.
    public sealed class CombatBuildSmokeTest : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Initialize()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "--combat-smoke-test") < 0) return;
            var go = new GameObject("CombatBuildSmokeTest");
            DontDestroyOnLoad(go); go.AddComponent<CombatBuildSmokeTest>();
        }
        IEnumerator Start()
        {
            yield return SceneManager.LoadSceneAsync("Game");
            yield return null;
            int result = 1;
            try
            {
                var p = FindFirstObjectByType<PlayerCharacter>();
                p.enabled = false; p.SetPaused(false);
                var controller = p.GetComponent<CharacterController>();
                controller.enabled = false; p.transform.SetPositionAndRotation(new Vector3(4, 0, -4), Quaternion.identity); controller.enabled = true;
                p.ViewCamera.transform.localRotation = Quaternion.identity;
                FindFirstObjectByType<TwoDoors.Events.RoomController>().SpawnZombies(null, 1);
                var enemy = FindFirstObjectByType<EnemyCharacter>();
                enemy.enabled = false;
                var agent = enemy.GetComponent<UnityEngine.AI.NavMeshAgent>();
                if (!agent.Warp(new Vector3(4, 0, -2))) throw new Exception("Enemy is not on NavMesh");
                agent.isStopped = true;
                p.Inventory.Add(ItemKind.Shotgun, 1); p.OnWeaponCollected(ItemKind.Shotgun);
                Physics.SyncTransforms();
                int before = enemy.CurrentHealth;
                if (!p.TryAttack() || before - enemy.CurrentHealth != 144 || p.Ammo != 5)
                    throw new Exception($"Shotgun damage={before-enemy.CurrentHealth}, ammo={p.Ammo}");
                foreach (string name in new[] { "HitSpark", "EnemyBolt" })
                {
                    var material = Resources.Load<Material>("Combat/" + name);
                    if (!material || !material.shader) throw new Exception("Missing built material: " + name);
                }
                EnemyBolt.Launch(enemy, p, new Vector3(4, 1.2f, -2), Vector3.forward, 20);
                if (!FindFirstObjectByType<EnemyBolt>()) throw new Exception("Projectile creation failed");
                Debug.Log("COMBAT_SMOKE_PASS: shotgun=144, ammo=5, hit effects and projectile materials included, NavMesh valid");
                result = 0;
            }
            catch (Exception e) { Debug.LogError("COMBAT_SMOKE_FAIL: " + e); }
            Application.Quit(result);
        }
    }
}
