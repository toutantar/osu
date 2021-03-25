using System;
using System.Collections.Generic;
using System.Text;
using osu.Game.Rulesets.Mods;

namespace osu.Game.Rulesets.Osu.Difficulty.Skills
{
    public class Stamina : Speed
    {
        protected override double SkillMultiplier => base.SkillMultiplier * 0.8;
        protected override double StrainDecayBase => base.StrainDecayBase * 1.35;
        public Stamina(Mod[] mods) : base(mods)
        {

        }

        protected override double CalculateEstimatedPeakStrain(double strainTime)
        {
            return Math.Pow(1290 / strainTime, 1.84) + Math.Pow(200 / strainTime, 3.7);
        }
    }
}
