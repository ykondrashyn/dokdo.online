#!/usr/bin/env python3
"""Fail-closed source, build and release inventory checks (Python standard library)."""
import argparse
import hashlib
import json
import os
from pathlib import Path, PurePosixPath
import re
import shutil
import stat
import subprocess
import tempfile
import zipfile

ROOT = Path(__file__).resolve().parents[1]
INVENTORY = 'docs/publication/source-inventory.json'
VERSION = 'dokdo-1.16.6-web9-r6-public.1'
SOURCE_TAG = 'dokdo-1.16.6-web9-r6'
SOURCE_COMMIT = '847f6e2284fcc87f9fa20b1e1addbd585f7627b8'
UNITY = '6000.3.18f1'
SCENARIOS = ['startup', 'steering', 'docking', 'combat', 'upgrades', 'settings',
             'reload-persistence', 'save-export-import', 'corrupt-save-rejection',
             'backup-recovery', 'visibility-resume', 'static-only-network']

def require(condition, message):
    if not condition: raise ValueError(message)

def sha(data): return hashlib.sha256(data).hexdigest()
def write_json(path, value):
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(value, indent=2, sort_keys=True)+'\n')
def git(*args): return subprocess.check_output(['git', '-C', str(ROOT), *args], text=True).strip()

def safe_name(name):
    p = PurePosixPath(name)
    require(bool(name) and not p.is_absolute() and '..' not in p.parts and
            '.' not in p.parts and '\\' not in name and str(p) == name and
            not any(ord(c) < 32 for c in name), 'Unsafe path: '+name)
    return p

def validate_file(name, data):
    p = safe_name(name)
    forbidden = {'.git', '.env', 'Library', 'Temp', 'Logs', 'UserSettings', 'node_modules',
                 'backups', 'secrets', 'server', 'deployment', 'systemd', 'Promo'}
    require(not forbidden.intersection(p.parts), 'Forbidden path: '+name)
    require(p.suffix.lower() not in {'.pem', '.key', '.p12', '.pfx', '.jks', '.keystore', '.ulf', '.log'}, 'Forbidden payload: '+name)
    # Broad commercial code is excluded; only the independent adapters remain.
    require('CodeStage' not in p.parts, 'Recovered anti-cheat payload: '+name)
    require(b'-----BEGIN '+b'PRIVATE KEY-----' not in data and
            b'-----BEGIN '+b'OPENSSH PRIVATE KEY-----' not in data, 'Signing material: '+name)
    for pattern in [rb'gh[pousr]_[A-Za-z0-9]{36,}', rb'AKIA[A-Z0-9]{16}',
                    rb'xox[baprs]-[A-Za-z0-9-]{20,}']:
        require(not re.search(pattern, data), 'Credential-shaped value: '+name)

def source_files():
    result = set(git('ls-files', '--cached', '--others', '--exclude-standard', '-z').split('\0')) - {''}
    result.discard(INVENTORY)
    return sorted(result)

def file_hashes(root, names):
    result = {}
    for name in names:
        safe_name(name); p = root/name
        require(p.is_file() and not p.is_symlink(), 'Missing or nonregular file: '+name)
        require(all(not (root/parent).is_symlink() for parent in PurePosixPath(name).parents), 'Symlink parent: '+name)
        data = p.read_bytes(); validate_file(name, data); result[name] = sha(data)
    return result

def inventory():
    write_json(ROOT/INVENTORY, {'schema': 1, 'files': file_hashes(ROOT, source_files())})

def audit():
    expected = json.loads((ROOT/INVENTORY).read_text())['files']
    roots={'Assets','Packages','ProjectSettings','ThirdParty','docs','tool','.github','screenshots'}
    root_files={'README.md','THIRD_PARTY_NOTICES.md','.gitignore','.gitleaks.toml','dokdo-1.16.6-r1.apk'}
    require(all(name in root_files or PurePosixPath(name).parts[0] in roots for name in expected), 'Unexpected source root')
    require(file_hashes(ROOT, source_files()) == expected, 'Source inventory differs; review every change before refreshing it')
    require(not (ROOT/'.git/objects/info/alternates').exists(), 'Shared Git object storage is forbidden')
    require(not git('ls-files', '--stage').startswith('160000'), 'Submodules are forbidden')
    require(not any(line.startswith(('120000', '160000')) for line in git('ls-files', '--stage').splitlines()), 'Git links are forbidden')
    settings = (ROOT/'ProjectSettings/ProjectSettings.asset').read_text()
    for key in ['cloudProjectId', 'organizationId', 'projectName', 'AndroidKeystoreName', 'AndroidKeyaliasName']:
        require(not re.search(r'^\s*'+key+r':[ \t]*\S.*$', settings, re.M), 'Unsanitized setting: '+key)
    require(UNITY in (ROOT/'ProjectSettings/ProjectVersion.txt').read_text(), 'Unexpected Unity version')
    print('Source inventory and policy: passed ('+str(len(expected))+' files)')

