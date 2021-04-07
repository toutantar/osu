using System;
using System.Collections.Generic;
using System.Text;
using osu.Game.Rulesets.Mods;

namespace osu.Game.Rulesets.Osu.Difficulty.Skills
{
    public class Stamina : Speed
    {
        protected override double SkillMultiplier => base.SkillMultiplier * 0.78 * 0;
        protected override double StrainDecayBase => base.StrainDecayBase * 1.4;
        public Stamina(Mod[] mods) : base(mods)
        {

        }

        protected override double CalculateEstimatedPeakStrain(double strainTime)
        {
            return Math.Pow(1360 / strainTime, 1.8) + Math.Pow(270 / strainTime, 3.2);
        }
    }
}
