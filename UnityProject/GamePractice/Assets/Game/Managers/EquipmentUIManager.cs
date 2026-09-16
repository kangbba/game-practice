using System.Collections.Generic;
using R3;
using UnityEngine;

namespace Sayne
{
    /// <summary>
    /// 장비창과 게임을 잇는 정책. 후보 목록을 채우고, 창의 적용 요청을 실제 장착으로 바꾸고,
    /// 히어로의 장비 상태(진실의 원천)를 구독해 창 표시를 따라가게 한다.
    /// </summary>
    public class EquipmentUIManager : ManagerBase
    {
        private readonly BattlePhaseUIPanel _panel;
        private readonly HeroManager _heroManager;
        private readonly EquipmentManager _equipmentManager;

        public EquipmentUIManager(BattlePhaseUIPanel panel, HeroManager heroManager, EquipmentManager equipmentManager)
        {
            _panel = panel;
            _heroManager = heroManager;
            _equipmentManager = equipmentManager;
        }

        protected override void OnInit()
        {
            var window = _panel.EquipmentWindow;
            window.SetCandidates(BuildCandidates());

            _heroManager.Spawned
                .Subscribe(this, (character, self) =>
                {
                    if (character is Hero hero)
                    {
                        self.BindHero(hero);
                    }
                })
                .RegisterTo(LifeToken);

            _panel.EquipMenuClicked
                .Subscribe((self: this, window), (_, state) => state.self.Toggle(state.window))
                .RegisterTo(LifeToken);

            window.Applied
                .Subscribe(this, (request, self) => self.Apply(request.BodyPart, request.EquipmentID))
                .RegisterTo(LifeToken);
        }

        protected override void OnRelease()
        {
        }

        /// <summary>등록된 파츠 전부가 후보다. 치장 부위엔 "벗기" 칸을 앞에 두고, 무기는 맨손도 무기라 벗기가 없다.</summary>
        private List<(string, BodyPart, string, Sprite)> BuildCandidates()
        {
            var candidates = new List<(string, BodyPart, string, Sprite)>();

            foreach (var bodyPart in BodyParts.All)
            {
                if (bodyPart != BodyPart.RightHand)
                {
                    candidates.Add((string.Empty, bodyPart, $"{BodyParts.DisplayName(bodyPart)} 벗기", null));
                }
            }

            foreach (var equipmentID in EquipmentPlans.IDs)
            {
                candidates.Add((equipmentID, EquipmentPlans.BodyPartOf(equipmentID), equipmentID, null));
            }

            return candidates;
        }

        private void BindHero(Hero hero)
        {
            foreach (var bodyPart in BodyParts.All)
            {
                hero.Equipment.Observe(bodyPart)
                    .Subscribe((self: this, bodyPart), (part, state) =>
                        state.self._panel.EquipmentWindow.SetEquipped(state.bodyPart, part != null ? part.ID : string.Empty))
                    .RegisterTo(hero.destroyCancellationToken);
            }
        }

        private void Toggle(EquipmentWindow window)
        {
            if (window.IsOpen)
            {
                window.Hide();
            }
            else
            {
                window.Show();
            }
        }

        private void Apply(BodyPart bodyPart, string equipmentID)
        {
            foreach (var hero in _heroManager.CurrentHeroes)
            {
                if (hero == null || !hero.IsAlive)
                {
                    continue;
                }

                if (string.IsNullOrEmpty(equipmentID))
                {
                    _equipmentManager.TakeOff(hero, bodyPart);
                }
                else
                {
                    _equipmentManager.Wear(hero, equipmentID);
                }

                return;
            }
        }
    }
}
