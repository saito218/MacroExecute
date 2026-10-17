const { test, expect } = require('@playwright/test');
const fs = require('node:fs');
const path = require('node:path');
const { probe, macro, step, K } = require('./probe');
const ITEM = fs.readFileSync(path.resolve(__dirname, '../../examples/item-data.txt'), 'utf8');
const IF = 24, COPY = 47, CLEAR = 42;
const branch = (Text, rest = {}) => [step(IF, Text, rest), step(K.message, 'match'), step(K.otherwise), step(K.message, 'skip'), step(K.endIf)];
const run = (info, steps, extra = {}) => probe(info, { Mode: 'clipboard', ClipboardText: ITEM, Macro: macro(steps), ...extra });
for (const [name, Text, fields, matches] of [
  ['exact retains whole text', ITEM, {}, true],
  ['exact rejects substring', 'Monthly Report', {}, false],
  ['any accepts one string', 'Missing field\nMonthly Report', { ClipboardMatch: 1 }, true],
  ['any rejects missing strings', 'Missing field\nPriority: 99', { ClipboardMatch: 1 }, false],
  ['all accepts item combination', 'Document Type: Status Report\nMonthly Report\nStatus: Approved', { ClipboardMatch: 2 }, true],
  ['all rejects one missing string', 'Monthly Report\nPriority: 1', { ClipboardMatch: 2 }, false],
  ['groups accepts second complete combination', 'Monthly Report\nPriority: 1\n\nMonthly Report\nStatus: Approved', { ClipboardMatch: 3 }, true],
  ['groups cannot combine partial alternatives', 'Monthly Report\nPriority: 1\n\nRing\nStatus: Approved', { ClipboardMatch: 3 }, false],
  ['case sensitive by default', 'monthly report', { ClipboardMatch: 2 }, false],
  ['optional ignore case', 'monthly report', { ClipboardMatch: 2, ClipboardIgnoreCase: true }, true],
  ['NOT rejects matching item', 'Monthly Report', { ClipboardMatch: 2, ClipboardNegate: true }, false],
  ['NOT accepts missing property', 'Archived', { ClipboardMatch: 1, ClipboardNegate: true }, true],
  ['punctuation and decimal comma stay literal', '3,80\nVariance: 36(38-33)%\n(Priority: 3)', { ClipboardMatch: 2 }, true],
  ['CRLF and whitespace around criteria', ' Monthly Report \r\n\r\nStatus: Approved\r', { ClipboardMatch: 2 }, true],
  ['regex syntax is not evaluated', 'Monthly.*Report', { ClipboardMatch: 1 }, false],
]) {
  test(`clipboard rules: ${name}`, async ({}, info) => {
    const r = await run(info, branch(Text, fields));
    expect(r.Error).toBeNull(); expect(r.Output).toEqual([matches ? 'match' : 'skip']); expect(r.InputEvents).toEqual([]);
    expect(r.Hidden).toBe(true); expect(r.Released).toBe(true); expect(r.Scope).toBe(0);
  });
}
for (const ClipboardText of [null, '']) {
  test(`missing clipboard text never satisfies NOT (${JSON.stringify(ClipboardText)})`, async ({}, info) => {
    const r = await run(info, branch('Archived', { ClipboardMatch: 1, ClipboardNegate: true }), { ClipboardText });
    expect(r.Error).toBeNull(); expect(r.Output).toEqual(['skip']);
  });
}
test('complete clear hover copy item-data chain follows matching branch', async ({}, info) => {
  const r = await run(info, [step(CLEAR), step(K.move, '', { Pointer: 1 }), step(COPY), ...branch('Monthly Report\nStatus: Approved', { ClipboardMatch: 2 })], { CopyText: ITEM, ClipboardText: 'stale other item' });
  expect(r.Error).toBeNull(); expect(r.InputEvents).toEqual(['down:17', 'down:67', 'up:67', 'up:17']); expect(r.Output).toEqual(['match']); expect(r.Conditions).toEqual([ITEM]);
});
for (const CopyText of [null, '']) {
  test(`copy refuses stale/empty result (${JSON.stringify(CopyText)})`, async ({}, info) => {
    const r = await run(info, [step(COPY, '', { TimeoutMs: 100, PollMs: 50 }), ...branch('Monthly Report', { ClipboardMatch: 1 })], { CopyText });
    expect(r.ErrorType).toBe('TimeoutException'); expect(r.Output).toEqual([]); expect(r.Released).toBe(true); expect(r.InputEvents.slice(-2)).toEqual(['up:67', 'up:17']);
  });
}
test('existing composed Ctrl+C shortcut also copies through the injected target', async ({}, info) => {
  const r = await run(info, [step(K.keyDown, '', { KeyCode: 17 }), step(K.keyPress, '', { KeyCode: 67 }), step(K.keyUp, '', { KeyCode: 17 }), ...branch('Monthly Report', { ClipboardMatch: 1 })], { ClipboardText: null, CopyText: ITEM });
  expect(r.Error).toBeNull(); expect(r.Output).toEqual(['match']);
});
test('empty ELSE skips selected action and continues after END IF', async ({}, info) => {
  const r = await run(info, [step(IF, 'Ring', { ClipboardMatch: 1 }), step(K.message, 'should not run'), step(K.otherwise), step(K.endIf), step(K.message, 'after')]);
  expect(r.Error).toBeNull(); expect(r.Output).toEqual(['after']);
});
test('nested clipboard conditions support independent actions in both branches', async ({}, info) => {
  const r = await run(info, [step(IF, 'Monthly Report', { ClipboardMatch: 1 }), ...branch('Priority: 1', { ClipboardMatch: 1 }), step(K.endIf)]);
  expect(r.Error).toBeNull(); expect(r.Output).toEqual(['skip']); expect(r.MaximumScope).toBe(2); expect(r.Scope).toBe(0);
});
for (const fields of [{ ClipboardMatch: 2, Text: '\n ' }, { ClipboardMatch: 99, Text: 'x' }, { ClipboardMatch: 3, Text: Array(129).fill('x').join('\n') }]) {
  test(`invalid clipboard rules fail before dispatch (${fields.ClipboardMatch}, ${fields.Text.length})`, async ({}, info) => {
    const r = await run(info, branch(fields.Text, fields));
    expect(r.ErrorType).toBe('InvalidDataException'); expect(r.Dispatches).toBe(0); expect(r.Output).toEqual([]);
  });
}
for (const file of ['clipboard-item-check.mexec', 'clipboard-item-check-groups.mexec']) {
  test(`saved example runs against copied item text: ${file}`, async ({}, info) => {
    const saved = JSON.parse(fs.readFileSync(path.resolve(__dirname, '../../examples', file), 'utf8'));
    const r = await probe(info, { Mode: 'clipboard', Macro: saved, ClipboardText: 'old clipboard text', CopyText: ITEM });
    expect(r.Error).toBeNull(); expect(r.Output).toEqual(['Item matches. Replace this message with your desired action(s).']);
    expect(r.InputEvents).toEqual(['down:17', 'down:67', 'up:67', 'up:17']); expect(r.Scope).toBe(0); expect(r.Released).toBe(true);
  });
}

