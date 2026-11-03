"""Skips the frame tests that fail because the bundle lacks a plugin, like the Windows only ones:
unbundled.py <inventory.json> <results> <index.tsv>"""
import json
import re
import sys
from pathlib import Path

inventory, results, index = sys.argv[1:4]
packages = json.load(open(inventory, encoding='utf-8-sig'))['packages']
scripts = {line.split('\t')[1].rstrip('\n'): line.split('\t')[0] for line in open(index) if line.split('\t')[0]}
avs, vs = {}, {}

for p in packages:
    for f in p['avs']:
        avs.setdefault(f.lower(), []).append(p)
    for f in p['vs']:
        vs.setdefault(f.split('.')[0], []).append(p)

rows = []

for line in open(results):
    status, name, detail = (line.rstrip('\n').split('\t') + [''])[:3]
    err = Path(scripts.get(name, '') + '.err')
    text = err.read_text(errors='replace') if name in scripts and err.exists() else detail
    owners = []

    if m := re.search(r"There is no function named '(\w+)'", text):
        owners = avs.get(m.group(1).lower(), [])
    elif m := re.search(r'No attribute with the name (\w+) exists|no attribute or namespace named (\w+)', text):
        owners = vs.get(m.group(1) or m.group(2), [])

    # a bundled script like dither.avsi names the functions of its plugin, but doesn't have them
    plugins = [p for p in owners if not (p['path'] or '').endswith(('.avsi', '.avs', '.py'))]

    if status == 'fail' and plugins and not any(p['path'] for p in plugins):
        status, detail = 'skip', f"{m.group(1) or m.group(2)}: {', '.join(sorted({p['name'] for p in plugins}))} isn't bundled"

    rows.append('\t'.join([status, name, detail]))

Path(results).write_text('\n'.join(rows) + '\n')
