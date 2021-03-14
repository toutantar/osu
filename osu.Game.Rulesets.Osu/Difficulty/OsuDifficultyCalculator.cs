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
        private const double c = 0.6;
        private const double beta = 0.3;

        private const double difficulty_multiplier = 0.0675;

        private FinalSkill finalAim = new FinalSkill(null);
        private FinalSkill finalSpeed = new FinalSkill(null);

        public OsuDifficultyCalculator(Ruleset ruleset, WorkingBeatmap beatmap)
            : base(ruleset, beatmap)
        {
        }

        private void processFinalSkills(Skill[] skills, IBeatmap beatmap)
        {
            double combinedBonus;
            double aimDifficulty;
            double speedDifficulty;
            for (int i = 0; i < skills[0].StrainPeaks.Count; i++)
            {
                aimDifficulty = Math.Max(skills[0].StrainPeaks[i], skills[2].StrainPeaks[i]);
                speedDifficulty = Math.Max(skills[1].StrainPeaks[i], skills[3].StrainPeaks[i]);
                combinedBonus = aimDifficulty * speedDifficulty / 1800;

                finalAim.StorePeak(aimDifficulty + combinedBonus);
                finalSpeed.StorePeak(speedDifficulty + combinedBonus);
            }
        }

        protected override DifficultyAttributes CreateDifficultyAttributes(IBeatmap beatmap, Mod[] mods, Skill[] skills, double clockRate)
        {
            if (beatmap.HitObjects.Count == 0)
                return new OsuDifficultyAttributes { Mods = mods, Skills = skills };

            processFinalSkills(skills, beatmap);

            double aimDifficulty = finalAim.DifficultyValue();
            double speedDifficulty = finalSpeed.DifficultyValue();

            double aimTotal = finalAim.TotalStrain;
            double speedTotal = finalSpeed.TotalStrain;

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

            double totalAimBonus = finalAim.ConsistencyValue(aimRating, sigmoidScale, strainCutoffPerc, thresholdDistanceExp, minConsistency, maxConsistency);
            double totalSpeedBonus = finalSpeed.ConsistencyValue(speedRating, sigmoidScale, strainCutoffPerc, thresholdDistanceExp, minConsistency, maxConsistency);

            totalAimBonus = Math.Pow(totalAimBonus, 0.7) * 0.045;
            totalSpeedBonus = Math.Pow(totalSpeedBonus, 0.7) * 0.035;

            totalAimBonus += Math.Pow(c + beta * Math.Log((aimTotal + aimDifficulty) / aimDifficulty, 10), 0.33);
            totalSpeedBonus += Math.Pow(c + beta * Math.Log((speedTotal + speedDifficulty) / speedDifficulty, 10), 0.33);

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
            new Speed(mods),
            new AimStamina(mods),
            new SpeedStamina(mods)
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
