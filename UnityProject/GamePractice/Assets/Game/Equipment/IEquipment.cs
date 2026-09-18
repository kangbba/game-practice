namespace Sayne
{
    /// <summary>
    /// 장착할 수 있는 것. 장비 프리팹 뿌리에 붙는 부위 스크립트(Weapon·Helmet·Armor·Boots)가 모두 이걸 구현하고,
    /// 자기가 어느 부위인지를 스스로 선언한다 — 부위의 진실은 여기 하나다.
    /// 부위끼리 공유하는 건 이것뿐이라 공통 부모 클래스는 두지 않는다. 종류가 갈리는 무기만 상속을 한 겹 더 쓴다.
    /// </summary>
    public interface IEquipment
    {
        EquipmentSlot Slot { get; }
    }
}
