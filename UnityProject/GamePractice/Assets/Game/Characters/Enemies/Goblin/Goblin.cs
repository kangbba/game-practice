namespace Sayne
{
    /// <summary>Goblin 본체. 싸우는 방식은 적 설계값(EnemyPlan)이 정한다.</summary>
    public class Goblin : Enemy
    {
        public override string ID => EnemyID.Goblin;
    }
}
