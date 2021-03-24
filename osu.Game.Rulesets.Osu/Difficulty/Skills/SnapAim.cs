using System;
using System.Collections.Generic;
using System.Text;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu.Objects;

namespace osu.Game.Rulesets.Osu.Difficulty.Skills
{
    public class SnapAim : Aim
    {
        public SnapAim(Mod[] mods) : base(mods)
        {
        }
        protected override double StrainValueOf(DifficultyHitObject current)
        {
            if (current.BaseObject is Spinner)
                return 0;

            (double snapAim, _) = CalculateAimValues(current);

            double strainValue = snapAim;

            AddTotalStrain(strainValue);

            return strainValue;
        }
    }
}
