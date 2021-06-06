// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
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
            return calculateDifficultyValue();
        }
    }
}
