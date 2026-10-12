using UnityEngine;

namespace TwoDoors.Characters
{
    /// <summary>Last-resort guard against depenetration pushing the controller beyond the outer walls.</summary>
    public sealed class ArenaContainment : MonoBehaviour
    {
        public Vector2 minimum = new Vector2(-11.05f, -10.05f);
        public Vector2 maximum = new Vector2(11.05f, 14.05f);
        CharacterController motor;
        void Awake() => motor = GetComponent<CharacterController>();
        void LateUpdate()
        {
            if (!motor || !motor.enabled) return;
            var position = transform.position;
            var corrected = new Vector3(Mathf.Clamp(position.x, minimum.x, maximum.x), position.y, Mathf.Clamp(position.z, minimum.y, maximum.y));
            if (position == corrected) return;
            motor.enabled = false;
            transform.position = corrected;
            motor.enabled = true;
        }
    }
}
