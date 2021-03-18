// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Difficulty;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Difficulty.Skills;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu.Difficulty.Preprocessing;
using osu.Game.Rulesets.Osu.Difficulty.Skills;
using osu.Game.Rulesets.Osu.Mods;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Osu.Scoring;
using osu.Game.Rulesets.Scoring;

namespace osu.Game.Rulesets.Osu.Difficulty
{
    public class OsuDifficultyCalculator : DifficultyCalculator
    {
        private const double difficulty_multiplier = 0.0675;

        public OsuDifficultyCalculator(Ruleset ruleset, WorkingBeatmap beatmap)
            : base(ruleset, beatmap)
        {
        }

        private List<double> calculateCombinedBonuses(Skill[] skills)
        {
            List<double> combinedBonuses = new List<double>();
            double aimDifficulty;
            double speedDifficulty;

            for (int i = 0; i < skills[0].StrainPeaks.Count; i++)
            {
                aimDifficulty = skills[0].StrainPeaks[i];
                speedDifficulty = skills[1].StrainPeaks[i];
                combinedBonuses.Add(aimDifficulty * speedDifficulty / 3600);
            }

            return combinedBonuses;
        }

        protected override DifficultyAttributes CreateDifficultyAttributes(IBeatmap beatmap, Mod[] mods, Skill[] skills, double clockRate)
        {
            if (beatmap.HitObjects.Count == 0)
                return new OsuDifficultyAttributes { Mods = mods, Skills = skills };

            List<double> combinedBonuses = calculateCombinedBonuses(skills);
            int totalHits = beatmap.HitObjects.Count(h => h is HitCircle || h is Slider);

            //bit of a discrepancy here because the combined bonus is not taken into account
            double aimTotal = (skills[0] as OsuSkill).LengthValue(totalHits) * 1.6;
            double speedTotal = (skills[1] as OsuSkill).LengthValue(totalHits) * 1.6;

            double aimDifficulty = (skills[0] as OsuSkill).CombinedDifficultyValue(combinedBonuses);
            double speedDifficulty = (skills[1] as OsuSkill).CombinedDifficultyValue(combinedBonuses);

            double aimRating = Math.Sqrt(aimDifficulty);
            double speedRating = Math.Sqrt(speedDifficulty);

            //We calculate the SR first to avoid unnecessary inflation, this should pose no problems as it is just a display value.
            double starRating = difficulty_multiplier * (aimRating + speedRating + Math.Abs(aimRating - speedRating)) / 2;

            //consistency
            double sigmoidScale = 6;
            double strainCutoffPerc = 0.6;
            double thresholdDistanceExp = 0.7;
            double minConsistency = 19;
            double maxConsistency = 38;

            double totalAimBonus = (skills[0] as OsuSkill).ConsistencyValue(aimRating, sigmoidScale, strainCutoffPerc, thresholdDistanceExp, minConsistency, maxConsistency);
            double totalSpeedBonus = (skills[1] as OsuSkill).ConsistencyValue(speedRating, sigmoidScale, strainCutoffPerc, thresholdDistanceExp, minConsistency, maxConsistency);

            totalAimBonus = Math.Pow(totalAimBonus, 0.7) * 0.045;
            totalSpeedBonus = Math.Pow(totalSpeedBonus, 0.7) * 0.045;

            //length bonus
            double aimLengthBonus = 1.0 + 0.5 * Math.Min(1.0, aimTotal / 2000.0) +
                                 (aimTotal > 2000 ? Math.Log10(aimTotal / 2000.0) * 0.25 : 0.0);
            double speedLengthBonus = 1.0 + 0.2 * Math.Min(1.0, speedTotal / 2000.0) +
                                 (speedTotal > 2000 ? Math.Log10(speedTotal / 2000.0) * 0.1 : 0.0);

            totalAimBonus += Math.Pow(aimLengthBonus, 0.33);
            totalSpeedBonus += Math.Pow(speedLengthBonus, 0.33);

            aimRating *= totalAimBonus * difficulty_multiplier;
            speedRating *= totalSpeedBonus * difficulty_multiplier;

            HitWindows hitWindows = new OsuHitWindows();
            hitWindows.SetDifficulty(beatmap.BeatmapInfo.BaseDifficulty.OverallDifficulty);

            // Todo: These int casts are temporary to achieve 1:1 results with osu!stable, and should be removed in the future
            double hitWindowGreat = (int)(hitWindows.WindowFor(HitResult.Great)) / clockRate;
            double preempt = (int)BeatmapDifficulty.DifficultyRange(beatmap.BeatmapInfo.BaseDifficulty.ApproachRate, 1800, 1200, 450) / clockRate;

            int maxCombo = beatmap.HitObjects.Count;
            // Add the ticks + tail of the slider. 1 is subtracted because the head circle would be counted twice (once for the slider itself in the line above)
            maxCombo += beatmap.HitObjects.OfType<Slider>().Sum(s => s.NestedHitObjects.Count - 1);

            int hitCirclesCount = beatmap.HitObjects.Count(h => h is HitCircle);
            int spinnerCount = beatmap.HitObjects.Count(h => h is Spinner);

            return new OsuDifficultyAttributes
            {
                StarRating = starRating,
                Mods = mods,
                AimStrain = aimRating,
                SpeedStrain = speedRating,
                ApproachRate = preempt > 1200 ? (1800 - preempt) / 120 : (1200 - preempt) / 150 + 5,
                OverallDifficulty = (80 - hitWindowGreat) / 6,
                MaxCombo = maxCombo,
                HitCircleCount = hitCirclesCount,
                SpinnerCount = spinnerCount,
                Skills = skills
            };
        }

        protected override IEnumerable<DifficultyHitObject> CreateDifficultyHitObjects(IBeatmap beatmap, double clockRate)
        {
            // The first jump is formed by the first two hitobjects of the map.
            // If the map has less than two OsuHitObjects, the enumerator will not return anything.
            for (int i = 1; i < beatmap.HitObjects.Count; i++)
            {
                var lastLast = i > 1 ? beatmap.HitObjects[i - 2] : null;
                var last = beatmap.HitObjects[i - 1];
                var current = beatmap.HitObjects[i];

                yield return new OsuDifficultyHitObject(current, lastLast, last, clockRate);
            }
        }

        protected override Skill[] CreateSkills(IBeatmap beatmap, Mod[] mods) => new Skill[]
        {
            new Aim(mods),
            new Speed(mods)
        };

        protected override Mod[] DifficultyAdjustmentMods => new Mod[]
        {
            new OsuModDoubleTime(),
            new OsuModHalfTime(),
            new OsuModEasy(),
            new OsuModHardRock(),
        };
    }
}
