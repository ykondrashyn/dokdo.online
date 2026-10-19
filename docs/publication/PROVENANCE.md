# Provenance and dependencies

Original DOKDO code, scenes, models, textures, audio, fonts and other recovered
material retain their original ownership. This is an unofficial reconstruction,
not endorsed by Zzoo or 111%. The material is source available. No blanket
open-source license is granted over recovered material. Availability here does
not establish new redistribution rights for recovered third-party content.

The baseline is DOKDO 1.16.6 reconstruction tag `dokdo-1.16.6-web9-r6`, commit
`847f6e2284fcc87f9fa20b1e1addbd585f7627b8`, verified locally with its Git SSH
signature. Its original Git history, tag objects, backend, production tools,
archives and deployment bundles are not included. The public repository keeps
its earlier documentation, screenshots and historical Android artifact.

Recovered material is primarily under `Assets/0.my`, `Assets/Scripts`, the
asset-type folders under `Assets`, and the legacy water components in
`Assets/Plugins/Assembly-CSharp-firstpass/UnityStandardAssets`. Reconstruction
code is primarily under `Assets/Dokdo`; earlier repairs also modify recovered
scripts, scenes, shaders and import metadata. The export hash list identifies
baseline bytes, not a license boundary or a claim of original authorship.

Standalone changes remove online runtime services and the browser social UI,
replace commercial compatibility dependencies, clear cloud/signing settings,
and supply public build and verification tools. Normal persistence and its
receipt fields remain compatible with the pinned reconstruction.
Four obsolete Unity GUI Layer components are removed from the recovered
cameras; Unity 6 no longer supports them. Asset and gameplay script GUIDs
remain intact. The standalone project has a separate local product identity.
Unity 6000.3.18f1 normalizes the legacy joystick mesh, its importer metadata,
and quality/time settings during import. These normalized files are included
in the public inventory so a clean checkout starts from the reviewed form.

## Anti-cheat compatibility

Recovered CodeStage implementation files are excluded. Independently written
`Assets/Plugins/Compatibility/ObscuredValues.cs` preserves only DOKDO's integer
and float serialized field layouts, primitive arithmetic/conversions and its
one detector call. It performs no cheat detection. Default uninitialized
values decode to zero; initialized values decode using their stored key.
Compatibility tests cover stored bits and round trips. Current save envelopes
store ordinary values and retain their existing checksum validation.

## DOTween

The free DOTween core and modules remain pinned to official upstream commit
`bfb02b05551a45204352dc43bdc15b882147891a`:
https://github.com/Demigiant/dotween/tree/bfb02b05551a45204352dc43bdc15b882147891a

The core DLL SHA-256 is
`5cee7f2b16fd3ba509ecac8bfc95efc9347682956fc4c1efde14618176e34491`.
The upstream source is under `ThirdParty/DOTween`. Original notices and readme
accompany the dependency; see [third-party notices](../../THIRD_PARTY_NOTICES.md).
The recovered commercial DOTweenAnimation implementation is replaced by the
independent Good Pirate scene adapter, retaining DOKDO's script GUID. Its
reference is Good Pirate commit `fe4dd7c93ea0338d4efd06d4a5dad752f8044687`:
https://github.com/ykondrashyn/good-pirate/tree/fe4dd7c93ea0338d4efd06d4a5dad752f8044687

DOKDO's existing independent path/visual-manager compatibility adapters remain
under `Assets/Dokdo/Runtime/Tweening`. No recovered DOTween Pro DLL is included.

## Historical Android artifact

`dokdo-1.16.6-r1.apk` and the older `v1.16.6-r1` GitHub release are preserved
historical artifacts. The [Android build workflow](ANDROID.md) creates a new
standalone app with a separate package ID and the builder's local signing key.
It does not reproduce the historical APK or establish update compatibility
with it. The unused recovered PreciseLocale wrapper has been removed;
localization uses Unity's system language without that Android plugin.
The WebGL ZIP is built freshly from the exact public commit named in its
publication manifest.
