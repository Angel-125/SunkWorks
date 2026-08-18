using System;
using System.Collections.Generic;
using UnityEngine;

#pragma warning disable 1591

namespace SunkWorks.Submarine
{
    /// <summary>Identifies the proportional fill group used by the longitudinal solver.</summary>
    public enum LongitudinalTrimGroup
    {
        None,
        Forward,
        Aft
    }

    /// <summary>Describes why a longitudinal trim analysis did not produce a normal solution.</summary>
    public enum TrimLimitingCondition
    {
        None,
        NoCraft,
        NoForwardTrim,
        NoAftTrim,
        NoLongitudinalSeparation,
        BowHeavy,
        SternHeavy,
        TankAtLimit,
        BuoyancyUnavailable
    }

    /// <summary>
    /// Pure scalar snapshot of one editor part. Positions are metres along root-part local +Y,
    /// which is positive toward the vessel's nominal bow. A positive trim error is bow-down.
    /// </summary>
    public sealed class LongitudinalPartSnapshot
    {
        public double massWithoutTrimBallast;
        public double massPosition;
        public double buoyancyPosition;
        public double fixedBuoyancy;
        public LongitudinalTrimGroup trimGroup;
        public double ballastCapacityMass;
        public double unitBuoyancy;
        public double emptyBuoyancyCoefficient;
        public double fullBuoyancyCoefficient;
    }

    /// <summary>Input to the KSP-independent two-variable trim solver.</summary>
    public sealed class LongitudinalTrimInput
    {
        public readonly List<LongitudinalPartSnapshot> parts = new List<LongitudinalPartSnapshot>();
        public int forwardTankCount;
        public int aftTankCount;
        public double forwardCapacityMass;
        public double aftCapacityMass;
        public double currentForwardFillFraction;
        public double currentAftFillFraction;
        public double forwardMeanPosition;
        public double aftMeanPosition;
        public double surfaceGravity = 9.80665;
    }

    /// <summary>Structured result shared by the editor UI and future automation.</summary>
    public sealed class TrimAnalysisResult
    {
        public bool calculationSucceeded;
        public bool canAchieveLevelTrim;
        public int forwardTankCount;
        public int aftTankCount;
        public double forwardCapacityMass;
        public double aftCapacityMass;
        public double suggestedForwardFillFraction;
        public double suggestedAftFillFraction;
        public double longitudinalCoM;
        public double longitudinalCoB;
        public double trimOffset;
        public double residualPitchMoment;
        public double bowDownAuthority;
        public double sternDownAuthority;
        public TrimLimitingCondition limitingCondition;
        public string diagnosticDetails;
    }

    /// <summary>
    /// Pure longitudinal solver. All tanks of a role share one fill fraction, matching the dive
    /// computer's behavior of commanding every tank in that role together. The fill-to-error
    /// relationship is sampled as a bounded 2-D envelope and every sampled sign-changing edge
    /// is refined with bisection; no linearity assumption is made.
    /// </summary>
    public static class LongitudinalTrimSolver
    {
        public const double LevelTolerance = 0.02; // metres between effective CoM and CoB
        public const double SeparationTolerance = 0.25;
        public const double TankLimitTolerance = 0.02;
        const int GridSteps = 20;
        const int BisectionIterations = 40;

        struct State
        {
            public double forwardFill;
            public double aftFill;
            public double mass;
            public double centerOfMass;
            public double centerOfBuoyancy;
            public double error;
        }

