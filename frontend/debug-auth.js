const { chromium } = require('playwright');

(async () => {
  const browser = await chromium.launch({ headless: false });
  const page = await browser.newPage();

  const logs = [];
  const errors = [];

  page.on('console', msg => {
    const text = `[${msg.type()}] ${msg.text()}`;
    logs.push(text);
    if (msg.type() === 'error') errors.push(text);
  });

  page.on('response', resp => {
    if (resp.status() >= 400) {
      console.log(`❌ ${resp.request().method()} ${resp.url()} → ${resp.status()}`);
    }
  });

  try {
    console.log('Navigating...');
    await page.goto('http://localhost:3000/app/dashboard', { waitUntil: 'domcontentloaded' });
    await page.waitForTimeout(3000);

    console.log('\n=== Console Logs ===');
    logs.slice(-10).forEach(l => console.log(l));

    if (errors.length > 0) {
      console.log('\n=== Errors ===');
      errors.forEach(e => console.log(e));
    }

    console.log('\n=== LocalStorage ===');
    const storage = await page.evaluate(() => {
      const keys = Object.keys(localStorage);
      const data = {};
      keys.forEach(k => {
        try {
          data[k] = JSON.parse(localStorage.getItem(k));
        } catch {
          data[k] = localStorage.getItem(k);
        }
      });
      return data;
    });
    console.log(JSON.stringify(storage, null, 2));

  } finally {
    await browser.close();
  }
})();
