#!/usr/bin/env python3
"""Update talkgroup priorities in the CSV using a scored scheme.

This script reads two files:
 - the talkgroups CSV (e.g. `config/danecom-talkgroups.csv`)
 - a call summary TSV (e.g. `config/callsummary.tsv`) which provides AvgDurationSeconds per talkgroup

It computes a score combining category weight, subtype weight, and avg-duration buckets
and maps the score to a final Priority (1..5) where 1 is highest.

The original CSV is backed up with a .bak suffix by default.
"""
import csv
import sys
from pathlib import Path


def load_callsummary(callsummary_path: Path):
    """Return a dict mapping TalkgroupDecimal (string) -> avg duration seconds (int).
    If AvgDurationSeconds is missing or malformed, treat as 0.
    """
    mapping = {}
    with callsummary_path.open('r', newline='') as fh:
        reader = csv.DictReader(fh, delimiter='\t')
        for row in reader:
            key = (row.get('TalkgroupDecimal') or '').strip()
            if not key:
                continue
            try:
                avg = int(float(row.get('AvgDurationSeconds') or 0))
            except Exception:
                avg = 0
            mapping[key] = avg
    return mapping


def category_weight(cat: str):
    cat = (cat or '').lower()
    if 'emergency' in cat or 'multi' in cat or 'announcement' in cat:
        return 50
    if cat == 'law' or 'law' in cat:
        return 40
    if cat in ('medical', 'hospital') or 'medical' in cat:
        return 35
    if cat == 'fire' or 'fire' in cat:
        return 30
    if 'school' in cat or 'schools' in cat:
        return 20
    if 'public works' in cat or 'dpw' in cat:
        return 10
    return 0


def subtype_weight(alpha: str, tag: str, desc: str, cat: str):
    a = (alpha or '').lower()
    t = (tag or '').lower()
    d = (desc or '').lower()
    c = (cat or '').lower()

    # Law subtypes
    if 'dispatch' in t or 'dispatch' in d or 'dispatch' in a:
        return 20
    if 'tac' in t or 'tactical' in t or 'tactical' in d:
        return 15
    # general law talk
    if 'law' in t or 'law' in a or c == 'law':
        return 10

    # Medical/hospital/ems
    if 'hospital' in d or 'hospital' in t or 'uw' in t or 'va' in t:
        return 15
    if 'ems' in t or 'ems' in d:
        return 12

    # Fire
    if 'fire' in t or 'fire' in d or c == 'fire':
        # dispatch-like gets higher
        if 'dispatch' in t or 'dispatch' in d:
            return 12
        return 8

    # Multi/announcement
    if 'announcement' in d or 'announcement' in t or 'multi' in c:
        return 12

    # Schools / DPW
    if 'school' in d or 'school' in t or 'schools' in c:
        return 6
    if 'dpw' in t or 'public works' in c or 'public works' in t:
        return 4

    return 0


def duration_score(avg_seconds: int):
    if avg_seconds >= 20:
        return 20
    if avg_seconds >= 15:
        return 15
    if avg_seconds >= 8:
        return 10
    if avg_seconds >= 4:
        return 5
    return 0


def map_score_to_priority(total_score: int):
    if total_score >= 60:
        return '1'
    if total_score >= 45:
        return '2'
    if total_score >= 30:
        return '3'
    if total_score >= 15:
        return '4'
    return '5'


def process(input_path: Path, callsummary: Path, backup: bool = True, sample: int = 8):
    out_path = input_path
    bak_path = input_path.with_suffix(input_path.suffix + '.bak')
    if backup:
        bak_path.write_bytes(input_path.read_bytes())

    durations = load_callsummary(callsummary)

    with input_path.open('r', newline='') as fh:
        reader = csv.DictReader(fh)
        rows = list(reader)
        fieldnames = reader.fieldnames

    if not fieldnames or 'Priority' not in fieldnames:
        print('CSV missing Priority column or headers; aborting.', file=sys.stderr)
        sys.exit(2)

    updated = []
    for r in rows:
        key = (r.get('Decimal') or '').strip()
        avg = durations.get(key, 0)
        cat = r.get('Category') or ''
        alpha = r.get('Alpha Tag') or ''
        tag = r.get('Tag') or ''
        desc = r.get('Description') or ''

        total = category_weight(cat) + subtype_weight(alpha, tag, desc, cat) + duration_score(avg)
        newp = map_score_to_priority(total)
        if r.get('Priority') != newp:
            updated.append((key, r.get('Alpha Tag') or '', r.get('Tag') or '', r.get('Priority'), newp, avg, total))
        r['Priority'] = newp

    with out_path.open('w', newline='') as fh:
        writer = csv.DictWriter(fh, fieldnames=fieldnames)
        writer.writeheader()
        writer.writerows(rows)

    print(f'Updated priorities written to {out_path} (backup: {bak_path if backup else "none"})')
    print(f'{len(updated)} rows changed. Showing up to {sample} changes:')
    for item in updated[:sample]:
        print('\t'.join(map(str, item)))


if __name__ == '__main__':
    import argparse
    p = argparse.ArgumentParser()
    p.add_argument('--input', '-i', required=True, help='Path to talkgroup CSV')
    p.add_argument('--callsummary', '-c', required=True, help='Path to callsummary TSV')
    p.add_argument('--no-backup', dest='backup', action='store_false')
    p.add_argument('--sample', type=int, default=8, help='How many changed rows to show')
    args = p.parse_args()
    process(Path(args.input), Path(args.callsummary), backup=args.backup, sample=args.sample)
