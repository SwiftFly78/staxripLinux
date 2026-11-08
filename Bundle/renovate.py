"""Renovate support of the recipes in Bundle/components, see "Updates" in Bundle/README.md.

    renovate.py post-upgrade <recipe>   completes an update of Renovate, which only changes the annotated versions:
                                        the commits of git sources, the URLs and checksums of downloads and the names
                                        of their files and folders in the recipe, for requirements-build.txt the hashes
    renovate.py check [--online]        verifies the annotations of all recipes, --online also the tag commits and
                                        the revisions

A version variable carries its annotation as a comment, which doesn't change the cache key of the recipe:

    version=8.1.3 # renovate: datasource=github-tags depName=FFmpeg/FFmpeg extractVersion=^n(?<version>.+)$
    zimg_version=1-a5c7f1e # renovate: datasource=git-refs depName=https://... branch=master digest=<commit> source=2

datasource, depName, registryUrl, versioning and extractVersion are read by Renovate. source lists the lines of
sources the version belongs to (default: the git line of a git datasource, otherwise every download that contains the
version), urlversion=nodots also replaces the version without dots in URLs (7z2605), none=<reason> marks sources
without updates.
"""
import hashlib
import json
import re
import subprocess
import sys
import tempfile
import urllib.request
from pathlib import Path

B = Path(__file__).resolve().parent
ANNOTATION = re.compile(r'^(?P<var>[a-z0-9_]*version)=(?P<value>\S+) # renovate: (?P<keys>.+)$', re.M)
SOURCES = re.compile(r'^sources="(.*?)"', re.S | re.M)
DATED = re.compile(r'^\d{4}-\d{2}-\d{2}-[0-9a-f]{7}$')


def annotations(text):
    ret = []
    for m in ANNOTATION.finditer(text):
        keys = dict(k.split('=', 1) for k in m.group('keys').split() if '=' in k)
        ret.append({'var': m.group('var'), 'value': m.group('value'), 'keys': keys, 'line': m.group(0)})
    return ret


def source_lines(text):
    m = SOURCES.search(text)
    return [line.strip() for line in m.group(1).splitlines() if line.strip()] if m else []


def run(*args, cwd=None):
    return subprocess.run(args, capture_output=True, text=True, check=True, cwd=cwd, timeout=600).stdout


# the prefix of the tags in front of the version group of extractVersion, which mustn't contain groups
TAG_PREFIX = re.compile(r'\^(.*)\(\?<version>[^)]*\)\$')


def tag_of(keys, version):
    if 'tag' in keys:
        return keys['tag'].replace('{version_}', version.replace('.', '_')).replace('{version}', version)
    m = TAG_PREFIX.fullmatch(keys.get('extractVersion', '^(?<version>.+)$'))
    return (m.group(1).replace('\\', '') if m else '') + version


# a version of the datasource in the style of the recipe, like 29.2 for the tag v29_2 or R80 for the PyPI version 80
def normalize(old, new):
    if new.startswith('v') and not old.startswith('v'):
        new = new[1:]
    if '_' in new and '.' in old:
        new = new.replace('_', '.')
    prefix = re.match(r'[A-Za-z]*', old).group(0)
    if prefix and new[:1].isdigit():
        new = prefix + new
    return new


def json_of(url):
    with urllib.request.urlopen(urllib.request.Request(url, headers={'User-Agent': 'StaxRipLinux'}), timeout=120) as r:
        return json.load(r)


def tag_commit(url, tag):
    refs = dict(reversed(line.split('\t')) for line in
                run('git', 'ls-remote', url, f'refs/tags/{tag}', f'refs/tags/{tag}^{{}}').splitlines())
    # the commit of an annotated tag is the peeled one
    commit = refs.get(f'refs/tags/{tag}^{{}}') or refs.get(f'refs/tags/{tag}')
    if not commit:
        raise SystemExit(f'tag {tag} not found in {url}')
    return commit