        public static TrimAnalysisResult Analyze(LongitudinalTrimInput input)
        {
            TrimAnalysisResult result = new TrimAnalysisResult();
            if (input == null || input.parts.Count == 0)
            {
                result.limitingCondition = TrimLimitingCondition.NoCraft;
                result.diagnosticDetails = "No editor craft is available.";
                return result;
            }

            result.forwardTankCount = input.forwardTankCount;
            result.aftTankCount = input.aftTankCount;
            result.forwardCapacityMass = input.forwardCapacityMass;
            result.aftCapacityMass = input.aftCapacityMass;

            State best = Evaluate(input, input.currentForwardFillFraction, input.currentAftFillFraction);
            if (double.IsNaN(best.error))
            {
                result.limitingCondition = TrimLimitingCondition.BuoyancyUnavailable;
                result.diagnosticDetails = "The effective buoyancy distribution could not be determined.";
                return result;
            }

            double minimumError = double.PositiveInfinity;
            double maximumError = double.NegativeInfinity;
            State minimumState = best;
            State maximumState = best;
            List<State> grid = new List<State>((GridSteps + 1) * (GridSteps + 1));
            for (int forwardIndex = 0; forwardIndex <= GridSteps; forwardIndex++)
            {
                double forwardFill = (double)forwardIndex / GridSteps;
                for (int aftIndex = 0; aftIndex <= GridSteps; aftIndex++)
                {
                    double aftFill = (double)aftIndex / GridSteps;
                    State state = Evaluate(input, forwardFill, aftFill);
                    grid.Add(state);
                    if (state.error < minimumError)
                    {
                        minimumError = state.error;
                        minimumState = state;
                    }
                    if (state.error > maximumError)
                    {
                        maximumError = state.error;
                        maximumState = state;
                    }
                    if (IsBetterSolution(state, best, input))
                        best = state;
                }
            }

            // Refine all horizontal and vertical grid edges that bracket zero. Searching every
            // edge also handles a curved or non-monotonic zero contour deterministically.
            for (int forwardIndex = 0; forwardIndex <= GridSteps; forwardIndex++)
            {
                for (int aftIndex = 0; aftIndex <= GridSteps; aftIndex++)
                {
                    int index = forwardIndex * (GridSteps + 1) + aftIndex;
                    if (forwardIndex < GridSteps)
                        ConsiderEdge(input, grid[index], grid[index + GridSteps + 1], ref best);
                    if (aftIndex < GridSteps)
                        ConsiderEdge(input, grid[index], grid[index + 1], ref best);
                }
            }

            bool hasForward = input.forwardTankCount > 0 && input.forwardCapacityMass > 0;
            bool hasAft = input.aftTankCount > 0 && input.aftCapacityMass > 0;
            bool separated = Math.Abs(input.forwardMeanPosition - input.aftMeanPosition) >= SeparationTolerance;
            bool zeroInEnvelope = minimumError <= LevelTolerance && maximumError >= -LevelTolerance;

            result.calculationSucceeded = true;
            result.suggestedForwardFillFraction = best.forwardFill;
            result.suggestedAftFillFraction = best.aftFill;
            result.longitudinalCoM = best.centerOfMass;
            result.longitudinalCoB = best.centerOfBuoyancy;
            result.trimOffset = best.error;
            result.residualPitchMoment = best.mass * input.surfaceGravity * best.error;
            result.bowDownAuthority = Math.Max(0,
                maximumError * maximumState.mass * input.surfaceGravity);
            result.sternDownAuthority = Math.Max(0,
                -minimumError * minimumState.mass * input.surfaceGravity);
            result.canAchieveLevelTrim = hasForward && hasAft && separated && zeroInEnvelope &&
                Math.Abs(best.error) <= LevelTolerance;

            if (!hasForward)
                result.limitingCondition = TrimLimitingCondition.NoForwardTrim;
            else if (!hasAft)
                result.limitingCondition = TrimLimitingCondition.NoAftTrim;
            else if (!separated)
                result.limitingCondition = TrimLimitingCondition.NoLongitudinalSeparation;
            else if (!result.canAchieveLevelTrim)
                result.limitingCondition = best.error > 0
                    ? TrimLimitingCondition.BowHeavy
                    : TrimLimitingCondition.SternHeavy;
            else if (IsAtLimit(best.forwardFill) || IsAtLimit(best.aftFill))
                result.limitingCondition = TrimLimitingCondition.TankAtLimit;

            result.diagnosticDetails = "Reachable trim offset: " + minimumError.ToString("+0.000;-0.000;0.000") +
                " m to " + maximumError.ToString("+0.000;-0.000;0.000") + " m.";
            return result;
        }

