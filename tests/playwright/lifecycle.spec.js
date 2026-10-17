const { test, expect } = require('@playwright/test');
const fs = require('node:fs/promises');
const path = require('node:path');
const { probe, step, macro, K, executable, runFile } = require('./probe');

test('Pause action resumes, executes the tail once, and clears paused state', async ({}, info) => {
  const r = await probe(info, { ResumeAfterMs: 60, Macro: macro([step(K.message, 'before'), step(K.pause), step(K.message, 'after')]) });
  expect(r.Error).toBeNull(); expect(r.Cancelled).toBe(false); expect(r.Output).toEqual(['before', 'after']);
  expect(r.Pauses).toBe(1); expect(r.PausedAtEnd).toBe(false); expect(r.Released).toBe(true);
});
test('Stop while paused releases inputs without executing the tail', async ({}, info) => {
  const r = await probe(info, { DeadlineMs: 100, Macro: macro([step(K.pause), step(K.message, 'must-not-run')]) });
  expect(r.Error).toBeNull(); expect(r.Cancelled).toBe(true); expect(r.Output).toEqual([]);
  expect(r.Pauses).toBe(1); expect(r.PausedAtEnd).toBe(false); expect(r.Released).toBe(true);
});
test('Stop during a one-day delay returns promptly and releases inputs', async ({}, info) => {
  const r = await probe(info, { DeadlineMs: 100, Macro: macro([step(K.delay, '', { DelayMs: 86400000 }), step(K.message, 'must-not-run')]) });
  expect(r.Error).toBeNull(); expect(r.Cancelled).toBe(true); expect(r.Output).toEqual([]); expect(r.Released).toBe(true);
  expect(r.ElapsedMs).toBeLessThan(5000);
});
for (const speed of [0, 0.09, 20.01, -1]) {
  test(`invalid playback speed ${speed} fails before actions`, async ({}, info) => {
    const r = await probe(info, { Speed: speed, Macro: macro([step(K.message, 'must-not-run')]) });
    expect(r.ErrorType).toBe('ArgumentOutOfRangeException'); expect(r.Output).toEqual([]);
  });
}
for (const count of [-1, 1000001]) {
  test(`invalid whole-macro count ${count} fails before actions`, async ({}, info) => {
    const r = await probe(info, { Repetitions: count, Macro: macro([step(K.message, 'must-not-run')]) });
    expect(r.ErrorType).toBe('ArgumentOutOfRangeException'); expect(r.Output).toEqual([]);
  });
}
for (const depth of [16, 17]) {
  test(`include nesting ${depth} tests the supported depth boundary`, async ({}, info) => {
    const directory = info.outputPath('chain'); await fs.mkdir(directory, { recursive: true });
    for (let i = depth; i >= 0; i--) {
      const steps = i === depth ? [step(K.message, 'leaf')] : [step(K.include, '', { Target: `${i + 1}.json` })];
      await fs.writeFile(path.join(directory, `${i}.json`), JSON.stringify(macro(steps)));
    }
    // Root is level zero; children 1..depth are actual include expansions.
    const r = await probe(info, { Macro: macro([step(K.include, '', { Target: path.join(directory, '1.json') })]) });
    if (depth === 16) { expect(r.Error).toBeNull(); expect(r.Output).toEqual(['leaf']); }
    else { expect(r.Error).toContain('16 levels'); expect(r.Output).toEqual([]); }
  });
}
test('1000-step portable export and verification use the actual executable', async ({}, info) => {
  const source = info.outputPath('large.mmacro.json'), player = info.outputPath('large-player.exe');
  await fs.writeFile(source, JSON.stringify(macro(Array.from({ length: 1000 }, (_, i) => step(K.note, `step-${i}`)))));
  await runFile(executable, ['--export', source, player], { windowsHide: true, timeout: 30000 });
  await runFile(executable, ['--verify-player', player], { windowsHide: true, timeout: 30000 });
  expect((await fs.stat(player)).size).toBeGreaterThan((await fs.stat(executable)).size);
  // Exported player is inspected, never executed.
});
test('malformed JSON returns structured error without opening a dialog', async ({}, info) => {
  const request = info.outputPath('broken.json'), result = info.outputPath('broken-result.json');
  await fs.writeFile(request, '{"Macro":');
  await runFile(executable, ['--headless-probe', request, result], { windowsHide: true, timeout: 10000 });
  const r = JSON.parse(await fs.readFile(result, 'utf8'));
  expect(r.Error).toBeTruthy(); expect(r.Output).toEqual([]);
});
