const { test, expect } = require('@playwright/test');
const { probe, step, macro, K } = require('./probe');

for (const count of [0, 1, 100, 101, 250, 1000]) {
  test(`${count} actions execute in exact order without dropped or duplicate tasks`, async ({}, info) => {
    const values = Array.from({ length: count }, (_, i) => `task-${i}`);
    const r = await probe(info, { Macro: macro(values.map(v => step(K.message, v))) });
    expect(r.Error).toBeNull(); expect(r.Output).toEqual(values);
    expect(r.Cancelled).toBe(false); expect(r.Steps).toBe(count);
    if (count) expect(r.Released).toBe(true);
  });
}
for (const count of [1000, 10000, 100000, 100001]) {
  test(`validation boundary: ${count} steps`, async ({}, info) => {
    const r = await probe(info, { Mode: 'validate', Macro: macro(Array.from({ length: count }, () => step(K.note))) });
    if (count > 100000) { expect(r.ErrorType).toBe('InvalidDataException'); expect(r.Error).toContain('Too many'); }
    else { expect(r.Error).toBeNull(); expect(r.Steps).toBe(count); }
  });
}
test('500 mixed tasks survive save/load exactly', async ({}, info) => {
  const types = [K.text, K.click, K.keyDown, K.keyPress, K.keyUp];
  const steps = Array.from({ length: 500 }, (_, i) => step(types[i % types.length], `日本語 ${i}\n"quoted"`, { X: i - 250, Y: i, Enabled: i % 7 !== 0 }));
  const r = await probe(info, { Mode: 'roundtrip', Macro: macro(steps) });
  expect(r.Error).toBeNull(); const restored = JSON.parse(r.Output[0]);
  expect(restored.Steps).toHaveLength(500);
  restored.Steps.forEach((s, i) => expect(s).toMatchObject(steps[i]));
});
for (const count of [100, 500, 1000]) {
  test(`Aftershock hidden editor finds final task in ${count}-row workflow`, async ({}, info) => {
    const values = Array.from({ length: count }, (_, i) => `task-${i}`);
    const r = await probe(info, { Mode: 'ui', Macro: macro(values.map(v => step(K.message, v))), Select: count - 1, Find: `task-${count - 1}` });
    expect(r.Error).toBeNull(); expect(r.Hidden).toBe(true); expect(r.Steps).toBe(count); expect(r.StepNumbersFit).toBe(true);
    expect(r.Selected).toBe(count - 1); expect(r.Matches).toEqual([count - 1]); expect(r.Output).toEqual(values);
  });
}
test('move task 0 to the end of a 500-row list without losing steps', async ({}, info) => {
  const values = Array.from({ length: 500 }, (_, i) => `task-${i}`);
  const r = await probe(info, { Mode: 'ui', Macro: macro(values.map(v => step(K.message, v))), MoveStart: 0, MoveGap: 500 });
  expect(r.Error).toBeNull(); expect(r.Hidden).toBe(true); expect(r.Moved).toBe(true);
  expect(r.Output).toEqual([...values.slice(1), values[0]]);
});
test('simulated input/script actions never invoke real OS integrations', async ({}, info) => {
  const steps = [K.click, K.keyPress, K.text, K.launch, K.script].map((k, i) => step(k, `simulated-${i}`, { Target: 'does-not-exist.exe' }));
  const r = await probe(info, { Macro: macro(steps) });
  expect(r.Error).toBeNull(); expect(r.Output).toEqual(steps.map(s => s.Text));
});
