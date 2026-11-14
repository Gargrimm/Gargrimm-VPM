import json
import os
from pathlib import Path
import re
import subprocess
import tempfile

ROOT = Path(__file__).resolve().parent.parent
NAME_PATTERN = re.compile(r'[a-z0-9][a-z0-9.-]*\Z')
VERSION_PATTERN = re.compile(r'\d+\.\d+\.\d+(?:-[0-9A-Za-z.-]+)?\Z')


def pending_packages(packages_dir, releases, selected=None):
    releases_by_tag = {release['tag_name']: release for release in releases}
    folders = sorted(p for p in packages_dir.iterdir() if p.is_dir() and (p / 'package.json').is_file())
    if selected:
        folders = [p for p in folders if p.name == selected]
        if not folders:
            raise ValueError(f'Package not found: {selected}')
    pending = []
    for folder in folders:
        manifest = json.loads((folder / 'package.json').read_text(encoding='utf-8-sig'))
        name, version = manifest['name'], manifest['version']
        if not NAME_PATTERN.fullmatch(folder.name) or name != folder.name or not VERSION_PATTERN.fullmatch(version):
            raise ValueError(f'Invalid package name or version: {folder.name}')
        tag = f'{name}-v{version}'
        release = releases_by_tag.get(tag)
        if release:
            if release.get('draft'):
                raise ValueError(f'Publish or delete the draft release before continuing: {tag}')
            print(f'Already published: {tag}', flush=True)
        else:
            pending.append((folder.name, version, tag))
    return pending


def main():
    repo = os.environ['GITHUB_REPOSITORY']
    pages = json.loads(subprocess.check_output([
        'gh', 'api', f'repos/{repo}/releases', '--paginate', '--slurp',
        '-f', 'per_page=100', '-X', 'GET'
    ]))
    releases = [release for page in pages for release in page]
    selected = os.environ.get('PACKAGE_FOLDER', '').strip() or None
    for folder, version, tag in pending_packages(ROOT / 'Packages', releases, selected):
        print(f'Publishing {tag}', flush=True)
        with tempfile.TemporaryDirectory() as temp:
            env = os.environ.copy()
            env['PACKAGE_FOLDER'] = folder
            env['GITHUB_OUTPUT'] = str(Path(temp) / 'output')
            subprocess.run(['python3', str(ROOT / 'scripts' / 'build_package.py')], env=env, check=True)
        archive = ROOT / f'{folder}-{version}.zip'
        manifest = ROOT / 'Packages' / folder / 'package.json'
        command = ['gh', 'release', 'create', tag, str(archive), str(manifest), '--title', tag, '--generate-notes']
        if '-' in version:
            command.append('--prerelease')
        subprocess.run(command, check=True)


if __name__ == '__main__':
    main()
