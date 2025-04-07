namespace TitleEdit.Data.Lobby
{
    // All of these were decided and named via 10 minutes of clicking around
    public enum LobbyUiStage : byte
    {
        Unknown0 = 0,
        CommonBaseStage = 1, // Character creation, when logged, when displaying title screen
        CharacterSelect = 2,
        InitialLobbyLoading = 3,
        LoadingSplashScreen = 4,
        EnteringTitleScreen = 6,
        LoadingTitleScreen1 = 7, // CAN'T RELOAD HERE, WAIT FOR LoadingTitleScreen2
        LoadingTitleScreen2 = 8,
        TitleScreen = 9,
        LoadingDataCenter = 11,
        ExitingTitleScreen = 12, // To character select
        Movie = 14,
        LoadingCharacterSelect3 = 15,
        LoadingCharacterCreation = 16,
        LoggingOut = 18,
        TitleScreenDataCenter = 19,
        TitleScreenMovies = 20,
        TitleScreenOptions = 21,
        TitleScreenLicense = 22,
        TitleScreenConfiguration = 24,
        TitleScreenInstallationDetails = 25,
        LoadingCharacterSelect1 = 27,
        LoadingCharacterSelect2 = 28,
        UnloadingCharacterSelect1 = 30,
        UnloadingCharacterSelect2 = 31
    }
}
