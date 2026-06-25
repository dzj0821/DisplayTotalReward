using Duckov.UI;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DisplayTotalReward
{
    [HarmonyPatch(typeof(MoneyDisplay), "Awake")]
    public class PatchMoneyDisplayAwake
    {
        public static void Prefix(MoneyDisplay __instance)
        {
            var moneyTransform = __instance.transform.Find("Money");
            if (moneyTransform == null)
            {
                return;
            }

            var parent = moneyTransform.parent;
            var totalReward = Object.Instantiate(moneyTransform.gameObject, parent);
            totalReward.name = "TotalRewardText";

            for (var i = totalReward.transform.childCount - 1; i >= 0; i--)
            {
                var child = totalReward.transform.GetChild(i);
                if (child.GetComponentInChildren<TextMeshProUGUI>(true) == null)
                {
                    Object.Destroy(child.gameObject);
                }
            }

            var text = totalReward.GetComponentInChildren<TextMeshProUGUI>(true);
            if (text == null)
            {
                return;
            }

            var layoutElement = text.GetComponent<LayoutElement>();
            if (layoutElement == null)
            {
                layoutElement = text.gameObject.AddComponent<LayoutElement>();
            }
            layoutElement.preferredHeight = 60;
        }
    }
}
