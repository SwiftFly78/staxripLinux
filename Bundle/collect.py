#!/usr/bin/env python3
# Copies the shared libraries the ELF files of the Apps tree need into Apps/lib and points their RUNPATH
# there, libraries of the excludelists come from the system, libraries of the fallbacklist from the system
# if it has them, otherwise from Apps/lib/fallback. Libraries of Ubuntu packages are shipped with the
# copyright file of the package, and with its source package if the license is a copyleft one.
# usage: collect.py <Apps folder> <prefix> <sources folder> <download folder> <fallbacklist> <excludelist>...
import hashlib
import os
import re
import shutil
import subprocess
import sys
import urllib.parse
import urllib.request

apps, prefix, sources, downloads = sys.argv[1:5]
libdir = os.path.join(apps, "lib")
fallback_dir = os.path.join(libdir, "fallback")
search = [libdir, f"{prefix}/lib", f"{prefix}/lib/x86_64-linux-gnu", "/lib/x86_64-linux-gnu",
          "/usr/lib/x86_64-linux-gnu", "/lib", "/usr/lib"]
# the library folders of Debian and Ubuntu, Fedora and openSUSE, and Arch Linux, the RUNPATH lists them before
# the fallback folder because it comes before the library cache of the system
system_dirs = ["/usr/lib/x86_64-linux-gnu", "/usr/lib64", "/usr/lib"]


def read_list(path):
    names = set()
    for line in open(path):
        line = line.split("#")[0].strip()
        if line:
            names.add(line.split()[0])
    return names


fallback = read_list(sys.argv[5])
excluded = set().union(*(read_list(path) for path in sys.argv[6:]))


def is_elf(path):
    try:
        with open(path, "rb") as f:
            return f.read(4) == b"\x7fELF"
    except OSError:
        return False


def patchelf(*args):
    return subprocess.run(["patchelf", *args], capture_output=True, text=True, check=True).stdout.strip()


def runpath_dirs(path):
    origin = os.path.dirname(path)
    rp = patchelf("--print-rpath", path)
    return [d.replace("$ORIGIN", origin).replace("${ORIGIN}", origin) for d in rp.split(":") if d]


def find(soname):
    for d in search:
        p = os.path.join(d, soname)
        if os.path.exists(p):
            return p
    return None


def is_copyleft(text):
    dep5 = False
    for paragraph in text.split("\n\n"):
        files = re.search(r"^Files:[ \t]*(.*)$", paragraph, re.M)
        lic = re.search(r"^License:[ \t]*(.*)$", paragraph, re.M)
        if files:
            dep5 = True
            # the packaging, it isn't part of the binaries
            if files.group(1).split() and all(f.startswith("debian/") for f in files.group(1).split()):
                continue
            if lic and re.search(r"GPL|MPL", lic.group(1)):
                return True
    return not dep5 and re.search(r"\bL?GPL|General Public License|\bMPL\b", text) is not None


def download(url, target, sha256=None):
    if not os.path.exists(target):
        with urllib.request.urlopen(url, timeout=60) as r, open(target + ".tmp", "wb") as f:
            shutil.copyfileobj(r, f)
        os.rename(target + ".tmp", target)
    if sha256 and hashlib.sha256(open(target, "rb").read()).hexdigest() != sha256:
        os.remove(target)
        sys.exit(f"checksum mismatch: {url}")


# the files of an Ubuntu source package, listed in its .dsc
def fetch_source_package(src, version):
    base = f"https://launchpad.net/ubuntu/+archive/primary/+sourcefiles/{src}/{urllib.parse.quote(version)}/"
    dsc = f"{src}_{version.split(':', 1)[-1]}.dsc"
    os.makedirs(os.path.join(downloads, "ubuntu"), exist_ok=True)
    os.makedirs(os.path.join(sources, "ubuntu"), exist_ok=True)
    download(base + dsc, os.path.join(downloads, "ubuntu", dsc))
    text = open(os.path.join(downloads, "ubuntu", dsc)).read()
    files = re.findall(r"^ ([0-9a-f]{64}) \d+ (\S+)$", text.split("Checksums-Sha256:")[1], re.M)
    for name in [dsc] + [f for _, f in files]:
        sha = next((h for h, f in files if f == name), None)
        download(base + name, os.path.join(downloads, "ubuntu", name), sha)
        shutil.copy(os.path.join(downloads, "ubuntu", name), os.path.join(sources, "ubuntu", name))


