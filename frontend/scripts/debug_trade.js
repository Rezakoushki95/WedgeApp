// One-off: drive the new arm->tap->enter flow step by step with text dumps.
const fs = require('fs');
const { chromium } = require('playwright-core');
const OUT = process.env.SHOT_OUT || '.';
const sleep = (ms) => new Promise((r) => setTimeout(r, ms));

function findBrowser() {
  const candidates = [
    'C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe',
    'C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe',
  ];
  return process.env.CHROME_PATH || candidates.find((p) => fs.existsSync(p));
}

(async () => {
  const b = await chromium.launch({ executablePath: findBrowser(), args: ['--no-sandbox'] });
  const p = await b.newPage({ viewport: { width: 390, height: 844 } });
  const errs = [];
  p.on('pageerror', (e) => errs.push('PAGEERROR: ' + e.message));
  p.on('console', (m) => { if (m.type() === 'error') errs.push('CONSOLE: ' + m.text().slice(0, 200)); });

  await p.goto('http://localhost:8081', { waitUntil: 'load', timeout: 120000 });
  await p.waitForFunction(() => /\$[\d,]+/.test(document.body.innerText), { timeout: 120000 });
  await p.getByText(/\$[\d,]+/).first().click();
  await p.waitForFunction(() => /Bar \d+\/\d+/.test(document.body.innerText), { timeout: 60000 });
  await sleep(1000);
  console.log('=== trade screen text ===');
  console.log((await p.evaluate(() => document.body.innerText)).replace(/\n/g, ' | ').slice(0, 300));

  const long = p.getByText('LONG', { exact: true });
  console.log('LONG count:', await long.count());
  await long.first().click({ timeout: 8000 }).catch((e) => console.log('LONG click failed:', e.message.split('\n')[0]));
  await sleep(800);
  console.log('=== after LONG tap ===');
  console.log((await p.evaluate(() => document.body.innerText)).replace(/\n/g, ' | ').slice(0, 300));

  const canvas = p.locator('canvas').first();
  console.log('canvas count:', await canvas.count());
  const box = await canvas.boundingBox().catch(() => null);
  console.log('canvas box:', JSON.stringify(box));
  if (box) {
    await p.mouse.click(box.x + box.width * 0.5, box.y + box.height * 0.9);
    await sleep(800);
    console.log('=== after chart tap ===');
    console.log((await p.evaluate(() => document.body.innerText)).replace(/\n/g, ' | ').slice(0, 300));
  }
  // Decisive probe: cancel, arm SHORT, tap the very TOP (highest price).
  await p.getByText('CANCEL', { exact: true }).first().click().catch(() => {});
  await sleep(400);
  await p.getByText('SHORT', { exact: true }).first().click();
  await sleep(500);
  if (box) {
    await p.mouse.click(box.x + box.width * 0.5, box.y + box.height * 0.05);
    await sleep(800);
    console.log('=== SHORT + top tap ===');
    console.log((await p.evaluate(() => document.body.innerText)).replace(/\n/g, ' | ').slice(0, 300));
  }
  await p.screenshot({ path: `${OUT}/debug_trade.png` });
  console.log('=== page errors ===');
  console.log(errs.slice(0, 8).join('\n') || '(none)');
  await b.close();
})().catch((e) => { console.error('FATAL', e.message); process.exit(1); });
