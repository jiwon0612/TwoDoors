using System;
using UnityEngine;

namespace TwoDoors.Characters
{
    /// <summary>
    /// 플레이어와 적이 공유하는 체력 및 생존 상태입니다.
    /// 이동, 입력, AI, 애니메이션은 파생 클래스에서 구현합니다.
    /// </summary>
    [DisallowMultipleComponent]
    public class CharacterBase : MonoBehaviour
    {
        [SerializeField, Min(1)] private int maxHealth = 100;

        public int MaxHealth => maxHealth;
        public int CurrentHealth { get; private set; }
        public bool IsDead => CurrentHealth <= 0;

        /// <summary>현재 체력, 최대 체력 순서로 전달합니다.</summary>
        public event Action<int, int> HealthChanged;
        public event Action<CharacterBase> Died;

        /// <summary>재정의할 때 base.Awake()를 호출해야 합니다.</summary>
        protected virtual void Awake()
        {
            maxHealth = Mathf.Max(1, maxHealth);
            CurrentHealth = maxHealth;
        }

        protected virtual void OnValidate()
        {
            maxHealth = Mathf.Max(1, maxHealth);
        }

        /// <summary>
        /// 피해를 적용하고 실제 감소한 체력을 반환합니다.
        /// 사망했거나 피해량이 0 이하이면 무시합니다.
        /// </summary>
        public int TakeDamage(int amount)
        {
            if (IsDead || amount <= 0)
                return 0;

            int appliedDamage = Mathf.Min(amount, CurrentHealth);
            CurrentHealth -= appliedDamage;
            bool killed = IsDead;
            HealthChanged?.Invoke(CurrentHealth, maxHealth);

            if (killed)
            {
                OnDeath();
                Died?.Invoke(this);
            }

            return appliedDamage;
        }

        /// <summary>
        /// 최대 체력까지만 회복하고 실제 회복량을 반환합니다.
        /// 사망한 캐릭터를 부활시키지는 않습니다.
        /// </summary>
        public int Heal(int amount)
        {
            if (IsDead || amount <= 0)
                return 0;

            int restoredHealth = Mathf.Min(amount, maxHealth - CurrentHealth);
            if (restoredHealth == 0)
                return 0;

            CurrentHealth += restoredHealth;
            HealthChanged?.Invoke(CurrentHealth, maxHealth);
            return restoredHealth;
        }

        /// <summary>
        /// 사망 시 한 번 호출됩니다. 적은 AI 중지, 플레이어는 입력 중지 등으로 확장합니다.
        /// 오브젝트 제거와 사망 애니메이션은 파생 클래스가 결정합니다.
        /// </summary>
        protected virtual void OnDeath() { }
    }
}
