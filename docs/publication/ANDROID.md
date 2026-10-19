# Build a standalone Android APK

The source builds a separate offline DOKDO app. It contains gameplay, touch
controls, audio, local file saves, backup recovery, and clipboard save
transfer. Hosted social features are absent. This does not reproduce the
historical `dokdo-1.16.6-r1.apk` or establish signing compatibility with it.

## Prerequisites

Use **Unity 6000.3.18f1**. In Unity Hub, add **Android Build Support**,
**Android SDK & NDK Tools**, and **OpenJDK** to that editor. Keep the
Hub-installed tools selected in **Preferences > External Tools > Android**.
This Unity version uses **NDK r27c (27.2.12479018)**, **OpenJDK 17**, and
**SDK Build Tools 36.0.0**. The project targets **SDK Platform 36**; install
that platform using the SDK manager if necessary and accept the SDK licenses
locally. See Unity's [setup instructions][setup] and [dependency versions][versions].

Clone the `standalone-publication` branch, not the older default branch.
For the command-line workflow, install Python 3.10+, Git and Bash. Use
project/editor paths without spaces, commas or double quotes. This restriction
comes from the IL2CPP compiler flags that remove local paths from diagnostics.
Unity activation, Android tools and signing keys stay outside Git.

## Command-line build

Close any Editor using this checkout, then run:

```bash
git clone --branch standalone-publication \
  https://github.com/ykondrashyn/dokdo.online.git
cd dokdo.online
export UNITY_EDITOR=/path/to/6000.3.18f1/Editor/Unity
python3 tool/publication.py build-android
```

The command requires a clean, committed checkout and checks the source
inventory. It compiles with Android selected, runs EditMode and PlayMode
tests **in the Editor**, then builds an APK and checks its ZIP contents.
It restores tracked project settings and writes separate Android logs and
test reports under `.artifacts/android-*`; it does not replace WebGL reports.

The output directory contains:

- `dokdo-standalone.apk`: locally debug-signed APK.
- `build.json`: exact source commit, original source tag, Unity version,
  build settings, test results and APK SHA-256. Device acceptance is pending.
- `SHA256SUMS`: hashes of the APK and its build record.

Existing `Builds/Android` output is refused. Move it elsewhere before another
run, including after a failed build. After intentional source changes, review
them, run `python3 tool/publication.py inventory`, audit and commit before
building again. Do not add generated files or signing material to the inventory.

## Unity Editor build

This route also avoids needing Bash or Python for the build itself:

1. Add the cloned project to Unity Hub and open it with 6000.3.18f1.
2. Open **File > Build Profiles**, add an **Android** profile and switch to it.
3. Choose **DOKDO > Android > Build local APK**. This applies the settings
   below and writes `Builds/Android/dokdo-standalone.apk`.

The menu route leaves the selected Android settings in the project and does
not run the Python inventory checks, tests or build-record generation. For
the full recorded workflow, use the command above. Unity's normal
[Android build process][build] is also available after choosing
**DOKDO > Android > Configure APK build**.

| Setting | Value |
| --- | --- |
| Package ID | `com.reconstruction.dokdo.standalone` |
| Display name | DOKDO Standalone |
| Version / version code | `1.16.6-public.1` / `1160601` |
| Backend / architecture | IL2CPP / ARM64 only |
| Minimum / target Android API | 25 / 36 |
| Graphics / texture compression | OpenGL ES 3 / ETC2 |
| Orientation | Portrait |
| Scenes, in order | `load`, `Intro`, `Main` under `Assets/0.my/Scenes` |
| Output format | APK, no split expansion file or App Bundle |
| Signing | Local debug key; custom keystore disabled |

## Install and preserve saves

Enable USB debugging on an ARM64 Android device, authorize the connected
computer, and use the SDK's platform tools:

```bash
adb devices
adb install -r Builds/Android/dokdo-standalone.apk
```

The separate package ID allows this app and the historical APK to coexist.
Saves remain separate too: the new app stores `save-v1.json` and a backup in
its own persistent data directory. Use **Save Transfer** to copy/paste a
compatible save between apps or between the browser and Android. Moving the
APK does not move existing saves; clearing app data or uninstalling removes
local progress. Export your save first.

An update requires the same package ID and compatible signing key. A debug
key from another computer usually differs. If installation reports
`INSTALL_FAILED_UPDATE_INCOMPATIBLE`, preserve your save and use the original
signing key, or install under a different package ID. Uninstalling is a last
resort because it removes the app's saves. A 32-bit-only device cannot install
this ARM64 build.

## Sign your own release

The automated command and **Build local APK** menu intentionally use debug
signing. To create an APK with your own release identity:

1. Switch to Android and select **DOKDO > Android > Configure APK build**.
2. In **Project Settings > Player > Android**, set your own package ID,
   version and increasing version code.
3. Under **Publishing Settings**, use **Keystore Manager** to create or
   select your own keystore outside the checkout, then enable **Custom
   Keystore** and choose its key. Keep its passwords and backup private.
4. Build using Unity's **File > Build Profiles > Build** button. Keep
   **Export Project** and **Build App Bundle** disabled for an APK. Do not
   invoke the DOKDO configure/build commands afterward: they restore the
   documented local-build settings, including debug signing.

See Unity's [keystore guide][signing]. The repository does not provide the
historical APK's signing key. This guide covers APKs for local installation;
it does not qualify an application for an app store.

## Validation and troubleshooting

Android build verification results are recorded here after the first clean
build. Physical device acceptance remains separate: test startup, touch
steering, docking/combat, upgrades, audio/settings, Android Back, safe areas,
background/resume, force-stop/relaunch persistence, save export/import,
corrupt-save rejection and backup recovery. Inspect `adb logcat` for errors
and confirm ordinary play needs no network services. Editor test success
does not establish GPU, touchscreen or Android lifecycle behavior on a phone.

If Android is unavailable, install the Hub module for the exact editor version.
For SDK/NDK/JDK errors, restore the Hub-installed paths in External Tools and
ensure API 36 is installed. First builds need internet access for Unity
packages and Gradle dependencies. Compiler and Gradle diagnostics appear in
`.artifacts/android.log`; compile/test logs use the other `android-*` names.
The unused recovered PreciseLocale Java wrapper was removed: current
localization uses Unity's system language and needs no third-party Android
locale plugin.

[setup]: https://docs.unity3d.com/6000.3/Documentation/Manual/android-sdksetup.html
[versions]: https://docs.unity3d.com/6000.3/Documentation/Manual/android-supported-dependency-versions.html
[build]: https://docs.unity3d.com/6000.3/Documentation/Manual/android-BuildProcess.html
[signing]: https://docs.unity3d.com/6000.3/Documentation/Manual/android-keystore.html