def secrets():
    audit()
    scanner = os.environ.get('GITLEAKS', 'gitleaks')
    results = ROOT/'.artifacts'; results.mkdir(exist_ok=True)
    with tempfile.TemporaryDirectory() as tmp:
        stage = Path(tmp)
        for name in source_files()+[INVENTORY]:
            p=stage/name; p.parent.mkdir(parents=True, exist_ok=True); shutil.copyfile(ROOT/name, p)
        subprocess.run([scanner, 'dir', str(stage), '--config', str(ROOT/'.gitleaks.toml'), '--redact=100', '--report-format', 'json',
                        '--report-path', str(results/'gitleaks-files.json')], check=True)
    subprocess.run([scanner, 'git', str(ROOT), '--config', str(ROOT/'.gitleaks.toml'), '--log-opts=--all', '--redact=100', '--report-format', 'json',
                    '--report-path', str(results/'gitleaks-history.json')], check=True)

def build_trees():
    return {p: git('rev-parse', 'HEAD:'+p) for p in ['Assets', 'Packages', 'ProjectSettings']}

def clean(): require(not git('status', '--porcelain'), 'Commit reviewed changes before building or packaging')

def build():
    audit(); clean()
    build_commit = git('rev-parse', 'HEAD'); trees = build_trees()
    # Unity writes build settings even in batch mode; restore their exact input bytes.
    settings = {p:p.read_bytes() for p in (ROOT/'ProjectSettings').glob('*') if p.is_file()}
    try:
        for mode in ['compile', 'test', 'desktop', 'mobile']:
            subprocess.run(['bash', str(ROOT/'tool/unity.sh'), mode], cwd=ROOT, check=True)
    finally:
        for path, data in settings.items(): path.write_bytes(data)
    clean()
    output=ROOT/'Builds/Standalone'
    shutil.copyfile(ROOT/'tool/selector.html', output/'index.html')
    files=payload_hashes(output)
    write_json(ROOT/'.artifacts/build.json', {'build_commit': build_commit, 'trees': trees,
               'unity': UNITY, 'files': files, 'compile': 'passed', 'editmode': 'passed'})

def payload_hashes(root):
    require(root.is_dir() and not root.is_symlink(), 'Player directory is missing or a symlink')
    names=[]
    for p in root.rglob('*'):
        require(not p.is_symlink(), 'Payload contains symlink')
        if not p.is_file(): continue
        name=p.relative_to(root).as_posix()
        require(name == 'index.html' or re.fullmatch(r'(Desktop|Mobile)/(index\.html|Build/[0-9a-f]+\.(data|wasm|loader\.js|framework\.js)|TemplateData/standalone\.(css|js))', name), 'Unexpected player file: '+name)
        names.append(name)
    for variant in ['Desktop', 'Mobile']:
        subset=[n for n in names if n.startswith(variant+'/')]
        require(len(subset) == 7, 'Incomplete player: '+variant)
    require('index.html' in names, 'Missing capability selector')
    return file_hashes(root, sorted(names))

def acceptance(path, files):
    data=json.loads(path.read_text())
    require(data.get('artifact_files') == files, 'Acceptance is for different player bytes')
    sessions=data.get('sessions', [])
    for browser in ['chromium', 'firefox', 'physical-mobile']:
        matches=[s for s in sessions if s.get('browser') == browser]
        require(bool(matches), 'Missing browser acceptance: '+browser)
        s=matches[-1]
        require(bool(s.get('platform')) and bool(s.get('started_utc')) and bool(s.get('ended_utc')) and bool(s.get('evidence')), 'Missing session evidence: '+browser)
        require(s.get('network_external_requests') == [] and s.get('websockets') == [], 'Non-static network activity: '+browser)
        require(all(s.get('scenarios', {}).get(k) == 'passed' for k in SCENARIOS), 'Incomplete scenarios: '+browser)
        if browser == 'physical-mobile': require(s.get('physical_device') is True, 'Emulation is not physical-device acceptance')
    return data

