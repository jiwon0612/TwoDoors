using System;
using TwoDoors.Characters;
using TwoDoors.Events;
using TwoDoors.Items;
using UnityEditor;
using UnityEngine;

namespace TwoDoors.EditorTools
{
    /// <summary>Regression checks for free healing trades and event completion rules.</summary>
    public static class EventBalanceValidation
    {
        [MenuItem("Tools/TwoDoors/Validate Event Balance")]
        public static void ValidateMenu() => Debug.Log(Validate());

        public static string Validate()
        {
            int checkedEvents = 0;
            foreach (var id in EventCatalogV2.Order)
            {
                var choice = AssetDatabase.LoadAssetAtPath<StageChoice>($"{EventCatalogV2.Root}/{id}.asset");
                Require(choice, id + " missing");
                foreach (var ev in new[] { choice.RedDoor, choice.BlueDoor })
                {
                    Require(ev, id + " missing event");
                    // Both sufficient-payment and empty-inventory routes must have a real cost.
                    foreach (bool supplied in new[] { false, true })
                    {
                        var ctx = new Context();
                        if (supplied) { ctx.Items[(int)ItemKind.Medkit] = 3; ctx.Items[(int)ItemKind.Ammo] = 56; }
                        float before = ctx.Health + ctx.Items[(int)ItemKind.Medkit] * 35;
                        ev.Begin(ctx);
                        if (ev is InstantEvent)
                        {
                            ev.OnCleared(ctx);
                            float after = ctx.Health + ctx.Items[(int)ItemKind.Medkit] * 35;
                            Require(after <= before || ctx.SpentAmmo > 0, ev.name + ": free net healing");
                        }
                        else if (ev is SurvivalEvent)
                        {
                            Require(!ev.IsCleared(ctx, 0), ev.name + ": premature clear");
                            Require(ev.IsCleared(ctx, ev.Duration), ev.name + ": timer did not clear");
                            ctx.AliveEnemyCount = 0;
                            Require(ev.IsCleared(ctx, 0), ev.name + ": wipe did not clear");
                        }
                    }
                    if (ev is HealZoneEvent)
                    {
                        var ctx = new Context { Health = 1 };
                        ev.Tick(ctx, 5, 5);
                        Require(Mathf.Approximately(ctx.Health, 41), "Healing zone must restore 8 HP per second without the old 32 HP cap");
                        ctx.Health = 1;
                        ev.Tick(ctx, 10, 5);
                        Require(Mathf.Approximately(ctx.Health, 41), "Healing must continue after taking damage again");
                    }
                    checkedEvents++;
                }
            }
            return $"PASS {checkedEvents} events: paid/insufficient inventory economy, timer/wipe, continuous healing";
        }

        static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }

        sealed class Context : IRoomContext
        {
            public readonly int[] Items = new int[5];
            public float Health = 100;
            public int SpentAmmo;
            public int AliveEnemyCount { get; set; }
            public bool IsPlayerAlive => Health > 0;
            public int PressedButtonCount => 0;
            public bool IsPlayerInHealZone => true;
            public void ChangeHealth(float value) => Health = Mathf.Clamp(Health + value, 0, 100);
            public void GiveItem(ItemKind kind, int amount) => Items[(int)kind] = Mathf.Min(PlayerInventory.Capacity(kind), Items[(int)kind] + amount);
            public bool SpendItem(ItemKind kind, int amount)
            {
                if (Items[(int)kind] < amount) return false;
                Items[(int)kind] -= amount;
                if (kind == ItemKind.Ammo) SpentAmmo += amount;
                return true;
            }
            public void SpawnZombies(EnemyCharacter prefab, int count) => AliveEnemyCount += count;
            public void SetLights(bool on) { }
            public void ActivateButtons(int count) { }
            public void SetHealZoneActive(bool active) { }
            public void ResetRoom() => AliveEnemyCount = 0;
        }
    }
}
