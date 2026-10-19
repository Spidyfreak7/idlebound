#!/usr/bin/env python3
"""Reproduce Stage 8B-03B curve and balance arithmetic without Unity.

Writes only with --write-curve. Otherwise validates committed CSV data and prints
training-path estimates. This is not execution of C# or Unity tests.
"""
import argparse
import csv
from fractions import Fraction
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
DATA = ROOT / 'Assets/Foundation/Development/Resources/Idlebound'
ANCHORS = {1: 0, 30: 500000, 60: 2000000, 80: 4500000, 90: 6500000, 99: 10000000}

def round_half_up(value):
    return (2 * value.numerator + value.denominator) // (2 * value.denominator)

def curve():
    result = {}
    for level in range(1, 100):
        if level <= 30:
            value = Fraction(500000 * (level - 1) ** 2, 29 ** 2)
        else:
            a = max(l for l in ANCHORS if l < level)
            b = min(l for l in ANCHORS if l >= level)
            value = Fraction(ANCHORS[a]) + Fraction((ANCHORS[b] - ANCHORS[a]) * (level - a), b - a)
        result[level] = round_half_up(value)
    return result

def rows(name):
    return list(csv.reader(line for line in (DATA / name).read_text().splitlines() if line and not line.startswith('#')))

def model(thresholds, schedule, best):
    xp = 0
    seconds = Fraction(0)
    actions = 0
    while xp < 10000000:
        level = max(l for l, threshold in thresholds.items() if threshold <= xp)
        tier = max(i for i, (required, _, _) in enumerate(schedule) if required <= level) if best else 0
        _, duration, reward = schedule[tier]
        seconds += duration
        xp += reward
        actions += 1
    return float(seconds / 3600), actions

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--write-curve', action='store_true')
    args = parser.parse_args()
    expected = curve()
    assert all(expected[l] == xp for l, xp in ANCHORS.items())
    assert all(expected[l] < expected[l + 1] for l in range(1, 99))
    if args.write_curve:
        (DATA / 'ProductionXpCurve.csv').write_text(
            '# Stage 8B-03B reconstruction; see balance report. level,cumulativeXP\n' +
            ''.join(f'{l},{xp}\n' for l, xp in expected.items()))
    actual = {int(l): int(xp) for l, xp in rows('ProductionXpCurve.csv')}
    assert actual == expected
    resources = rows('ResourceTiers.csv')
    xp_rows = rows('GatheringXp.csv')
    approved_rewards = [56, 89, 124, 159, 197, 236, 277, 318, 362, 407]
    assert len(xp_rows) == 10
    schedule = []
    print('tier,requirement,seconds,XP,XP/hour')
    for index, (row, material) in enumerate(zip(xp_rows, resources), 1):
        tier, duration, reward = row
        assert int(tier) == index and Fraction(duration) == Fraction(material[6])
        assert int(reward) == approved_rewards[index - 1]
        schedule.append((int(material[0]), Fraction(duration), int(reward)))
        print(index, material[0], duration, reward, float(Fraction(reward) * 3600 / Fraction(duration)), sep=',')
    for best, name in [(False, 'Stay at tier 1'), (True, 'Highest unlocked tier')]:
        hours, actions = model(actual, schedule, best)
        print(f'{name}: {hours:.6f}h productive, {actions} completed actions; '
              f'10% downtime: {hours/.9:.6f}h; 25% downtime: {hours/.75:.6f}h')
    print('Starter recipe: XP/work-hour and XP/(gather+work)-hour; no queue/click delays')
    for r in rows('StarterProductionRecipes.csv'):
        gather = Fraction(0)
        for input_row in r[7].split(';'):
            item, count = input_row.split(':')
            resource = next(material for material in resources if item.endswith('-' + material[0]))
            gather += Fraction(resource[6]) * int(count)
        duration = Fraction(r[9]); reward = int(r[10])
        rate = Fraction(56, 5)
        proposed = round_half_up(min(rate * (gather + duration) * Fraction(90, 100), 2 * rate * duration) / 10) * 10
        # Iron/sardine use their own approved tier's gathering XP/second.
        if any(item.split(':')[0].endswith('-10') for item in r[7].split(';')):
            rate = Fraction(89, 1) / Fraction('7.8')
            proposed = round_half_up(min(rate * (gather + duration) * Fraction(90, 100), 2 * rate * duration) / 10) * 10
        assert reward == proposed, (r[0], reward, proposed)
        print(f'{r[0]}: gather {float(gather):g}s + work {duration}s, XP {reward}, '
              f'work {float(reward*3600/duration):.2f}/h, self-supply {float(reward*3600/(gather+duration)):.2f}/h')
    from bisect import bisect_right
    ordered = list(actual.values())
    def level(xp): return bisect_right(ordered, xp)
    definitions = rows('StarterProductionRecipes.csv')
    resource_info = {}
    for i, material in enumerate(resources):
        for family, skill in [('metal', 'mining'), ('wood', 'woodcutting'), ('fish', 'fishing')]:
            resource_info[family + '-' + material[0]] = (skill, int(material[0]), float(Fraction(material[6])), approved_rewards[i])
    for skill in ['smithing', 'crafting', 'cooking']:
        xp = {s: 0 for s in ['mining', 'woodcutting', 'fishing', skill]}
        work = gather_time = 0.0
        jobs = 0
        used = {}
        while xp[skill] < 10000000:
            eligible = []
            for r in definitions:
                if r[2] != skill or int(r[8]) > level(xp[skill]): continue
                inputs = [(resource_info[k], int(n)) for k, n in (field.split(':') for field in r[7].split(';'))]
                if any(required > level(xp[gskill]) for (gskill, required, _, _), _ in inputs): continue
                supply = sum(duration * count for (_, _, duration, _), count in inputs)
                eligible.append((int(r[10]) / (supply + float(r[9])), r, inputs, supply))
            _, r, inputs, supply = max(eligible, key=lambda entry: entry[0])
            for (gskill, _, _, reward), count in inputs: xp[gskill] = min(10000000, xp[gskill] + reward * count)
            xp[skill] = min(10000000, xp[skill] + int(r[10]))
            jobs += 1; used[r[0]] = used.get(r[0], 0) + 1
            work += float(r[9]); gather_time += supply
        total = (work + gather_time) / 3600
        print(f'{skill} self-supplied starter-only optimizer: {total:.6f}h productive, '
              f'{work/3600:.6f}h production + {gather_time/3600:.6f}h gathering, '
              f'{jobs} jobs, 10% downtime {total/.9:.6f}h; choices {used}')
    print('PASS: reconstructed full curve, approved gathering data and starter reward assumptions match.')

if __name__ == '__main__':
    main()
