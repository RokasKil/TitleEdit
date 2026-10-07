# Title Edit Chronicle

A fork of [Title Edit](https://github.com/RokasKil/TitleEdit) (attick, perchbird & Speedas) that makes the vanilla title screen and idle movie follow **the currently logged-in character's Main Scenario**.

## Why this fork exists

Patch 7.1 added "Title Screen Displayed on Launch" as a setting, the most interesting aspect of which synchs up with the player's MSQ progress. It's a functionality that should have been in TitleEdit from its conception, but even in the light of the 7.1 update  still hasn't been superseded from inclusion in the plugin. That is because the setting that is provided by the vanilla game:

- picks the **furthest** character on the service account, not the one you just played
- [by design](https://na.finalfantasyxiv.com/lodestone/topics/detail/9b42b2425f3a680caea3281ccd65c99677cb00e2/) **does not function correctly** on Free Trial accounts 
- often waits for a full client relaunch before the new title appears

Title Edit Chronicle fixes all three of these issues.

| | Vanilla 7.5 | Title Edit | Chronicle |
|---|---|---|---|
| Custom 3D title / character select | No | Yes | Yes (inherited) |
| Follow MSQ automatically | Furthest character | Manual preset | **This character** |
| Free Trial | Forced title | Manual preset only | **Automatic, if the client has the files** |
| Apply without relaunch | No | When you pick a preset | **On logout / MSQ change** |

I have been advised but am unable to personally verify: Free Trial still cannot load expansion data you do not have on disk (Endwalker+), but that ARR through Shadowbringers will map correctly.

## How it tracks MSQ

On login and logout, Chronicle:

1. Resolves expansion-start quests from the live `Quest` sheet (`Coming to Ishgard`, `Beyond the Great Wall`, `The Syrcus Trench`, `The Next Hunt` / `Old Sharlayan, New to You`, `A New World to Explore`)
2. Treats a quest as reached if it is **complete or accepted** (story skips included)
3. Falls back to scanning Main Scenario journal genres if a gate name moves
4. Stores the result **per Content ID** and remembers the last played character
5. Points Title Edit's vanilla preset + logo + movie at that expansion
6. Calls `ReloadTitleScreen()` so the lobby you return to is already correct

## Commands

- `/titleedit` or `/te` — configuration
- `/titleedit msq` — print the mapped expansion for this character
- `Ctrl+T` on the title screen — same as original Title Edit

## Disclosure to Possible Objections and Problems You May Have

This project fork has been AI-slopped to hell and back (level of AI assistance: "Copilot"), and I attest that I did actually give a good look-over on the code and fixed some errors and warnings that the IDE threw at me, and removed some ridiculous functions that were included in the initial output. Having built and installed it myself, I confirm that this does in fact run and can at the very least swap between ARR and Heavensward, but I cannot guarantee that it can continue working correctly for the title and movie settings of later expansions. Due to the environment of my game install and the account I used for testing, I also cannot guarantee that the "Ignore Free Trial title lock" option in the config is a necessary function **or even does anything at all**.

## License

AGPL-3.0, same as Title Edit V3. Credit to attick, perchbird, Speedas / RokasKil, and the original Title Edit contributors.
