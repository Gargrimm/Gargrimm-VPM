import json
import os
from pathlib import Path
import re
import zipfile

root = Path(__file__).resolve().parent.parent
folder = os.environ['PACKAGE_FOLDER']
if not re.fullmatch(r'[a-z0-9][a-z0-9.-]*', folder):
    raise SystemExit('Invalid package folder name.')
package = root / 'Packages' / folder
manifest_path = package / 'package.json'
manifest = json.loads(manifest_path.read_text(encoding='utf-8-sig'))
name, version = manifest['name'], manifest['version']
if name != folder or not re.fullmatch(r'\d+\.\d+\.\d+(?:-[0-9A-Za-z.-]+)?', version):
    raise SystemExit('The folder must match the package name and the version must be SemVer.')
archive = root / f'{name}-{version}.zip'
with zipfile.ZipFile(archive, 'w', zipfile.ZIP_DEFLATED) as out:
    for path in sorted(package.rglob('*')):
        if path.is_file() and not any(part in ('.git', '.github') for part in path.relative_to(package).parts):
            out.write(path, path.relative_to(package).as_posix())
with open(os.environ['GITHUB_OUTPUT'], 'a', encoding='utf-8') as output:
    output.write(f'tag={name}-v{version}\nzip_path={archive}\nmanifest_path={manifest_path}\n')
