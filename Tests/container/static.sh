#!/bin/bash
# Checks the bundled files without media: missing libraries, undefined symbols, plugins that don't load or lack the
# functions StaxRip calls, and executables that don't start
SUITE=static
. /t/common.sh
: > /work/results/static.tsv

echo "-- libraries"
missing=$(find $A -type f \( -name '*.so*' -o -perm -u+x \) | while read -r f; do
    head -c 4 "$f" 2> /dev/null | grep -q ELF || continue
    ldd "$f" 2> /dev/null | grep 'not found' | awk -v f="${f#$A/}" '{print f "\t" $1}'
done)
while IFS=$'\t' read -r file lib; do
    [ -n "$file" ] && result fail "libraries $file" "$lib not found"
done <<< "$missing"
[ -z "$missing" ] && result pass "libraries"

echo "-- symbols"
# the AviSynth+ and Python APIs come from the host process
undefined=$(find $A/Plugins $A/lib $A/Encoders $A/FrameServer -type f -name '*.so*' | while read -r f; do
    head -c 4 "$f" | grep -q ELF || continue
    ldd -r "$f" 2>&1 | grep 'undefined symbol' | grep -v 'undefined symbol: \(avs_\|Py\|_Py\)' |
        awk -v f="${f#$A/}" '{print f "\t" $3}'
done | sort -u)
while IFS=$'\t' read -r file sym; do
    [ -n "$file" ] && result fail "symbols $file" "undefined symbol $sym"
done <<< "$undefined"
[ -z "$undefined" ] && result pass "symbols"

echo "-- AviSynth+ plugins"
mkdir -p /tmp/static
$PYTHON -I - /work/inventory.json /tmp/static << 'EOF'
import json, sys
inv = json.load(open(sys.argv[1]))
with open(sys.argv[2] + '/avs.tsv', 'w') as f:
    for p in inv['packages']:
        if p['avs'] and p['path'] and p['path'].startswith('/tmp/app/') and p['path'].endswith(('.so', '.avsi')):
            f.write('\t'.join([p['name'], p['path'], ' '.join(p['avs'])]) + '\n')
EOF
while IFS=$'\t' read -r name path functions; do
    load=$([ "${path##*.}" = avsi ] && echo Import || echo LoadPlugin)
    {
        echo "$load(\"$path\")"
        echo 'missing = ""'
        for fn in $functions; do echo "missing = FunctionExists(\"$fn\") ? missing : missing + \" $fn\""; done
        echo 'Assert(missing == "", "missing functions:" + missing)'
        echo 'BlankClip(length=1, pixel_type="YV12")'
    } > /tmp/static/plugin.avs
    check "avs $name" $AVS2YUV /tmp/static/plugin.avs -frames 1 -o /dev/null
done < /tmp/static/avs.tsv

echo "-- VapourSynth plugins and scripts"
$PYTHON -I - /work/inventory.json /work/results/static.tsv << 'EOF'
import importlib, json, os, sys
import vapoursynth as vs
inv = json.load(open(sys.argv[1]))
core = vs.core
out = open(sys.argv[2], 'a')

# like StaxRip, which adds the script folders to the module path
for p in inv['packages']:
    if (p['path'] or '').startswith('/tmp/app/') and p['path'].endswith('.py'):
        sys.path.append(os.path.dirname(p['path']))

def result(status, name, detail=''):
    out.write('\t'.join([status, name, detail.replace('\t', ' ').replace('\n', ' ')[:500]]) + '\n')

for p in inv['packages']:
    path = p['path'] or ''
    if not p['vs'] or not path.startswith('/tmp/app/'):
        continue
    try:
        if path.endswith('.so'):
            try:
                core.std.LoadPlugin(path)
            except vs.Error as e:
                if 'already loaded' not in str(e):
                    raise
            missing = [f for f in p['vs'] if '.' in f and not hasattr(getattr(core, f.split('.')[0], None), f.split('.', 1)[1])]
        elif path.endswith('.py'):
            module = importlib.import_module(os.path.basename(path)[:-3])
            missing = [f for f in p['vs'] if not hasattr(module, f.split('.')[-1])]
        else:
            continue
        if missing:
            result('fail', 'vs ' + p['name'], 'missing functions: ' + ', '.join(missing))
        else:
            result('pass', 'vs ' + p['name'])
    except Exception as e:
        result('fail', 'vs ' + p['name'], '%s: %s' % (type(e).__name__, e))
EOF

echo "-- executables"
find $A -type f -perm -u+x ! -name '*.so*' ! -path '*/FrameServer/Python/lib/*' | sort | while read -r f; do
    head -c 4 "$f" 2> /dev/null | grep -q ELF || continue
    timeout 20 "$f" --help < /dev/null > /tmp/static/start.out 2>&1
    code=$?
    out=$(head -c 20000 /tmp/static/start.out)
    if printf '%s' "$out" | grep -q 'error while loading shared libraries\|symbol lookup error\|undefined symbol'; then
        result fail "start ${f#$A/}" "$(printf '%s' "$out" | grep -m1 'error while loading\|symbol lookup\|undefined symbol')"
    elif [ "$code" -gt 128 ] && [ "$code" -le 160 ]; then
        result fail "start ${f#$A/}" "ended with signal $((code - 128))"
    else
        result pass "start ${f#$A/}"
    fi
done
