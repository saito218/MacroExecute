const { test, expect } = require('@playwright/test');
const { probe, step, macro, K } = require('./probe');

function nested(depth, failAt) {
  const steps = [];
  for (let i = 0; i < depth; i++) steps.push(step(K.ifWindow, `${i === failAt ? 'false' : 'true'}:${i}`));
  steps.push(step(K.message, 'leaf'));
  for (let i = depth - 1; i >= 0; i--) steps.push(step(K.otherwise), step(K.message, `fallback-${i}`), step(K.endIf), step(K.message, `after-${i}`));
  return steps;
}
for (const depth of [16, 64, 256]) {
  for (const failure of [-1, 0, Math.floor(depth / 2), depth - 1]) {
    test(`${depth} nested IFs, false condition at ${failure === -1 ? 'none' : failure}`, async ({}, info) => {
      const r = await probe(info, { Macro: macro(nested(depth, failure)) });
      const last = failure === -1 ? depth - 1 : failure;
      const expected = [failure === -1 ? 'leaf' : `fallback-${failure}`];
      for (let i = last; i >= 0; i--) expected.push(`after-${i}`);
      expect(r.Error).toBeNull(); expect(r.Cancelled).toBe(false); expect(r.Output).toEqual(expected);
      expect(r.Conditions).toEqual(Array.from({ length: last + 1 }, (_, i) => `${i === failure ? 'false' : 'true'}:${i}`));
      expect(r.MaximumScope).toBe(last + 1); expect(r.Scope).toBe(0); expect(r.Released).toBe(true);
    });
  }
}
test('nested IF block reorders as a complete unit in a 300+ step editor', async ({}, info) => {
  const block = nested(64, -1); const original = [...block, step(K.message, 'tail')];
  // The outer block ends before its separate after-0 action.
  const end = original.length - 2;
  const r = await probe(info, { Mode: 'ui', Macro: macro(original), MoveStart: 0, MoveGap: original.length });
  expect(r.Error).toBeNull(); expect(r.Hidden).toBe(true); expect(r.Moved).toBe(true);
  expect(r.Output).toEqual([...original.slice(end), ...original.slice(0, end)].map(s => s.Text));
});
for (const count of [2, 5, 10]) {
  test(`IF/ELSE branches reset correctly over ${count} whole-macro repetitions`, async ({}, info) => {
    const r = await probe(info, { Macro: macro(nested(10, 6)), Repetitions: count });
    const once = ['fallback-6', ...Array.from({ length: 7 }, (_, i) => `after-${6 - i}`)];
    expect(r.Error).toBeNull(); expect(r.Output).toEqual(Array.from({ length: count }, () => once).flat());
    expect(r.Resets).toBe(count + 1); expect(r.Scope).toBe(0);
  });
}

// A tree-shaped reference interpreter is intentionally independent of the flat
// native program counter, IF indexes and loop-stack implementation.
function randomTree(seed) {
  let state = seed >>> 0, id = 0;
  const random = n => { state = (Math.imul(state, 1664525) + 1013904223) >>> 0; return state % n; };
  function branch(depth) {
    return Array.from({ length: 2 + random(3) }, () => {
      const node = { id: id++, enabled: random(9) !== 0 };
      const type = depth ? random(4) : 0;
      if (type === 1) return { ...node, type: 'if', value: random(2) === 1, yes: branch(depth - 1), no: branch(depth - 1) };
      if (type === 2) return { ...node, type: 'repeat', count: 1 + random(3), body: branch(depth - 1) };
      return { ...node, type: 'action' };
    });
  }
  return branch(4);
}
function flatten(nodes) {
  return nodes.flatMap(n => {
    let result;
    if (n.type === 'if') result = [step(K.ifWindow, `${n.value}:${n.id}`), ...flatten(n.yes), step(K.otherwise), ...flatten(n.no), step(K.endIf)];
    else if (n.type === 'repeat') result = [step(K.repeat, '', { Count: n.count }), ...flatten(n.body), step(K.endRepeat)];
    else result = [step(K.message, `action-${n.id}`)];
    return n.enabled ? result : result.map(s => ({ ...s, Enabled: false }));
  });
}
function interpret(nodes, result = { output: [], conditions: [] }) {
  for (const n of nodes) {
    if (!n.enabled) continue;
    if (n.type === 'if') { result.conditions.push(`${n.value}:${n.id}`); interpret(n.value ? n.yes : n.no, result); }
    else if (n.type === 'repeat') for (let i = 0; i < n.count; i++) interpret(n.body, result);
    else result.output.push(`action-${n.id}`);
  }
  return result;
}
for (let seed = 1; seed <= 30; seed++) {
  test(`seed ${seed}: random nested IF/ELSE/repeats/disabled steps match independent oracle`, async ({}, info) => {
    const tree = randomTree(seed), expected = interpret(tree);
    const r = await probe(info, { Macro: macro(flatten(tree)) }, `seed-${seed}`);
    expect(r.Error).toBeNull(); expect(r.Cancelled).toBe(false);
    expect(r.Output).toEqual(expected.output); expect(r.Conditions).toEqual(expected.conditions);
    expect(r.Scope).toBe(0);
  });
}
