﻿using Pacemaker.Extensions;

using HarmonyLib;

using System;

using TaleWorlds.CampaignSystem;

namespace Pacemaker.Patches
{
    [HarmonyDebug]
    [HarmonyPatch(typeof(CampaignTime))]
    internal static class CampaignTimePatch
    {
        private delegate long CurrentTicksDelegate();
        private static readonly Reflect.DeclaredGetter<CampaignTime> CurrentTicksRG = new("CurrentTicks");
        private static readonly CurrentTicksDelegate CurrentTicks = CurrentTicksRG.GetDelegate<CurrentTicksDelegate>();

        /////////////////////////////////////////////////////////////////////////////////////////////
        // Elapsed[UNIT]sUntilNow

        [HarmonyPrefix]
        [HarmonyPatch("ElapsedSeasonsUntilNow", MethodType.Getter)]
        public static bool ElapsedSeasonsUntilNow(ref float __result, ref long ____numTicks)
        {
            __result = (CurrentTicks() - ____numTicks) / Main.TimeParam.TickPerSeasonF;
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch("ElapsedYearsUntilNow", MethodType.Getter)]
        public static bool ElapsedYearsUntilNow(ref float __result, ref long ____numTicks)
        {
            __result = (CurrentTicks() - ____numTicks) / Main.TimeParam.TickPerYearF;
            return false;
        }

        /////////////////////////////////////////////////////////////////////////////////////////////
        // Remaining[UNIT]sFromNow

        [HarmonyPrefix]
        [HarmonyPatch("RemainingSeasonsFromNow", MethodType.Getter)]
        public static bool RemainingSeasonsFromNow(ref float __result, ref long ____numTicks)
        {
            __result = (____numTicks - CurrentTicks()) / Main.TimeParam.TickPerSeasonF;
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch("RemainingYearsFromNow", MethodType.Getter)]
        public static bool RemainingYearsFromNow(ref float __result, ref long ____numTicks)
        {
            __result = (____numTicks - CurrentTicks()) / Main.TimeParam.TickPerYearF;
            return false;
        }

        /////////////////////////////////////////////////////////////////////////////////////////////
        // To[UNIT]s

        [HarmonyPrefix]
        [HarmonyPatch("ToSeasons", MethodType.Getter)]
        public static bool ToSeasons(ref double __result, ref long ____numTicks)
        {
            __result = ____numTicks / Main.TimeParam.TickPerSeasonD;
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch("ToYears", MethodType.Getter)]
        public static bool ToYears(ref double __result, ref long ____numTicks)
        {
            __result = ____numTicks / Main.TimeParam.TickPerYearD;
            return false;
        }

        /////////////////////////////////////////////////////////////////////////////////////////////
        // Get[UNIT]Of[UNIT]

        [HarmonyPrefix]
        [HarmonyPatch("GetDayOfSeason", MethodType.Getter)]
        public static bool GetDayOfSeason(ref int __result, ref long ____numTicks)
        {
            __result = (int)((____numTicks / TimeParams.TickPerDayL) % Main.TimeParam.DayPerSeason);
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch("GetDayOfYear", MethodType.Getter)]
        public static bool GetDayOfYear(ref int __result, ref long ____numTicks)
        {
            __result = (int)((____numTicks / TimeParams.TickPerDayL) % Main.TimeParam.DayPerYear);
            return false;
        }

        //[HarmonyPrefix]
        //[HarmonyPatch("GetWeekOfSeason", MethodType.Getter)]
        //static bool GetWeekOfSeason(ref int __result, long ____numTicks)
        //{
        //	__result = (int)((____numTicks / Main.TimeParam.TickPerWeekL) % Main.TimeParam.WeekPerSeasonL);
        //	return false;
        //}

        [HarmonyPrefix]
        [HarmonyPatch("GetSeasonOfYear", MethodType.Getter)]
        public static bool GetSeasonOfYear(ref int __result, ref long ____numTicks)
        {
            long nSeason = ____numTicks / Main.TimeParam.TickPerSeasonL;
            __result = (int)(nSeason % TimeParams.SeasonPerYear);
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch("GetYear", MethodType.Getter)]
        public static bool GetYear(ref int __result, ref long ____numTicks)
        {
            __result = (int)(____numTicks / Main.TimeParam.TickPerYearL);
            return false;
        }

        /*
        /////////////////////////////////////////////////////////////////////////////////////////////
        // Get[UNIT]Of[UNIT]f

        [HarmonyPrefix]
        [HarmonyPatch("GetDayOfSeasonf", MethodType.Getter)]
        public static bool GetDayOfSeasonf(ref float __result, ref long ____numTicks)
        {
            __result = (float)Math.IEEERemainder(____numTicks / TimeParams.TickPerDayL, Main.TimeParam.DayPerSeason);
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch("GetSeasonOfYearf", MethodType.Getter)]
        public static bool GetSeasonOfYearf(ref float __result, ref long ____numTicks)
        {
            __result = (float)Math.IEEERemainder(____numTicks / Main.TimeParam.TickPerSeasonL, TimeParams.SeasonPerYear);
            return false;
        }
        */
        /////////////////////////////////////////////////////////////////////////////////////////////
        /* [UNIT]s (factory methods) */

        [HarmonyPrefix]
        [HarmonyPatch("Seasons")]
        public static bool Seasons(float valueInSeasons, ref CampaignTime __result)
        {
            __result = CampaignTimeExtensions.Ticks((long)(valueInSeasons * Main.TimeParam.TickPerSeasonF));
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch("Years")]
        public static bool Years(float valueInYears, ref CampaignTime __result)
        {
            __result = CampaignTimeExtensions.Ticks((long)(valueInYears * Main.TimeParam.TickPerYearF));
            return false;
        }

        /////////////////////////////////////////////////////////////////////////////////////////////
        // [UNIT]sFromNow (factory methods)

        // NOTE: SeasonsFromNow doesn't exist

        [HarmonyPrefix]
        [HarmonyPatch("YearsFromNow")]
        public static bool YearsFromNow(float valueInYears, ref CampaignTime __result)
        {
            __result = CampaignTimeExtensions.Ticks(CurrentTicks() + (long)(valueInYears * Main.TimeParam.TickPerYearF));
            return false;
        }
    }
}
