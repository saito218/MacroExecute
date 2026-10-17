const { test, expect } = require('@playwright/test');
const fs = require('node:fs/promises');
const path = require('node:path');
const { probe, step, macro, K, executable, runFile } = require('./probe');

for (const extension of ['mexec', 'mmacro.json', 'json']) {
  test(`CLI validates ${extension} workflows by content`, async ({}, info) => {
    const file = info.outputPath(`workflow.${extension}`);
    await fs.writeFile(file, JSON.stringify(macro([step(K.note, 'compatible')])));
    await runFile(executable, ['--validate', file], { windowsHide: true, timeout: 10000 });
  });
}
test('new file type retains embedded images during roundtrip', async ({}, info) => {
  const example = JSON.parse(await fs.readFile(path.resolve(__dirname, '../../examples/nested-image-if.mexec'), 'utf8'));
  const r = await probe(info, { Mode: 'roundtrip', Macro: example });
  expect(r.Error).toBeNull();
  const saved = JSON.parse(await fs.readFile(info.outputPath('probe', 'roundtrip.mexec'), 'utf8'));
  expect(saved.SchemaVersion).toBe(1); expect(saved.Steps[0].ImageData).toBe(example.Steps[0].ImageData);
  expect(saved.Steps[2].ImageData).toBe(example.Steps[2].ImageData);
});
test('new parent includes a legacy child which includes a new grandchild', async ({}, info) => {
  const directory = info.outputPath('mixed');await fs.mkdir(directory, { recursive: true });
  await fs.writeFile(path.join(directory, 'grandchild.mexec'), JSON.stringify(macro([step(K.message, 'nested-leaf')])));
  const child = path.join(directory, 'child.mmacro.json');
  await fs.writeFile(child, JSON.stringify(macro([step(K.include, '', { Target: 'grandchild.mexec' })])));
  const parent = path.join(directory, 'parent.mexec');
  await fs.writeFile(parent, JSON.stringify(macro([step(K.repeat, '', { Count: 3 }), step(K.include, '', { Target: 'child.mmacro.json' }), step(K.endRepeat)])));
  await runFile(executable, ['--validate', parent], { windowsHide: true, timeout: 10000 });
  const r = await probe(info, { Macro: macro([step(K.include, '', { Target: parent })]) });
  expect(r.Error).toBeNull();expect(r.Output).toEqual(['nested-leaf', 'nested-leaf', 'nested-leaf']);
});
test('mexec player exports and verifies with embedded image references', async ({}, info) => {
  const source = path.resolve(__dirname, '../../examples/nested-image-if.mexec');
  const player = info.outputPath('new-format-player.exe');
  await runFile(executable, ['--export', source, player], { windowsHide: true, timeout: 10000 });
  await runFile(executable, ['--verify-player', player], { windowsHide: true, timeout: 10000 });
  expect((await fs.stat(player)).size).toBeGreaterThan((await fs.stat(executable)).size);
});
