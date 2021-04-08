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
        public double TotalObjectStrain { get; private set; }

        private List<double> osuStrainPeaks = new List<double>();

        protected OsuSkill(Mod[] mods) : base(mods)
        {
        }

        protected void AddTotalStrain(double strainValue)
        {
            TotalObjectStrain += strainValue + CurrentStrain;
        }

        protected double GetFlowProbability(double deltaTime, double distance, double angle)
        {
            angle = Math.Clamp(angle, Math.PI / 6, Math.PI / 2);
            double angleOffset = 10.0 * Math.Sin(1.5 * (Math.PI / 2 - angle));

            double distanceOffset = Math.Pow(distance, 1.7) / 325;
            return 1.0 / (1.0 + Math.Pow(Math.E, deltaTime - 126.0 + distanceOffset + angleOffset));
        }

        public void MergeStrainPeaks(Skill otherSkill)
        {
            for (int i = 0; i < StrainPeaks.Count; i++)
            {
                osuStrainPeaks.Add(Math.Max(StrainPeaks[i], otherSkill.StrainPeaks[i]));
            }
        }

        public double OsuDifficultyValue()
        {
            double difficulty = 0;
            double weight = 1;

            // Difficulty is the weighted sum of the highest strains from every section.
            // We're sorting from highest to lowest strain.
            foreach (double strain in osuStrainPeaks.OrderByDescending(d => d))
            {
                difficulty += strain * weight;
                weight *= DecayWeight;
            }

            return difficulty;
        }

        public double ConsistencyValue(double baseDifficulty)
        {
            IEnumerable<double> strains = osuStrainPeaks.Count == 0 ? StrainPeaks : osuStrainPeaks;

            double sigmoidScale = 6;
            double strainCutoffPerc = 0.6;
            double thresholdDistanceExp = 0.7;
            double minConsistency = 19;
            double maxConsistency = 38;

            double difficulty = 0;
            double weight = 1;

            double seriesFactor = -1 / (DecayWeight - 1);
            double baseStrain = baseDifficulty / seriesFactor;

            double upperRange = baseStrain * (1.0 - strainCutoffPerc);
            double bottomRange = baseStrain * strainCutoffPerc;

            double cutoffStrain;
            double thresholdDistanceFactor;

            int x = 0;
            foreach (double strain in strains.OrderByDescending(d => d))
            {
                cutoffStrain = Math.Clamp(strain - bottomRange, 0, upperRange);
                thresholdDistanceFactor = Math.Pow(cutoffStrain / upperRange, thresholdDistanceExp);

                difficulty += thresholdDistanceFactor * weight;

                weight = 1.0 / (1.0 + Math.Pow(Math.E, x / sigmoidScale - 6.0));
                x++;
            }

            return Math.Max(0, difficulty - minConsistency) / (maxConsistency - minConsistency);
        }

        public double LengthValue(double baseDifficulty, int totalHits)
        {
            double seriesFactor = -1 / (DecayWeight - 1);
            double baseStrain = baseDifficulty / seriesFactor;

            double fixedObjectStrain = totalHits * baseStrain * 1;
            double adjustedTotalObjStrain = TotalObjectStrain * 0;

            double lengthTotal = (fixedObjectStrain + adjustedTotalObjStrain) / baseStrain;

            return lengthTotal;
        }

        protected double ApplyDiminishingExp(double val) => Math.Pow(val, 0.99);
    }
}
