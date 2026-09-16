namespace Sayne
{
    /// <summary>몸의 부위. 캐릭터는 자기 부위만 알고, 파츠가 어느 부위에 붙는지는 파츠가 말한다.</summary>
    public enum BodyPart
    {
        RightHand,
        Head,
        Hair,
        Back,
        Neck
    }

    public static class BodyParts
    {
        public static readonly BodyPart[] All =
        {
            BodyPart.RightHand,
            BodyPart.Head,
            BodyPart.Hair,
            BodyPart.Back,
            BodyPart.Neck
        };

        public static string DisplayName(BodyPart bodyPart)
        {
            return bodyPart switch
            {
                BodyPart.RightHand => "오른손",
                BodyPart.Head => "머리",
                BodyPart.Hair => "머리카락",
                BodyPart.Back => "등",
                _ => "목"
            };
        }
    }
}
