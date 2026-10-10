# Rise the Bar

<p align="center">
  <img src="Assets/Publish/Screenshots/S1.png" width="720" alt="Rise the Bar gameplay">
</p>

<p align="center"><strong>A timing-based arcade platformer about building the bridge to the next island.</strong></p>

<p align="center">
  <img src="https://img.shields.io/badge/Engine-Unity%206-222c32?logo=unity&logoColor=white" alt="Unity 6">
  <img src="https://img.shields.io/badge/Genre-Arcade%20Platformer-5b8def" alt="Arcade platformer">
  <img src="https://img.shields.io/badge/Status-Complete-36b37e" alt="Complete">
</p>

## About

**Rise the Bar** is a 2D side-scrolling score challenge. Hold to grow a bar, release it to bridge the gap, then keep the character moving forward. A bridge that is too short ends the run.

## Highlights

- **Simple precision mechanic:** Estimate the gap and build the right bridge length.
- **Continuous momentum:** The character keeps moving, making every decision immediate.
- **Score chasing:** Better placements and collected stars reward accurate runs.
- **Persistent play:** Audio and local save systems are included.

## Technical details

- **Engine:** Unity 6; the project version is recorded in [ProjectVersion.txt](ProjectSettings/ProjectVersion.txt).
- **Status:** Complete

## Run locally

1. Clone the repository.
2. Open it in Unity Hub with the version recorded in `ProjectSettings/ProjectVersion.txt` (Android Build Support required for mobile builds).
3. Open a scene in `Assets/Scenes` and press Play.

Hold the left mouse button in the editor or hold one finger on Android to grow a bar. Release to drop it. Interrupted touches and app switches cancel an unfinished bar without spending a retry. Perfect placements award 200 points, plus a combo bonus of 50 points per subsequent perfect placement, capped at 250 bonus points per bar. Normal placements and failed bars reset the combo; cancelled input does not. Two consecutive perfect placements still award one retry. Perfect hits show an animated popup and a sound that rises with the combo. Distance measures horizontal progress and freezes when the run ends.

Optional Android haptic feedback can be enabled with the **Vibration** button before starting or after game over. It is off by default and the preference survives replay and app restarts. Device touch-feedback settings also apply.

Perfect-hit feedback appears above the target landing area and follows the camera. Its dark background keeps the text readable, and the entire popup stays inside the screen safe area even when the target platform is offscreen.

The green perfect zone starts 0.01 world units inside the platform's front edge and extends 0.34 units along the platform. The marker and perfect-hit scoring use the same zone; the increasing bar growth speed still tightens the timing window as the run progresses.

## Validation and Android releases

- Run the EditMode and PlayMode suites in **Window > General > Test Runner**.
- Android builds target API 36 and use ARM64 with IL2CPP. Install Android SDK platform 36 through Unity Hub/SDK tools if needed.
- Keep the existing application ID and PlayerPrefs keys when updating the published game so local highscores remain available.
- Configure the release keystore on the build machine; the saved absolute keystore path is machine-specific.
- Before publishing, validate the final AAB's 16 KB page-size support and test an Android device: long runs, retries, rapid/multiple touches, app pause/resume, game over and replay.

## Android release workflow

