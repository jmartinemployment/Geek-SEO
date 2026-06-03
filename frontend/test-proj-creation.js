const { chromium } = require('playwright');

(async () => {
  const browser = await chromium.launch({ headless: true });
  const page = await browser.newPage();

  await page.goto('http://localhost:3000/app/strategy/topical-map', { waitUntil: 'domcontentloaded' });
  await page.waitForTimeout(1000);

  const res = await page.evaluate(async () => {
    const r = await fetch('http://localhost:5051/api/seo/projects', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        name: 'Test Project',
        domain: 'test.com',
      }),
    });
    return {
      status: r.status,
      text: await r.text(),
    };
  });

  console.log('Status:', res.status);
  console.log('Response:', res.text);

  await browser.close();
})();
