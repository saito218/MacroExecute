const { test, expect } = require('@playwright/test');
const fs = require('node:fs/promises');
const path = require('node:path');
const { probe, step, macro, K, executable, runFile } = require('./probe');

const invalid = [
  ['orphan ELSE', [step(K.otherwise)]],
  ['orphan END IF', [step(K.endIf)]],
  ['unclosed IF', [step(K.ifWindow, 'true:x')]],
  ['crossed blocks', [step(K.repeat), step(K.ifWindow), step(K.endRepeat), step(K.endIf)]],
  ['duplicate ELSE', [step(K.ifWindow), step(K.otherwise), step(K.otherwise), step(K.endIf)]],
  ['BREAK outside loop', [step(K.break)]],
  ['unknown label', [step(K.jump, 'missing')]],
  ['duplicate labels', [step(K.label, 'x'), step(K.label, 'x')]],
  ['invalid action', [step(999)]],
  ['negative repeat', [step(K.repeat, '', { Count: -1 }), step(K.endRepeat)]],
  ['invalid polling', [step(K.note, '', { PollMs: 0 })]],
  ['invalid tolerance', [step(K.ifImage, '', { Tolerance: 256 }), step(K.endIf)]],
];
for (const [name, steps] of invalid) {
  test(`malformed workflow rejected before actions: ${name}`, async ({}, info) => {
    const r = await probe(info, { Macro: macro([step(K.message, 'must-not-run'), ...steps]) });
    expect(r.ErrorType).toBe('InvalidDataException'); expect(r.Output).toEqual([]); expect(r.Dispatches).toBe(0);
  });
}
for (const CancelAt of [1, 51, 250]) {
  test(`cancel a 1000-action workflow at dispatch ${CancelAt}`, async ({}, info) => {
    const r = await probe(info, { Macro: macro(Array.from({ length: 1000 }, (_, i) => step(K.message, `task-${i}`))), CancelAt });
    expect(r.Cancelled).toBe(true); expect(r.Error).toBeNull(); expect(r.Dispatches).toBe(CancelAt);
    expect(r.Output).toHaveLength(CancelAt - 1); expect(r.Released).toBe(true); expect(r.Scope).toBe(0);
  });
}
test('failure halfway through nested loop releases inputs and clears scope', async ({}, info) => {
  const r = await probe(info, { FailAt: 150, Macro: macro([step(K.repeat, '', { Count: 1000 }), step(K.ifWindow, 'true:x'), step(K.message, 'body'), step(K.endIf), step(K.endRepeat)]) });
  expect(r.Error).toBe('Injected target failure'); expect(r.Output).toHaveLength(149); expect(r.Released).toBe(true); expect(r.Scope).toBe(0);
});
for (const forever of ['whole workflow', 'repeat block', 'label jump']) {
  test(`stop intentional infinite ${forever} without desktop input`, async ({}, info) => {
    let Steps = [step(K.message, 'body')], Repetitions = 0;
    if (forever === 'repeat block') { Steps = [step(K.repeat, '', { Count: 0 }), ...Steps, step(K.endRepeat)]; Repetitions = 1; }
    if (forever === 'label jump') { Steps = [step(K.label, 'again'), step(K.jump, 'again')]; Repetitions = 1; }
    const r = await probe(info, { Macro: macro(Steps), Repetitions, CancelAt: 128 });
    expect(r.Cancelled).toBe(true); expect(r.Error).toBeNull(); expect(r.Released).toBe(true); expect(r.Dispatches).toBe(128);
  });
}
test('BREAK exits only the inner forever repeat', async ({}, info) => {
  const r = await probe(info, { Macro: macro([step(K.repeat, '', { Count: 3 }), step(K.repeat, '', { Count: 0 }), step(K.message, 'inner'), step(K.break), step(K.endRepeat), step(K.message, 'outer'), step(K.endRepeat)]) });
  expect(r.Error).toBeNull(); expect(r.Output).toEqual(['inner', 'outer', 'inner', 'outer', 'inner', 'outer']);
});
test('jump cannot enter a nested inactive IF branch', async ({}, info) => {
  const r = await probe(info, { Macro: macro([step(K.jump, 'inside'), step(K.ifWindow, 'true:x'), step(K.label, 'inside'), step(K.message, 'must-not-run'), step(K.endIf)]) });
  expect(r.Error).toContain('inactive IF'); expect(r.Output).toEqual([]); expect(r.Released).toBe(true);
});
test('150-step include expands independently twice, preserving label scope', async ({}, info) => {
  const directory = info.outputPath('includes'); await fs.mkdir(directory, { recursive: true });
  const child = path.join(directory, 'child.mmacro.json');
  const values = Array.from({ length: 150 }, (_, i) => `child-${i}`);
  await fs.writeFile(child, JSON.stringify(macro([step(K.jump, 'start'), step(K.message, 'must-not-run'), step(K.label, 'start'), ...values.map(v => step(K.message, v))])));
  const r = await probe(info, { Macro: macro([step(K.include, '', { Target: child }), step(K.include, '', { Target: child })]) });
  expect(r.Error).toBeNull(); expect(r.Output).toEqual([...values, ...values]);
});
test('include cycle fails before execution', async ({}, info) => {
  const file = info.outputPath('cycle.mmacro.json');
  await fs.writeFile(file, JSON.stringify(macro([step(K.include, '', { Target: file })])));
  const r = await probe(info, { Macro: macro([step(K.include, '', { Target: file })]) });
  expect(r.Error).toContain('cycle'); expect(r.Output).toEqual([]);
});
test('native suite passes with all detailed assertions retained', async ({}, info) => {
  // Includes hidden rendering in all palettes and compact guide layouts.
  test.setTimeout(180000);
  const report = info.outputPath('native-tests.txt');
  await runFile(executable, ['--self-test', report], { windowsHide: true, timeout: 165000, maxBuffer: 4 * 1024 * 1024 });
  const text = await fs.readFile(report, 'utf8');
  expect(text).toContain('0 failed.'); expect(text).not.toContain('\nFAIL ');
  expect((text.match(/^PASS /gm) || []).length).toBeGreaterThanOrEqual(268);
  await info.attach('native-results', { path: report, contentType: 'text/plain' });
});
