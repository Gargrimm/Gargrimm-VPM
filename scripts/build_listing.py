import json
import os
from pathlib import Path
import subprocess
from urllib.parse import quote


def gh(*args):
    return subprocess.check_output(['gh', 'api', *args])


def build_listing(releases, read_asset, listing_url):
    listing = {'name': 'Gargrimm VPM', 'id': 'com.gargrimm.vpm',
               'author': 'Gargrimm', 'url': listing_url, 'packages': {}}
    for release in releases:
        if release.get('draft'):
            continue
        assets = {asset['name']: asset for asset in release['assets']}
        if 'package.json' not in assets:
            continue
        manifest = json.loads(read_asset(assets['package.json']['id']).decode('utf-8-sig'))
        name, version = manifest['name'], manifest['version']
        zip_name = f'{name}-{version}.zip'
        if zip_name not in assets:
            raise ValueError(f'Missing {zip_name} in {release["tag_name"]}')
        versions = listing['packages'].setdefault(name, {'versions': {}})['versions']
        if version in versions:
            raise ValueError(f'Duplicate version: {name} {version}')
        manifest['url'] = assets[zip_name]['browser_download_url']
        versions[version] = manifest
    return listing


def main():
    repo = os.environ['GITHUB_REPOSITORY']
    owner, repo_name = repo.split('/', 1)
    page_path = '' if repo_name.lower() == f'{owner.lower()}.github.io' else f'/{quote(repo_name)}'
    listing_url = f'https://{owner.lower()}.github.io{page_path}/index.json'
    pages = json.loads(gh(f'repos/{repo}/releases', '--paginate', '--slurp', '-f', 'per_page=100', '-X', 'GET'))
    releases = [release for page in pages for release in page]
    def read_asset(asset_id):
        return gh(f'repos/{repo}/releases/assets/{asset_id}', '-H', 'Accept: application/octet-stream')
    listing = build_listing(releases, read_asset, listing_url)
    site = Path('site')
    site.mkdir(exist_ok=True)
    (site / 'index.json').write_text(json.dumps(listing, indent=2, ensure_ascii=False) + '\n', encoding='utf-8')
    (site / 'index.html').write_text('<!doctype html><meta charset="utf-8"><title>Gargrimm VPM</title><h1>Gargrimm VPM</h1><p>URL for VCC and ALCOM: <a href="index.json">index.json</a></p>', encoding='utf-8')
    print(f'Listing generated: {listing_url} ({len(listing["packages"])} packages)')


if __name__ == '__main__':
    main()
