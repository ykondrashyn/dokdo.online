#!/usr/bin/env bash
set -euo pipefail
root=$(cd "$(dirname "$0")/.." && pwd)
cd "$root"
: "${UNITY_EDITOR:?Set UNITY_EDITOR to your Unity 6000.3.18f1 executable}"
mkdir -p .artifacts
mode=${1:-compile}
target=WebGL
case "$mode" in android|android-*) target=Android ;; esac
args=(-batchmode -nographics -projectPath "$root" -buildTarget "$target" -job-worker-count 2 -logFile "$root/.artifacts/$mode.log")
case "$mode" in
  compile|android-compile) args+=(-executeMethod Dokdo.Web.Editor.StandaloneBuild.Compile -quit) ;;
  test) args+=(-runTests -testPlatform EditMode -testResults "$root/.artifacts/editmode.xml") ;;
  test-play) args+=(-runTests -testPlatform PlayMode -testResults "$root/.artifacts/playmode.xml") ;;
  android-test) args+=(-runTests -testPlatform EditMode -testResults "$root/.artifacts/android-editmode.xml") ;;
  android-test-play) args+=(-runTests -testPlatform PlayMode -testResults "$root/.artifacts/android-playmode.xml") ;;
  android) args+=(-executeMethod Dokdo.Web.Editor.AndroidBuild.Build -quit) ;;
  desktop) args+=(-executeMethod Dokdo.Web.Editor.StandaloneBuild.Desktop -quit) ;;
  mobile) args+=(-executeMethod Dokdo.Web.Editor.StandaloneBuild.Mobile -quit) ;;
  report) args+=(-executeMethod Dokdo.Web.Editor.StandaloneBuild.InspectReport -quit) ;;
  *) echo 'Usage: tool/unity.sh compile|test|test-play|desktop|mobile|report|android|android-compile|android-test|android-test-play' >&2; exit 2 ;;
esac
# Stale evidence must never turn a failed or interrupted run into a pass.
rm -f "$root/.artifacts/$mode.log"
case "$mode" in
  test) rm -f "$root/.artifacts/editmode.xml" ;;
  test-play) rm -f "$root/.artifacts/playmode.xml" ;;
  android-test) rm -f "$root/.artifacts/android-editmode.xml" ;;
  android-test-play) rm -f "$root/.artifacts/android-playmode.xml" ;;
esac
"$UNITY_EDITOR" "${args[@]}"
python3 - "$mode" <<'PY'
import pathlib, sys, xml.etree.ElementTree as ET
mode = sys.argv[1]
log = pathlib.Path('.artifacts', mode+'.log').read_text()
if mode in ('test', 'test-play', 'android-test', 'android-test-play'):
    suite = 'playmode' if mode.endswith('test-play') else 'editmode'
    prefix = 'android-' if mode.startswith('android-') else ''
    result = ET.parse('.artifacts/'+prefix+suite+'.xml').getroot()
    assert result.get('result') == 'Passed' and int(result.get('passed', '0')) > 0, 'Unity tests did not pass'
    print('Unity '+mode+':', result.attrib)
elif mode == 'report':
    print('\n'.join(line for line in log.splitlines() if 'DOKDO_BUILD_REPORT' in line))
else:
    marker = 'DOKDO_STANDALONE_COMPILE_OK' if mode in ('compile', 'android-compile') else 'DOKDO_STANDALONE_BUILD_OK ' + mode.capitalize()
    assert marker in log, 'Unity did not report successful completion; inspect .artifacts/' + mode + '.log'
    print(marker)
PY