def package_of(soname):
    owner = subprocess.run(["dpkg", "-S", "*/" + soname], capture_output=True, text=True).stdout
    if not owner:
        sys.exit(f"no Ubuntu package contains {soname}")
    return owner.split(":")[0]


def ship_package(pkg):
    version, src, src_version = subprocess.run(
        ["dpkg-query", "-W", "-f", "${Version}\t${source:Package}\t${source:Version}", pkg],
        capture_output=True, text=True, check=True).stdout.split("\t")
    text = open(f"/usr/share/doc/{pkg}/copyright", errors="replace").read()
    os.makedirs(os.path.join(apps, "Licenses", pkg), exist_ok=True)
    with open(os.path.join(apps, "Licenses", pkg, "copyright"), "w") as f:
        f.write(text)
    with open(os.path.join(apps, "Licenses", "README.txt"), "a") as f:
        f.write(f"{pkg} {version} (Ubuntu 22.04)\n")
    if is_copyleft(text):
        fetch_source_package(src, src_version)


queue = []
system_libs = []
for root, dirs, files in os.walk(apps):
    for f in files:
        p = os.path.join(root, f)
        if not os.path.islink(p) and is_elf(p):
            queue.append(p)

missing = {}
done = set()
while queue:
    path = queue.pop()
    if path in done:
        continue
    done.add(path)

    try:
        needed = patchelf("--print-needed", path).split()
    except subprocess.CalledProcessError:
        continue

    own = runpath_dirs(path) + [os.path.dirname(path)]
    uses_lib = False
    uses_fallback = False

    for soname in needed:
        if soname in fallback:
            target = os.path.join(fallback_dir, soname)
            if not os.path.exists(target):
                source = find(soname)
                if source is None:
                    missing.setdefault(soname, []).append(os.path.relpath(path, apps))
                    continue
                os.makedirs(fallback_dir, exist_ok=True)
                shutil.copy2(os.path.realpath(source), target)
                os.chmod(target, 0o755)
                queue.append(target)
                system_libs.append(soname)
            uses_fallback = True
            continue
        if soname in excluded:
            continue
        # a path instead of a name, like $ORIGIN/../lib/libpython3.13.so.1.0
        if "/" in soname:
            if os.path.exists(soname.replace("$ORIGIN", os.path.dirname(path))):
                continue
            missing.setdefault(soname, []).append(os.path.relpath(path, apps))
            continue
        if any(os.path.exists(os.path.join(d, soname)) and not os.path.join(d, soname).startswith(libdir)
               for d in own if d.startswith(apps)):
            continue

        target = os.path.join(libdir, soname)
        if not os.path.exists(target):
            source = find(soname)
            if source is None:
                missing.setdefault(soname, []).append(os.path.relpath(path, apps))
                continue
            shutil.copy2(os.path.realpath(source), target)
            os.chmod(target, 0o755)
            queue.append(target)
            if not source.startswith(prefix + "/"):
                system_libs.append(soname)
        uses_lib = True

    if uses_lib or uses_fallback:
        def origin(d):
            rel = os.path.relpath(d, os.path.dirname(path))
            return "$ORIGIN" if rel == "." else f"$ORIGIN/{rel}"

        rpath = [d for d in patchelf("--print-rpath", path).split(":") if d and not d.startswith("/")]
        if uses_lib and origin(libdir) not in rpath:
            rpath.append(origin(libdir))
        if uses_fallback:
            rpath = [d for d in rpath if d != origin(fallback_dir)] + system_dirs + [origin(fallback_dir)]
        patchelf("--set-rpath", ":".join(rpath), path)

if missing:
    for soname, users in sorted(missing.items()):
        print(f"unresolved library {soname}, needed by {', '.join(sorted(users)[:3])}", file=sys.stderr)
    sys.exit(1)

for pkg in sorted({package_of(soname) for soname in system_libs}):
    ship_package(pkg)

count = sum(1 for f in os.listdir(libdir) if os.path.isfile(os.path.join(libdir, f)))
fallbacks = len(os.listdir(fallback_dir)) if os.path.isdir(fallback_dir) else 0
print(f"collected {count} libraries and {fallbacks} fallbacks")
