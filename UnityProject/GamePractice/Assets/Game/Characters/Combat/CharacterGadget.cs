namespace Sayne
{
    /// <summary>
    /// 쿨타임이 붙은 잡기술. 스킬과 같은 꼴(이름·쿨·쓰는 동안 잠금)이지만 때리지 않고 몸을 옮긴다 — 지금은 적에게 달려가 붙는 돌진 하나다.
    /// 무게는 가젯 &lt; 스킬 &lt; 궁극기. 쿨이 제일 짧고, 싸움을 여는 발판일 뿐 피해를 주지 않는다.
    /// </summary>
    public class CharacterGadget
    {
        public string Name { get; }
        public float Cooldown { get; }

        /// <summary>달려갈 상대를 찾는 바닥 거리(위아래도 똑같이 잰다). 이보다 먼 적에게는 달려가지 않는다.</summary>
        public float Reach { get; }

        /// <summary>달려가는 빠르기(초당 월드 단위). 걷기보다 훨씬 빨라서 "슉" 하고 붙는다.</summary>
        public float DashSpeed { get; }

        public CharacterGadget(string name, float cooldown, float reach, float dashSpeed)
        {
            Name = name;
            Cooldown = cooldown;
            Reach = reach;
            DashSpeed = dashSpeed;
        }
    }
}
