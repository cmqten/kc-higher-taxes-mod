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

        void Preload(KCModHelper __helper) 
        {
            helper = __helper;
            var harmony = HarmonyInstance.Create("harmony");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

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
