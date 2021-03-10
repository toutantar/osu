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
    }
}
