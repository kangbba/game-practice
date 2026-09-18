namespace Sayne
{
    /// <summary>
    /// 몸의 상태. 모션도 생사도 이것 하나로 본다.
    /// 살아 있는 동안은 Idle·Walk·Hit 을 오가고, HP 가 0 이 되면 Dying, 죽음처리되면 Death 로 가서 돌아오지 않는다.
    /// </summary>
    public enum CharacterStateType
    {
        Idle,
        Walk,
        Hit,

        /// <summary>
        /// HP 가 0 이 됐지만 아직 죽음처리 전인 몸. 맞는 자세로 선 채 계속 맞고 데미지도 뜬다.
        /// 평소엔 수명 주인(매니저)이 곧바로 죽음처리해서 한 호출 안에 Death 로 덮이고, 궁극기의 죽음 보류 동안만 길어진다.
        /// </summary>
        Dying,

        Death
    }
}
