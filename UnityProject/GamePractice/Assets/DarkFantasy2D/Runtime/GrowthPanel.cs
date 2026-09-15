using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
namespace DarkFantasy2D
{
    public sealed class GrowthPanel : MonoBehaviour
    {
        public Text currencyText, attackText, healthText;
        public Button attackTrain, healthTrain, close;
        public UnityEvent onTrained = new UnityEvent();
        public int coins = 1004, attackLevel = 1, healthLevel = 1;
        void Awake() { attackTrain.onClick.AddListener(() => Train(true)); healthTrain.onClick.AddListener(() => Train(false)); close.onClick.AddListener(() => gameObject.SetActive(false)); Refresh(); }
        public void Train(bool attack) { if(coins < 5)return; coins -= 5; if(attack)attackLevel++;else healthLevel++; Refresh();onTrained.Invoke(); }
        void Refresh() { currencyText.text=coins.ToString("N0"); attackText.text="공격력\n<color=#FFE08A>+"+(attackLevel*3)+"</color>  Lv."+attackLevel;healthText.text="체력\n<color=#FFE08A>+"+(healthLevel*10)+"</color>  Lv."+healthLevel;attackTrain.interactable=healthTrain.interactable=coins>=5; }
    }
}