# the number of commits up to a commit like git rev-list --count, the last page of the listing with one per page
def commit_count(url, commit):
    repo = re.sub(r'^https://github\.com/|\.git$', '', url)
    request = urllib.request.Request(f'https://api.github.com/repos/{repo}/commits?sha={commit}&per_page=1',
                                     headers={'User-Agent': 'StaxRipLinux'})
    with urllib.request.urlopen(request, timeout=120) as r:
        return int(re.search(r'[?&]page=(\d+)>; rel="last"', r.headers['Link']).group(1))


# the revision of recipes like AviSynth+, the commit count of their git source
def revision(text):
    m = re.search(r'^revision=(\d+)', text, re.M)
    git = next((line.split() for line in source_lines(text) if line.startswith('git ')), None)
    return (m, git) if m and git else (None, None)


# the version of a branch commit in the style of the old version: <date>-<commit>, <branch>-<commit> or
# <tag>-<commit> like git describe
def branch_version(old, url, branch, commit):
    if DATED.match(old):
        with tempfile.TemporaryDirectory() as tmp:
            run('git', 'init', '-q', tmp)
            run('git', 'fetch', '-q', '--depth', '1', url, commit, cwd=tmp)
            return run('git', 'log', '-1', '--format=%cs', 'FETCH_HEAD', cwd=tmp).strip() + '-' + commit[:7]
    if old.startswith(branch + '-'):
        return f'{branch}-{commit[:7]}'
    with tempfile.TemporaryDirectory() as tmp:
        run('git', 'init', '-q', tmp)
        run('git', 'fetch', '-q', '--filter=blob:none', '--tags', url, branch, cwd=tmp)
        described = run('git', 'describe', '--tags', '--abbrev=0', commit, cwd=tmp).strip()
    if described.startswith('v') and not old.startswith('v'):
        described = described[1:]
    return f'{described}-{commit[:7]}'


def sha256(url):
    h = hashlib.sha256()
    with urllib.request.urlopen(urllib.request.Request(url, headers={'User-Agent': 'StaxRipLinux'}), timeout=600) as r:
        for chunk in iter(lambda: r.read(1 << 20), b''):
            h.update(chunk)
    return h.hexdigest()


def variants(old, new, keys):
    pairs = [(tag_of(keys, old), tag_of(keys, new)), (old, new)]
    if keys.get('urlversion') == 'nodots':
        pairs.append((old.replace('.', ''), new.replace('.', '')))
    # the folder of the minor version in URLs like .../6.11/6.11.3/...
    om, nm = '.'.join(old.split('.')[:2]), '.'.join(new.split('.')[:2])
    if old.count('.') >= 2 and om != nm:
        pairs.append(('/' + om + '/', '/' + nm + '/'))
    return [(o, n) for o, n in pairs if o and o != n]


# URLs also get versions after letters (7z2605), the recipe text only versions after other characters, commit hashes
# contain letters and digits. A version inside a longer one, like 0.2 in 0.2.1 or 3.0.2, stays. All pairs and the
# literal names are replaced in one pass, so the new version of one pair isn't replaced again by another pair.
def replace_version(text, pairs, url=False, literals=None):
    literals, versions = dict(literals or {}), {}
    for o, n in pairs:
        if o[0] == '/':
            literals[o] = n
        elif versions.setdefault(o, n) != n:
            raise SystemExit(f'{o} becomes both {versions[o]} and {n}, complete the update by hand')
    alternatives = [re.escape(o) for o in sorted(literals, key=len, reverse=True)]
    if versions:
        before = r'(?<![0-9])' if url else r'(?<![0-9A-Za-z])'
        olds = '|'.join(re.escape(o) for o in sorted(versions, key=len, reverse=True))
        alternatives.append(before + r'(?<![0-9]\.)(?:' + olds + r')(?![0-9]|\.[0-9])')
    if not alternatives:
        return text
    return re.sub('|'.join(alternatives), lambda m: literals.get(m.group(0), versions.get(m.group(0))), text)


def file_name(line):
    parts = line.split()
    return parts[2] if len(parts) > 2 else urllib.request.unquote(parts[0].rsplit('/', 1)[1])


def stem(name):
    return re.sub(r'(\.tar)?\.(gz|xz|bz2|zst|tgz|zip|7z|whl)$', '', name)


