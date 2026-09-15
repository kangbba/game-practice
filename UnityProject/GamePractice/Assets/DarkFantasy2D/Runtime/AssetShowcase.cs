using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
namespace DarkFantasy2D
{
    public sealed class AssetShowcase : MonoBehaviour
    {
        public CharacterRig[] characters;
        public CombatHUD hud;
        public Button[] animationButtons, effectButtons;
        public GameObject[] effects;
        public Transform effectOrigin;
        public Text statusText;
        public GameObject growthPanel; public Button growthButton;
        float cooldown, autoTimer;
        bool auto;
        void Start()
        {
            growthButton.onClick.AddListener(() => { auto=false; growthPanel.SetActive(true); });
            growthPanel.GetComponent<GrowthPanel>().onTrained.AddListener(() => hud.SetCurrency(growthPanel.GetComponent<GrowthPanel>().coins,500));
            hud.SetCurrency(1004, 500); hud.SetExperience(.6f); hud.SetLevel(1);
            for (int i = 0; i < animationButtons.Length; i++) { int mode = i; animationButtons[i].onClick.AddListener(() => PlayMode(mode)); }
            for (int i = 0; i < effectButtons.Length; i++) { int index = i; effectButtons[i].onClick.AddListener(() => PlayEffect(index)); }
            hud.attackButton.onClick.AddListener(Attack);
            hud.healButton.onClick.AddListener(() => { foreach (var c in characters) c.Heal(35); PlayEffect(5); });
            hud.autoButton.onClick.AddListener(() => { auto = !auto; statusText.text = auto ? "자동 미리보기 켜짐" : "자동 미리보기 꺼짐"; });
        }
        void Update()
        {
            cooldown = Mathf.Max(0, cooldown - Time.deltaTime); hud.SetCooldown(cooldown / .8f);
            hud.SetHealth(characters[0].Health, characters[0].maxHealth);
            if (auto && (autoTimer -= Time.deltaTime) <= 0) { Attack(); autoTimer = 2; }
#if ENABLE_INPUT_SYSTEM
            if (!growthPanel.activeSelf && Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) Attack();
#else
            if (!growthPanel.activeSelf && Input.GetKeyDown(KeyCode.Space)) Attack();
#endif
        }
        void Attack() { if (cooldown > 0) return; foreach (var c in characters) c.Attack(); cooldown = .8f; }
        public void PlayMode(int mode)
        {
            auto = false;
            foreach (var c in characters)
            {
                if (mode != 4 && c.IsDead) c.Revive();
                c.SetMoving(mode == 1);
                if (mode == 2) c.Attack();
                if (mode == 3) c.TakeDamage(15);
                if (mode == 4) c.Die();
                if (mode == 5) c.Revive();
            }
            statusText.text = new[] { "대기 / 호흡", "이동 / 팔과 다리 교차", "공격 / 무기 휘두르기와 타격 이펙트", "피격 / 반동과 파티클", "사망 / 쓰러짐과 연기", "부활 / 체력과 자세 복구" }[mode];
        }
        public void PlayEffect(int index)
        { Instantiate(effects[index], effectOrigin.position, Quaternion.identity); statusText.text = "VFX / " + effects[index].name; }
    }
}
