using System;
using System.Collections.Generic;
using System.Text;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu.Objects;

namespace osu.Game.Rulesets.Osu.Difficulty.Skills
{
    public class FlowAim : Aim
    {
        public FlowAim(Mod[] mods) : base(mods)
        {
        }
        protected override double StrainValueOf(DifficultyHitObject current)
        {
            if (current.BaseObject is Spinner)
                return 0;

            (_, double flowAim) = CalculateAimValues(current);

            double strainValue = flowAim;

            AddTotalStrain(strainValue);

            return strainValue;
        }
    }
}
