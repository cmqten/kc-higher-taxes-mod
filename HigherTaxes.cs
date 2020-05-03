/*
Why limit yourself to 30%? Tax your peasants as high as you want, the sky is the limit.

Author: cmjten10 (https://steamcommunity.com/id/cmjten10/)
Mod Version: 1
Target K&C Version: 117r5s-mods
Date: 2020-05-03
*/
using Harmony;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace HigherTaxes
{
    public class HigherTaxesMod : MonoBehaviour 
    {
        public static KCModHelper helper;

        private const string modName = "cmjten10-kc-higher-taxes-mod";
        private static Dictionary<int, float> taxRates = new Dictionary<int, float>();

        void Preload(KCModHelper __helper) 
        {
            helper = __helper;
            var harmony = HarmonyInstance.Create("harmony");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        void SceneLoaded(KCModHelper __helper)
        {
            Broadcast.OnLoadedEvent.Listen(OnLoaded);
            Broadcast.OnSaveEvent.Listen(OnSave);
        }

        // Loads higher tax rates.
        public void OnLoaded(object sender, OnLoadedEvent loadedEvent)
        {
            int landmassesCount = Player.inst.PlayerLandmassOwner.ownedLandMasses.Count;
            for (int i = 0; i < landmassesCount; i++)
            {
                int landmassId = Player.inst.PlayerLandmassOwner.ownedLandMasses.data[i];
                float currentTaxRate = Player.inst.GetTaxRate(landmassId);
                string higherTaxRateString = LoadSave.ReadDataGeneric(modName, $"{landmassId}");

                // If the current tax rate is less than 30%, don't set the higher tax rate. This covers the case where
                // the player set a higher tax rate, deleted the mod, then set a <30% tax rate, then installed the mod
                // again. Most likely the player would want to stay at the <30% tax rate.
                if (!string.IsNullOrEmpty(higherTaxRateString) && currentTaxRate >= 3f)
                {
                    float higherTaxRate = float.Parse(higherTaxRateString);
                    Player.inst.SetTaxRate(landmassId, higherTaxRate);
                }
            }
        }

        // Saves higher tax rates to mod-specific data. Anything above 30% will be reset to 30% temporarily to be saved 
        // with PlayerSaveData. In the case where the mod is deleted, higher tax rates would not persist.
        public void OnSave(object sender, OnSaveEvent saveEvent)
        {
            taxRates.Clear();
            int landmassesCount = Player.inst.PlayerLandmassOwner.ownedLandMasses.Count;
            for (int i = 0; i < landmassesCount; i++)
            {
                int landmassId = Player.inst.PlayerLandmassOwner.ownedLandMasses.data[i];
                float taxRate = Player.inst.GetTaxRate(landmassId);
                LoadSave.SaveDataGeneric(modName, $"{landmassId}", taxRate.ToString());
                if (taxRate > 3f)
                {
                    // Temporarily set tax to 30% until PlayerSaveData::Pack is run to save a valid max tax rate, then
                    // restore to the higher tax rate in PlayerSaveData::Pack Postfix patch.
                    taxRates[landmassId] = taxRate;
                    Player.inst.SetTaxRate(landmassId, 3f);
                }
            }
        }

        // This restores higher tax rates after saving them as 30% in PlayerSaveData. In the case where the mod is 
        // deleted, the next load would reset the higher tax rates to 30%.
        [HarmonyPatch(typeof(Player.PlayerSaveData))]
        [HarmonyPatch("Pack")]
        public static class RestoreHigherTaxRatesAfterPackPatch
        {
            static void Postfix()
            {
                foreach (KeyValuePair<int, float> entry in taxRates)
                {
                    int landmassId = entry.Key;
                    float taxRate = entry.Value;
                    Player.inst.SetTaxRate(landmassId, taxRate);
                }
                taxRates.Clear();
            }
        }

        // Removes the upper limit of 30% tax rate. Completely replaces IncreaseTaxRate.
        [HarmonyPatch(typeof(Player))]
        [HarmonyPatch("IncreaseTaxRate")]
        public static class TaxNoUpperLimitPatch
        {
            static bool Prefix(Player __instance, int landMass) 
            {
                float taxRate = __instance.GetTaxRate(landMass);
                taxRate += 0.5f;
                __instance.SetTaxRate(landMass, taxRate);
                return false;
            }
        }

        // Beyond 30%, happiness decreases by 10 for every 5% increase in tax. Completely replaces GetHappinessFromTax.
        [HarmonyPatch(typeof(Home))]
        [HarmonyPatch("GetHappinessFromTax")]
        public static class NewHappinessCalculationPatch
        {
            static bool Prefix(Home __instance, Building ___b, ref int __result)
            {
                float taxRate = Player.inst.GetTaxRate(___b.LandMass());
                float absoluteTaxRate = Math.Abs(taxRate);
                int happinessModifier = 0;

                if (absoluteTaxRate >= 0.5f && absoluteTaxRate < 1f)
                {
                    happinessModifier = -3;
                }
                if (absoluteTaxRate >= 1f && absoluteTaxRate < 1.5f)
                {
                    happinessModifier = -7;
                }
                if (absoluteTaxRate >= 1.5f && absoluteTaxRate < 2f)
                {
                    happinessModifier = -12;
                }
                if (absoluteTaxRate == 2f && absoluteTaxRate < 2.5f)
                {
                    happinessModifier = -18;
                }
                if (absoluteTaxRate >= 2.5f && absoluteTaxRate < 3f)
                {
                    happinessModifier = -30;
                }
                if (absoluteTaxRate >= 3f)
                {
                    happinessModifier = -(40 + (int)((absoluteTaxRate - 3f) / 0.5f) * 10);
                }
                __result = happinessModifier;
                return false;
            }
        }
    }
}
