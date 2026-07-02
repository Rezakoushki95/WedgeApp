// One-off debug: dump page text + console errors, screenshot before/after nav.
const { chromium } = require('playwright-core');
const OUT = process.env.SHOT_OUT || '.';
const sleep = (ms) => new Promise((r) => setTimeout(r, ms));
(async () => {
  const b = await chromium.launch({ executablePath: 'C:/Program Files/Google/Chrome/Application/chrome.exe', args: ['--no-sandbox'] });
  const p = await b.newPage({ viewport: { width: 390, height: 844 } });
  const errs = [];
  p.on('pageerror', (e) => errs.push('PAGEERROR: ' + e.message));
  p.on('console', (m) => { if (m.type() === 'error') errs.push('CONSOLE: ' + m.text().slice(0, 300)); });
  await p.goto('http://localhost:8081', { waitUntil: 'load', timeout: 120000 });
  await sleep(12000);
  console.log('=== body text (first 400) ===');
  console.log((await p.evaluate(() => document.body.innerText)).slice(0, 400));
  console.log('=== errors ===');
  console.log(errs.slice(0, 10).join('\n') || '(none)');
  await p.screenshot({ path: `${OUT}/debug1.png` });
  const cards = await p.locator('[role="button"]').count();
  console.log('role=button count:', cards);
  if (cards > 0) {
    await p.locator('[role="button"]').first().click().catch((e) => console.log('click fail', e.message));
    await sleep(9000);
    console.log('=== after click (first 400) ===');
    console.log((await p.evaluate(() => document.body.innerText)).slice(0, 400));
    console.log('=== errors after click ===');
    console.log(errs.slice(0, 14).join('\n') || '(none)');
    await p.screenshot({ path: `${OUT}/debug2.png` });
  }
  await b.close();
})().catch((e) => { console.error('FATAL', e.message); process.exit(1); });