def package(acceptance_path=None):
    audit(); clean()
    build_record=json.loads((ROOT/'.artifacts/build.json').read_text())
    require(build_record['trees'] == build_trees(), 'Unity input trees changed after build')
    output=ROOT/'Builds/Standalone'; files=payload_hashes(output)
    require(files == build_record['files'], 'Player checksums differ from the recorded build')
    accepted=acceptance(acceptance_path, files) if acceptance_path else None
    directory=ROOT/'Builds'/('Release' if accepted else 'Review')
    require(not directory.exists(), 'Output already exists; do not overwrite reviewed artifacts')
    directory.mkdir(parents=True)
    zip_path=directory/(VERSION+'-standalone.zip')
    with zipfile.ZipFile(zip_path, 'w', compression=zipfile.ZIP_DEFLATED) as archive:
        for name in files:
            item=zipfile.ZipInfo(name, date_time=(2026, 10, 19, 0, 0, 0))
            item.external_attr=0o100644 << 16; item.compress_type=zipfile.ZIP_DEFLATED
            archive.writestr(item, (output/name).read_bytes())
    manifest={'schema':1, 'version':VERSION, 'source_tag':SOURCE_TAG, 'source_commit':SOURCE_COMMIT,
        'public_build_commit':build_record['build_commit'], 'publication_commit':git('rev-parse','HEAD'),
        'unity_version':UNITY, 'status':'accepted' if accepted else 'unqualified-review',
        'files':files, 'artifacts':{zip_path.name:sha(zip_path.read_bytes())},
        'validation':accepted or {'compile':'passed','editmode':'passed','browser_acceptance':'pending'}}
    write_json(directory/'publication.json',manifest)
    (directory/'SHA256SUMS').write_text(''.join(sha(p.read_bytes())+'  '+p.name+'\n' for p in [zip_path,directory/'publication.json']))
    verify(directory)
    print('Created '+str(directory))

def verify(directory):
    require(directory.is_dir() and not directory.is_symlink(), 'Release directory is missing or a symlink')
    require(all(p.is_file() and not p.is_symlink() for p in directory.iterdir()), 'Release must contain only regular files')
    manifest=json.loads((directory/'publication.json').read_text())
    expected=set(manifest['artifacts']) | {'publication.json','SHA256SUMS'}
    require({p.name for p in directory.iterdir()} == expected, 'Unexpected release contents')
    checks={}
    for line in (directory/'SHA256SUMS').read_text().splitlines():
        digest,name=line.split('  ',1); safe_name(name)
        require(name not in checks, 'Duplicate checksum'); checks[name]=digest
    require(set(checks) == expected-{'SHA256SUMS'}, 'Incomplete checksums')
    for name,digest in checks.items():
        p=directory/name; require(not p.is_symlink(), 'Release symlink')
        require(sha(p.read_bytes()) == digest, 'Release checksum mismatch: '+name)
    for name,digest in manifest['artifacts'].items():
        require(checks[name] == digest, 'Manifest artifact hash mismatch')
        with zipfile.ZipFile(directory/name) as archive:
            entries=archive.infolist()
            require(len(entries) == len({i.filename for i in entries}), 'Duplicate ZIP entries')
            require({i.filename for i in entries} == set(manifest['files']), 'ZIP inventory mismatch')
            for item in entries:
                safe_name(item.filename)
                require(stat.S_IFMT(item.external_attr >> 16) in (0,stat.S_IFREG), 'Nonregular ZIP member')
                blob=archive.read(item); validate_file(item.filename,blob)
                require(sha(blob) == manifest['files'][item.filename], 'ZIP checksum mismatch')
    print('Release inventory and checksums: passed')

if __name__ == '__main__':
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('command', choices=['inventory','audit','secrets','build','candidate','package','verify'])
    parser.add_argument('--acceptance', type=Path)
    parser.add_argument('--directory', type=Path)
    args=parser.parse_args()
    try:
        if args.command == 'package':
            require(args.acceptance is not None, 'Release packaging requires --acceptance FILE'); package(args.acceptance)
        elif args.command == 'candidate': package()
        elif args.command == 'verify': verify(args.directory or ROOT/'Builds/Release')
        else: globals()[args.command]()
    except (ValueError, OSError, subprocess.CalledProcessError) as error:
        parser.exit(1, str(error)+'\n')
