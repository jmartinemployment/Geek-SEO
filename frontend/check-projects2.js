const { chromium } = require('playwright');

(async () => {
  const browser = await chromium.launch({ headless: true });
  const page = await browser.newPage();

  try {
    await page.goto('http://localhost:3000/app/strategy/topical-map', { waitUntil: 'domcontentloaded' });
    
    console.log('Waiting for auth token to be set...');
    let token = null;
    for (let i = 0; i < 15; i++) {
      await page.waitForTimeout(500);
      token = await page.evaluate(() => {
        const stored = localStorage.getItem('geek_access');
        return stored ? JSON.parse(stored).access_token : null;
      });
      if (token) {
        console.log(`✓ Token found after ${(i+1)*500}ms`);
        break;
      }
    }

    if (!token) {
      console.log('No token found after 7.5s');
      await browser.close();
      return;
    }

    console.log('Fetching projects...');
    const res = await fetch('http://localhost:5051/api/seo/projects', {
      headers: { 'Authorization': `Bearer ${token}` },
    });

    console.log(`Status: ${res.status}`);
    const data = await res.json();
    console.log(`Projects: ${JSON.stringify(data, null, 2)}`);

  } finally {
    await browser.close();
  }
})();
