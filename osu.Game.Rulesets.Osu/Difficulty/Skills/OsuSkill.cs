﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Difficulty.Skills;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu.Difficulty.Preprocessing;
using osu.Game.Rulesets.Osu.Objects;

namespace osu.Game.Rulesets.Osu.Difficulty.Skills
{
    public abstract class OsuSkill : Skill
    {
        private readonly List<double> strains = new List<double>();
        private readonly List<double> times = new List<double>();
        private double target_fc_precision = 0.01;
        private double target_fc_time = 30 * 60 * 1000; // estimated time it takes us to FC (30 minutes)

        protected virtual int decayExcessThreshold => 500;
        protected virtual double baseDecay => .75;

        protected virtual double StarsPerDouble => 1.0;

        private double difficultyExponent => 1.0 / Math.Log(StarsPerDouble, 2);

        protected OsuSkill(Mod[] mods) : base(mods)
        {
        }

        /// <summary>
        /// The calculated strain value associated with this <see cref="DifficultyHitObject"/>.
        /// </summary>
        /// <param name="current">The current DifficultyHitObject being processed.</param>
        protected abstract double strainValueAt(DifficultyHitObject current);

        /// <summary>
        /// Utility to decay strain over a period of deltaTime.
        /// </summary>
        /// <param name="baseDecay">The rate of decay per object.</param>
        /// <param name="deltaTime">The time between objects.</param>
        protected double computeDecay(double baseDecay, double deltaTime)
        {
            double decay = 0;
            if (deltaTime < decayExcessThreshold)
                decay = baseDecay;
            else // Beyond 500 MS (or whatever decayExcessThreshold is), we decay geometrically to avoid keeping strain going over long breaks.
                decay = Math.Pow(Math.Pow(baseDecay, 1000 / Math.Min(deltaTime, decayExcessThreshold)), deltaTime / 1000);

            return decay;
        }

        protected override void Process(DifficultyHitObject current)
        {
            strains.Add(strainValueAt(current));
            times.Add(current.StartTime);
        }

        /// <summary>
        /// The total summarized difficulty value of all strains for every <see cref="DifficultyHitObject"/> in the beatmap.
        /// </summary>
        private double calculateDifficultyValue()
        {
            double difficultyExponent = 1.0 / Math.Log(StarsPerDouble, 2);
            double SR = 0;

            // Math here preserves the property that two notes of equal difficulty x, we have their summed difficulty = x*StarsPerDouble
            // This also applies to two sets of notes with equal difficulty.

            for (int i = 0; i < strains.Count; i++)
            {
                SR += Math.Pow(strains[i], difficultyExponent);
            }

            return Math.Pow(SR, 1.0 / difficultyExponent);
        }

        /// <summary>
        /// The peak difficulty value of the map. Used to calculate the total star rating.
        /// </summary>
        public double CalculateDisplayDifficultyValue()
        {
            double difficulty = 0;
            double weight = 1;
            double decayWeight = 0.9;

            // Difficulty is the weighted sum of the highest strains from every section.
            // We're sorting from highest to lowest strain.
            foreach (double strain in strains.OrderByDescending(d => d))
            {
                difficulty += strain * weight;
                weight *= decayWeight;
            }

            return difficulty;
        }

        public override double DifficultyValue()
        {
            return fcTimeSkillLevel(calculateDifficultyValue());
        }

        /// <summary>
        /// The probability a player of the given skill full combos a map of the given difficulty.
        /// </summary>
        /// <param name="skill">The skill level of the player.</param>
        /// <param name="difficulty">The difficulty of a range of notes.</param>
        private double fcProbability(double skill, double difficulty) => Math.Exp(-Math.Pow(difficulty / Math.Max(1e-10, skill), difficultyExponent));


        /// <summary>
        /// Approximates the skill level of a player that can FC a map with the given <paramref name="difficulty"/>,
        /// if their probability of success in doing so is equal to <paramref name="probability"/>.
        /// </summary>
        private double skillLevel(double probability, double difficulty) => difficulty * Math.Pow(-Math.Log(probability), -1 / difficultyExponent);

        // A implementation to not overbuff length with a long map. Longer maps = more retries.
        private double expectedTargetTime(double totalDifficulty)
        {
            double targetTime = 0;

            for (int i=1;i<strains.Count;i++)
            {
                targetTime += Math.Min(2000, times[i] - times[i-1]) * (strains[i] / totalDifficulty);
            }

            return targetTime;
        }

        private double expectedFcTime(double skill)
        {
            double last_timestamp = times[0]-5; // time taken to retry map
            double fcTime = 0;

            for (int i=0;i<strains.Count;i++)
            {
                double dt = times[i]-last_timestamp;
                last_timestamp = times[i];
                fcTime = (fcTime + dt) / fcProbability(skill, strains[i]);
            }
            return fcTime - (times[times.Count - 1] - times[0]);
        }

        /// <summary>
        /// The final estimated skill level necessary to full combo the entire beatmap.
        /// </summary>
        /// <param name="totalDifficulty">The total difficulty of all objects in the map.</param>
        private double fcTimeSkillLevel(double totalDifficulty)
        {
            double lengthEstimate = 0.4 * (times[times.Count - 1] - times[0]);
            target_fc_time += 30 * Math.Max(0, expectedTargetTime(totalDifficulty) - 60000);
            // for every 30 seconds past 3 mins, add 5 mins to estimated time to FC. ^
            double fcProb = lengthEstimate / target_fc_time;
            double skill = skillLevel(fcProb, totalDifficulty);
            for (int i=0; i<5; ++i)
            {
                double fcTime = expectedFcTime(skill);
                lengthEstimate = fcTime * fcProb;
                fcProb = lengthEstimate / target_fc_time;
                skill = skillLevel(fcProb, totalDifficulty);
                if (Math.Abs(fcTime - target_fc_time) < target_fc_precision * target_fc_time)
                {
                    //enough precision
                    break;
                }
            }
            return skill;
        }
    }
}
