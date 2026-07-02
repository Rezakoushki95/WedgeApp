// One-off debug: dump page text + console errors, screenshot before/after nav.
const fs = require('fs');
const { chromium } = require('playwright-core');
const OUT = process.env.SHOT_OUT || '.';
const sleep = (ms) => new Promise((r) => setTimeout(r, ms));

function findBrowser() {
  if (process.env.CHROME_PATH) return process.env.CHROME_PATH;
  const candidates = [
    'C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe',
    'C:\\Program Files (x86)\\Google\\Chrome\\Application\\chrome.exe',
    'C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe',
    'C:\\Program Files\\Microsoft\\Edge\\Application\\msedge.exe',
    '/Applications/Google Chrome.app/Contents/MacOS/Google Chrome',
    '/usr/bin/google-chrome',
    '/usr/bin/chromium',
  ];
  const exe = candidates.find((p) => fs.existsSync(p));
  if (!exe) throw new Error('No Chrome/Edge found; set CHROME_PATH');
  return exe;
}

(async () => {
  const b = await chromium.launch({ executablePath: findBrowser(), args: ['--no-sandbox'] });
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
  // react-native-web 0.21+ emits no role="button"; find the journey card by bankroll text.
  const card = p.getByText(/\$[\d,]+/).first();
  const cards = await card.count();
  console.log('journey card count:', cards);
  if (cards > 0) {
    await card.click().catch((e) => console.log('click fail', e.message));
    await sleep(9000);
    console.log('=== after click (first 400) ===');
    console.log((await p.evaluate(() => document.body.innerText)).slice(0, 400));
    console.log('=== errors after click ===');
    console.log(errs.slice(0, 14).join('\n') || '(none)');
    await p.screenshot({ path: `${OUT}/debug2.png` });
  }
  await b.close();
})().catch((e) => { console.error('FATAL', e.message); process.exit(1); });
