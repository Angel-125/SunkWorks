namespace SunkWorks
{
    /// <summary>
    /// Difficulty settings for SunkWorks gameplay features.
    /// </summary>
    public class SunkWorksSettings : GameParameters.CustomParameterNode
    {
        /// <summary>
        /// When enabled, aquatic engines and aquatic RCS cannot operate while
        /// their parts are covered by a supercavity.
        /// </summary>
        [GameParameters.CustomParameterUI(
            "#LOC_SUNKWORKS_settingsSupercavitationFlameoutTitle",
            toolTip = "#LOC_SUNKWORKS_settingsSupercavitationFlameoutTooltip",
            autoPersistance = true,
            gameMode = GameParameters.GameMode.ANY)]
        public bool supercavitationFlameout = true;

        public override string DisplaySection
        {
            get { return Section; }
        }

        public override string Section
        {
            get { return "SunkWorks"; }
        }

        public override string Title
        {
            get { return "Gameplay"; }
        }

        public override int SectionOrder
        {
            get { return 1; }
        }

        public override GameParameters.GameMode GameMode
        {
            get { return GameParameters.GameMode.ANY; }
        }

        public override bool HasPresets
        {
            get { return false; }
        }

        /// <summary>
        /// Indicates whether supercavitation should disable aquatic propulsion.
        /// Defaults to enabled when no game is loaded.
        /// </summary>
        public static bool SupercavitationFlameoutEnabled
        {
            get
            {
                if (HighLogic.CurrentGame == null)
                    return true;

                SunkWorksSettings settings =
                    HighLogic.CurrentGame.Parameters.CustomParams<SunkWorksSettings>();
                return settings == null || settings.supercavitationFlameout;
            }
        }
    }
}
