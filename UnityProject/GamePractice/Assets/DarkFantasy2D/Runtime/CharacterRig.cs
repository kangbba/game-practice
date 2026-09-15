using UnityEngine;
using UnityEngine.Events;

namespace DarkFantasy2D
{
    /// <summary>Right-facing cutout bone rig. Animator parameters: Speed, Attack, Hit, Die, Revive.</summary>
    [RequireComponent(typeof(Animator))]
    public sealed class CharacterRig : MonoBehaviour
    {
        public float maxHealth = 100;
        public GameObject slashPrefab, hitPrefab, deathPrefab;
        public Transform effectSocket;
        public UnityEvent onStrike = new UnityEvent();
        public float Health { get; private set; }
        public bool IsDead => Health <= 0;
        Animator animator;
        void Awake() { animator = GetComponent<Animator>(); Health = maxHealth; }
        public void SetMoving(bool moving) { if (!IsDead) animator.SetFloat("Speed", moving ? 1 : 0); }
        public void Attack() { if (!IsDead) animator.SetTrigger("Attack"); }
        public void TakeDamage(float damage)
        {
            if (IsDead) return;
            Health = Mathf.Clamp(Health - Mathf.Max(0, damage), 0, maxHealth);
            animator.SetTrigger(IsDead ? "Die" : "Hit");
            Spawn(IsDead ? deathPrefab : hitPrefab);
        }
        public void Heal(float value) { if (!IsDead) Health = Mathf.Min(maxHealth, Health + Mathf.Max(0, value)); }
        public void Die() { TakeDamage(maxHealth); }
        public void Revive() { Health = maxHealth; animator.ResetTrigger("Die"); animator.SetFloat("Speed", 0); animator.SetTrigger("Revive"); }
        public void FaceRight(bool right) { var s = transform.localScale; s.x = Mathf.Abs(s.x) * (right ? 1 : -1); transform.localScale = s; }
        // Called by the Attack animation at its contact frame.
        public void Strike() { if (!IsDead) { Spawn(slashPrefab); onStrike.Invoke(); } }
        void Spawn(GameObject prefab)
        {
            if (!prefab) return;
            var fx = Instantiate(prefab, effectSocket ? effectSocket.position : transform.position + Vector3.up, Quaternion.identity);
            var s = fx.transform.localScale; s.x *= Mathf.Sign(transform.lossyScale.x); fx.transform.localScale = s;
        }
    }
}