        static State Evaluate(LongitudinalTrimInput input, double forwardFill, double aftFill)
        {
            State state = new State
            {
                forwardFill = Clamp01(forwardFill),
                aftFill = Clamp01(aftFill)
            };
            double massMoment = 0;
            double buoyancy = 0;
            double buoyancyMoment = 0;

            for (int index = 0; index < input.parts.Count; index++)
            {
                LongitudinalPartSnapshot part = input.parts[index];
                double fill = part.trimGroup == LongitudinalTrimGroup.Forward
                    ? state.forwardFill
                    : part.trimGroup == LongitudinalTrimGroup.Aft ? state.aftFill : 0;
                double partMass = part.massWithoutTrimBallast;
                double partBuoyancy = part.fixedBuoyancy;
                if (part.trimGroup != LongitudinalTrimGroup.None)
                {
                    partMass += part.ballastCapacityMass * fill;
                    double coefficient = Math.Max(part.fullBuoyancyCoefficient,
                        part.emptyBuoyancyCoefficient * (1 - fill));
                    partBuoyancy = part.unitBuoyancy * coefficient;
                }

                state.mass += partMass;
                massMoment += partMass * part.massPosition;
                buoyancy += partBuoyancy;
                buoyancyMoment += partBuoyancy * part.buoyancyPosition;
            }

            if (state.mass <= 0 || buoyancy <= 0)
            {
                state.error = double.NaN;
                return state;
            }

            state.centerOfMass = massMoment / state.mass;
            state.centerOfBuoyancy = buoyancyMoment / buoyancy;
            state.error = state.centerOfMass - state.centerOfBuoyancy;
            return state;
        }

        static void ConsiderEdge(LongitudinalTrimInput input, State first, State second, ref State best)
        {
            if (Math.Sign(first.error) == Math.Sign(second.error) && first.error != 0 && second.error != 0)
                return;

            State low = first;
            State high = second;
            for (int iteration = 0; iteration < BisectionIterations; iteration++)
            {
                State middle = Evaluate(input,
                    (low.forwardFill + high.forwardFill) * 0.5,
                    (low.aftFill + high.aftFill) * 0.5);
                if (IsBetterSolution(middle, best, input))
                    best = middle;
                if (Math.Abs(middle.error) <= LevelTolerance * 0.001)
                    break;
                if (Math.Sign(low.error) == Math.Sign(middle.error))
                    low = middle;
                else
                    high = middle;
            }
        }

        static bool IsBetterSolution(State candidate, State current, LongitudinalTrimInput input)
        {
            double candidateError = Math.Abs(candidate.error);
            double currentError = Math.Abs(current.error);
            double refinedTolerance = LevelTolerance * 0.001;
            bool candidateIsRefined = candidateError <= refinedTolerance;
            bool currentIsRefined = currentError <= refinedTolerance;
            if (candidateIsRefined != currentIsRefined)
                return candidateIsRefined;

            if (candidateIsRefined)
            {
                double candidateDistance = Math.Abs(candidate.forwardFill - input.currentForwardFillFraction) +
                    Math.Abs(candidate.aftFill - input.currentAftFillFraction);
                double currentDistance = Math.Abs(current.forwardFill - input.currentForwardFillFraction) +
                    Math.Abs(current.aftFill - input.currentAftFillFraction);
                if (Math.Abs(candidateDistance - currentDistance) > 1e-9)
                    return candidateDistance < currentDistance;
            }

            if (candidateError < currentError - 1e-9)
                return true;
            if (Math.Abs(candidateError - currentError) > 1e-9)
                return false;

            double finalCandidateDistance = Math.Abs(candidate.forwardFill - input.currentForwardFillFraction) +
                Math.Abs(candidate.aftFill - input.currentAftFillFraction);
            double finalCurrentDistance = Math.Abs(current.forwardFill - input.currentForwardFillFraction) +
                Math.Abs(current.aftFill - input.currentAftFillFraction);
            return finalCandidateDistance < finalCurrentDistance;
        }

        static bool IsAtLimit(double fill)
        {
            return fill <= TankLimitTolerance || fill >= 1 - TankLimitTolerance;
        }

        static double Clamp01(double value)
        {
            return Math.Max(0, Math.Min(1, value));
        }
    }

    /// <summary>Builds a non-mutating scalar snapshot from a KSP editor craft.</summary>
    public static class SunkWorksTrimAnalyzer
    {
        public static TrimAnalysisResult Analyze(ShipConstruct ship)
        {
            LongitudinalTrimInput input;
            string failure;
            if (!TryBuildInput(ship, out input, out failure))
            {
                return new TrimAnalysisResult
                {
                    limitingCondition = ship == null || ship.parts == null || ship.parts.Count == 0
                        ? TrimLimitingCondition.NoCraft
                        : TrimLimitingCondition.BuoyancyUnavailable,
                    diagnosticDetails = failure
                };
            }
            return LongitudinalTrimSolver.Analyze(input);
        }

