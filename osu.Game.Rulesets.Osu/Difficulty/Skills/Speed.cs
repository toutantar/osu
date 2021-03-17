// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Difficulty.Skills;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu.Difficulty.Preprocessing;
using osu.Game.Rulesets.Osu.Objects;

namespace osu.Game.Rulesets.Osu.Difficulty.Skills
{
    /// <summary>
    /// Represents the skill required to press keys with regards to keeping up with the speed at which objects need to be hit.
    /// </summary>
    public class Speed : OsuSkill
    {
        protected override double SkillMultiplier => 1400;
        protected override double StrainDecayBase => 0.3;

        private const double min_speed_bonus = 75; // ~200BPM
        private const double max_speed_bonus = 45; // ~330BPM
        private const double speed_balancing_factor = 40;

        public Speed(Mod[] mods)
            : base(mods)
        {
        }

        protected override double StrainValueOf(DifficultyHitObject current)
        {
            if (current.BaseObject is Spinner)
                return 0;

            var osuCurrent = (OsuDifficultyHitObject)current;

            double deltaTime = Math.Max(max_speed_bonus, current.DeltaTime);

            double speedBonus = 0;
            if (deltaTime < min_speed_bonus)
                speedBonus = Math.Pow((min_speed_bonus - deltaTime) / speed_balancing_factor, 2) * 0.7;

            double rhythmBonus = 0;
            if (Previous.Count > 0)
            {
                var osuPrevious = (OsuDifficultyHitObject)Previous[0];
                double timingDistance = Math.Abs(osuCurrent.DeltaTime - osuPrevious.DeltaTime);
                rhythmBonus = Math.Pow(Math.Min(Math.Max(timingDistance - 5.0, 0) / 10.0, 1.0), 2.0) * 0.1;
            }

            double strainValue = (1.0 + speedBonus + rhythmBonus) / osuCurrent.StrainTime;

            TotalObjectStrain += strainValue + CurrentStrain;

            return strainValue;
        }
    }
}
