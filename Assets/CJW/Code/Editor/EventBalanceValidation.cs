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
            int supplyEvents = 0, killEvents = 0, survivalEvents = 0;
            var soloTypes = new System.Collections.Generic.HashSet<Type>();
            int mixedEvents = 0;
            foreach (var id in EventCatalogV2.Order)
            {
                var choice = AssetDatabase.LoadAssetAtPath<StageChoice>($"{EventCatalogV2.Root}/{id}.asset");
                Require(choice, id + " missing");
                foreach (var ev in new[] { choice.RedDoor, choice.BlueDoor })
                {
                    Require(ev, id + " missing event");
                    var settings = new SerializedObject(ev);
                    Require(settings.FindProperty("clearItems").arraySize == 0, ev.name + ": completion reward remains");
                    Require(!ev.GetDescription().Contains("[섬멸]") && !ev.GetDescription().Contains("한도"), ev.name + ": unwanted door annotation");
                    bool suppliesWeapon = false;
                    foreach (var field in new[] { "entryItems", "clearItems" })
                    {
                        var grants = settings.FindProperty(field);
                        for (int i = 0; i < grants.arraySize; i++)
                        {
                            var kind = (ItemKind)grants.GetArrayElementAtIndex(i).FindPropertyRelative("kind").enumValueIndex;
                            Require(!(ev is InstantEvent && ev.HealthChange < 0 && (kind == ItemKind.Pistol || kind == ItemKind.MachineGun || kind == ItemKind.Shotgun)), ev.name + ": health toll must not grant a pistol");
                            suppliesWeapon |= PlayerInventory.IsWeapon(kind) || kind == ItemKind.Ammo || kind == ItemKind.Medkit;
                        }
                    }
                    if (suppliesWeapon) supplyEvents++;
                    Require(ev.EnemyCount > 0 && ev.Duration == 0 && !ev.SkipNextStage, ev.name + ": every room requires combat");
                    var enemyGroups = settings.FindProperty("enemyGroups");
                    for (int g = 0; g < enemyGroups.arraySize; g++)
                        Require(enemyGroups.GetArrayElementAtIndex(g).FindPropertyRelative("prefab").objectReferenceValue is ZombieEnemy or GolemEnemy or MannequinEnemy, "Non-threatening enemy in catalog");
                    if (ev is SurvivalEvent combat)
                    {
                        var groups = settings.FindProperty("enemyGroups");
                        if (groups.arraySize == 1)
                            soloTypes.Add(groups.GetArrayElementAtIndex(0).FindPropertyRelative("prefab").objectReferenceValue.GetType());
                        else mixedEvents++;
                        if (combat.RequireAllKills) killEvents++;
                        else
                        {
                            survivalEvents++;
                            Require(ev.Duration > 0 && ev.Duration <= 40, ev.name + ": survival exceeds 40 seconds");
                            Require(ev.EnemyCount >= (groups.arraySize == 1 ? 6 : 10), ev.name + ": survival wave too small");
                        }
                    }
                    // Both sufficient-payment and empty-inventory routes must have a real cost.
                    foreach (bool supplied in new[] { false, true })
                    {
                        var ctx = new Context();
                        if (supplied) { ctx.Items[(int)ItemKind.Medkit] = 3; ctx.Items[(int)ItemKind.Ammo] = 56; }
                        float before = ctx.Health + ctx.Items[(int)ItemKind.Medkit] * 35;
                        ev.Begin(ctx);
                        var entrySupplies = settings.FindProperty("entryItems");
                        for (int itemIndex = 0; itemIndex < entrySupplies.arraySize; itemIndex++)
                        {
                            var grant = entrySupplies.GetArrayElementAtIndex(itemIndex);
                            var kind = (ItemKind)grant.FindPropertyRelative("kind").enumValueIndex;
                            Require(ctx.Items[(int)kind] > 0, ev.name + ": entry supply missing");
                        }
                        var beforeClear = (int[])ctx.Items.Clone();
                        ev.OnCleared(ctx);
                        for (int itemIndex = 0; itemIndex < beforeClear.Length; itemIndex++)
                            Require(beforeClear[itemIndex] == ctx.Items[itemIndex], ev.name + ": completion awarded items twice");
                        Require(!ev.IsCleared(ctx, 99999), ev.name + ": time bypassed combat");
                        ctx.AliveEnemyCount = 1;
                        Require(!ev.IsCleared(ctx, 99999), ev.name + ": last enemy not required");
                        ctx.AliveEnemyCount = 0;
                        Require(ev.IsCleared(ctx, 0), ev.name + ": wipe did not clear");
                        if (ev is InstantEvent)
                        {
                            ev.OnCleared(ctx);
                            float after = ctx.Health + ctx.Items[(int)ItemKind.Medkit] * 35;
                            Require(after <= before || ctx.SpentAmmo > 0, ev.name + ": free net healing");
                        }
                        else if (ev is SurvivalEvent survival)
                        {
                            ctx.AliveEnemyCount = ev.EnemyCount;
                            Require(!ev.IsCleared(ctx, 0), ev.name + ": premature clear");
                            if (survival.RequireAllKills)
                            {
                                Require(ev.Duration == 0, ev.name + ": kill event has timer");
                                ctx.AliveEnemyCount = 1;
                                Require(!ev.IsCleared(ctx, 9999), ev.name + ": kill event cleared with one survivor");
                            }
                            else Require(ev.IsCleared(ctx, ev.Duration), ev.name + ": timer did not clear");
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
            Require(supplyEvents == 10, "Health/weapon/ammo grants should occur in 10 of 18 choices (previously 6)");
            Require(soloTypes.Contains(typeof(ZombieEnemy)) && soloTypes.Contains(typeof(GolemEnemy)) && soloTypes.Contains(typeof(MannequinEnemy)), "Each remaining enemy needs a single-type event");
            Require(mixedEvents > 0, "Keep mixed combat events");
            Require(killEvents > 0 && survivalEvents == 0, "Only elimination objectives may remain");
            ValidateDifficulty();
            return $"PASS {checkedEvents} elimination events, zombie/golem/ranged mannequin, {supplyEvents} item choices, 9 difficulty tiers and catalog immutability";
        }

        static void ValidateDifficulty()
        {
            foreach (var id in EventCatalogV2.Order)
            {
                var stage = AssetDatabase.LoadAssetAtPath<StageChoice>($"{EventCatalogV2.Root}/{id}.asset");
                foreach (var source in new[] { stage.RedDoor, stage.BlueDoor })
                {
                    string before = EditorJsonUtility.ToJson(source);
                    int lastCount = 0;
                    float lastDuration = 0;
                    for (int tier = 0; tier <= 8; tier++)
                    {
                        var ev = source.CreateForDifficulty(tier);
                        try
                        {
                            Require(ev.EnemyCount >= lastCount && ev.EnemyCount <= 14, ev.name + ": invalid enemy progression");
                            Require(ev.Duration >= lastDuration && ev.Duration <= 40, ev.name + ": invalid timer progression");
                            var ctx = new Context();
                            ev.Begin(ctx);
                            if (ev is SurvivalEvent survival)
                            {
                                Require(!ev.IsCleared(ctx, 0), ev.name + ": early clear");
                                Require(ev.IsCleared(ctx, 40) != survival.RequireAllKills, ev.name + ": incorrect timer objective");
                                ctx.AliveEnemyCount = 0;
                                Require(ev.IsCleared(ctx, 0), ev.name + ": scaled wipe failed");
                            }
                            if (ev is InstantEvent && ctx.IsPlayerAlive)
                            {
                                ev.OnCleared(ctx);
                                Require(ctx.Health + ctx.Items[(int)ItemKind.Medkit] * 35 <= 100, ev.name + ": scaled trade grants free healing");
                            }
                            lastCount = ev.EnemyCount;
                            lastDuration = ev.Duration;
                        }
                        finally { UnityEngine.Object.DestroyImmediate(ev); }
                    }
                    Require(EditorJsonUtility.ToJson(source) == before, "Difficulty mutated a catalog asset");
                }
                var swapped = stage.CreateForRun(0, true);
                Require(swapped.RedDoor.name == stage.BlueDoor.name && swapped.BlueDoor.name == stage.RedDoor.name, "Door color swap failed");
                UnityEngine.Object.DestroyImmediate(swapped.RedDoor);
                UnityEngine.Object.DestroyImmediate(swapped.BlueDoor);
                UnityEngine.Object.DestroyImmediate(swapped);
            }
        }

        static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }

        sealed class Context : IRoomContext
        {
            public readonly int[] Items = new int[8];
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
