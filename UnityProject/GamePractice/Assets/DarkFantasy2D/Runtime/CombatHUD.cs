using UnityEngine;
using UnityEngine.UI;
namespace DarkFantasy2D
{
    public sealed class CombatHUD : MonoBehaviour
    {
        public Image healthFill, experienceFill, cooldownFill;
        public Text healthText, levelText, coinsText, gemsText, stageText;
        public Button attackButton, healButton, autoButton;
        public void SetHealth(float value, float maximum)
        { float ratio = maximum > 0 ? Mathf.Clamp01(value / maximum) : 0; healthFill.fillAmount = ratio; healthText.text = $"{Mathf.CeilToInt(value)} / {Mathf.CeilToInt(maximum)}"; }
        public void SetExperience(float ratio) => experienceFill.fillAmount = Mathf.Clamp01(ratio);
        public void SetLevel(int value) => levelText.text = "LV. " + value;
        public void SetCurrency(int coins, int gems) { coinsText.text = coins.ToString("N0"); gemsText.text = gems.ToString("N0"); }
        public void SetStage(string value) => stageText.text = value;
        public void SetCooldown(float ratio) { cooldownFill.fillAmount = Mathf.Clamp01(ratio); attackButton.interactable = ratio <= 0; }
    }
}
