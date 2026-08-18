using System;
using UnityEngine;
using WildBlueCore;

#pragma warning disable 1591

namespace SunkWorks.Submarine
{
    /// <summary>IMGUI presentation for the editor trim report.</summary>
    public sealed class SunkWorksTrimAnalysisView : Dialog<SunkWorksTrimAnalysisView>
    {
        public TrimAnalysisResult Result { get; set; }
        public Action<bool> VisibilityChanged { get; set; }
        Vector2 scrollPosition;
        GUIStyle headingStyle;
        GUIStyle goodStyle;
        GUIStyle warningStyle;

        public SunkWorksTrimAnalysisView() : base("SunkWorks Trim Analysis", 410, 430)
        {
            Resizable = true;
        }

        public override void SetVisible(bool newValue)
        {
            bool changed = IsVisible() != newValue;
            base.SetVisible(newValue);
            if (changed && VisibilityChanged != null)
                VisibilityChanged(newValue);
        }

        protected override void ConfigureStyles()
        {
            base.ConfigureStyles();
            if (headingStyle != null)
                return;
            headingStyle = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold };
            goodStyle = new GUIStyle(headingStyle);
            goodStyle.normal.textColor = Color.green;
            warningStyle = new GUIStyle(headingStyle);
            warningStyle.normal.textColor = Color.yellow;
        }

        protected override void DrawWindowContents(int windowId)
        {
            scrollPosition = GUILayout.BeginScrollView(scrollPosition);
            GUILayout.Label("SUNKWORKS TRIM ANALYSIS", headingStyle);

            TrimAnalysisResult result = Result;
            if (result == null)
            {
                GUILayout.Label("Waiting for the editor analysis...");
                GUILayout.EndScrollView();
                return;
            }
            if (!result.calculationSucceeded)
            {
                GUILayout.Label("Longitudinal Trim: UNAVAILABLE", warningStyle);
                GUILayout.Space(8);
                GUILayout.Label(result.diagnosticDetails ?? "No craft is available for analysis.");
                GUILayout.EndScrollView();
                return;
            }

            GUILayout.Label("Longitudinal Trim: " + (result.canAchieveLevelTrim ? "GOOD" : "INSUFFICIENT"),
                result.canAchieveLevelTrim ? goodStyle : warningStyle);
            GUILayout.Space(8);

            DrawTankGroup("Forward Trim", result.forwardTankCount, result.forwardCapacityMass,
                result.suggestedForwardFillFraction, result.canAchieveLevelTrim);
            GUILayout.Space(5);
            DrawTankGroup("Aft Trim", result.aftTankCount, result.aftCapacityMass,
                result.suggestedAftFillFraction, result.canAchieveLevelTrim);
            GUILayout.Space(8);

            GUILayout.Label(result.canAchieveLevelTrim ? "Suggested Trim" : "Best Available Trim", headingStyle);
            GUILayout.Label("  Forward: " + FormatPercent(result.suggestedForwardFillFraction));
            GUILayout.Label("  Aft: " + FormatPercent(result.suggestedAftFillFraction));
            GUILayout.Label("Trim Offset: " + Math.Abs(result.trimOffset).ToString("0.000") + " m " +
                Direction(result.trimOffset));
            GUILayout.Label("Residual Moment: " + Math.Abs(result.residualPitchMoment).ToString("0.0") +
                " kN·m " + Direction(result.residualPitchMoment));
            GUILayout.Space(8);

            GUILayout.Label(result.canAchieveLevelTrim ? "Status" : "Warning", headingStyle);
            GUILayout.Label(GetStatus(result));
            GUILayout.Space(8);
            GUILayout.Label("Authority", headingStyle);
            GUILayout.Label("  Bow-down range: " + result.bowDownAuthority.ToString("0.0") + " kN·m");
            GUILayout.Label("  Stern-down range: " + result.sternDownAuthority.ToString("0.0") + " kN·m");
            GUILayout.Label(result.diagnosticDetails ?? string.Empty);
            GUILayout.EndScrollView();
        }

        static void DrawTankGroup(string label, int count, double capacity, double fill, bool showSuggestion)
        {
            GUILayout.Label(label + ":", GUI.skin.label);
            GUILayout.Label("  Tanks: " + count);
            GUILayout.Label("  Capacity: " + capacity.ToString("0.00") + " t");
            if (showSuggestion)
                GUILayout.Label("  Suggested Fill: " + FormatPercent(fill));
        }

        static string GetStatus(TrimAnalysisResult result)
        {
            switch (result.limitingCondition)
            {
                case TrimLimitingCondition.NoForwardTrim:
                    return "No Forward Trim tanks are configured. Add a forward trim tank for normal two-ended authority.";
                case TrimLimitingCondition.NoAftTrim:
                    return "No Aft Trim tanks are configured. Add an aft trim tank for normal two-ended authority.";
                case TrimLimitingCondition.NoLongitudinalSeparation:
                    return "Forward and aft trim tanks have essentially no longitudinal separation. Move them farther apart.";
                case TrimLimitingCondition.BowHeavy:
                    return "The vessel remains bow-heavy. Add/move aft trim authority, move heavy equipment aft, or reduce forward-heavy mass.";
                case TrimLimitingCondition.SternHeavy:
                    return "The vessel remains stern-heavy. Add/move forward trim authority, move heavy equipment forward, or reduce aft-heavy mass.";
                case TrimLimitingCondition.TankAtLimit:
                    return "Level submerged trim is achievable, but at least one trim group is essentially at its limit.";
                case TrimLimitingCondition.BuoyancyUnavailable:
                    return "Effective buoyancy could not be determined for this craft.";
                default:
                    return "Level submerged trim is achievable.";
            }
        }

        static string Direction(double signedValue)
        {
            if (Math.Abs(signedValue) < 0.0005)
                return "level";
            return signedValue > 0 ? "bow-down" : "stern-down";
        }

        static string FormatPercent(double fraction)
        {
            return (fraction * 100).ToString("0") + "%";
        }
    }
}
#pragma warning restore 1591
