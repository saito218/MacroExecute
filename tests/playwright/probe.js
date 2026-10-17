const { execFile } = require('node:child_process');
const { promisify } = require('node:util');
const fs = require('node:fs/promises');
const path = require('node:path');
const runFile = promisify(execFile);
const executable = process.env.MACROEXECUTE_TEST_EXE ? path.resolve(process.env.MACROEXECUTE_TEST_EXE) : path.resolve(__dirname, '../../dist/MacroExecute.exe');

async function probe(testInfo, request, label = 'probe') {
  const directory = testInfo.outputPath(label);
  await fs.mkdir(directory, { recursive: true });
  const input = path.join(directory, 'request.json');
  const output = path.join(directory, 'result.json');
  await fs.writeFile(input, JSON.stringify(request));
  await runFile(executable, ['--headless-probe', input, output], {
    windowsHide: true, timeout: 55000, maxBuffer: 1024 * 1024,
  });
  const report = JSON.parse(await fs.readFile(output, 'utf8'));
  await testInfo.attach(label, { path: output, contentType: 'application/json' });
  if (request.Mode === 'ui' || request.Mode === 'settings') {
    await testInfo.attach(`${label}-Aftershock`, { path: path.join(directory, 'workbench.png'), contentType: 'image/png' });
  }
  return report;
}

const step = (Kind, Text = '', rest = {}) => ({ Kind, Text, ...rest });
const macro = Steps => ({ SchemaVersion: 1, Name: 'Isolated test workflow', Steps });
const K = { note: 0, delay: 1, keyDown: 2, keyUp: 3, keyPress: 4, move: 5,
  click: 8, text: 10, launch: 14, ifImage: 23, ifWindow: 25, otherwise: 26,
  endIf: 27, repeat: 28, endRepeat: 29, break: 30, label: 31, jump: 32,
  include: 33, message: 34, pause: 35, script: 44 };
module.exports = { probe, step, macro, K, executable, runFile };
