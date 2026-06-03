const { chromium } = require('playwright');

(async () => {
  const browser = await chromium.launch({ headless: false });
  const page = await browser.newPage({ viewport: { width: 1440, height: 900 } });

  try {
    console.log('🔍 Testing Entity Gaps at Production\n');

    console.log('Navigating to: https://seo.geekatyourspot.com/app/strategy/topical-map');
    await page.goto('https://seo.geekatyourspot.com/app/strategy/topical-map', {
      waitUntil: 'domcontentloaded',
      timeout: 15000,
    });

    console.log('Waiting for page to load...');
    await page.waitForTimeout(3000);

    // Check if logged in
    const loginBtn = await page.locator('a[href*="login"]').isVisible().catch(() => false);
    if (loginBtn) {
      console.log('⚠️  Not logged in — user needs to login manually');
      console.log('Test must be run with authenticated session');
      await browser.close();
      return;
    }

    console.log('✓ Authenticated');

    // Wait for project list to load
    await page.waitForTimeout(2000);

    // Check for projects
    const projects = await page.locator('[role="option"]').count();
    console.log(`Projects available: ${projects}`);

    if (projects === 0) {
      console.log('⚠️  No projects — cannot test');
      await browser.close();
      return;
    }

    // Select first project
    console.log('Selecting first project...');
    await page.locator('[role="option"]').first().click();
    await page.waitForTimeout(3000);

    // Check for Entity Gaps tab
    console.log('\nLooking for Entity Gaps tab...');
    const entityGapsBtn = page.locator('button:has-text("Entity Gaps")');
    const found = await entityGapsBtn.isVisible().catch(() => false);

    if (found) {
      console.log('✅ Entity Gaps tab FOUND');
      
      await entityGapsBtn.click();
      await page.waitForTimeout(1500);

      const table = await page.locator('table').isVisible().catch(() => false);
      if (table) {
        const rows = await page.locator('tbody tr').count();
        console.log(`✅ Table displayed with ${rows} rows`);
      } else {
        console.log('⚠️  Tab active but table not visible');
      }
    } else {
      console.log('❌ Entity Gaps tab NOT found');
      const allBtns = await page.locator('button').allTextContents();
      const tabs = allBtns.filter(b => ['Table', 'Map', 'Links', 'Internal', 'Entity'].some(t => b.includes(t)));
      console.log(`Available tabs: ${tabs.join(', ')}`);
    }

    console.log('\nTaking screenshot...');
    await page.screenshot({ path: '/tmp/prod-entity-gaps.png' });
    console.log('Screenshot: /tmp/prod-entity-gaps.png');

  } catch (error) {
    console.error('Error:', error.message);
  } finally {
    await browser.close();
  }
})();
