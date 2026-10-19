#!/usr/bin/env bash
set -euo pipefail
root=$(cd "$(dirname "$0")/.." && pwd)
cd "$root"
: "${UNITY_EDITOR:?Set UNITY_EDITOR to your Unity 6000.3.18f1 executable}"
mkdir -p .artifacts
mode=${1:-compile}
args=(-batchmode -nographics -projectPath "$root" -buildTarget WebGL -job-worker-count 2 -logFile "$root/.artifacts/$mode.log")
case "$mode" in
  compile) args+=(-executeMethod Dokdo.Web.Editor.StandaloneBuild.Compile -quit) ;;
  test) args+=(-runTests -testPlatform EditMode -testResults "$root/.artifacts/editmode.xml") ;;
  test-play) args+=(-runTests -testPlatform PlayMode -testResults "$root/.artifacts/playmode.xml") ;;
  desktop) args+=(-executeMethod Dokdo.Web.Editor.StandaloneBuild.Desktop -quit) ;;
  mobile) args+=(-executeMethod Dokdo.Web.Editor.StandaloneBuild.Mobile -quit) ;;
  report) args+=(-executeMethod Dokdo.Web.Editor.StandaloneBuild.InspectReport -quit) ;;
  *) echo 'Usage: tool/unity.sh compile|test|test-play|desktop|mobile|report' >&2; exit 2 ;;
esac
"$UNITY_EDITOR" "${args[@]}"
python3 - "$mode" <<'PY'
import pathlib, sys, xml.etree.ElementTree as ET
mode = sys.argv[1]
log = pathlib.Path('.artifacts', mode+'.log').read_text()
if mode in ('test', 'test-play'):
    result = ET.parse('.artifacts/'+('editmode' if mode=='test' else 'playmode')+'.xml').getroot()
    assert result.get('result') == 'Passed' and int(result.get('passed', '0')) > 0, 'Unity tests did not pass'
    print('Unity '+mode+':', result.attrib)
elif mode == 'report':
    print('\n'.join(line for line in log.splitlines() if 'DOKDO_BUILD_REPORT' in line))
else:
    marker = 'DOKDO_STANDALONE_COMPILE_OK' if mode == 'compile' else 'DOKDO_STANDALONE_BUILD_OK ' + mode.capitalize()
    assert marker in log, 'Unity did not report successful completion; inspect .artifacts/' + mode + '.log'
    print(marker)
PY
