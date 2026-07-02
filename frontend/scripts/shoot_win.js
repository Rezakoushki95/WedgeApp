// Capture Home + Trade screenshots of the Expo-web app with a headless browser.
// Windows/macOS/Linux: auto-detects an installed Chrome/Edge (no browser download).
//
// Prereqs: backend running (has market data + at least one journey), and
//   `npx expo start --web --port 8081` serving the app.
// Run:  SHOT_OUT=/path/to/dir  node scripts/shoot_win.js
//   SHOT_OUT   output dir for the PNGs (default: current dir)
//   APP_URL    app URL (default: http://localhost:8081)
//   CHROME_PATH override the browser executable path
const fs = require('fs');
const { chromium } = require('playwright-core');

const OUT = process.env.SHOT_OUT || '.';
const APP_URL = process.env.APP_URL || 'http://localhost:8081';
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
    '/usr/bin/chromium-browser',
  ];
  const exe = candidates.find((p) => fs.existsSync(p));
  if (!exe) throw new Error('No Chrome/Edge found; set CHROME_PATH');
  return exe;
}

(async () => {
  const browser = await chromium.launch({ executablePath: findBrowser(), args: ['--no-sandbox'] });
  const page = await browser.newPage({ viewport: { width: 390, height: 844 }, deviceScaleFactor: 2 });
  page.on('pageerror', (e) => console.log('[pageerror]', e.message));

  await page.goto(APP_URL, { waitUntil: 'load', timeout: 180000 });

  // Wait for Home to render either a journey card (shows a $ bankroll) or its empty state.
  await page.waitForFunction(
    () => /\$[\d,]+/.test(document.body.innerText) || /No journeys yet/.test(document.body.innerText),
    { timeout: 180000 }
  );
  await sleep(1500);
  await page.screenshot({ path: `${OUT}/app_home.png` });
  console.log('home shot');

  // Open the first journey card -> Trade screen (skip if Home is empty).
  // Note: react-native-web 0.21+ no longer emits role="button" on touchables,
  // so locate the card by its bankroll text instead.
  const card = page.getByText(/\$[\d,]+/).first();
  if ((await card.count()) === 0) {
    console.warn('no journey card found (empty Home?) — skipping Trade shots');
    await browser.close();
    console.log('DONE');
    return;
  }
  await card.click({ timeout: 15000 }).catch(() => {});
  await page.waitForFunction(() => /Bar \d+\/\d+/.test(document.body.innerText), { timeout: 60000 });
  await sleep(1500);
  await page.screenshot({ path: `${OUT}/app_trade_start.png` });
  console.log('trade-start shot');

  const magnets = page.getByText('Magnets', { exact: false });
  if (await magnets.count()) { await magnets.first().click().catch(() => {}); await sleep(400); }

  // Reveal ~30 bars.
  for (let i = 0; i < 30; i++) {
    const next = page.getByText(/NEXT BAR/i);
    if (!(await next.count())) break;
    await next.first().click({ timeout: 5000 }).catch(() => {});
    await sleep(60);
  }
  await sleep(1000);
  const bars = await page.evaluate(() => (document.body.innerText.match(/Bar (\d+)\/(\d+)/) || [])[0]);
  console.log('revealed:', bars);
  await page.screenshot({ path: `${OUT}/app_trade_revealed.png` });
  console.log('trade-revealed shot');

  await browser.close();
  console.log('DONE');
})().catch((e) => { console.error('FAILED:', e.message); process.exit(1); });
