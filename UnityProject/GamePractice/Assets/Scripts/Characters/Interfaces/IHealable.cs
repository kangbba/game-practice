namespace Sayne
{
    /// <summary>회복 가능.</summary>
    public interface IHealable
    {
        void Heal(int amount, Character source);
    }
}
