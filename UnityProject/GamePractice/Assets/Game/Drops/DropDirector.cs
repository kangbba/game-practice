using R3;
using UnityEngine;

namespace Sayne
{
    /// <summary>
    /// 무엇을 떨어뜨리고 주우면 무슨 일이 나는지를 정하는 쪽. 적이 죽으면 기본 드랍(골드·회복, BaseDropPlan)과
    /// 그 적의 설계값에 적힌 장비 드랍을 굴려, 종류에 맞는 그림과 효과를 얹어 DropManager 에 스폰을 부탁한다.
    /// 재화·회복·가방·장비 아이콘을 아는 건 여기까지고, 구슬을 뿌리는 쪽은 그걸 모른다.
    /// </summary>
    public class DropDirector : ManagerBase
    {
        private readonly EnemyManager _enemyManager;
        private readonly DropManager _dropManager;
        private readonly CurrencyManager _currencyManager;
        private readonly PartyManager _partyManager;
        private readonly EquipmentManager _equipmentManager;
        private readonly IDropAssets _itemAssets;

        public DropDirector(EnemyManager enemyManager, DropManager dropManager, CurrencyManager currencyManager,
            PartyManager partyManager, EquipmentManager equipmentManager, IDropAssets itemAssets)
        {
            _enemyManager = enemyManager;
            _dropManager = dropManager;
            _currencyManager = currencyManager;
            _partyManager = partyManager;
            _equipmentManager = equipmentManager;
            _itemAssets = itemAssets;
        }

        protected override void OnInit()
        {
            _enemyManager.Died
                .Subscribe(this, (enemy, self) => self.DropFrom(enemy))
                .RegisterTo(LifeToken);
        }

        protected override void OnRelease()
        {
        }

        private void DropFrom(Enemy enemy)
        {
            var position = enemy.transform.position;

            DropBase(enemy.IsBoss, position);

            foreach (var entry in enemy.Data.Roll())
            {
                DropEquipment(entry.EquipmentID, position);
            }
        }

        /// <summary>모든 적이 굴리는 골드와 회복. 보스는 굴리지 않고 둘 다 준다.</summary>
        private void DropBase(bool isBoss, Vector3 position)
        {
            if (isBoss || Random.value < BaseDropPlan.GoldChance)
            {
                // 지역 변수로 받아 둔다 — 나중에 실행될 때 다시 계산하지 않게.
                var gold = isBoss ? BaseDropPlan.GoldAmount * BaseDropPlan.BossGoldMultiplier : BaseDropPlan.GoldAmount;
                _dropManager.Spawn(position, _itemAssets.CoinIcon, $"Gold {gold}",
                    _ => _currencyManager.AddGold(gold));
            }

            if (isBoss || Random.value < BaseDropPlan.HealChance)
            {
                // 채우는 양은 주운 영웅의 최대 체력을 따른다 — 누가 주울지는 떨어질 때 모른다.
                _dropManager.Spawn(position, _itemAssets.HealIcon, "Heal",
                    hero => hero.Heal(Mathf.CeilToInt(hero.FinalMaxHP * BaseDropPlan.HealRatio)));
            }
        }

        /// <summary>장비 아이콘은 장비 자기 것이다. 가방은 영웅들이 함께 쓰는 하나라 누가 주웠는지는 안 본다.</summary>
        private void DropEquipment(string equipmentID, Vector3 position)
        {
            _dropManager.Spawn(position, _equipmentManager.GetIcon(equipmentID), equipmentID,
                _ => _partyManager.Inventory.Add(equipmentID));
        }
    }
}