# the names of the downloaded files, their folders and the version in the build and bundle functions
def replace_in_functions(text, names, pairs):
    start = text.index('build()') if 'build()' in text else len(text)
    literals = {}
    for o, n in names:
        literals[o], literals[stem(o)] = n, stem(n)
    return text[:start] + replace_version(text[start:], pairs, literals=literals)


# the hashes of the build tools in requirements-build.txt, of the files of the same platforms in the new versions
# the glibc of the build image, Ubuntu 22.04, and the versions of the legacy manylinux tags
GLIBC = (2, 35)
MANYLINUX = {'manylinux1': (2, 5), 'manylinux2010': (2, 12), 'manylinux2014': (2, 17)}


# the python and abi tags and the platform tags of a wheel, a compound platform tag lists them in any order,
# or the archive type of a source distribution
def file_tags(name):
    if not name.endswith('.whl'):
        return ('sdist', '.tar.gz' if name.endswith('.tar.gz') else Path(name).suffix), set()
    python, abi, platform = name[:-4].split('-')[-3:]
    return (python, abi), set(platform.split('.'))


def installs_on_image(platform):
    m = re.fullmatch(r'manylinux_(\d+)_(\d+)_x86_64', platform)
    if m:
        return (int(m.group(1)), int(m.group(2))) <= GLIBC
    m = re.fullmatch(r'(manylinux1|manylinux2010|manylinux2014)_x86_64', platform)
    return platform == 'any' or bool(m) and MANYLINUX[m.group(1)] <= GLIBC


def update_requirements(path):
    line = re.compile(r'^([A-Za-z0-9._-]+)==(\S+) --hash=sha256:([0-9a-f]{64})$', re.M)
    old = {m.group(1): (m.group(2), m.group(3)) for m in line.finditer(run('git', 'show', f'HEAD:Bundle/{path.name}', cwd=B.parent))}
    text = path.read_text()
    for m in list(line.finditer(text)):
        name, version, digest = m.groups()
        if name not in old or old[name][0] == version:
            continue
        files = json_of(f'https://pypi.org/pypi/{name}/{old[name][0]}/json')['urls']
        old_file = next(f for f in files if f['digests']['sha256'] == old[name][1])['filename']
        kind, platforms = file_tags(old_file)
        # a file of the same kind that installs on the build image, preferably with the platform tags of the old one
        new_files = []
        for f in json_of(f'https://pypi.org/pypi/{name}/{version}/json')['urls']:
            new_kind, new_platforms = file_tags(f['filename'])
            if new_kind == kind and (kind[0] == 'sdist' or any(installs_on_image(p) for p in new_platforms)):
                new_files.append((len(platforms & new_platforms), f))
        if not new_files:
            raise SystemExit(f'{name} {version} has no file that installs like {old_file}')
        new_file = max(new_files, key=lambda x: x[0])[1]
        text = text.replace(m.group(0), f"{name}=={version} --hash=sha256:{new_file['digests']['sha256']}")
    path.write_text(text)


