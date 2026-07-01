const { chromium } = require('playwright-core');
const EXE = '/opt/pw-browsers/chromium-1194/chrome-linux/chrome';
const OUT = '/tmp/claude-0/-home-user-WedgeApp/ee6ac01f-20d7-55f5-930a-ce0f13e635e3/scratchpad';
const sleep = (ms) => new Promise((r) => setTimeout(r, ms));

(async () => {
  const browser = await chromium.launch({ executablePath: EXE, args: ['--no-sandbox'] });
  const page = await browser.newPage({ viewport: { width: 390, height: 844 }, deviceScaleFactor: 2 });
  page.on('pageerror', (e) => console.log('[pageerror]', e.message));

  await page.goto('http://localhost:8081', { waitUntil: 'load', timeout: 120000 });
  await page.waitForFunction(() => document.body.innerText.includes('Bull Flags'), { timeout: 120000 });
  await page.getByText('Bull Flags', { exact: false }).first().click();
  await page.waitForFunction(() => /Bar \d+\/\d+/.test(document.body.innerText), { timeout: 60000 });
  await sleep(800);

  const magnets = page.getByText('Magnets');
  if (await magnets.count()) { await magnets.click(); await sleep(300); }

  // Reveal the whole day: click NEXT BAR until it turns into NEW CHART.
  let guard = 0;
  while (guard++ < 120) {
    const next = page.getByText(/NEXT BAR/);
    if (!(await next.count())) break;
    await next.click({ timeout: 5000 }).catch(() => {});
    await sleep(40);
  }
  await sleep(800);
  const bars = await page.evaluate(() => (document.body.innerText.match(/Bar (\d+)\/(\d+)/) || [])[0]);
  console.log('revealed:', bars);
  await page.screenshot({ path: `${OUT}/s_day.png` });
  console.log('day shot');
  await browser.close();
  console.log('DONE');
})().catch((e) => { console.error('FAILED:', e.message); process.exit(1); });