        public static bool TryBuildInput(ShipConstruct ship, out LongitudinalTrimInput input, out string failure)
        {
            input = new LongitudinalTrimInput();
            failure = null;
            if (ship == null || ship.parts == null || ship.parts.Count == 0 || ship.parts[0] == null)
            {
                failure = "No editor craft is available.";
                return false;
            }

            Part rootPart = ship.parts[0];
            for (int index = 0; index < ship.parts.Count; index++)
            {
                if (ship.parts[index].parent == null)
                {
                    rootPart = ship.parts[index];
                    break;
                }
            }
            Vector3 origin = rootPart.transform.position;
            Vector3 longitudinalAxis = rootPart.transform.up.normalized;
            CelestialBody homeBody = FlightGlobals.GetHomeBody();
            double oceanDensity = homeBody != null ? homeBody.oceanDensity : 1.0;
            input.surfaceGravity = homeBody != null ? homeBody.GeeASL * 9.80665 : 9.80665;

            Dictionary<Part, WBIBallastTank> trimHosts = new Dictionary<Part, WBIBallastTank>();
            Dictionary<Part, WBIBallastTank> allBallastHosts = new Dictionary<Part, WBIBallastTank>();
            for (int index = 0; index < ship.parts.Count; index++)
            {
                List<WBIBallastTank> modules = ship.parts[index].FindModulesImplementing<WBIBallastTank>();
                for (int moduleIndex = 0; moduleIndex < modules.Count; moduleIndex++)
                {
                    WBIBallastTank tank = modules[moduleIndex];
                    Part host = tank.GetBallastHostPart();
                    if (host != null && !allBallastHosts.ContainsKey(host))
                        allBallastHosts.Add(host, tank);
                    if (host != null && !trimHosts.ContainsKey(host) &&
                        (tank.tankType == BallastTankTypes.ForwardTrim || tank.tankType == BallastTankTypes.AftTrim))
                        trimHosts.Add(host, tank);
                }
            }

            double forwardPositionMoment = 0;
            double aftPositionMoment = 0;
            double forwardPositionWeight = 0;
            double aftPositionWeight = 0;
            double forwardFillMass = 0;
            double aftFillMass = 0;

            for (int index = 0; index < ship.parts.Count; index++)
            {
                Part editorPart = ship.parts[index];
                double unitBuoyancy;
                if (!TryGetUnitBuoyancy(editorPart, oceanDensity, out unitBuoyancy))
                {
                    failure = "Unable to determine submerged displacement for " +
                        (editorPart.partInfo != null ? editorPart.partInfo.title : editorPart.partName) + ".";
                    return false;
                }

                Vector3 massPoint = editorPart.transform.TransformPoint(editorPart.CoMOffset);
                Vector3 buoyancyPoint = editorPart.transform.TransformPoint(editorPart.CenterOfBuoyancy);
                LongitudinalPartSnapshot snapshot = new LongitudinalPartSnapshot
                {
                    massPosition = Vector3.Dot(massPoint - origin, longitudinalAxis),
                    buoyancyPosition = Vector3.Dot(buoyancyPoint - origin, longitudinalAxis),
                    massWithoutTrimBallast = editorPart.mass + editorPart.GetResourceMass(),
                    fixedBuoyancy = unitBuoyancy * editorPart.buoyancy,
                    trimGroup = LongitudinalTrimGroup.None
                };

                // Normal ballast and ignored port/starboard roles remain fixed at their current
                // editor fill. Derive their coefficient from the resource rather than trusting a
                // possibly stale Part.buoyancy after the player moves a resource slider.
                WBIBallastTank fixedBallastTank;
                if (allBallastHosts.TryGetValue(editorPart, out fixedBallastTank))
                {
                    PartResource fixedResource = GetBallastResource(fixedBallastTank, editorPart);
                    if (fixedResource != null && fixedResource.maxAmount > 0)
                    {
                        double fixedFill = fixedResource.amount / fixedResource.maxAmount;
                        snapshot.fixedBuoyancy = unitBuoyancy *
                            fixedBallastTank.GetBuoyancyAtFillFraction((float)fixedFill);
                    }
                }

                WBIBallastTank trimTank;
                PartResource trimResource = null;
                if (trimHosts.TryGetValue(editorPart, out trimTank))
                    trimResource = GetBallastResource(trimTank, editorPart);
                if (trimResource != null && trimResource.maxAmount > 0 && trimResource.info != null)
                {
                    PartResource resource = trimResource;
                    double currentBallastMass = resource.amount * resource.info.density;
                    double capacityMass = resource.maxAmount * resource.info.density;
                    snapshot.massWithoutTrimBallast -= currentBallastMass;
                    snapshot.ballastCapacityMass = capacityMass;
                    snapshot.unitBuoyancy = unitBuoyancy;
                    snapshot.emptyBuoyancyCoefficient = trimTank.GetBuoyancyAtFillFraction(0);
                    snapshot.fullBuoyancyCoefficient = trimTank.GetBuoyancyAtFillFraction(1);
                    snapshot.fixedBuoyancy = 0;

                    if (trimTank.tankType == BallastTankTypes.ForwardTrim)
                    {
                        snapshot.trimGroup = LongitudinalTrimGroup.Forward;
                        input.forwardTankCount++;
                        input.forwardCapacityMass += capacityMass;
                        forwardFillMass += currentBallastMass;
                        forwardPositionMoment += snapshot.massPosition * Math.Max(capacityMass, 1e-9);
                        forwardPositionWeight += Math.Max(capacityMass, 1e-9);
                    }
                    else
                    {
                        snapshot.trimGroup = LongitudinalTrimGroup.Aft;
                        input.aftTankCount++;
                        input.aftCapacityMass += capacityMass;
                        aftFillMass += currentBallastMass;
                        aftPositionMoment += snapshot.massPosition * Math.Max(capacityMass, 1e-9);
                        aftPositionWeight += Math.Max(capacityMass, 1e-9);
                    }
                }

                input.parts.Add(snapshot);
            }

            input.currentForwardFillFraction = input.forwardCapacityMass > 0
                ? forwardFillMass / input.forwardCapacityMass : 0;
            input.currentAftFillFraction = input.aftCapacityMass > 0
                ? aftFillMass / input.aftCapacityMass : 0;
            input.forwardMeanPosition = forwardPositionWeight > 0
                ? forwardPositionMoment / forwardPositionWeight : 0;
            input.aftMeanPosition = aftPositionWeight > 0
                ? aftPositionMoment / aftPositionWeight : 0;
            return true;
        }