def post_upgrade(recipe):
    path = Path(recipe)
    if path.name == 'requirements-build.txt':
        update_requirements(path)
        return
    text = path.read_text()
    old_text = run('git', 'show', f'HEAD:{path.as_posix()}', cwd=B.parent)
    old = {a['var']: a for a in annotations(old_text)}
    lines, old_lines = source_lines(text), source_lines(old_text)
    # Renovate runs this for every update of a group, a version whose sources differ from HEAD is already completed
    done = lambda indexes: any(lines[i] != old_lines[i] for i in indexes)
    all_names, all_pairs = [], []

    for a in annotations(text):
        keys, before = a['keys'], old.get(a['var'])
        if not before or 'datasource' not in keys:
            continue
        indexes = [int(i) - 1 for i in keys['source'].split(',')] if 'source' in keys else None

        if keys['datasource'] == 'git-refs':
            commit, old_commit = keys['digest'], before['keys']['digest']
            i = indexes[0] if indexes else next(k for k, line in enumerate(lines) if line.startswith('git '))
            if commit == old_commit or done([i]):
                continue
            url = lines[i].split()[1]
            version = branch_version(before['value'], url, keys['branch'], commit)
            text = text.replace(a['line'], a['line'].replace(f"{a['var']}={a['value']}", f"{a['var']}={version}", 1))
            text = text.replace(old_commit, commit).replace(old_commit[:12], commit[:12])
            continue

        if a['value'] == before['value']:
            continue
        raw, value = a['value'], normalize(before['value'], a['value'])
        pairs = variants(before['value'], value, keys)

        if keys['datasource'] == 'pypi' or keys.get('special'):
            indexes = indexes or list(range(len(lines)))
        elif indexes is None:
            if keys['datasource'].endswith('-tags') and any(line.startswith('git ') for line in old_lines):
                indexes = [next(k for k, line in enumerate(old_lines) if line.startswith('git '))]
            else:
                indexes = [k for k, line in enumerate(old_lines) if not line.startswith('git ') and
                           replace_version(line.split()[0], pairs, url=True) != line.split()[0]]
        if done(indexes):
            continue

        if value != raw:
            text = text.replace(a['line'], a['line'].replace(f"{a['var']}={raw}", f"{a['var']}={value}", 1))
            a['value'] = value

        if keys['datasource'] == 'pypi':
            text, names = update_wheels(text, lines, keys, before['value'], value, indexes)
            number = lambda v: re.sub('^[A-Za-z]+', '', v)
            all_names += names
            all_pairs += pairs + [(number(before['value']), number(value))]
            continue
        if keys.get('special') == 'python-build-standalone':
            text = update_python(text, lines, before['value'], value)
            continue

        names = []
        for i in indexes:
            parts = lines[i].split()
            if parts[0] == 'git':
                commit = tag_commit(parts[1], tag_of(keys, raw))
                text = text.replace(parts[2], commit).replace(parts[2][:12], commit[:12])
                lines[i] = ' '.join([parts[0], parts[1], commit] + parts[3:])
            else:
                url = replace_version(parts[0], pairs, url=True)
                name = replace_version(parts[2], pairs, url=True) if len(parts) > 2 else None
                new_line = ' '.join([url, sha256(url)] + ([name] if name else []))
                names.append((file_name(lines[i]), file_name(new_line)))
                text = text.replace(lines[i], new_line)
                lines[i] = new_line

        all_names += names
        all_pairs += pairs

    text = replace_in_functions(text, all_names, all_pairs)
    m, git = revision(text)
    if m and git[2] not in old_text:
        text = text.replace(m.group(0), f'revision={commit_count(git[1], git[2])}', 1)
    path.write_text(text)


# the files of the same platforms in the new version, with the checksums of PyPI, and the pairs of old and new names
def update_wheels(text, lines, keys, old, new, indexes):
    number = lambda v: re.sub('^[A-Za-z]+', '', v)
    files = json_of(f"https://pypi.org/pypi/{keys['depName']}/{number(new)}/json")['urls']
    names = []
    for i in indexes or range(len(lines)):
        parts = lines[i].split()
        name = file_name(lines[i])
        prefix, suffix = name.split(f'-{number(old)}', 1)
        wanted = f'{prefix}-{number(new)}{suffix}'.lower()
        file = next((f for f in files if f['filename'].lower() == wanted), None)
        if not file:
            raise SystemExit(f'{keys["depName"]} {new} has no file {wanted}')
        names.append((name, file['filename']))
        text = text.replace(lines[i], ' '.join([file['url'], file['digests']['sha256']] + parts[2:]))
    return text, names


