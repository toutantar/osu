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
    /// Represents the skill required to correctly aim at every object in the map with a uniform CircleSize and normalized distances.
    /// </summary>
    public class Aim : Skill
    {
        private const double angle_bonus_begin = Math.PI / 3;
        private const double timing_threshold = 107;

        private const double control_spacing_scale = 3.5;

        private const double repeatjump_min_spacing = 3 * 52;
        private const double repeatjump_max_spacing = 6 * 52;

        public Aim(Mod[] mods)
            : base(mods)
        {
        }

        protected override double SkillMultiplier => 26.25;
        protected override double StrainDecayBase => 0.15;

        protected override double StrainValueOf(DifficultyHitObject current)
        {
            if (current.BaseObject is Spinner)
                return 0;

            var osuCurrent = (OsuDifficultyHitObject)current;

            double result = 0;
            double controlBonus = 0;

            if (Previous.Count > 0)
            {
                var osuPrevious = (OsuDifficultyHitObject)Previous[0];

                if (osuCurrent.Angle != null && osuCurrent.Angle.Value > angle_bonus_begin)
                {
                    const double scale = 90;

                    var angleBonus = Math.Sqrt(
                        Math.Max(osuPrevious.JumpDistance - scale, 0)
                        * Math.Pow(Math.Sin(osuCurrent.Angle.Value - angle_bonus_begin), 2)
                        * Math.Max(osuCurrent.JumpDistance - scale, 0));
                    result = 1.5 * applyDiminishingExp(Math.Max(0, angleBonus)) / Math.Max(timing_threshold, osuPrevious.StrainTime);
                }

                controlBonus = calculateControlBonus(osuCurrent, osuPrevious);
            }

            double jumpDistanceExp = applyDiminishingExp(osuCurrent.JumpDistance);
            double travelDistanceExp = applyDiminishingExp(osuCurrent.TravelDistance);

            if (osuCurrent.OverlapScaling != null)
            {
                jumpDistanceExp *= osuCurrent.OverlapScaling.Value;
                travelDistanceExp *= osuCurrent.OverlapScaling.Value;
            }

            double repeatJumpPenalty = calculateRepeatJumpPenalty(osuCurrent);

            double aimValue = Math.Max(
                result + (jumpDistanceExp + travelDistanceExp + Math.Sqrt(travelDistanceExp * jumpDistanceExp)) / Math.Max(osuCurrent.StrainTime, timing_threshold),
                (Math.Sqrt(travelDistanceExp * jumpDistanceExp) + jumpDistanceExp + travelDistanceExp) / osuCurrent.StrainTime
            ) + controlBonus;


            return (1.0 - repeatJumpPenalty) * aimValue;
        }

        private double calculateControlBonus(OsuDifficultyHitObject osuCurrent, OsuDifficultyHitObject osuPrevious)
        {
            var prevCursSpeed = Math.Max(1, applyDiminishingExp(osuPrevious.JumpDistance) / osuPrevious.StrainTime * control_spacing_scale);
            var curCursSpeed = Math.Max(1, applyDiminishingExp(osuCurrent.JumpDistance) / osuCurrent.StrainTime * control_spacing_scale);

            var speedRatio = Math.Max(prevCursSpeed / curCursSpeed, curCursSpeed / prevCursSpeed);

            var speedChange = applyDiminishingExp(Math.Pow(speedRatio, 2.5)) / (osuCurrent.StrainTime + osuPrevious.StrainTime);
            return speedChange * osuCurrent.JumpDistance / 1500;
        }

        private double calculateRepeatJumpPenalty(OsuDifficultyHitObject osuCurrent)
        {
            if (osuCurrent.LastDistance == null)
                return 0;

            if (osuCurrent.JumpDistance < repeatjump_min_spacing)
                return 0;

            const double spacing_range = repeatjump_max_spacing - repeatjump_min_spacing;
            double spacingScale = Math.Min(osuCurrent.JumpDistance - repeatjump_min_spacing, spacing_range) / spacing_range;

            const double start_distance = 2.5 * 52;
            double jumpScale = osuCurrent.JumpDistance / (6 * 52);
            double scaledStartDistance = jumpScale * start_distance;
            double innerDistance = Math.Max(scaledStartDistance - (double)osuCurrent.LastDistance, 0) / scaledStartDistance;

            return innerDistance * spacingScale * 0.1;
        }

        private double applyDiminishingExp(double val) => Math.Pow(val, 0.99);
    }
}