const ROW_RULES = 'string1 string2 "string 3"\nAND\nstring2 "string 3" string4\nGROUP OR\nstring4 "string 5" string6\nAND\n"string 5" string6 string7';
for (const [ClipboardText, matches] of [
  ['string1 string2', true], ['string2', false], ['string4 string 5', true],
  ['string1 string 5', false], ['string 3', false], ['string6', false],
  ['string6 string7', true], ['string1 string 3', true],
]) {
  test(`expandable rules: ${ClipboardText} => ${matches}`, async ({}, info) => {
    const r = await run(info, branch(ROW_RULES, { ClipboardMatch: 4 }), { ClipboardText });
    expect(r.Error).toBeNull(); expect(r.Output).toEqual([matches ? 'match' : 'skip']);
    expect(r.Hidden).toBe(true); expect(r.InputEvents).toEqual([]); expect(r.Scope).toBe(0);
  });
}
for (const [Text, ClipboardText, fields, matches] of [
  ['a b\nAND\na\nAND\nb c', 'a b c', {}, true],
  ['a b\nAND\na\nAND\nb', 'a b', {}, false],
  ['same\nAND\nSAME', 'same', { ClipboardIgnoreCase: true }, false],
  ['red\nOR\nblue green\nAND\nround', 'blue round', {}, true],
  ['"Monthly Report"\nAND\n"Status: Approved"', ITEM, {}, true],
  ['"Monthly Report"\nAND\n"Priority: 1"', ITEM, { ClipboardNegate: true }, true],
  ['"AND" "GROUP OR"', 'GROUP OR', {}, true],
]) {
  test(`row parser and matching: ${JSON.stringify(Text)}`, async ({}, info) => {
    const r = await run(info, branch(Text, { ClipboardMatch: 4, ...fields }), { ClipboardText });
    expect(r.Error).toBeNull(); expect(r.Output).toEqual([matches ? 'match' : 'skip']);
  });
}
for (const Text of ['"unclosed', '""', 'a\nAND', 'AND\na', 'a\nb', 'a\nAND\nGROUP OR\nb', '"phrase"suffix']) {
  test(`invalid row expression rejected before playback: ${JSON.stringify(Text)}`, async ({}, info) => {
    const r = await run(info, branch(Text, { ClipboardMatch: 4 }));
    expect(r.ErrorType).toBe('InvalidDataException'); expect(r.Dispatches).toBe(0); expect(r.InputEvents).toEqual([]);
  });
}
test('saved expandable row example follows the alternate group', async ({}, info) => {
  const saved = JSON.parse(fs.readFileSync(path.resolve(__dirname, '../../examples/clipboard-row-groups.mexec'), 'utf8'));
  const r = await probe(info, { Mode: 'clipboard', Macro: saved, ClipboardText: 'string4 string 5' });
  expect(r.Error).toBeNull(); expect(r.Output).toEqual(['The clipboard matches a complete group.']); expect(r.Scope).toBe(0);
});

