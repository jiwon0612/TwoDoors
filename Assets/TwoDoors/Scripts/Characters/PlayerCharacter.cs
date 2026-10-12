using UnityEngine;
using UnityEngine.InputSystem;
using TwoDoors.Items;

namespace TwoDoors.Characters
{
    [RequireComponent(typeof(CharacterController), typeof(PlayerInventory))]
    public sealed class PlayerCharacter : CharacterBase
    {
        [SerializeField] private Camera viewCamera;
        [SerializeField] private Transform weaponRoot;
        [SerializeField] private GameObject pistol;
        [SerializeField] private GameObject knife;
        [SerializeField] private Light flashlight;
        [SerializeField] private Light muzzleFlash;
        [SerializeField] private float walkSpeed = 3.5f;
        [SerializeField] private float sprintSpeed = 5.5f;
        [SerializeField] private float lookSensitivity = 0.1f;
        [SerializeField] private int shotDamage = 25;
        [SerializeField] private int meleeDamage = 20;
        [SerializeField] private int magazineSize = 7;
        [SerializeField] private float reloadDuration = 1.3f;
        private CharacterController motor;
        private Animator bodyAnimator;
        private Vector3 weaponOrigin;
        private float pitch, verticalSpeed, attackReady, reloadUntil, flashUntil, recoil;
        public Camera ViewCamera => viewCamera;
        public int Ammo { get; private set; } = 7;
        public PlayerInventory Inventory { get; private set; }
        public int ReserveAmmo => Inventory.Count(ItemKind.Ammo);
        public int SelectedSlot { get; private set; } = 1;
        public int Medkits => Inventory.Count(ItemKind.Medkit);
        public bool IsReloading => reloadUntil > 0;
        public bool FlashlightOn => flashlight && flashlight.enabled;
        public bool IsPaused { get; private set; }
        public float LastHitTime { get; private set; } = -10;

        protected override void Awake()
        {
            base.Awake();
            motor = GetComponent<CharacterController>();
            Inventory = GetComponent<PlayerInventory>();
            Inventory.Initialize();
            bodyAnimator = GetComponentInChildren<Animator>();
            if (!viewCamera) viewCamera = GetComponentInChildren<Camera>();
            if (weaponRoot) weaponOrigin = weaponRoot.localPosition;
            Ammo = Inventory.Count(ItemKind.Pistol) > 0 ? magazineSize : 0;
            if (muzzleFlash) muzzleFlash.enabled = false;
            SelectSlot(Inventory.Count(ItemKind.Pistol) > 0 ? 1 : 0);
            if (flashlight) flashlight.enabled = false;
        }

        private void Start() => SetPaused(false);

        private void Update()
        {
            if (!IsDead && !IsPaused)
            {
                if (reloadUntil > 0 && Time.time >= reloadUntil)
                {
                    int count = Mathf.Min(magazineSize - Ammo, ReserveAmmo);
                    Ammo += count;
                    if (count > 0) Inventory.Consume(ItemKind.Ammo, count);
                    reloadUntil = 0;
                }
                if (muzzleFlash) muzzleFlash.enabled = Time.time < flashUntil;
            }
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            if (keyboard == null || mouse == null) return;
            if (keyboard.escapeKey.wasPressedThisFrame && !IsDead)
            {
                if (Inventory.IsOpen) Inventory.Close(); else SetPaused(!IsPaused);
            }
            if (IsDead || IsPaused || Cursor.lockState != CursorLockMode.Locked) return;

            Vector2 look = mouse.delta.ReadValue() * lookSensitivity;
            transform.Rotate(0, look.x, 0);
            pitch = Mathf.Clamp(pitch - look.y, -80, 80);
            viewCamera.transform.localRotation = Quaternion.Euler(pitch, 0, 0);
            Vector2 move = new Vector2((keyboard.dKey.isPressed ? 1 : 0) - (keyboard.aKey.isPressed ? 1 : 0),
                (keyboard.wKey.isPressed ? 1 : 0) - (keyboard.sKey.isPressed ? 1 : 0));
            move = Vector2.ClampMagnitude(move, 1);
            if (motor.isGrounded && verticalSpeed < 0) verticalSpeed = -2;
            verticalSpeed += Physics.gravity.y * Time.deltaTime;
            motor.Move(((transform.right * move.x + transform.forward * move.y) *
                (keyboard.leftShiftKey.isPressed ? sprintSpeed : walkSpeed) + Vector3.up * verticalSpeed) * Time.deltaTime);
            if (bodyAnimator) bodyAnimator.SetBool("Moving", move.sqrMagnitude > 0.01f);

            if (keyboard.digit1Key.wasPressedThisFrame) SelectSlot(0);
            if (keyboard.digit2Key.wasPressedThisFrame) SelectSlot(1);
            if (keyboard.digit3Key.wasPressedThisFrame) UseMedkit();
            if (keyboard.digit4Key.wasPressedThisFrame || keyboard.fKey.wasPressedThisFrame)
                ToggleFlashlight();
            if (keyboard.rKey.wasPressedThisFrame) Reload();
            if (mouse.leftButton.wasPressedThisFrame) TryAttack();
            recoil = Mathf.MoveTowards(recoil, 0, Time.deltaTime * 3);
            if (weaponRoot)
            {
                float bob = motor.isGrounded ? Mathf.Sin(Time.time * 9) * move.magnitude * 0.012f : 0;
                weaponRoot.localPosition = weaponOrigin + new Vector3(0, bob - (IsReloading ? 0.16f : 0), -recoil * 0.08f);
                weaponRoot.localRotation = Quaternion.Euler(-recoil * 14, 0, SelectedSlot == 0 ? recoil * -40 : 0);
            }
            if (muzzleFlash) muzzleFlash.enabled = Time.time < flashUntil;
        }

