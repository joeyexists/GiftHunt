namespace GiftHunt.LevelUtils
{
    internal class LevelLoader
    {
        private static readonly HashSet<MainMenu.State> LoadableMenuStates =
        [ 
            MainMenu.State.None, MainMenu.State.Map, MainMenu.State.Location, MainMenu.State.Level,
            MainMenu.State.Mission, MainMenu.State.Staging, MainMenu.State.Pause
        ];

        private static readonly HashSet<string> GreenSidequestLevelIds =
        [
            "SIDEQUEST_GREEN_MEMORY", "SIDEQUEST_GREEN_MEMORY_2", "SIDEQUEST_GREEN_MEMORY_3", "SIDEQUEST_GREEN_MEMORY_4"
        ];

        public static bool TryLoadLevel(string targetLevelId, out string error)
        {
            error = null;

            if (LevelRush.IsLevelRush())
            {
                error = "Cannot load level during level rush.";
                return false;
            }

            var menu = MainMenu.Instance();
            if (menu == null || !LoadableMenuStates.Contains(menu.GetCurrentState()))
            {
                error = "Cannot load level from current menu.";
                return false;
            }

            var gameData = GiftHunt.Game.GetGameData();
            if (gameData == null)
            {
                error = "Game data is not yet initialized.";
                return false;
            }

            var targetLevel = gameData.GetLevelData(targetLevelId);
            if (targetLevel == null)
            {
                error = "Target level is null.";
                return false;
            }

            if (!gameData.IsArchiveUnlockedForLevel(targetLevel))
            {
                error = "Level has not been unlocked.";
                return false;
            }

            if (GreenSidequestLevelIds.Contains(targetLevel.levelID))
            {
                // require complete campaign to load green sidequests
                var storyStatus = gameData.GetStoryStatus();
                if (storyStatus != StoryStatus.CampaignComplete)
                {
                    error = "Level has not been unlocked.";
                    return false;
                }
            }

            var currentLevel = GiftHunt.Game.GetCurrentLevel();
            if (currentLevel == null)
            {
                error = "Current level is null.";
                return false;
            }

            var isSameLevel = currentLevel.levelID == targetLevel.levelID;
            GiftHunt.Game.PlayLevel(targetLevel, fromArchive: true, fromRestart: isSameLevel);
            return true;
        }
    }
}