        static PartResource GetBallastResource(WBIBallastTank tank, Part host)
        {
            if (tank == null || host == null || string.IsNullOrEmpty(tank.ballastResourceName))
                return null;
            if (tank.ballastResource != null)
                return tank.ballastResource;
            return host.Resources.Contains(tank.ballastResourceName)
                ? host.Resources[tank.ballastResourceName]
                : null;
        }

        static bool TryGetUnitBuoyancy(Part part, double oceanDensity, out double unitBuoyancy)
        {
            unitBuoyancy = 0;
            if (part == null)
                return false;
            if (part.buoyancy <= 0)
                return true;

            PartBuoyancy partBuoyancy = part.GetComponent<PartBuoyancy>();
            if (partBuoyancy != null && partBuoyancy.displacement > 0)
            {
                unitBuoyancy = partBuoyancy.displacement * oceanDensity * PhysicsGlobals.BuoyancyScalar;
                return true;
            }

            // PartBuoyancy.Start may not yet have run immediately after a placement event.
            // Reproduce its drag-cube displacement approximation without touching the component.
            if (part.DragCubes != null && !part.DragCubes.None)
            {
                Vector3 size = part.DragCubes.WeightedSize;
                float[] area = part.DragCubes.WeightedArea;
                if (size.x > 0 && size.y > 0 && size.z > 0 && area != null && area.Length >= 5)
                {
                    double xPortion = area[0] / (size.y * size.z);
                    double yPortion = area[2] / (size.x * size.z);
                    double zPortion = area[4] / (size.x * size.y);
                    double xzPortion = (Math.Min(xPortion, zPortion) + 2 * xPortion * zPortion) / 3;
                    double displacement = size.x * size.y * size.z * xzPortion * yPortion;
                    if (displacement > 0 && !double.IsNaN(displacement))
                    {
                        unitBuoyancy = displacement * oceanDensity * PhysicsGlobals.BuoyancyScalar;
                        return true;
                    }
                }
            }
            if (PhysicsGlobals.BuoyancyDefaultVolume > 0)
            {
                unitBuoyancy = PhysicsGlobals.BuoyancyDefaultVolume * oceanDensity *
                    PhysicsGlobals.BuoyancyScalar;
                return true;
            }
            return false;
        }
    }
}
#pragma warning restore 1591
