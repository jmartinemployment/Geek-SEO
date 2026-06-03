const { chromium } = require('playwright');

const TARGET_URL = 'http://localhost:3000';

(async () => {
  const browser = await chromium.launch({ headless: false });
  const page = await browser.newPage({ viewport: { width: 1440, height: 900 } });

  try {
    console.log('Navigating to topical map...');
    await page.goto(`${TARGET_URL}/app/strategy/topical-map`, { waitUntil: 'domcontentloaded' });
    await page.waitForTimeout(2000);

    // Check if project picker
    const projectPicker = await page.locator('text=Select a project').isVisible().catch(() => false);
    if (projectPicker) {
      console.log('At project picker, selecting first project...');
      await page.waitForTimeout(1000);
      const firstProject = page.locator('[role="option"]').first();
      await firstProject.click();
      await page.waitForTimeout(2000);
    }

    // Debug: print all buttons
    console.log('\n=== ALL BUTTONS ===');
    const buttons = await page.locator('button').allTextContents();
    buttons.forEach((b, i) => console.log(`${i}: ${b}`));

    // Debug: look for "Entity Gaps" text anywhere
    console.log('\n=== SEARCHING FOR "Entity Gaps" ===');
    const entityGapsText = await page.locator('text=Entity Gaps').count().catch(() => 0);
    console.log(`Found "Entity Gaps" text: ${entityGapsText}`);

    // Debug: look for view tabs with different selectors
    console.log('\n=== TAB BUTTONS ===');
    const tabButtons = await page.locator('button:has-text("Table"), button:has-text("Map"), button:has-text("Internal"), button:has-text("Entity")').allTextContents();
    console.log(`Tab buttons found: ${tabButtons.length}`);
    tabButtons.forEach((b) => console.log(`  - ${b}`));

    // Debug: look at page structure
    console.log('\n=== PAGE STRUCTURE (first 2000 chars of innerText) ===');
    const pageText = await page.locator('body').textContent().catch(() => '');
    console.log(pageText.substring(0, 2000));

  } catch (error) {
    console.error('Error:', error.message);
  } finally {
    await browser.close();
  }
})();
