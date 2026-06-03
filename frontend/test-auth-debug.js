const { chromium } = require('playwright');

(async () => {
  const browser = await chromium.launch({ headless: false });
  const page = await browser.newPage();

  const requests = [];
  const responses = [];

  page.on('request', req => {
    requests.push(`→ ${req.method()} ${req.url()}`);
  });

  page.on('response', resp => {
    if (resp.url().includes('auth') || resp.url().includes('token') || resp.url().includes('connect')) {
      responses.push(`← ${resp.status()} ${resp.url()}`);
    }
  });

  page.on('console', msg => {
    if (msg.text().includes('auth') || msg.text().includes('token')) {
      console.log(`[Browser] ${msg.text()}`);
    }
  });

  try {
    console.log('Navigating to /app/strategy/topical-map...');
    await page.goto('http://localhost:3000/app/strategy/topical-map', { waitUntil: 'domcontentloaded' });
    await page.waitForTimeout(3000);

    console.log('\n=== Auth Requests/Responses ===');
    responses.forEach(r => console.log(r));

    if (responses.length === 0) {
      console.log('No auth API calls made');
    }

    console.log('\n=== LocalStorage ===');
    const store = await page.evaluate(() => localStorage);
    console.log(JSON.stringify(Object.fromEntries(Object.entries(store)), null, 2));

  } finally {
    await browser.close();
  }
})();