Integrated from [Unity Android Release Tools](https://github.com/WRB-Studio/unity-android-release-tools), `origin/main` commit `8d9fce859fa9a606a621acd524665e109e7b8e28` (updated on 2026-10-10; first integrated on 2026-10-04). Scripts and both Editor files (`UnityAndroidBuild.cs` and `UnityAndroidReleaseMenu.cs`) must always be updated together; preserve this project's `scripts/release.config.json`, local credentials and existing `.meta` GUIDs. Follow the upstream [update instructions](https://github.com/WRB-Studio/unity-android-release-tools/blob/main/UPDATING.md).

Project configuration uses package `com.wrbstudio.raisethebar`, artifact `Raise-the-Bar`, and local configuration key `com-wrbstudio-raisethebar`. Signing credentials are encrypted with Windows DPAPI under `%LOCALAPPDATA%/UnityAndroidRelease/<SecretsKey>/release-secrets.xml`; the existing signing key and shared Play key are referenced there. No private paths or signing alias are stored in project settings. On another machine/account, configure credentials with `scripts/Set-ReleaseSecrets.ps1` using its hidden password prompts.

For direct CLI builds without -BuildRoot, save and close this project's Unity Editor. Menu builds and CLI builds with -BuildRoot use an isolated copy and can keep the source Editor open. These commands create local artifacts only:

```powershell
./scripts/Build-Apk.ps1

# Local AAB build, without submitting to Play:
. ./scripts/ReleaseCommon.ps1
Invoke-UnityAndroidBuild -Format aab

# Offline regression suite; also runs in GitHub Actions on PowerShell 5.1 and 7:
./scripts/Test-ReleaseWorkflow.ps1

# Online Play access/version-code check; no build or upload:
./scripts/Build-AabAndSubmitToPlay.ps1 -CheckOnly
```

Artifacts, build receipts and logs are written to `Builds/Android`. Local builds preserve the project's version code; a future Play release must use a free code, which the Play script resolves automatically. At integration time Play's highest code was 3 and the next free code was 4; query again before a release. The initial integration preserved version 1.1.1 and code 3. The subsequent authorized release uses version 1.1.2 and code 4.

The shared Play account has app-specific read, test-release, production-release and Store listing permissions for this game, granted with explicit confirmation for the release. Google includes policy declarations and deep-link management with Store listing management. Reading production version codes alone does not prove production upload permission. No build, Play upload, metadata submission or Drive export runs automatically on Git push; GitHub Actions only executes offline checks.

Drive export remains unconfigured. If requested later, set an existing destination with `scripts/Set-DriveExportDirectory.ps1`; `Build-AndroidAndExportToDrive.ps1 -CheckOnly` verifies it without copying. An actual export into a synchronized folder may trigger an upload. Approved German and English Store texts and release notes are maintained in `release/play-metadata.json`.

Validation on 2026-10-04: offline regression suites passed under Windows PowerShell 5.1 and PowerShell 7; the helper compiled against Unity 6000.3.15f1 with warnings treated as errors. Real signed APK and AAB builds succeeded, their receipts matched the package/version, APK signature verification and AAB JAR signature verification passed. APK ZIP alignment passed at 16 KB and all six ARM64 native libraries had LOAD alignment of at least 16 KB. Play access/version-code checking succeeded after the confirmed app-specific permission change. No upload, publication or Drive export was performed. Device testing and a future production release remain separate checks.

Local integration fix: For direct builds without `-BuildRoot`, `Invoke-UnityAndroidBuild` snapshots `ProjectSettings/ProjectSettings.asset` before launching Unity and restores its exact bytes in `finally`. Isolated builds never rewrite the source settings, so the open Editor and concurrent edits are preserved. Unity 6000.3.15f1 persisted temporary keystore paths during a real build despite the helper's in-memory restoration. This prevents those private settings and build-time settings changes from remaining in Git. The regression suite verifies restoration after successful APK/AAB builds and all eight failure cases. Preserve this patch in `ReleaseCommon.ps1` and its regression coverage in `Test-UnityAndroidBuild.ps1` when updating from upstream.

The real AAB build with the local restoration patch also passed a byte-for-byte before/after SHA-256 comparison of `ProjectSettings.asset`.

Authorized release on 2026-10-04: signed AAB 1.1.2, version code 4, was committed to Google Play production with the approved German/English descriptions and bullet-point release notes. The Play API confirmed the production track's code 4 and exact listing/release-note contents in both languages. Play Console showed automatic pre-review checks running with the full rollout and both Store listing updates queued for review; managed publishing is off. Public availability was not yet confirmed. Project version and code now match this release. No images or screenshots were uploaded.

Tool update on 2026-10-10: adopted upstream `origin/main` commit `8d9fce859fa9a606a621acd524665e109e7b8e28`, including both Editor files, all release/device/menu scripts and the offline CI workflow. Project configuration, signing credentials, Store metadata, existing asset GUID and unrelated Unity/package changes were preserved. The menu is available under `Tools/Unity Android Release Tools`, with APK/AAB, phone build/install/start, Drive, Play, setup and check actions. Menu builds and CLI `-BuildRoot` use a dedicated owned cache; device installation and exports require a separate user request.

The direct-build settings restoration patch remains local; it is deliberately disabled for isolated builds so an open source Editor is never overwritten. A second local patch lets Unity detection fall back to Unity Hub's `editors-v2.json` when the secondary install path is stale. Its regression test verifies version selection, missing executable filtering and explicit override errors. Preserve these local patches and their test coverage during subsequent updates.

Update verification: PowerShell 5.1/7 workflow suites passed, including simulated device build/install/start and menu dispatch. Both Editor files compiled without warnings against the installed project Editor, Unity 6000.3.25f1. All 18 menu actions were confirmed registered in a disposable Unity Editor project; no menu action was executed. Automatic CLI Editor detection and Android SDK 36/NDK/OpenJDK availability were checked. No real Android build, device installation, Play upload or Drive export was performed for this tool update; live menu interaction and device behavior in the game remain unverified.
