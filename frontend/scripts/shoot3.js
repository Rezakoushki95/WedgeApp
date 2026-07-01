const { chromium } = require('playwright-core');
const EXE = '/opt/pw-browsers/chromium-1194/chrome-linux/chrome';
const OUT = '/tmp/claude-0/-home-user-WedgeApp/ee6ac01f-20d7-55f5-930a-ce0f13e635e3/scratchpad';
const sleep = (ms) => new Promise((r) => setTimeout(r, ms));

(async () => {
  const browser = await chromium.launch({ executablePath: EXE, args: ['--no-sandbox'] });
  const ctx = await browser.newContext({
    viewport: { width: 390, height: 844 }, deviceScaleFactor: 2,
    recordVideo: { dir: OUT, size: { width: 390, height: 844 } },
  });
  const page = await ctx.newPage();
  page.on('pageerror', (e) => console.log('[pageerror]', e.message));

  await page.goto('http://localhost:8081', { waitUntil: 'load', timeout: 120000 });
  await page.waitForFunction(() => document.body.innerText.includes('Bull Flags'), { timeout: 120000 });
  await sleep(1500); // let the user "see" Home in the video

  await page.getByText('Bull Flags', { exact: false }).first().click();
  await page.waitForFunction(() => /Bar \d+\/\d+/.test(document.body.innerText), { timeout: 60000 });
  await sleep(1200);

  const magnets = page.getByText('Magnets');
  if (await magnets.count()) { await magnets.click(); await sleep(600); }
  for (let i = 0; i < 5; i++) { const n = page.getByText(/NEXT BAR/); if (await n.count()) { await n.click(); await sleep(550); } }
  await sleep(700);
  await page.screenshot({ path: `${OUT}/s_trade.png` });
  console.log('trade shot');

  // open a long, advance, exit — for the video
  const long = page.getByText('LONG');
  if (await long.count()) {
    await long.click(); await sleep(700);
    for (let i = 0; i < 2; i++) { const n = page.getByText(/NEXT BAR/); if (await n.count()) { await n.click(); await sleep(550); } }
    const exit = page.getByText('EXIT'); if (await exit.count()) { await exit.click(); await sleep(1000); }
  }
  await sleep(600);

  await ctx.close();
  const vid = await page.video().path();
  console.log('video at', vid);
  await browser.close();
  console.log('DONE');
})().catch((e) => { console.error('FAILED:', e.message); process.exit(1); });
