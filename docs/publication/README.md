# Standalone publication

This is a source-available Unity reconstruction of DOKDO 1.16.6. It derives
from the signed source tag `dokdo-1.16.6-web9-r6`, commit
`847f6e2284fcc87f9fa20b1e1addbd585f7627b8`. Later source changes and private
development history are excluded. See [provenance](PROVENANCE.md).

The standalone player has ordinary gameplay, pointer/touch and WASD steering,
audio, browser-local saves, backup recovery and clipboard save transfer.
Public names, chat, emotes, shared-sea ghosts, online counts, achievement feeds
and shared treasure events are absent. It makes no service requests. Existing
treasure receipt fields are retained for save compatibility. Promo mode is
not used. External links in the recovered in-game UI are disabled.

Use Unity **6000.3.18f1** with Web Build Support, Python 3.10 or newer, Git,
and ffmpeg available on PATH. Activate Unity on your own machine; credentials
and license files stay outside the repository. Linux installations may also
need their editor's documented native system libraries. No original APK,
private repository or server tool is required for normal builds.

```sh
export UNITY_EDITOR=/path/to/6000.3.18f1/Editor/Unity
python3 tool/publication.py compile
python3 tool/publication.py test
python3 -m unittest discover -s tool -p 'test_*.py'
python3 tool/publication.py audit
# Requires a local Gitleaks executable; tested with version 8.30.1.
python3 tool/publication.py secrets
# Run from a clean committed checkout. Includes compile and both Unity suites.
python3 tool/publication.py build
python3 tool/publication.py candidate
python3 -m http.server 8000 --directory Builds/Standalone
```

Open `http://localhost:8000/`. The selector chooses Mobile/ASTC when the
browser supports ASTC, otherwise Desktop/DXT. Both players are uncompressed
WebGL output. All URLs are relative: the extracted ZIP also works below a
subdirectory on a basic HTTP server. Opening index.html as a local file does
not work. Clipboard APIs require a secure context; manual copy/paste remains
available on ordinary HTTP. Saves belong to the browser origin; transfer a
save before changing origins or clearing browser storage.

For browser bridge checks and a real-player startup/network smoke, install
Playwright in your own Python environment, install its Chromium and Firefox
browsers, and run:

```sh
python3 tool/browser_check.py http://localhost:8000/ --stub --browser chromium
python3 tool/browser_check.py http://localhost:8000/ --stub --browser firefox
python3 tool/browser_check.py http://localhost:8000/Desktop/ --browser chromium
python3 tool/browser_check.py http://localhost:8000/Desktop/ --browser firefox
```

`--executable /path/to/browser` selects an existing browser installation.
Evidence goes to `.artifacts/browser`. The stub mode tests the shell; the
real-player smoke records only startup, reload and observed network traffic.
Neither completes the gameplay or physical-device acceptance checklist.

`candidate` produces an **unqualified review ZIP**, manifest and checksums in
`Builds/Review`. This does not assert browser acceptance. Test those exact
bytes using [the acceptance checklist](ACCEPTANCE.md). Release packaging
requires a complete acceptance record:

```sh
python3 tool/publication.py package --acceptance /path/outside/git/acceptance.json
python3 tool/publication.py verify --directory Builds/Release
python3 tool/publication.py verify-source --archive /path/to/downloaded-source.tar.gz
```

The manifest records the exact public build commit, original source tag and
commit, Unity version, file hashes and actual validation. The build command
restores settings changed by Unity and refuses a dirty source tree. Packaging
refuses changed Unity input trees or changed build bytes. Output directories
must be new; reviewed release assets are never overwritten automatically.

`export-allowlist.json` records the original hashes of the exact source-tag
files selected for the baseline. `tool/export-source.py REPOSITORY NEW_DIR`
can recreate that baseline for maintainers who possess the signed source
repository. It imports individual blobs only and verifies their hashes;
it never copies Git history or object storage. The baseline precedes the
standalone edits and must not itself be published. The committed tree is
the sanitized result. `source-inventory.json` covers the final public files
(excluding itself). After reviewing intentional changes, regenerate it with
`python3 tool/publication.py inventory`, then audit and commit it.

Publication requires a local review of the complete public diff and history,
inventory, redacted secret scans, private identifier scan, build results and
artifact hashes, followed by the owner's approval. Only then create the
signed public tag `dokdo-1.16.6-web9-r6-public.1`, push the reviewed commits
and tag, and create a GitHub prerelease with the three release files. Download
the uploaded assets and source archive and verify them against the reviewed
hashes and inventory. Keep GitHub Pages disabled. Nothing in this workflow
deploys or changes `dokdo.online`.
