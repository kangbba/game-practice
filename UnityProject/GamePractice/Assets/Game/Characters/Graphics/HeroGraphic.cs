using R3;

namespace Sayne
{
    public class HeroGraphic : CharacterGraphic
    {
        protected override void Bind(Character character)
        {
            base.Bind(character);

            ((Hero)character).UltimateUsed
                .Subscribe(this, (_, self) => self.PlayOnce(CharacterAnimations.Hero.Ultimate))
                .AddTo(this);
        }
    }
}
