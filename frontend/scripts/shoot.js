const { chromium } = require('playwright-core');

const EXE = '/opt/pw-browsers/chromium-1194/chrome-linux/chrome';
const OUT = process.env.SHOT_DIR || '/tmp/claude-0/-home-user-WedgeApp/ee6ac01f-20d7-55f5-930a-ce0f13e635e3/scratchpad';

(async () => {
  const browser = await chromium.launch({ executablePath: EXE, args: ['--no-sandbox'] });
  const page = await browser.newPage({ viewport: { width: 390, height: 844 } });
  page.on('console', (m) => console.log('[browser]', m.type(), m.text()));
  page.on('pageerror', (e) => console.log('[pageerror]', e.message));

  console.log('loading app (first Metro bundle can take a while)...');
  await page.goto('http://localhost:8081', { waitUntil: 'load', timeout: 120000 });

  // Wait for the Home title to render (RN-web renders real DOM text).
  await page.waitForFunction(() => document.body && document.body.innerText.includes('Journeys'), { timeout: 120000 });
  console.log('Home rendered.');
  await page.waitForTimeout(1500);
  await page.screenshot({ path: `${OUT}/home.png` });
  console.log('saved home.png');

  // Go to the Trade screen to exercise the Skia chart. Prefer an existing
  // journey card; fall back to "+ New Journey".
  const card = page.getByText('Bull Flags', { exact: false }).first();
  if (await card.count()) {
    await card.click();
  } else {
    await page.getByText('+ New Journey').click();
  }

  // Wait for the chart to fetch + render: a <canvas> from Skia, and the bar counter.
  await page.waitForFunction(() => document.body.innerText.match(/Bar \d+\/\d+/), { timeout: 60000 });
  await page.waitForTimeout(2500);
  const hasCanvas = await page.locator('canvas').count();
  console.log('canvas elements on Trade screen:', hasCanvas);

  // Advance a few bars so the chart has candles, then screenshot.
  for (let i = 0; i < 5; i++) {
    const next = page.getByText(/NEXT BAR/);
    if (await next.count()) { await next.click(); await page.waitForTimeout(250); }
  }
  await page.waitForTimeout(800);
  await page.screenshot({ path: `${OUT}/trade.png` });
  console.log('saved trade.png');

  await browser.close();
  console.log('DONE');
})().catch((e) => { console.error('SHOOT FAILED:', e.message); process.exit(1); });
