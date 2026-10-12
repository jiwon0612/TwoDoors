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
        [SerializeField] private GameObject machineGun;
        [SerializeField] private GameObject shotgun;
        [SerializeField] private GameObject machete;
        [SerializeField] private Light flashlight;
        [SerializeField] private Light muzzleFlash;
        [SerializeField] private float walkSpeed = 3.5f;
        [SerializeField] private float sprintSpeed = 9.8f;
        public float Stamina { get; private set; } = 1f;
        public bool IsSprinting { get; private set; }
        bool exhausted;

        void TickStamina(bool wantsSprint, float deltaTime)
        {
            IsSprinting = wantsSprint && !exhausted && Stamina > 0f;
            Stamina = Mathf.Clamp01(Stamina + deltaTime * (IsSprinting ? -1f / 5f : 1f / 5f));
            if (Stamina <= 0f) { exhausted = true; IsSprinting = false; }
            if (Stamina >= 1f) exhausted = false;
        }
        [SerializeField] private float lookSensitivity = 0.1f;
        [SerializeField] private int shotDamage = 50;
        [SerializeField] private int meleeDamage = 50;
        [SerializeField] private int magazineSize = 7;
        [SerializeField] private float reloadDuration = 1.3f;
        private CharacterController motor;
        private Animator bodyAnimator;
        private Vector3 weaponOrigin;
        private float pitch, verticalSpeed, attackReady, reloadUntil, flashUntil, recoil;
        public Camera ViewCamera => viewCamera;
        readonly int[] magazines = new int[8];
        public int Ammo { get => magazines[SelectedSlot]; private set => magazines[SelectedSlot] = value; }
        public ItemKind EquippedWeapon => (ItemKind)SelectedSlot;
        public bool IsFirearm => EquippedWeapon is ItemKind.Pistol or ItemKind.MachineGun or ItemKind.Shotgun;
        public int MagazineCapacity => EquippedWeapon == ItemKind.MachineGun ? 30 : EquippedWeapon == ItemKind.Shotgun ? 6 : magazineSize;
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
                    int count = Mathf.Min(MagazineCapacity - Ammo, ReserveAmmo);
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
            viewCamera.transform.localRotation = Quaternion.Euler(pitch - recoil * 1.4f, 0, 0);
            Vector2 move = new Vector2((keyboard.dKey.isPressed ? 1 : 0) - (keyboard.aKey.isPressed ? 1 : 0),
                (keyboard.wKey.isPressed ? 1 : 0) - (keyboard.sKey.isPressed ? 1 : 0));
            move = Vector2.ClampMagnitude(move, 1);
            TickStamina(keyboard.leftShiftKey.isPressed && move.sqrMagnitude > 0.01f, Time.deltaTime);
            if (motor.isGrounded && verticalSpeed < 0) verticalSpeed = -2;
            verticalSpeed += Physics.gravity.y * Time.deltaTime;
            motor.Move(((transform.right * move.x + transform.forward * move.y) *
                (IsSprinting ? sprintSpeed : walkSpeed) + Vector3.up * verticalSpeed) * Time.deltaTime);
            if (bodyAnimator) bodyAnimator.SetBool("Moving", move.sqrMagnitude > 0.01f);

            if (keyboard.digit1Key.wasPressedThisFrame) SelectSlot(0);
            if (keyboard.digit2Key.wasPressedThisFrame) SelectSlot(1);
            if (keyboard.digit3Key.wasPressedThisFrame) SelectSlot((int)ItemKind.MachineGun);
            if (keyboard.digit4Key.wasPressedThisFrame) SelectSlot((int)ItemKind.Shotgun);
            if (keyboard.digit5Key.wasPressedThisFrame) SelectSlot((int)ItemKind.Machete);
            if (keyboard.eKey.wasPressedThisFrame) UseMedkit();
            if (keyboard.fKey.wasPressedThisFrame)
                ToggleFlashlight();
            if (keyboard.rKey.wasPressedThisFrame) Reload();
            if (EquippedWeapon == ItemKind.MachineGun ? mouse.leftButton.isPressed : mouse.leftButton.wasPressedThisFrame) TryAttack();
            recoil = Mathf.MoveTowards(recoil, 0, Time.deltaTime * 3);
            if (weaponRoot)
            {
                float bob = motor.isGrounded ? Mathf.Sin(Time.time * 9) * move.magnitude * 0.012f : 0;
                weaponRoot.localPosition = weaponOrigin + new Vector3(0, bob - (IsReloading ? 0.16f : 0), -recoil * 0.08f);
                weaponRoot.localRotation = Quaternion.Euler(-recoil * 14, 0, !IsFirearm ? recoil * -40 : 0);
            }
            if (muzzleFlash) muzzleFlash.enabled = Time.time < flashUntil;
        }

        public void SelectSlot(int slot)
        {
            if (IsDead) return;
            if (slot < 0 || slot >= magazines.Length || !PlayerInventory.IsWeapon((ItemKind)slot) || Inventory.Count((ItemKind)slot) == 0) return;
            SelectedSlot = slot;
            reloadUntil = 0;
            if (pistol) pistol.SetActive(SelectedSlot == 1);
            if (knife) knife.SetActive(SelectedSlot == 0);
            if (machineGun) machineGun.SetActive(EquippedWeapon == ItemKind.MachineGun);
            if (shotgun) shotgun.SetActive(EquippedWeapon == ItemKind.Shotgun);
            if (machete) machete.SetActive(EquippedWeapon == ItemKind.Machete);
        }

        public bool TryAttack()
        {
            if (IsDead || IsPaused || !viewCamera || IsReloading || Time.time < attackReady) return false;
            bool shooting = IsFirearm;
            if (shooting && Ammo == 0) { Reload(); return false; }
            if (shooting) { Ammo--; flashUntil = Time.time + 0.055f; }
            attackReady = Time.time + (EquippedWeapon == ItemKind.MachineGun ? 0.1f : EquippedWeapon == ItemKind.Shotgun ? 0.9f : EquippedWeapon == ItemKind.Machete ? 0.7f : shooting ? 0.25f : 0.65f);
            recoil = EquippedWeapon == ItemKind.Shotgun ? 2f : 1f;
            CombatImpact.PlayShot(viewCamera.gameObject, EquippedWeapon);
            if (!shooting && bodyAnimator) bodyAnimator.SetTrigger("Attack");
            if (EquippedWeapon == ItemKind.Machete)
            {
                var struck = new System.Collections.Generic.HashSet<EnemyCharacter>();
                foreach (var collider in Physics.OverlapSphere(viewCamera.transform.position, 2.8f, ~0, QueryTriggerInteraction.Ignore))
                {
                    var enemy = collider.GetComponentInParent<EnemyCharacter>();
                    if (!enemy || enemy.IsDead || !struck.Add(enemy)) continue;
                    var point = collider.bounds.center;
                    var direction = point - viewCamera.transform.position;
                    if (Vector3.Dot(viewCamera.transform.forward, direction.normalized) < 0.5f) continue;
                    if (Physics.Raycast(viewCamera.transform.position, direction.normalized, out var block, direction.magnitude + 0.1f, ~0, QueryTriggerInteraction.Ignore) && block.collider.GetComponentInParent<EnemyCharacter>() == enemy)
                        ApplyHit(enemy, 65, block.point, block.normal);
                }
            }
            else for (int pellet = 0; pellet < (EquippedWeapon == ItemKind.Shotgun ? 8 : 1); pellet++)
            {
                var spread = EquippedWeapon == ItemKind.Shotgun ? Random.insideUnitCircle * 0.07f : Vector2.zero;
                var direction = (viewCamera.transform.forward + viewCamera.transform.right * spread.x + viewCamera.transform.up * spread.y).normalized;
                float range = EquippedWeapon == ItemKind.Shotgun ? 22f : shooting ? 60f : 2.3f;
                if (Physics.Raycast(viewCamera.transform.position, direction, out var hit, range, ~0, QueryTriggerInteraction.Ignore))
                {
                    var enemy = hit.collider.GetComponentInParent<EnemyCharacter>();
                    int damage = EquippedWeapon == ItemKind.MachineGun ? 25 : EquippedWeapon == ItemKind.Shotgun ? 18 : shooting ? shotDamage : meleeDamage;
                    if (enemy) ApplyHit(enemy, damage, hit.point, hit.normal);
                    else CombatImpact.Emit(hit.point, hit.normal, false);
                }
            }
            return true;
        }

        void ApplyHit(EnemyCharacter enemy, int damage, Vector3 point, Vector3 normal)
        {
            if (enemy.TakeDamage(damage) <= 0) return;
            LastHitTime = Time.time;
            enemy.ReactToHit();
            CombatImpact.Emit(point, normal, enemy.IsDead);
        }

        public void Reload()
        {
            if (!IsDead && !IsPaused && IsFirearm && !IsReloading && Ammo < MagazineCapacity && ReserveAmmo > 0)
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

        public void OnPistolCollected() => OnWeaponCollected(ItemKind.Pistol);
        public void OnWeaponCollected(ItemKind kind)
        {
            if (!PlayerInventory.IsWeapon(kind)) return;
            SelectSlot((int)kind);
            if (IsFirearm) Ammo = MagazineCapacity;
        }

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
