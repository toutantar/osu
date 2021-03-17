using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Difficulty.Skills;
using osu.Game.Rulesets.Mods;

namespace osu.Game.Rulesets.Osu.Difficulty.Skills
{
    public abstract class OsuSkill : Skill
    {
        public double TotalObjectStrain { get; set; }

        private List<double> combinedStrainPeaks = new List<double>();

        protected OsuSkill(Mod[] mods) : base(mods)
        {
        }

        public double CombinedDifficultyValue(List<double> combinedStrains)
        {
            double difficulty = 0;
            double weight = 1;

            for(int i = 0; i < StrainPeaks.Count; i++)
            {
                combinedStrainPeaks.Add(StrainPeaks[i] + combinedStrains[i]);
            }

            // Difficulty is the weighted sum of the highest strains from every section.
            // We're sorting from highest to lowest strain.
            foreach (double strain in combinedStrainPeaks.OrderByDescending(d => d))
            {
                difficulty += strain * weight;
                weight *= DecayWeight;
            }

            return difficulty;
        }

        public double LengthValue(int totalHits)
        {
            double seriesFactor = -1 / (DecayWeight - 1);
            double baseStrain = DifficultyValue() / seriesFactor;

            double fixedObjectStrain = totalHits * baseStrain * 0.25;
            double adjustedTotalObjStrain = TotalObjectStrain * 0.75;

            double lengthTotal = (fixedObjectStrain + adjustedTotalObjStrain) / baseStrain;

            return lengthTotal;
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
            foreach (double strain in combinedStrainPeaks.OrderByDescending(d => d))
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
