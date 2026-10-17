const { test, expect } = require('@playwright/test');
const { probe, macro, step, K } = require('./probe');

for (const [name, text, expected] of [
  ['Unicode and supplementary characters', 'é🚀', ['char:233', 'char:55357', 'char:56960']],
  ['all common line endings', 'a\r\nb\rc\n', ['char:97', 'down:13', 'up:13', 'char:98', 'down:13', 'up:13', 'char:99', 'down:13', 'up:13']],
  ['tabs as keyboard navigation', '\t', ['down:9', 'up:9']],
]) {
  test(`synthetic text playback: ${name}`, async ({}, info) => {
    const r = await probe(info, { Mode: 'text', Macro: macro([step(K.text, text, { InputHoldMs: 80, TextIntervalMs: 2 })]) });
    expect(r.Error).toBeNull(); expect(r.Hidden).toBe(true); expect(r.Output).toEqual(expected);
    expect(r.Conditions).toEqual(['original']);
  });
}
test('opt-in paste restores synthetic clipboard and releases modifier keys', async ({}, info) => {
  const r = await probe(info, { Mode: 'text', Macro: macro([step(K.text, 'paste me', { TextEntry: 1 })]) });
  expect(r.Error).toBeNull(); expect(r.Output).toEqual(['down:17', 'down:86', 'up:86', 'up:17']); expect(r.Conditions).toEqual(['original']);
});
test('empty paste sends no synthetic input', async ({}, info) => {
  const r = await probe(info, { Mode: 'text', Macro: macro([step(K.text, '', { TextEntry: 1 })]) });
  expect(r.Error).toBeNull(); expect(r.Output).toEqual([]); expect(r.Conditions).toEqual(['original']);
});
for (const fields of [{ InputHoldMs: -1 }, { ToleranceMode: 99 }]) {
  test(`invalid behavior options rejected: ${JSON.stringify(fields)}`, async ({}, info) => {
    const r = await probe(info, { Mode: 'text', Macro: macro([step(K.text, 'x', fields)]) });
    expect(r.Error).toContain('Invalid input'); expect(r.Output).toEqual([]);
  });
}
test('saved random seed reproduces randomized repeat counts', async ({}, info) => {
  const flow = { ...macro([step(K.repeat, '', { Count: 1, CountMax: 10 }), step(K.text, 'marker'), step(K.endRepeat)]), RandomSeed: 77 };
  const a = await probe(info, { Macro: flow, Repetitions: 6 }, 'first');
  const b = await probe(info, { Macro: flow, Repetitions: 6 }, 'second');
  expect(a.Error).toBeNull(); expect(b.Error).toBeNull(); expect(a.Output).toEqual(b.Output); expect(a.Output.length).toBeGreaterThanOrEqual(6);
});