        public void SelectSlot(int slot)
        {
            if (IsDead) return;
            if (slot < 0 || slot > 1 || Inventory.Count(slot == 0 ? ItemKind.Knife : ItemKind.Pistol) == 0) return;
            SelectedSlot = Mathf.Clamp(slot, 0, 1);
            reloadUntil = 0;
            if (pistol) pistol.SetActive(SelectedSlot == 1);
            if (knife) knife.SetActive(SelectedSlot == 0);
        }

        public bool TryAttack()
        {
            if (IsDead || IsPaused || !viewCamera || IsReloading || Time.time < attackReady) return false;
            bool shooting = SelectedSlot == 1;
            if (shooting && Ammo == 0) { Reload(); return false; }
            if (shooting) { Ammo--; flashUntil = Time.time + 0.055f; }
            attackReady = Time.time + (shooting ? 0.25f : 0.65f);
            recoil = 1;
            if (!shooting && bodyAnimator) bodyAnimator.SetTrigger("Attack");
            if (Physics.Raycast(viewCamera.transform.position, viewCamera.transform.forward, out RaycastHit hit,
                shooting ? 60 : 2.3f, ~0, QueryTriggerInteraction.Ignore))
            {
                var enemy = hit.collider.GetComponentInParent<EnemyCharacter>();
                if (enemy && enemy.TakeDamage(shooting ? shotDamage : meleeDamage) > 0) LastHitTime = Time.time;
            }
            return true;
        }

        public void Reload()
        {
            if (!IsDead && !IsPaused && SelectedSlot == 1 && !IsReloading && Ammo < magazineSize && ReserveAmmo > 0)
                reloadUntil = Time.time + reloadDuration;
        }

        public void UseMedkit()
        {
            if (IsDead || (IsPaused && !Inventory.IsOpen)) return;
            if (Medkits == 0) { Inventory.Notify("No medkits"); return; }
            int recovered = Heal(35);
            if (recovered == 0) { Inventory.Notify("Health is already full"); return; }
            Inventory.Consume(ItemKind.Medkit, 1);
            Inventory.Notify($"Recovered {recovered} HP");
        }

        public void OnPistolCollected() { Ammo = magazineSize; SelectSlot(1); }

        public void ToggleFlashlight()
        {
            if (IsDead || (IsPaused && !Inventory.IsOpen)) return;
            if (Inventory.Count(ItemKind.Flashlight) == 0) { Inventory.Notify("Find a flashlight first"); return; }
            if (flashlight) flashlight.enabled = !flashlight.enabled;
        }

        public void SetPaused(bool paused)
        {
            IsPaused = paused;
            Time.timeScale = paused ? 0 : 1;
            Cursor.lockState = paused ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = paused;
        }

        protected override void OnDeath()
        {
            reloadUntil = 0;
            if (muzzleFlash) muzzleFlash.enabled = false;
            if (bodyAnimator) bodyAnimator.SetBool("Dead", true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void OnDisable()
        {
            Time.timeScale = 1;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
