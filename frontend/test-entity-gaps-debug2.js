const { chromium } = require('playwright');

const TARGET_URL = 'http://localhost:3000';

(async () => {
  const browser = await chromium.launch({ headless: false });
  const page = await browser.newPage({ viewport: { width: 1440, height: 900 } });

  try {
    console.log('Step 1: Navigating...');
    await page.goto(`${TARGET_URL}/app/strategy/topical-map`, { waitUntil: 'domcontentloaded' });
    await page.waitForTimeout(2000);

    // Check for auth
    const loginLink = await page.locator('a[href="/auth/login"]').isVisible().catch(() => false);
    if (loginLink) {
      console.log('❌ Not authenticated — login link visible');
      await browser.close();
      return;
    }

    console.log('✓ Authenticated');

    // Check project picker
    const projectLabel = await page.locator('text=/select.*project/i').isVisible().catch(() => false);
    console.log(`Project picker visible: ${projectLabel}`);

    if (projectLabel) {
      console.log('Waiting for project options...');
      await page.waitForTimeout(1500);
      
      const options = await page.locator('[role="option"]').count();
      console.log(`Found ${options} project options`);
      
      if (options > 0) {
        console.log('Clicking first project...');
        await page.locator('[role="option"]').first().click();
        await page.waitForTimeout(3000);
        
        // Check if project loaded
        const projectName = await page.locator('[class*="project"]').first().textContent().catch(() => '');
        console.log(`Project loaded (text): ${projectName.substring(0, 50)}`);
      }
    }

    // Now check for topical map content
    console.log('\nStep 2: Looking for topical map content...');
    const hasTableView = await page.locator('table').count().catch(() => 0);
    console.log(`Tables found: ${hasTableView}`);

    const hasTabs = await page.locator('button:has-text("Table")').count().catch(() => 0);
    console.log(`"Table" button found: ${hasTabs}`);

    // Screenshot for debugging
    await page.screenshot({ path: '/tmp/entity-gaps-debug.png', fullPage: true });
    console.log('\nScreenshot saved to /tmp/entity-gaps-debug.png');

  } catch (error) {
    console.error('Error:', error.message);
  } finally {
    await browser.close();
  }
})();
