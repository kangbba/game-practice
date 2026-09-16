using UnityEngine;

namespace Sayne
{
    /// <summary>무기의 불변 기본 설정. 에셋 이름 = 무기 ID.</summary>
    [CreateAssetMenu(menuName = "Game/Weapon Definition", fileName = "WeaponDefinition")]
    public class WeaponDefinition : ScriptableObject
    {
        [SerializeField] private int _attackPower = 10;
        [SerializeField] private float _attackRange = 2f;
        [SerializeField] private float _attackInterval = 1f;
        [SerializeField] private bool _hasKnockback = true;

        /// <summary>공격 모션 이름. 비어 있으면 기본 공격 모션.</summary>
        [SerializeField] private string _attackAnimation = "";

        /// <summary>장비창에 보여줄 아이콘. 비어 있으면 자리 박스만 보인다.</summary>
        [SerializeField] private Sprite _icon;

        public string AttackAnimation => _attackAnimation;
        public Sprite Icon => _icon;

        public AttackProfile ToProfile()
        {
            return new AttackProfile(_attackPower, _attackRange, _attackInterval, _hasKnockback);
        }
    }
}
