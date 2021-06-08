﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Difficulty.Skills;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu.Difficulty.Preprocessing;
using osu.Game.Rulesets.Osu.Objects;
using osuTK;

namespace osu.Game.Rulesets.Osu.Difficulty.Skills
{
    /// <summary>
    /// Represents the skill required to correctly aim at every object in the map with a uniform CircleSize and normalized distances.
    /// </summary>
    public class Aim : OsuSkill
    {
        protected override double StarsPerDouble => 1.125;
        protected override int HistoryLength => 2;
        protected override int decayExcessThreshold => 500;
        protected override double baseDecay => 0.75;

        private double currStrain = 1;
        private double currSnapStrain = 1;
        private double currFlowStrain = 1;

        private double distanceConstant = 4;

        private List<double> snapStrains = new List<double>();
        private List<double> flowStrains = new List<double>();

        // Global Constants for the different types of aim.
        private double snapStrainMultiplier = 25.727;
        private double flowStrainMultiplier = 44.727;
        private double hybridStrainMultiplier = 0;//30.727;
        private double sliderStrainMultiplier = 75;
        private double totalStrainMultiplier = .1025;

        public Aim(Mod[] mods)
            : base(mods)
        {
        }

        /// <summary>
        /// Calculates the difficulty to flow from the previous <see cref="OsuDifficultyHitObject"/> the current <see cref="OsuDifficultyHitObject"/>.
        /// </summary>
        private double flowStrainAt(OsuDifficultyHitObject osuPrevObj, OsuDifficultyHitObject osuCurrObj, OsuDifficultyHitObject osuNextObj,
                                    Vector2 prevVector, Vector2 currVector, Vector2 nextVector)
        {
            double strain = 0;

            var observedCurrDistance = Vector2.Subtract(currVector, Vector2.Multiply(prevVector, (float)0.1));
            var observedPrevDistance = Vector2.Subtract(prevVector, Vector2.Multiply(currVector, (float)0.1));

            double prevAngularMomentumChange = Math.Abs(osuCurrObj.Angle * currVector.Length - osuPrevObj.Angle * prevVector.Length);
            double nextAngularMomentumChange = Math.Abs(osuCurrObj.Angle * currVector.Length - osuNextObj.Angle * nextVector.Length);

            double momentumChange = Math.Sqrt(Math.Min(currVector.Length, prevVector.Length)
                                               * Math.Max(0, prevVector.Length - currVector.Length));

            double angularMomentumChange = Math.Sqrt(Math.Min(currVector.Length, prevVector.Length)
                                                      * Math.Min(Math.PI / 2, Math.Abs(nextAngularMomentumChange - prevAngularMomentumChange)) / (2 * Math.PI));

            strain = osuCurrObj.FlowProbability * ((.75 * observedCurrDistance.Length + .25 * osuPrevObj.FlowProbability * observedPrevDistance.Length)
                     + Math.Max(momentumChange * (0.5 + 0.5 * osuPrevObj.FlowProbability),
                                angularMomentumChange * osuPrevObj.FlowProbability));

            return strain;
        }

        /// <summary>
        /// Alters the distance traveled for snapping to match the results from Fitt's law.
        /// </summary>
        private double snapScaling(double distance)
        {
            if (distance <= distanceConstant)
                return 1;
            else
                return (distanceConstant + (distance - distanceConstant) * (Math.Log(1 + (distance - distanceConstant) / Math.Sqrt(2)) / Math.Log(2)) / (distance - distanceConstant)) / distance;
        }

        /// <summary>
        /// Calculates the difficulty to snap from the previous <see cref="OsuDifficultyHitObject"/> the current <see cref="OsuDifficultyHitObject"/>.
        /// </summary>
        private double snapStrainAt(OsuDifficultyHitObject osuPrevObj, OsuDifficultyHitObject osuCurrObj, OsuDifficultyHitObject osuNextObj,
                                    Vector2 prevVector, Vector2 currVector, Vector2 nextVector)
        {
            double strain = 0;

            currVector = Vector2.Multiply(currVector, (float)snapScaling(osuCurrObj.JumpDistance / 100));
            prevVector = Vector2.Multiply(prevVector, (float)snapScaling(osuPrevObj.JumpDistance / 100));

            var observedDistance = Vector2.Add(currVector, Vector2.Multiply(prevVector, (float)(0.35 * osuPrevObj.SnapProbability)));

            strain = observedDistance.Length * osuCurrObj.SnapProbability;

            strain *= Math.Min(osuCurrObj.StrainTime / (osuCurrObj.StrainTime - 20) , osuPrevObj.StrainTime / (osuPrevObj.StrainTime - 20));
            // buff high BPM slightly.

            return strain;
        }

