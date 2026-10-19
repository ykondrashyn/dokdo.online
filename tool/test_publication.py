import importlib.util
import json
from pathlib import Path
import tempfile
import unittest
import zipfile
import publication as p

class PublicationTests(unittest.TestCase):
    def test_forbidden_files_and_credentials(self):
        for name in ['secrets/value.txt', 'Library/asset', '../escape', '/absolute',
                     'Assets/key.p12', 'Assets/Promo/capture.cs', 'Assets/.env']:
            with self.subTest(name=name), self.assertRaises(ValueError): p.validate_file(name, b'harmless')
        for value in [b'gh' + b'p_' + b'x'*36, b'AK' + b'IA' + b'A'*16,
                      b'-----BEGIN ' + b'PRIVATE KEY-----']:
            with self.assertRaises(ValueError): p.validate_file('README.md', value)

    def test_symlink_files_and_parent_directories_rejected(self):
        with tempfile.TemporaryDirectory() as temp:
            root=Path(temp); (root/'real').write_text('payload')
            (root/'link').symlink_to(root/'real')
            with self.assertRaises(ValueError): p.file_hashes(root, ['link'])
            (root/'dir').symlink_to(root, target_is_directory=True)
            with self.assertRaises(ValueError): p.file_hashes(root, ['dir/real'])

    def fixture(self, root):
        blob=b'<!doctype html><title>fixture</title>'
        archive=root/'player.zip'
        with zipfile.ZipFile(archive,'w') as z: z.writestr('index.html',blob)
        manifest={'artifacts':{'player.zip':p.sha(archive.read_bytes())}, 'files':{'index.html':p.sha(blob)}}
        p.write_json(root/'publication.json',manifest)
        (root/'SHA256SUMS').write_text(''.join(p.sha((root/n).read_bytes())+'  '+n+'\n' for n in ['player.zip','publication.json']))

    def test_release_rejects_checksum_mismatch_and_extra_file(self):
        with tempfile.TemporaryDirectory() as temp:
            root=Path(temp); self.fixture(root); p.verify(root)
            (root/'extra.txt').write_text('unexpected')
            with self.assertRaises(ValueError): p.verify(root)
            (root/'extra.txt').unlink(); (root/'player.zip').write_bytes(b'changed')
            with self.assertRaises(ValueError): p.verify(root)

    def test_zip_rejects_symlink_and_unexpected_member(self):
        for link in [False, True]:
            with tempfile.TemporaryDirectory() as temp:
                root=Path(temp); self.fixture(root)
                with zipfile.ZipFile(root/'player.zip','w') as z:
                    item=zipfile.ZipInfo('index.html' if link else '../escape')
                    if link: item.external_attr=0o120777 << 16
                    z.writestr(item,b'payload')
                manifest=json.loads((root/'publication.json').read_text())
                manifest['artifacts']['player.zip']=p.sha((root/'player.zip').read_bytes())
                p.write_json(root/'publication.json',manifest)
                (root/'SHA256SUMS').write_text(''.join(p.sha((root/n).read_bytes())+'  '+n+'\n' for n in ['player.zip','publication.json']))
                with self.assertRaises(ValueError): p.verify(root)

    def test_acceptance_requires_physical_browser_and_exact_files(self):
        with tempfile.TemporaryDirectory() as temp:
            path=Path(temp)/'acceptance.json'
            data={'artifact_files':{'index.html':'abc'}, 'sessions':[]}
            p.write_json(path,data)
            with self.assertRaises(ValueError): p.acceptance(path,data['artifact_files'])
            for browser in ['chromium','firefox','physical-mobile']:
                data['sessions'].append({'browser':browser,'platform':'fixture',
                    'started_utc':'fixture','ended_utc':'fixture','evidence':['fixture'],
                    'network_external_requests':[],'websockets':[], 'physical_device':False,
                    'scenarios':{s:'passed' for s in p.SCENARIOS}})
            p.write_json(path,data)
            with self.assertRaises(ValueError): p.acceptance(path,data['artifact_files'])
            data['sessions'][-1]['physical_device']=True; p.write_json(path,data)
            p.acceptance(path,data['artifact_files'])
            with self.assertRaises(ValueError): p.acceptance(path,{'index.html':'changed'})

if __name__ == '__main__': unittest.main()
