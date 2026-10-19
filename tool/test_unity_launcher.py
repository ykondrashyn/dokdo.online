import json
import os
from pathlib import Path
import shutil
import subprocess
import tempfile
import unittest


class UnityLauncherTests(unittest.TestCase):
    def fixture(self, root):
        (root/'tool').mkdir()
        shutil.copyfile(Path(__file__).with_name('unity.sh'), root/'tool/unity.sh')
        editor=root/'editor.py'
        editor.write_text('''#!/usr/bin/env python3
import json, os, pathlib, sys
args=sys.argv[1:]
pathlib.Path('editor-args.json').write_text(json.dumps(args))
if os.environ.get('SILENT_EDITOR') == '1': sys.exit(0)
if '-runTests' in args:
    pathlib.Path(args[args.index('-testResults')+1]).write_text('<test-run result="Passed" passed="1"/>')
    marker='Editor tests completed'
else:
    method=args[args.index('-executeMethod')+1]
    marker='DOKDO_STANDALONE_COMPILE_OK' if method.endswith('.Compile') else 'DOKDO_STANDALONE_BUILD_OK '+('Android' if 'AndroidBuild' in method else method.split('.')[-1])
pathlib.Path(args[args.index('-logFile')+1]).write_text(marker)
''')
        editor.chmod(0o755)
        return dict(os.environ, UNITY_EDITOR=str(editor))

    def test_android_target_and_reports_are_separate_from_webgl(self):
        with tempfile.TemporaryDirectory() as tmp:
            root=Path(tmp); env=self.fixture(root)
            for mode in ['compile','desktop','mobile','test','test-play',
                         'android-compile','android','android-test','android-test-play']:
                with self.subTest(mode=mode):
                    result=subprocess.run(['bash',str(root/'tool/unity.sh'),mode],env=env,capture_output=True,text=True)
                    self.assertEqual(result.returncode,0,result.stderr)
                    args=json.loads((root/'editor-args.json').read_text())
                    self.assertEqual(args[args.index('-buildTarget')+1],
                                     'Android' if mode.startswith('android') else 'WebGL')
            for suite in ['editmode','playmode']:
                self.assertTrue((root/'.artifacts'/(suite+'.xml')).exists())
                self.assertTrue((root/'.artifacts'/('android-'+suite+'.xml')).exists())

    def test_silent_editor_cannot_reuse_old_success(self):
        with tempfile.TemporaryDirectory() as tmp:
            root=Path(tmp); env=self.fixture(root)
            for mode in ['android-compile','android-test']:
                command=['bash',str(root/'tool/unity.sh'),mode]
                subprocess.run(command,env=env,check=True,capture_output=True)
                result=subprocess.run(command,env=dict(env,SILENT_EDITOR='1'),capture_output=True)
                self.assertNotEqual(result.returncode,0)


if __name__ == '__main__': unittest.main()
