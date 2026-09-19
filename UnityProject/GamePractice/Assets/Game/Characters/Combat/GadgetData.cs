using System;
using UnityEngine;

namespace Sayne
{
    /// <summary>
    /// 가젯 하나의 저장 형태. 히어로 설계값(HeroData)이 들고 있다가 CharacterGadget 으로 굳힌다. 적에게는 없다.
    /// 기본값이 채워져 있어서 따로 적지 않은 히어로도 돌진을 들고 나온다. 이름을 비우면 가젯이 없다.
    /// </summary>
    [Serializable]
    public class GadgetData
    {
        [SerializeField] private string _name = "돌진";
        [SerializeField] private float _cooldown = 3f;
        [SerializeField] private float _reach = 10f;
        [SerializeField] private float _dashSpeed = 24f;

        public bool IsEmpty => string.IsNullOrEmpty(_name);

        public CharacterGadget ToGadget()
        {
            return IsEmpty ? null : new CharacterGadget(_name, _cooldown, _reach, _dashSpeed);
        }
    }
}
