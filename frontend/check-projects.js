const { chromium } = require('playwright');

(async () => {
  const browser = await chromium.launch({ headless: true });
  const page = await browser.newPage();

  try {
    // Go to page to get auth token
    await page.goto('http://localhost:3000/app/strategy/topical-map', { waitUntil: 'domcontentloaded' });
    await page.waitForTimeout(1000);

    const accessToken = await page.evaluate(() => {
      const stored = localStorage.getItem('geek_access');
      return stored ? JSON.parse(stored).access_token : null;
    });

    if (!accessToken) {
      console.log('No auth token');
      await browser.close();
      return;
    }

    // Query projects via API
    console.log('Fetching projects...');
    const res = await page.evaluate(async (token) => {
      const r = await fetch('http://localhost:5051/api/seo/projects', {
        headers: { 'Authorization': `Bearer ${token}` },
      });
      return {
        status: r.status,
        data: r.status === 200 ? await r.json() : await r.text(),
      };
    }, accessToken);

    console.log(`Status: ${res.status}`);
    console.log(`Projects: ${JSON.stringify(res.data, null, 2)}`);

  } finally {
    await browser.close();
  }
})();
