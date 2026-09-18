namespace Sayne
{
    /// <summary>글 재생기가 지금 어디까지 왔나.</summary>
    public enum TextPlayState
    {
        /// <summary>아무것도 틀고 있지 않다.</summary>
        Idle,

        /// <summary>몸이 열리는 중.</summary>
        Opening,

        /// <summary>글자가 찍히는 중.</summary>
        Typing,

        /// <summary>다 찍고 넘김 신호를 기다리는 중.</summary>
        Waiting,

        /// <summary>몸이 닫히는 중.</summary>
        Closing,
    }
}
