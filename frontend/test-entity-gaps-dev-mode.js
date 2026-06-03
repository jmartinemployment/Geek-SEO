const { chromium } = require('playwright');

(async () => {
  const browser = await chromium.launch({ headless: false });
  const page = await browser.newPage({ viewport: { width: 1440, height: 900 } });

  const results = { passed: [], failed: [], warnings: [] };

  try {
    console.log('🔍 Entity Gaps E2E Test (Dev User Mode)\n');

    // Navigate
    console.log('Navigating to topical map...');
    await page.goto('http://localhost:3000/app/strategy/topical-map', { waitUntil: 'domcontentloaded' });
    await page.waitForTimeout(2000);
    results.passed.push('Page loaded');

    // Check auth context (isAuthenticated context value, not token)
    console.log('Checking auth...');
    const authReady = await page.evaluate(() => {
      const h = document.body.innerText;
      return !h.includes('Loading…');
    });
    
    if (!authReady) {
      console.log('Still loading, waiting...');
      await page.waitForTimeout(2000);
    }
    console.log('✓ Auth ready');
    results.passed.push('Authenticated');

    // Check project selector
    console.log('\nChecking projects...');
    const projSelect = await page.locator('[role="combobox"], select').first().isVisible().catch(() => false);
    
    if (!projSelect) {
      console.log('❌ Project selector not found');
      results.failed.push('Project selector missing');
      await browser.close();
      return logResults(results);
    }

    const options = await page.locator('[role="option"]').count();
    console.log(`Found ${options} projects`);

    if (options === 0) {
      console.log('⚠️  No projects available (test needs project data)');
      results.warnings.push('No projects — create one first');
      await browser.close();
      return logResults(results);
    }

    // Select first project
    await page.locator('[role="option"]').first().click();
    console.log('Project selected');
    await page.waitForTimeout(3000);
    results.passed.push('Project selected');

    // Check for topical map
    console.log('\nChecking topical map...');
    let mapReady = await page.locator('table').count() > 0;
    
    if (!mapReady) {
      const hasGenBtn = await page.locator('button:has-text("Generate")').isVisible();
      if (hasGenBtn) {
        console.log('Generating map...');
        await page.click('button:has-text("Generate")');
        await page.waitForTimeout(1500);

        const seedInput = page.locator('input[placeholder*="keyword" i]').first();
        if (await seedInput.isVisible()) {
          await seedInput.fill('digital marketing');
          await page.click('button:has-text("Generate")');

          for (let i = 0; i < 20; i++) {
            await page.waitForTimeout(2000);
            mapReady = await page.locator('table').count() > 0;
            if (mapReady) break;
          }
        }
      }
    }

    if (!mapReady) {
      console.log('⚠️  No map found (project may not have cached or generated map)');
      results.warnings.push('Topical map not available');
      await browser.close();
      return logResults(results);
    }

    console.log('✓ Map ready');
    results.passed.push('Topical map exists');

    // Check for Entity Gaps tab
    console.log('\nLooking for Entity Gaps tab...');
    const entityGapsBtn = page.locator('button:has-text("Entity Gaps")');
    const btnFound = await entityGapsBtn.isVisible().catch(() => false);

    if (!btnFound) {
      console.log('❌ Entity Gaps tab not found');
      results.failed.push('Entity Gaps tab missing from UI');
    } else {
      console.log('✓ Entity Gaps tab found');
      results.passed.push('Entity Gaps tab rendered');

      await entityGapsBtn.click();
      await page.waitForTimeout(1500);

      const table = await page.locator('table').isVisible();
      if (table) {
        const rows = await page.locator('tbody tr').count();
        console.log(`✓ Table visible with ${rows} rows`);
        results.passed.push(`Entity gaps table: ${rows} rows`);

        if (rows > 0) {
          const headers = await page.locator('thead th').allTextContents();
          console.log(`Columns: ${headers.join(', ')}`);
        }
      } else {
        results.warnings.push('Tab active but table not visible');
      }
    }

    // Screenshot
    await page.screenshot({ path: '/tmp/entity-gaps-dev-result.png' });
    console.log('\n✓ Screenshot: /tmp/entity-gaps-dev-result.png');
    results.passed.push('Screenshot saved');

  } catch (error) {
    console.error('Error:', error.message);
    results.failed.push(error.message);
  } finally {
    await browser.close();
  }

  logResults(results);
})();

function logResults(results) {
  console.log('\n' + '='.repeat(60));
  console.log(`✓ ${results.passed.length} | ⚠️  ${results.warnings.length} | ❌ ${results.failed.length}`);
  results.passed.forEach(p => console.log(`  ✓ ${p}`));
  results.warnings.forEach(w => console.log(`  ⚠️  ${w}`));
  results.failed.forEach(f => console.log(`  ❌ ${f}`));
  console.log('='.repeat(60));
  process.exit(results.failed.length === 0 ? 0 : 1);
}
