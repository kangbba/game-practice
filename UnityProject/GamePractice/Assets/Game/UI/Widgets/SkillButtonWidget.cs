using System;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Sayne
{
    /// <summary>
    /// 누를 수 있는 원형 스킬 버튼. 쿨타임을 구독해 남은 비율과 초를 그린다 — 해석은 캐릭터가 이미 해뒀다.
    /// 켜짐 조건은 눌렀을 때 실제로 쓰는지를 가르는 그 판정을 그대로 받아 매 프레임 묻는다 — 눌러도 아무 일 없는 버튼은 켜두지 않는다.
    /// </summary>
    public class SkillButtonWidget : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private TextMeshProUGUI _label;
        [SerializeField] private Image _cooldownFill;
        [SerializeField] private TextMeshProUGUI _cooldownText;

        public Observable<Unit> Clicked => _button.onClick.AsObservable();

        /// <summary>
        /// 이 자리의 기술을 맡는다. 히어로가 바뀌면 새 히어로의 같은 자리를 다시 문다.
        /// canUse 는 그 히어로가 지금 이 기술을 쓸 수 있는가 — 스킬은 CanUseSkill, 궁극기는 UltimateDirector.CanPlay 다.
        /// </summary>
        public void Init(HeroManager heroManager, SkillSlotType slot, Func<Character, bool> canUse)
        {
            heroManager.CurrentHero
                .Where(hero => hero != null)
                .Subscribe((self: this, slot, canUse), (hero, state) => state.self.SetHero(hero, state.slot, state.canUse))
                .AddTo(this);
        }

        /// <summary>
        /// 구독은 그 히어로의 수명을 따라간다 — 죽으면 같이 풀린다.
        /// 궁극기는 든 무기의 것이라 무기를 바꿔 들 때마다 이름과 쓸 수 있는지가 다시 정해진다.
        /// </summary>
        private void SetHero(Character hero, SkillSlotType slot, Func<Character, bool> canUse)
        {
            var cooldown = slot == SkillSlotType.Skill ? hero.Combat.SkillCooldown : hero.Combat.UltimateCooldown;

            var skillChanged = slot == SkillSlotType.Skill
                ? Observable.Return(Unit.Default)
                : hero.Equipment.Observe(EquipmentSlot.MainHand).Select(_ => Unit.Default);

            skillChanged
                .Subscribe((self: this, hero, slot), (_, state) =>
                {
                    var skill = SkillOf(state.hero, state.slot);
                    state.self._label.text = skill != null ? skill.Name : EmptyName(state.slot);
                })
                .AddTo(hero);

            cooldown.RemainRatio
                .Subscribe(this, (ratio, self) => self._cooldownFill.fillAmount = ratio)
                .AddTo(hero);

            cooldown.RemainSeconds
                .Subscribe(this, (remain, self) => self._cooldownText.text = remain > 0f ? $"{remain:0.0}" : string.Empty)
                .AddTo(hero);

            // 궁극기는 주변에 적이 있는지까지 보므로 값이 바뀌는 순간을 알릴 곳이 없다 — 매 프레임 묻는다.
            Observable.EveryUpdate(UnityFrameProvider.Update)
                .Select((hero, canUse), (_, state) => state.canUse(state.hero))
                .DistinctUntilChanged()
                .Subscribe(this, (usable, self) => self._button.interactable = usable)
                .AddTo(hero);
        }

        private static CharacterSkill SkillOf(Character hero, SkillSlotType slot)
        {
            return slot == SkillSlotType.Skill ? hero.Combat.Skill : hero.Combat.Ultimate;
        }

        /// <summary>구독 없이 최종 모습만 그린다. 프리팹을 굽거나 미리보기를 찍을 때 쓰는 문이다.</summary>
        public void Preview(string label, float cooldownRatio, float cooldownRemain)
        {
            _label.text = label;
            _cooldownFill.fillAmount = cooldownRatio;
            _cooldownText.text = cooldownRemain > 0f ? $"{cooldownRemain:0.0}" : string.Empty;
        }

        /// <summary>기술을 아직 안 가진 히어로도 버튼은 뜬다. 그때 쓰는 자리 이름이다.</summary>
        private static string EmptyName(SkillSlotType slot)
        {
            return slot == SkillSlotType.Skill ? "스킬" : "궁극기";
        }
    }
}
