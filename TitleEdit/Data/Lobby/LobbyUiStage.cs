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
        LoadingDataCenter = 12,
        ExitingTitleScreen = 13, // To character select
        Movie = 15,
        LoadingCharacterSelect3 = 16,
        LoadingCharacterCreation = 17,
        LoggingOut = 19,
        TitleScreenDataCenter = 20,
        TitleScreenMovies = 21,
        TitleScreenOptions = 22,
        TitleScreenLicense = 23,
        TitleScreenConfiguration = 25,
        TitleScreenInstallationDetails = 26,
        LoadingCharacterSelect1 = 28,
        LoadingCharacterSelect2 = 29,
        UnloadingCharacterSelect1 = 31,
        UnloadingCharacterSelect2 = 32
    }
}