# the newest Python of the same minor version in the new release of python-build-standalone
def update_python(text, lines, old, new):
    release = json_of(f'https://api.github.com/repos/astral-sh/python-build-standalone/releases/tags/{new}')
    sums = {}
    sums_asset = next(a for a in release['assets'] if a['name'] == 'SHA256SUMS')
    with urllib.request.urlopen(sums_asset['browser_download_url'], timeout=120) as r:
        for line in r.read().decode().splitlines():
            digest, name = line.split()
            sums[name] = digest
    version = re.search(r'^version=(\S+)', text, re.M).group(1)
    minor = '.'.join(version.split('.')[:2])
    for line in lines:
        url, digest, *rest = line.split()
        name = urllib.request.unquote(url.rsplit('/', 1)[1])
        pattern = re.escape(name).replace(re.escape(version), re.escape(minor) + r'\.\d+').replace(old, new)
        candidates = sorted((a for a in release['assets'] if re.fullmatch(pattern, a['name'])),
                            key=lambda a: [int(x) for x in re.search(re.escape(minor) + r'\.(\d+)', a['name']).groups()])
        if not candidates:
            raise SystemExit(f'python-build-standalone {new} has no asset like {name}')
        asset = candidates[-1]
        new_version = re.search(re.escape(minor) + r'\.\d+', asset['name']).group(0)
        # the file name of the third field, with the new version
        rest = [replace_version(r, [(version, new_version)]) for r in rest]
        text = text.replace(line, ' '.join([asset['browser_download_url'], sums[asset['name']]] + rest))
    text = re.sub(r'^version=\S+', f'version={new_version}', text, count=1, flags=re.M)
    return replace_version(text, [(version, new_version), (old, new)]) if version != new_version else text


def check(online):
    errors = []
    for recipe in sorted((B / 'components').glob('*.sh')) + [B.parent / 'Source/appimage-tools.sh']:
        text = recipe.read_text()
        lines = source_lines(text)
        anns = annotations(text)
        if lines and not anns:
            errors.append(f'{recipe.name}: no "# renovate:" annotation')
            continue
        covered = set()
        for a in anns:
            keys = a['keys']
            if 'none' in keys:
                covered.update(range(len(lines)))
                continue
            if keys.get('datasource') is None or keys.get('depName') is None:
                errors.append(f"{recipe.name}: {a['var']} lacks datasource or depName")
                continue
            if 'extractVersion' in keys and 'tag' not in keys and not TAG_PREFIX.fullmatch(keys['extractVersion']):
                errors.append(f"{recipe.name}: extractVersion of {a['var']} needs the form ^<prefix>(?<version>...)$ "
                              "without groups in the version")
            if 'source' in keys:
                indexes = [int(i) - 1 for i in keys['source'].split(',')]
            elif keys['datasource'] == 'git-refs' or keys['datasource'].endswith('-tags') and \
                    any(line.startswith('git ') for line in lines):
                indexes = [next((k for k, line in enumerate(lines) if line.startswith('git ')), 0)]
            else:
                indexes = [k for k, line in enumerate(lines) if not line.startswith('git ') and
                           a['value'] in line.split()[0]]
            if not indexes or any(i >= len(lines) for i in indexes):
                errors.append(f"{recipe.name}: {a['var']} matches no source")
                continue
            covered.update(indexes)
            for i in indexes:
                parts = lines[i].split()
                if keys['datasource'] == 'git-refs' and parts[2] != keys.get('digest'):
                    errors.append(f"{recipe.name}: the digest of {a['var']} isn't the commit of source {i + 1}")
                if online and keys['datasource'].endswith('-tags') and parts[0] == 'git':
                    commit = tag_commit(parts[1], tag_of(keys, a['value']))
                    if commit != parts[2]:
                        errors.append(f"{recipe.name}: source {i + 1} isn't the commit of tag {tag_of(keys, a['value'])}")
        for i in set(range(len(lines))) - covered:
            errors.append(f'{recipe.name}: source {i + 1} has no annotation')
        m, git = revision(text)
        if online and m and int(m.group(1)) != commit_count(git[1], git[2]):
            errors.append(f"{recipe.name}: revision isn't the commit count of {git[2]}")
    print('\n'.join(errors) or 'all recipes have annotations')
    return 1 if errors else 0


def main():
    if len(sys.argv) == 3 and sys.argv[1] == 'post-upgrade':
        post_upgrade(sys.argv[2])
        return 0
    if len(sys.argv) in (2, 3) and sys.argv[1] == 'check':
        return check('--online' in sys.argv)
    print(__doc__, file=sys.stderr)
    return 2


if __name__ == '__main__':
    sys.exit(main())