for (const [name, Text, ClipboardText, fields, matches] of [
  ['quoted and bare duplicates stay distinct by default', 'word\nAND\n"word"', 'word', {}, false],
  ['explicit group reuse accepts the shared string', 'word\nAND\n"word"', 'word', { ClipboardReuseGroups: [0] }, true],
  ['reuse does not skip another missing requirement', 'word\nAND\n"word"\nAND\nmissing', 'word', { ClipboardReuseGroups: [0] }, false],
  ['reuse applies only to the selected first group', 'a\nAND\na\nGROUP OR\nb\nAND\nb', 'b', { ClipboardReuseGroups: [0] }, false],
  ['second group independently enables reuse', 'a\nAND\na\nGROUP OR\nb\nAND\nb', 'b', { ClipboardReuseGroups: [1] }, true],
  ['ignore case identifies shared criteria', 'word\nAND\n"WORD"', 'word', { ClipboardIgnoreCase: true, ClipboardReuseGroups: [0] }, true],
  ['case sensitive criteria still need their actual text', 'word\nAND\n"WORD"', 'word', { ClipboardReuseGroups: [0] }, false],
  ['phrase duplicates reuse without splitting words', '"Monthly Report"\nAND\n"Monthly Report"', ITEM, { ClipboardReuseGroups: [0] }, true],
]) {
  test(`optional group reuse: ${name}`, async ({}, info) => {
    const r = await run(info, branch(Text, { ClipboardMatch: 4, ...fields }), { ClipboardText });
    expect(r.Error).toBeNull(); expect(r.Output).toEqual([matches ? 'match' : 'skip']);
    expect(r.InputEvents).toEqual([]); expect(r.Hidden).toBe(true); expect(r.Scope).toBe(0);
  });
}
for (const ClipboardReuseGroups of [[-1], [1], [0, 0], null]) {
  test(`invalid reuse group settings rejected: ${JSON.stringify(ClipboardReuseGroups)}`, async ({}, info) => {
    const r = await run(info, branch('a\nAND\na', { ClipboardMatch: 4, ClipboardReuseGroups }));
    expect(r.ErrorType).toBe('InvalidDataException'); expect(r.Dispatches).toBe(0);
  });
}
