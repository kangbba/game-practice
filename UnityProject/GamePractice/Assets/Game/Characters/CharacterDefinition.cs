using UnityEngine;

namespace Sayne
{
    /// <summary>캐릭터의 불변 기본 설정. 에셋 이름 = 캐릭터 ID. 공격 수치는 맨손(비무장) 명세다.</summary>
    [CreateAssetMenu(menuName = "Game/Character Definition", fileName = "CharacterDefinition")]
    public class CharacterDefinition : ScriptableObject
    {
        [SerializeField] private int _maxHP = 100;
        [SerializeField] private float _moveSpeed = 4.5f;

        [SerializeField] private int _attackPower = 10;
        [SerializeField] private float _attackRange = 2f;
        [SerializeField] private float _attackInterval = 0.5f;
        [SerializeField] private bool _hasKnockback = true;

        /// <summary>스폰 시 장착할 무기 ID. 비어 있으면 맨손으로 시작한다.</summary>
        [SerializeField] private string _defaultWeaponID = "";

        public string DefaultWeaponID => _defaultWeaponID;

        public CharacterStats ToStats()
        {
            return new CharacterStats(_maxHP, _moveSpeed);
        }

        public AttackProfile ToBareHandsAttack()
        {
            return new AttackProfile(_attackPower, _attackRange, _attackInterval, _hasKnockback);
        }
    }
}
