const { chromium } = require('playwright');

const TARGET_URL = 'http://localhost:3000';

(async () => {
  const browser = await chromium.launch({ headless: false });
  const page = await browser.newPage({ viewport: { width: 1440, height: 900 } });

  const results = { passed: [], failed: [], warnings: [] };

  try {
    console.log('🔍 Entity Gaps E2E Test\n');

    // Step 1: Navigate and wait for auth
    console.log('Step 1: Navigating to topical map...');
    await page.goto(`${TARGET_URL}/app/strategy/topical-map`, { waitUntil: 'domcontentloaded' });
    
    console.log('Waiting for auth to initialize...');
    let token = null;
    for (let i = 0; i < 20; i++) {
      await page.waitForTimeout(500);
      token = await page.evaluate(() => {
        const stored = localStorage.getItem('geek_access');
        return stored ? JSON.parse(stored).access_token : null;
      });
      if (token) {
        console.log(`✓ Authenticated after ${(i+1)*500}ms`);
        results.passed.push('User authenticated');
        break;
      }
    }

    if (!token) {
      console.log('❌ Auth timeout');
      results.failed.push('Authentication timeout');
      await browser.close();
      return logResults(results);
    }

    // Step 2: Check for projects
    console.log('\nStep 2: Checking for projects...');
    await page.waitForTimeout(2000);

    const projectSelect = page.locator('select, [role="combobox"]').first();
    const selectExists = await projectSelect.isVisible().catch(() => false);

    if (!selectExists) {
      console.log('❌ Project selector not found');
      results.failed.push('Project selector missing');
      await browser.close();
      return logResults(results);
    }

    // Try to select a project
    const options = await page.locator('[role="option"]').count();
    console.log(`Found ${options} project options`);

    if (options === 0) {
      console.log('⚠️  No projects available');
      results.warnings.push('No projects in database');
      await browser.close();
      return logResults(results);
    }

    // Click first project
    await page.locator('[role="option"]').first().click();
    console.log('Selected first project');
    await page.waitForTimeout(2000);
    results.passed.push('Project selected');

    // Step 3: Check topical map state
    console.log('\nStep 3: Checking topical map...');
    const hasMap = await page.locator('table').count().catch(() => 0) > 0;
    const hasGenerateBtn = await page.locator('button:has-text("Generate")').isVisible().catch(() => false);

    if (!hasMap && !hasGenerateBtn) {
      console.log('Loading map content...');
      await page.waitForTimeout(2000);
    }

    if (hasMap) {
      console.log('✓ Topical map exists');
      results.passed.push('Topical map found');
    } else if (hasGenerateBtn) {
      console.log('No map yet — generating with seed keyword...');
      await page.click('button:has-text("Generate")');
      await page.waitForTimeout(1500);

      const seedInput = page.locator('input[placeholder*="keyword" i]').first();
      if (await seedInput.isVisible()) {
        await seedInput.fill('digital marketing');
        await page.click('button:has-text("Generate")');
        console.log('Generation started, waiting...');

        for (let i = 0; i < 20; i++) {
          await page.waitForTimeout(2000);
          const mapReady = await page.locator('table').count() > 0;
          if (mapReady) {
            console.log('✓ Map generated');
            results.passed.push('Topical map generated');
            break;
          }
        }
      }
    }

    // Step 4: Look for Entity Gaps tab
    console.log('\nStep 4: Finding Entity Gaps tab...');
    await page.waitForTimeout(1000);

    const entityGapsBtn = page.locator('button:has-text("Entity Gaps")');
    const btnVisible = await entityGapsBtn.isVisible().catch(() => false);

    if (!btnVisible) {
      console.log('❌ Entity Gaps tab not found');
      const allBtns = await page.locator('button').allTextContents();
      console.log(`Available buttons: ${allBtns.filter(b => ['Table', 'Map', 'Links', 'Internal', 'Entity'].some(t => b.includes(t))).join(', ')}`);
      results.failed.push('Entity Gaps tab missing');
    } else {
      console.log('✓ Entity Gaps tab found');
      results.passed.push('Entity Gaps tab exists');

      await entityGapsBtn.click();
      await page.waitForTimeout(1500);

      const tableVisible = await page.locator('table').isVisible();
      if (tableVisible) {
        const rows = await page.locator('tbody tr').count();
        console.log(`✓ Table visible with ${rows} rows`);
        results.passed.push(`Entity gaps table displayed with ${rows} rows`);

        // Verify columns
        const headers = await page.locator('thead th').allTextContents();
        console.log(`Columns: ${headers.join(', ')}`);
        if (headers.length >= 3) results.passed.push('Table has expected columns');
      } else {
        results.warnings.push('Entity Gaps tab active but table not visible');
      }
    }

    // Step 5: Screenshot
    await page.screenshot({ path: '/tmp/entity-gaps-result.png' });
    console.log('\n✓ Screenshot: /tmp/entity-gaps-result.png');
    results.passed.push('Screenshot captured');

  } catch (error) {
    console.error('Error:', error.message);
    results.failed.push(`${error.message}`);
  } finally {
    await browser.close();
  }

  logResults(results);
})();

function logResults(results) {
  console.log('\n' + '='.repeat(60));
  console.log('RESULTS');
  console.log('='.repeat(60));
  console.log(`✓ Passed: ${results.passed.length}`);
  results.passed.forEach(p => console.log(`  • ${p}`));
  
  if (results.warnings.length > 0) {
    console.log(`⚠️  Warnings: ${results.warnings.length}`);
    results.warnings.forEach(w => console.log(`  • ${w}`));
  }
  
  if (results.failed.length > 0) {
    console.log(`❌ Failed: ${results.failed.length}`);
    results.failed.forEach(f => console.log(`  • ${f}`));
  }

  console.log('='.repeat(60));
  const status = results.failed.length === 0 ? '✅ PASS' : '❌ FAIL';
  console.log(`Overall: ${status}\n`);
  process.exit(results.failed.length === 0 ? 0 : 1);
}
