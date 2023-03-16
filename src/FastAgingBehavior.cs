﻿using Pacemaker.Extensions;

using System;
using System.Linq;

using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.Core;

namespace Pacemaker
{
    internal sealed class FastAgingBehavior : CampaignBehaviorBase
    {
        public FastAgingBehavior()
        {
            OnHeroComesOfAge = OnHeroComesOfAgeRM.GetDelegate<OnHeroComesOfAgeDelegate>(CampaignEventDispatcher.Instance);
            OnHeroReachesTeenAge = OnHeroReachesTeenAgeRM.GetDelegate<OnHeroReachesTeenAgeDelegate>(CampaignEventDispatcher.Instance);
            OnHeroGrowsOutOfInfancy = OnHeroGrowsOutOfInfancyRM.GetDelegate<OnHeroGrowsOutOfInfancyDelegate>(CampaignEventDispatcher.Instance);
        }

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(OnSessionLaunched));
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
        }

        public override void SyncData(IDataStore dataStore) { }

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            var agingBehavior = Campaign.Current.CampaignBehaviorManager.GetBehavior<AgingCampaignBehavior>();

            // Save these for later:
            adultAge = Campaign.Current.Models.AgeModel.HeroComesOfAge;
            teenAge = Campaign.Current.Models.AgeModel.BecomeTeenagerAge;
            childAge = Campaign.Current.Models.AgeModel.BecomeChildAge;
        }

        private void OnDailyTick()
        {
            bool adultAafEnabled = Main.Settings!.AdultAgeFactor > 1.02f;
            bool childAafEnabled = Main.Settings!.ChildAgeFactor > 1.02f;

            if (CampaignOptions.IsLifeDeathCycleDisabled)
                return;

            /* Send childhood growth stage transition events & perform AAF if enabled */

            // Subtract 1 for the daily tick's implicitly-aged day & the rest is
            // explicit, incremental age to add.
            var adultAgeDelta = CampaignTime.Days(Main.Settings.AdultAgeFactor - 1f);
            var childAgeDelta = CampaignTime.Days(Main.Settings.ChildAgeFactor - 1f);

            var oneDay = CampaignTime.Days(1f);

            foreach (var hero in Hero.AllAliveHeroes)
            {
                // When calculating the prevAge, we must take care to include the day
                // which the daily tick implicitly aged us since we last did this, or
                // else we could miss age transitions. Ergo, prevAge is the age we
                // were as if we were one day younger than our current BirthDay.
                int prevAge = (int)(hero.BirthDay + oneDay).ElapsedYearsUntilNow;

                if (adultAafEnabled && !hero.IsChild)
                    hero.SetBirthDay(hero.BirthDay - adultAgeDelta);
                else if (childAafEnabled && hero.IsChild)
                    hero.SetBirthDay(hero.BirthDay - childAgeDelta);

                hero.CharacterObject.Age = hero.Age;

                // And our new age, if different.
                int newAge = (int)hero.Age;

                // Did a relevant transition in age(s) occur?
                if (newAge > prevAge && prevAge < adultAge && !hero.IsTemplate)
                    ProcessAgeTransition(hero, prevAge, newAge);
            }
        }

        private void ProcessAgeTransition(Hero hero, int prevAge, int newAge)
        {
            // Loop over the aged years (extremely aggressive Days Per Season + AAF
            // could make it multiple), and thus we need to be able to handle the
            // possibility of multiple growth stage events needing to be fired.

            for (int age = prevAge + 1; age <= Math.Min(newAge, adultAge); ++age)
            {
                // This is a makeshift replacement for the interactive EducationCampaignBehavior,
                // but it applies to all children-- not just the player clan's:
                if (age == adultAge || GetChildAgeState(age) != ChildAgeState.Invalid)
                    ChildhoodSkillGrowth(hero);

                // This replaces AgingCampaignBehavior.OnDailyTick's campaign event triggers:

                if (age == childAge)
                    OnHeroGrowsOutOfInfancy(hero);

                if (age == teenAge)
                    OnHeroReachesTeenAge(hero);

                if (age == adultAge && !hero.IsActive)
                    OnHeroComesOfAge(hero);
            }
        }

        private void ChildhoodSkillGrowth(Hero child)
        {
            var skill = Skills.All
                .Where(s => child.GetAttributeValue(s.CharacterAttribute) < 3)
                .RandomPick();

            if (skill is null)
                return;

            child.HeroDeveloper.ChangeSkillLevel(skill, MBRandom.RandomInt(4, 6), false);
            child.HeroDeveloper.AddAttribute(skill.CharacterAttribute, 1, false);

            if (child.HeroDeveloper.CanAddFocusToSkill(skill))
                child.HeroDeveloper.AddFocus(skill, 1, false);
        }

        private static ChildAgeState GetChildAgeState(int age) => age switch
        {
            2  => ChildAgeState.Year2,
            5  => ChildAgeState.Year5,
            8  => ChildAgeState.Year8,
            11 => ChildAgeState.Year11,
            14 => ChildAgeState.Year14,
            16 => ChildAgeState.Year16,
            _  => ChildAgeState.Invalid
        };

        private enum ChildAgeState : short
        {
            Invalid = -1,
            Year2,
            Year5,
            Year8,
            Year11,
            Year14,
            Year16,
            Count,
            First = 0,
            Last = 5
        }

        // Year thresholds (cached):
        private int adultAge;
        private int teenAge;
        private int childAge;

        // Delegates, delegates, delegates...
        private delegate void UpdateHeroDeathProbabilitiesDelegate();
        private delegate void OnHeroComesOfAgeDelegate(Hero hero);
        private delegate void OnHeroReachesTeenAgeDelegate(Hero hero);
        private delegate void OnHeroGrowsOutOfInfancyDelegate(Hero hero);

        private UpdateHeroDeathProbabilitiesDelegate? UpdateHeroDeathProbabilities;
        private readonly OnHeroComesOfAgeDelegate OnHeroComesOfAge;
        private readonly OnHeroReachesTeenAgeDelegate OnHeroReachesTeenAge;
        private readonly OnHeroGrowsOutOfInfancyDelegate OnHeroGrowsOutOfInfancy;

        // Reflection for triggering campaign events & death probability updates & childhood education stage processing:
        private static readonly Reflect.Method<CampaignEventDispatcher> OnHeroComesOfAgeRM = new("OnHeroComesOfAge");
        private static readonly Reflect.Method<CampaignEventDispatcher> OnHeroReachesTeenAgeRM = new("OnHeroReachesTeenAge");
        private static readonly Reflect.Method<CampaignEventDispatcher> OnHeroGrowsOutOfInfancyRM = new("OnHeroGrowsOutOfInfancy");
    }
}
