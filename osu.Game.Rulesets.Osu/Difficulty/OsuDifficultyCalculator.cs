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

        private double calculateMixedAimBonus(SnapAim snapAim, FlowAim flowAim)
        {
            /*double flowDifficulty = flowAim.DifficultyValue();
            double snapDifficulty = snapAim.DifficultyValue();

            double flowConsistency = flowAim.ConsistentDifficultyValue(flowDifficulty);
            double snapConsistency = snapAim.ConsistentDifficultyValue(snapDifficulty);

            double flowRating = difficulty_multiplier * Math.Sqrt(flowDifficulty) * Math.Pow(Math.Log10(1 + flowConsistency / 4), 0.1);
            double snapRating = difficulty_multiplier * Math.Sqrt(snapDifficulty) * Math.Pow(Math.Log10(1 + snapConsistency / 4), 0.1);

            double difference = Math.Abs(flowRating - snapRating) / Math.Max(flowRating, snapRating);

            double mixedAimBonus = Math.Max(0, -Math.Pow(difference * 3.0, 1.5) + 1.0);

            return 1.0 + mixedAimBonus * 0.03;*/

            double flowDifficulty = flowAim.DifficultyValue();
            double snapDifficulty = snapAim.DifficultyValue();

            double flowRating = difficulty_multiplier * Math.Sqrt(flowDifficulty);
            double snapRating = difficulty_multiplier * Math.Sqrt(snapDifficulty);


            double lpExp = 3.0;

            double mixedValue =
                Math.Pow(
                    Math.Pow(flowRating, lpExp) +
                    Math.Pow(snapRating, lpExp), 1.0 / lpExp
                );

            //Console.WriteLine(mixedValue);

            //mixedValue = (flowRating + snapRating) / 2.0;
            //Console.WriteLine(mixedValue);

            Console.WriteLine("flow " + flowRating);
            Console.WriteLine("snap " + snapRating);
            //Console.WriteLine("mixed " + mixedValue);

            return mixedValue; // - Math.Max(flowRating, snapRating)
        }

        protected override DifficultyAttributes CreateDifficultyAttributes(IBeatmap beatmap, Mod[] mods, Skill[] skills, double clockRate)
        {
            if (beatmap.HitObjects.Count == 0)
                return new OsuDifficultyAttributes { Mods = mods, Skills = skills };

            (skills[1] as Speed).MergeStrainPeaks(skills[4]);

            /*foreach (double strain in skills[0].StrainPeaks)
                Console.WriteLine(strain.ToString(System.Globalization.CultureInfo.InvariantCulture));*/
            Console.WriteLine(beatmap.Metadata.Title);


            double aimRating = Math.Sqrt(skills[0].DifficultyValue()) * difficulty_multiplier;
            double speedRating = Math.Sqrt((skills[1] as OsuSkill).OsuDifficultyValue()) * difficulty_multiplier;

            double mixedRating = calculateMixedAimBonus(skills[2] as SnapAim, skills[3] as FlowAim);

            Console.WriteLine("aim " + aimRating);
            //Console.WriteLine(mixedRating);

            //aimRating = (aimRating + mixedRating) / 2.0;

            /*aimRating =
                Math.Pow(
                    Math.Pow(aimRating, 3.0) +
                    Math.Pow(mixedRating, 3.0), 1.0 / 3.0
                );*/

            //lp_sum( (lp_sum(flow, snap) - combined), combined)

            /*Console.WriteLine(aimRating);

            aimRating =
                Math.Pow(
                    Math.Pow(Math.Max(0, mixedRating), 9) +
                    Math.Pow(aimRating, 9), 1.0 / 9
                );

            Console.WriteLine(aimRating);*/

            //aimRating = mixedRating;

            //Console.WriteLine((aimRating/asd).ToString(System.Globalization.CultureInfo.InvariantCulture));

            double starRating = aimRating + speedRating + Math.Abs(aimRating - speedRating) / 2;

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

        public override void ProcessSkills(Skill[] skills, DifficultyHitObject h)
        {
            foreach(Skill skill in skills)
            {
                if (skill is Aim)
                    (skill as Aim).TappingStrain = Math.Max(skills[1].CurrentStrain, skills[4].CurrentStrain);
            }

            base.ProcessSkills(skills, h);
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
            new SnapAim(mods),
            new FlowAim(mods),
            new Stamina(mods)
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
