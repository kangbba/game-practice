namespace Sayne
{
    /// <summary>부위별 장비 ID 묶음. "스폰 때 입고 나올 한 벌" 선언이다. 빈 부위는 벗은 채로 시작한다.</summary>
    public class EquipmentIDs
    {
        private readonly string _rightHand;
        private readonly string _head;
        private readonly string _hair;
        private readonly string _back;
        private readonly string _neck;

        public EquipmentIDs(string rightHand = "", string head = "", string hair = "", string back = "", string neck = "")
        {
            _rightHand = rightHand;
            _head = head;
            _hair = hair;
            _back = back;
            _neck = neck;
        }

        public string Get(BodyPart bodyPart)
        {
            return bodyPart switch
            {
                BodyPart.RightHand => _rightHand,
                BodyPart.Head => _head,
                BodyPart.Hair => _hair,
                BodyPart.Back => _back,
                _ => _neck
            };
        }
    }
}
