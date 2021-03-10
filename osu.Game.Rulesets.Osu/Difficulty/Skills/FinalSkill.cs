using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Difficulty.Skills;
using osu.Game.Rulesets.Mods;

namespace osu.Game.Rulesets.Osu.Difficulty.Skills
{
    public class FinalSkill : Skill
    {
        public FinalSkill(Mod[] mods) : base(mods)
        {
        }

        protected override double SkillMultiplier => 0;

        protected override double StrainDecayBase => 0;

        protected override double StrainValueOf(DifficultyHitObject current)
        {
            return 0;
        }

        public double ConsistencyValue(double baseRating, double sigmoidScale, double strainCutoffPerc, double thresholdDistanceExp, double minConsistency, double maxConsistency)
        {
            double difficulty = 0;
            double weight = 1;

            double seriesFactor = -1 / (DecayWeight - 1);
            double baseStrain = Math.Pow(baseRating, 2) / seriesFactor;

            double upperRange = baseStrain * (1.0 - strainCutoffPerc);
            double bottomRange = baseStrain * strainCutoffPerc;

            double cutoffStrain;
            double thresholdDistanceFactor;

            int x = 0;
            foreach (double strain in StrainPeaks.OrderByDescending(d => d))
            {
                cutoffStrain = Math.Clamp(strain - bottomRange, 0, upperRange);
                thresholdDistanceFactor = Math.Pow(cutoffStrain / upperRange, thresholdDistanceExp);

                difficulty += thresholdDistanceFactor * weight;

                weight = 1.0 / (1.0 + Math.Pow(Math.E, x / sigmoidScale - 6.0));
                x++;
            }

            return Math.Max(0, difficulty - minConsistency) / (maxConsistency - minConsistency);
        }
    }
}
