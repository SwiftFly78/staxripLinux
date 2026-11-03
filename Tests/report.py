"""Summarizes the results of the test suites: report.py <results folder>

Failures listed in known-issues.txt count as expected. An issue with an error only matches failures whose detail
matches the error too. An issue whose tests ran without one of them failing with it is reported, so the list stays
current. Writes Markdown to stdout and to $GITHUB_STEP_SUMMARY, exits with 1 if anything else failed.
"""
import os
import re
import sys
from pathlib import Path


def known_issues():
    issues = []
    for line in (Path(__file__).parent / 'known-issues.txt').read_text().splitlines():
        if line.strip() and not line.startswith('#'):
            suite, test, reason, detail = (part.strip() for part in (line.split(' | ', 3) + [''])[:4])
            issues.append((suite, re.compile(test), reason, re.compile(detail) if detail else None))
    return issues


def main():
    issues = known_issues()
    rows = {'fail': [], 'xfail': [], 'skip': []}
    counts = {}
    named, seen = set(), set()

    for file in sorted(Path(sys.argv[1]).glob('*.tsv')):
        suite = re.sub(r'-\d+-of-\d+$', '', file.stem)
        for line in file.read_text().splitlines():
            status, test, detail = (line.split('\t') + ['', ''])[:3]
            matches = [i for i, issue in enumerate(issues) if issue[0] == suite and issue[1].fullmatch(test)]
            named.update(matches)
            issue = next((i for i in matches if issues[i][3] is None or issues[i][3].search(detail)), None)
            if status == 'fail' and issue is not None:
                seen.add(issue)
                status, detail = 'xfail', issues[issue][2]
            counts.setdefault(suite, {}).setdefault(status, 0)
            counts[suite][status] += 1
            if status in rows:
                rows[status].append((suite, test, detail))

    lines = ['## Tests', '', '| Suite | Passed | Failed | Known issues | Skipped |', '|---|---|---|---|---|']
    for suite, c in counts.items():
        lines.append(f"| {suite} | {c.get('pass', 0)} | {c.get('fail', 0)} | {c.get('xfail', 0)} | {c.get('skip', 0)} |")

    gone = [issues[i] for i in sorted(named - seen)]
    if rows['fail']:
        lines += ['', '### Failures', '']
        lines += [f'- {suite}: `{test}`' + (f': {detail}' if detail else '') for suite, test, detail in rows['fail']]
    if gone:
        lines += ['', '### Known issues that don\'t occur anymore, remove them from known-issues.txt', '']
        lines += [f'- {suite}: `{test.pattern}`: {reason}' for suite, test, reason, error in gone]
    for status, title in (('xfail', 'Known issues'), ('skip', 'Skipped')):
        if rows[status]:
            lines += ['', f'### {title}', '']
            lines += [f'- {suite}: `{test}`' + (f': {detail}' if detail else '') for suite, test, detail in rows[status]]

    text = '\n'.join(lines) + '\n'
    print(text)
    if os.environ.get('GITHUB_STEP_SUMMARY'):
        with open(os.environ['GITHUB_STEP_SUMMARY'], 'a') as f:
            f.write(text)

    if not counts:
        print('no results', file=sys.stderr)
        return 1
    return 1 if rows['fail'] else 0


if __name__ == '__main__':
    sys.exit(main())
