using System;
using System.Collections.Generic;
using System.Reflection;
using Duckov.Economy;
using Duckov.UI;
using HarmonyLib;
using ItemStatsSystem;
using Saves;
using TMPro;
using UnityEngine;

namespace DisplayTotalReward
{
    [HarmonyPatch(typeof(MoneyDisplay), "OnEnable")]
    public class PatchMoneyDisplayOnEnable
    {
        public static Dictionary<MoneyDisplay, Action<Item>> onSetStackCountCallbacks = new Dictionary<MoneyDisplay, Action<Item>>();
        public static Dictionary<MoneyDisplay, Action<Inventory, int>> onContentChangedCallbacks = new Dictionary<MoneyDisplay, Action<Inventory, int>>();
        public static Dictionary<MoneyDisplay, Action<long, long>> onMoneyChangedCallbacks = new Dictionary<MoneyDisplay, Action<long, long>>();
        public static Dictionary<MoneyDisplay, Action> onSetFileCallbacks = new Dictionary<MoneyDisplay, Action>();

        public static void Unregister(MoneyDisplay __instance)
        {
            if (onSetStackCountCallbacks.TryGetValue(__instance, out var onSetStackCount))
            {
                var itemList = ItemUtilities.FindAllBelongsToPlayer((Item e) => e != null);
                foreach (var item in itemList)
                {
                    item.onSetStackCount -= onSetStackCount;
                }
                Item characterItem = LevelManager.Instance?.MainCharacter?.CharacterItem;
                if (characterItem != null)
                {
                    characterItem.onSlotTreeChanged -= onSetStackCount;
                }
                onSetStackCountCallbacks.Remove(__instance);
            }

            if (onContentChangedCallbacks.TryGetValue(__instance, out var onContentChanged))
            {
                Inventory playerStorageInventory = PlayerStorage.Inventory;
                if (playerStorageInventory != null)
                {
                    playerStorageInventory.onContentChanged -= onContentChanged;
                }
                Inventory characterInventory =  LevelManager.Instance?.MainCharacter?.CharacterItem?.Inventory;
                if (characterInventory != null)
                {
                    characterInventory.onContentChanged -= onContentChanged;
                }
                Inventory inventory = LevelManager.Instance?.PetProxy?.Inventory;
                if (inventory != null)
                {
                    inventory.onContentChanged -= onContentChanged;
                }
                onContentChangedCallbacks.Remove(__instance);
            }
            if (onMoneyChangedCallbacks.TryGetValue(__instance, out var onMoneyChanged))
            {
                EconomyManager.OnMoneyChanged -= onMoneyChanged;
                onMoneyChangedCallbacks.Remove(__instance);
            }
            if (onSetFileCallbacks.TryGetValue(__instance, out var onSetFile))
            {
                SavesSystem.OnSetFile -= onSetFile;
                ItemUtilities.OnPlayerItemOperation -= onSetFile;
                onSetFileCallbacks.Remove(__instance);
            }
        }
        
        public static void Prefix(MoneyDisplay __instance)
        {
            Unregister(__instance);

            var text = (TextMeshProUGUI) typeof(MoneyDisplay).GetField("text", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(__instance);
            var totalRewardTextTransform = text.transform.parent.Find("TotalRewardText");
            if (totalRewardTextTransform == null)
            {
                return;
            }
            var isLootView = View.ActiveView is LootView;
            totalRewardTextTransform.gameObject.SetActive(isLootView);
            if (!isLootView)
            {
                return;
            }
            var totalRewardText = totalRewardTextTransform.GetComponent<TextMeshProUGUI>();
            if (ModBehaviour.CurrentSceneName == "Base")
            {
                // 在基地不动态刷新
                Refresh();
                return;
            }
            
            void Refresh()
            {
                if (!totalRewardText.gameObject.activeInHierarchy)
                {
                    return;
                }
                Debug.Log("DisplayTotalReward PatchMoneyDisplayOnEnable Refresh");
                if (ModBehaviour.CurrentSceneName == "Base")
                {
                    totalRewardText.text = ModBehaviour.GetTotalValueText((int)ModBehaviour.InventoryTotalValue());
                }
                else
                {
                    totalRewardText.text = ModBehaviour.GetTotalRewardText((int)(ModBehaviour.PlayerTotalValue() - ModBehaviour.EnterLevelTotalValue));
                }
            }

            Action<Item> onSetStackCount = item => {
                Refresh();
            };
            Action<Inventory, int> onContentChanged = (inventory, index) => {
                if (index < inventory.Content.Count && inventory.Content[index] != null)
                {
                    inventory.Content[index].onSetStackCount += onSetStackCount;
                }
                Refresh();
            };
            Action<long, long> onMoneyChanged = (before, after) => {
                Refresh();
            };
            Action onSetFile = () => Refresh();

            // 玩家物品操作时重新计算收益
            ItemUtilities.OnPlayerItemOperation += onSetFile;
            // 玩家金钱变化时重新计算收益
            EconomyManager.OnMoneyChanged += onMoneyChanged;
            onMoneyChangedCallbacks.Add(__instance, onMoneyChanged);
            // 玩家存档时重新计算收益
		    SavesSystem.OnSetFile += onSetFile;
            onSetFileCallbacks.Add(__instance, onSetFile);
            // 玩家物品堆叠数量发生变化时重新计算收益
            var itemList = ItemUtilities.FindAllBelongsToPlayer((Item e) => e != null);
            foreach (var item in itemList)
            {
                item.onSetStackCount += onSetStackCount;
            }
            // 玩家插槽树（装备及装备的插槽）变化时重新计算收益
            Item characterItem = LevelManager.Instance?.MainCharacter?.CharacterItem;
            if (characterItem != null)
            {
                characterItem.onSlotTreeChanged += onSetStackCount;
            }
            onSetStackCountCallbacks.Add(__instance, onSetStackCount);
            // 玩家背包物品变化时重新计算收益
            Inventory characterInventory = characterItem?.Inventory;
            if (characterInventory != null)
            {
                characterInventory.onContentChanged += onContentChanged;
            }
            // 宠物背包物品变化时重新计算收益
            Inventory inventory = LevelManager.Instance?.PetProxy?.Inventory;
            if (inventory != null)
            {
                inventory.onContentChanged += onContentChanged;
            }
            onContentChangedCallbacks.Add(__instance, onContentChanged);

            Refresh();
        }
    }
}