        /// <summary>
        /// Calculates the difficulty to flow from the previous <see cref="OsuDifficultyHitObject"/> the current <see cref="OsuDifficultyHitObject"/> with context to odd patterns.
        /// </summary>
        private double hybridStrainAt(OsuDifficultyHitObject osuPrevObj, OsuDifficultyHitObject osuCurrObj, OsuDifficultyHitObject osuNextObj,
                                      Vector2 prevVector, Vector2 currVector, Vector2 nextVector)
        {
            double strain = 0;

            double prevAngularMomentumChange = Math.Abs(osuCurrObj.Angle * currVector.Length - osuPrevObj.Angle * prevVector.Length);
            double nextAngularMomentumChange = Math.Abs(osuCurrObj.Angle * currVector.Length - osuNextObj.Angle * nextVector.Length);

            double angularMomentumChange = Math.Min(Math.PI / 2, Math.Abs(nextAngularMomentumChange - prevAngularMomentumChange)) / (2 * Math.PI);
            // buff for changes in angular momentum, but only if the momentum change doesnt equal the previous.

            double momentumChange = Math.Max(0, prevVector.Length - currVector.Length);
            // reward for accelerative changes in momentum

            strain = osuCurrObj.FlowProbability * Math.Sqrt(Math.Min(currVector.Length, prevVector.Length)
                                                            * Math.Max(momentumChange * (0.5 + 0.5 * osuPrevObj.FlowProbability),
                                                                       angularMomentumChange * osuPrevObj.FlowProbability));

            return strain;
        }

        /// <summary>
        /// Calculates the estimated difficulty associated with the slider movement from the previous <see cref="OsuDifficultyHitObject"/> to the current <see cref="OsuDifficultyHitObject"/>.
        /// </summary>
        private double sliderStrainAt(OsuDifficultyHitObject osuPrevObj, OsuDifficultyHitObject osuCurrObj, OsuDifficultyHitObject osuNextObj)
        {
            double strain = (osuPrevObj.TravelDistance) / osuPrevObj.StrainTime;

            return strain;
        }

        protected override double strainValueAt(DifficultyHitObject current)
        {
            if (current.BaseObject is Spinner)
                return 0;

            var osuCurrent = (OsuDifficultyHitObject)current;

            double strain = 0;

            if (Previous.Count > 1)
            {
                var osuNextObj = (OsuDifficultyHitObject)current;
                var osuCurrObj = (OsuDifficultyHitObject)Previous[0];
                var osuPrevObj = (OsuDifficultyHitObject)Previous[1];
                // Since it is easier to get history, we take the previous[0] as our current, so we can see our "next"

                Vector2 nextVector = Vector2.Divide(osuNextObj.DistanceVector, (float)osuNextObj.StrainTime);
                Vector2 currVector = Vector2.Divide(osuCurrObj.DistanceVector, (float)osuCurrObj.StrainTime);
                Vector2 prevVector = Vector2.Divide(osuPrevObj.DistanceVector, (float)osuPrevObj.StrainTime);

                double snapStrain = snapStrainAt(osuPrevObj,
                                                 osuCurrObj,
                                                 osuNextObj,
                                                 prevVector,
                                                 currVector,
                                                 nextVector);

                double flowStrain = flowStrainAt(osuPrevObj,
                                                 osuCurrObj,
                                                 osuNextObj,
                                                 prevVector,
                                                 currVector,
                                                 nextVector);

                double hybridStrain = hybridStrainAt(osuPrevObj,
                                                     osuCurrObj,
                                                     osuNextObj,
                                                     prevVector,
                                                     currVector,
                                                     nextVector);

                double sliderStrain = sliderStrainAt(osuPrevObj,
                                                     osuCurrObj,
                                                     osuNextObj);
                // Currently passing all available data, just incase it is useful for calculation.

                currSnapStrain *= computeDecay(baseDecay, osuCurrent.StrainTime);
                currSnapStrain += totalStrainMultiplier * snapStrain * snapStrainMultiplier;

                currFlowStrain *= computeDecay(baseDecay, osuCurrent.StrainTime);
                currFlowStrain += totalStrainMultiplier * flowStrain * flowStrainMultiplier;

                currStrain *= computeDecay(baseDecay, osuCurrent.StrainTime);
                currStrain += snapStrain * snapStrainMultiplier;
                currStrain += flowStrain * flowStrainMultiplier;
                currStrain += hybridStrain * hybridStrainMultiplier;
                currStrain += sliderStrain * sliderStrainMultiplier;

                flowStrains.Add(currFlowStrain);
                snapStrains.Add(currSnapStrain);

                strain = totalStrainMultiplier * currStrain;
            }

            return strain;
        }

        public double calculateFlowDifficulty()
        {
            return calculateDifficultyValue(flowStrains, StarsPerDouble);
        }

        public double calculateSnapDifficulty()
        {
            return calculateDifficultyValue(snapStrains, StarsPerDouble);
        }
    }
}
