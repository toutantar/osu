using System;
using System.Collections.Generic;
using System.Text;
using osu.Game.Rulesets.Mods;

namespace osu.Game.Rulesets.Osu.Difficulty.Skills
{
    public class SpeedStamina : Speed
    {
        public SpeedStamina(Mod[] mods) : base(mods)
        {
        }

        protected override double SkillMultiplier => base.SkillMultiplier * 0.8135;
        protected override double StrainDecayBase => base.StrainDecayBase * 1.3;
    }
}
