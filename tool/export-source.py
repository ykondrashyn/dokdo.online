#!/usr/bin/env python3
"""Export only the reviewed pinned files; never copy Git objects or history.

This produces the provenance baseline, BEFORE the documented standalone edits.
The committed publication tree is the final sanitized source.
"""
import argparse
import hashlib
import json
from pathlib import Path, PurePosixPath
import subprocess
from publication import validate_file

def export(repo, output, allowlist):
    spec = json.loads(allowlist.read_text())
    actual = subprocess.check_output(['git', '-C', str(repo), 'rev-parse', spec['source_tag']+'^{}'], text=True).strip()
    if actual != spec['source_commit']: raise ValueError('Source tag does not match pinned commit')
    subprocess.run(['git', '-C', str(repo), 'verify-tag', spec['source_tag']], check=True)
    if output.exists() or output.is_symlink(): raise ValueError('Destination must not exist')
    if any(parent.is_symlink() for parent in output.absolute().parents): raise ValueError('Symlink export parent')
    # Validate everything before writing anything.
    payloads = {}
    for name, digest in spec['files'].items():
        path = PurePosixPath(name)
        if path.is_absolute() or '..' in path.parts or '.git' in path.parts:
            raise ValueError('Unsafe export path')
        row = subprocess.check_output(['git', '-C', str(repo), 'ls-tree', actual, '--', name], text=True)
        if not row.startswith(('100644 blob ', '100755 blob ')): raise ValueError('Only regular blobs may be exported')
        blob = subprocess.check_output(['git', '-C', str(repo), 'show', actual+':'+name])
        if hashlib.sha256(blob).hexdigest() != digest: raise ValueError('Source hash mismatch: '+name)
        validate_file(name, blob)
        payloads[name] = blob
    output.mkdir(parents=True)
    for name, blob in payloads.items():
        target = output/name; target.parent.mkdir(parents=True, exist_ok=True); target.write_bytes(blob)

if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('repository', type=Path)
    parser.add_argument('output', type=Path)
    args = parser.parse_args()
    export(args.repository, args.output, Path(__file__).resolve().parents[1]/'docs/publication/export-allowlist.json')
