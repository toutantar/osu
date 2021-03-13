using System;
using System.Collections.Generic;
using System.Text;
using osu.Game.Rulesets.Mods;

namespace osu.Game.Rulesets.Osu.Difficulty.Skills
{
    public class AimStamina : Aim
    {
        public AimStamina(Mod[] mods) : base(mods)
        {
        }

        protected override double SkillMultiplier => base.SkillMultiplier * 0.81 * 0;
        protected override double StrainDecayBase => base.StrainDecayBase * 1.575;
    }
}
