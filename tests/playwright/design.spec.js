const { test, expect } = require('@playwright/test');
const fs = require('node:fs/promises');
const { probe, macro } = require('./probe');

for (const View of ['preferences', 'shortcuts', 'bindings', 'picker', 'schedule']) {
  test(`Aftershock ${View} view renders hidden with clear headings`, async ({}, info) => {
    const r = await probe(info, { Mode: 'settings', View, Macro: macro([]) });
    expect(r.Error).toBeNull(); expect(r.Hidden).toBe(true);
    const expected = { preferences: 'Preferences', shortcuts: 'Keyboard shortcuts', bindings: 'Workflow shortcuts', picker: 'Open a workflow', schedule: 'Schedule workflow' };
    expect(r.Output).toContain(expected[View]);
  });
}
test('workflow picker searches recent locations without opening or playing', async ({}, info) => {
  const one = info.outputPath('first.mexec'), two = info.outputPath('second.mexec');
  await fs.writeFile(one, JSON.stringify(macro([]))); await fs.writeFile(two, JSON.stringify(macro([])));
  const r = await probe(info, { Mode: 'settings', View: 'picker', RecentPaths: [one, two], Find: 'second', Macro: macro([]) });
  expect(r.Error).toBeNull(); expect(r.Hidden).toBe(true); expect(r.Steps).toBe(1); expect(r.Dispatches).toBe(0);
});
test('shortcut search gives inline no-match feedback while preserving all bindings', async ({}, info) => {
  const r = await probe(info, { Mode: 'settings', View: 'shortcuts', Find: 'no-such-action-9284', Macro: macro([]) });
  expect(r.Error).toBeNull(); expect(r.Hidden).toBe(true); expect(r.Steps).toBe(22);
  expect(r.Output.some(text => text.startsWith('No matching shortcuts'))).toBe(true);
});
