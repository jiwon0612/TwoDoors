using System;
using TwoDoors.Characters;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TwoDoors.Items
{
    [DisallowMultipleComponent]
    public sealed class PlayerInventory : MonoBehaviour
    {
        [SerializeField] private bool startWithPistol = true;
        [SerializeField] private bool startWithFlashlight = true;
        [SerializeField, Range(0, 210)] private int startingAmmo = 30;
        [SerializeField, Range(0, 9)] private int startingMedkits = 1;
        private readonly int[] counts = new int[8];
        private bool initialized;
        private PlayerCharacter player;
        public bool IsOpen { get; private set; }
        public string Message { get; private set; } = "";
        public float MessageUntil { get; private set; }
        public ItemPickup Nearby { get; private set; }
        public event Action Changed;

        public void Initialize()
        {
            if (initialized) return;
            initialized = true;
            counts[0] = 1;
            counts[1] = startWithPistol ? 1 : 0;
            counts[2] = Mathf.Clamp(startingAmmo, 0, 210);
            counts[3] = Mathf.Clamp(startingMedkits, 0, 9);
            counts[4] = startWithFlashlight ? 1 : 0;
            player = GetComponent<PlayerCharacter>();
        }

        private void Awake() => Initialize();
        public int Count(ItemKind kind) { Initialize(); return counts[(int)kind]; }
        public static int Capacity(ItemKind kind) => kind == ItemKind.Ammo ? 210 : kind == ItemKind.Medkit ? 9 : 1;
        public static string Label(ItemKind kind) => new[] { "KNIFE", "PISTOL", "AMMO", "MEDKIT", "FLASHLIGHT", "MACHINE GUN", "SHOTGUN", "MACHETE" }[(int)kind];
        public static bool IsWeapon(ItemKind kind) => kind is ItemKind.Knife or ItemKind.Pistol or ItemKind.MachineGun or ItemKind.Shotgun or ItemKind.Machete;

        public int Add(ItemKind kind, int amount)
        {
            Initialize();
            int added = Mathf.Clamp(amount, 0, Capacity(kind) - Count(kind));
            if (added == 0) return 0;
            counts[(int)kind] += added;
            Changed?.Invoke();
            return added;
        }

        public bool Consume(ItemKind kind, int amount)
        {
            if (amount <= 0 || Count(kind) < amount) return false;
            counts[(int)kind] -= amount;
            Changed?.Invoke();
            return true;
        }

        private void Update()
        {
            if (!player || player.IsDead) { if (IsOpen) Close(); Nearby = null; return; }
            var keys = Keyboard.current;
            Nearby = null;
            if (player.IsPaused || !player.ViewCamera) return;
            var camera = player.ViewCamera.transform;
            if (Physics.Raycast(camera.position, camera.forward, out var hit, 2.8f, ~0, QueryTriggerInteraction.Ignore))
                Nearby = hit.collider.GetComponentInParent<ItemPickup>();
            if (Nearby && keys != null && keys.gKey.wasPressedThisFrame) Nearby.TryCollect(player);
        }

        public void Close()
        {
            IsOpen = false;
            if (player && !player.IsDead) player.SetPaused(false);
        }

        public void Notify(string message) { Message = message; MessageUntil = Time.unscaledTime + 2.5f; }

        public void Use(ItemKind kind)
        {
            if (!player || player.IsDead || !IsOpen || Count(kind) == 0) return;
            switch (kind)
            {
                case ItemKind.MachineGun:
                case ItemKind.Shotgun:
                case ItemKind.Machete: player.SelectSlot((int)kind); Notify(Label(kind) + " equipped"); break;
                case ItemKind.Knife: player.SelectSlot(0); Notify("Knife equipped"); break;
                case ItemKind.Pistol: player.SelectSlot(1); Notify("Pistol equipped"); break;
                case ItemKind.Medkit: player.UseMedkit(); break;
                case ItemKind.Flashlight: player.ToggleFlashlight(); break;
                case ItemKind.Ammo:
                    if (!player.IsFirearm)
                    {
                        if (Count(ItemKind.MachineGun) > 0) player.SelectSlot((int)ItemKind.MachineGun);
                        else if (Count(ItemKind.Shotgun) > 0) player.SelectSlot((int)ItemKind.Shotgun);
                        else if (Count(ItemKind.Pistol) > 0) player.SelectSlot(1);
                        else { Notify("Find a firearm first"); break; }
                    }
                    Close(); player.Reload(); break;
            }
        }
    }
}
