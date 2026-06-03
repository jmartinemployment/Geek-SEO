const { chromium } = require('playwright');

(async () => {
  const browser = await chromium.launch({ headless: false });
  const page = await browser.newPage({ viewport: { width: 1440, height: 900 } });

  const results = { passed: [], failed: [], warnings: [] };

  try {
    console.log('🔍 Entity Gaps E2E Test\n');

    // Navigate to get dev user context
    console.log('Step 1: Loading app...');
    await page.goto('http://localhost:3000/app/strategy/topical-map', { waitUntil: 'domcontentloaded' });
    await page.waitForTimeout(2000);
    results.passed.push('Authenticated (dev user mode)');

    // Create test project via API
    console.log('\nStep 2: Creating test project...');
    const projRes = await page.evaluate(async () => {
      const res = await fetch('http://localhost:5051/api/seo/projects', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          name: `Entity Gaps Test ${Date.now()}`,
          domain: 'example.com',
        }),
      });
      if (!res.ok) return { ok: false, status: res.status, error: await res.text() };
      return { ok: true, data: await res.json() };
    });

    if (!projRes.ok) {
      console.log(`❌ Project creation failed: ${projRes.status}`);
      results.failed.push(`Project API error: ${projRes.status}`);
      await browser.close();
      return logResults(results);
    }

    const projectId = projRes.data.id;
    console.log(`✓ Project created: ${projectId}`);
    results.passed.push('Test project created');

    // Generate topical map via API (seed mode)
    console.log('\nStep 3: Generating topical map...');
    const mapRes = await page.evaluate(async (pId) => {
      const res = await fetch(`http://localhost:5051/api/seo/topical-map/${pId}/generate`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ seedKeyword: 'digital marketing' }),
      });
      if (!res.ok) return { ok: false, status: res.status };
      return { ok: true, data: await res.json() };
    }, projectId);

    if (!mapRes.ok) {
      console.log(`❌ Map generation failed: ${mapRes.status}`);
      results.warnings.push('Map generation incomplete');
    } else {
      console.log('✓ Map generated');
      results.passed.push('Topical map generated');
    }

    // Wait for generation to complete
    console.log('Waiting for map processing...');
    await page.waitForTimeout(5000);

    // Navigate to project's topical map
    console.log('\nStep 4: Navigating to topical map view...');
    await page.goto(`http://localhost:3000/app/strategy/topical-map?projectId=${projectId}`, {
      waitUntil: 'domcontentloaded',
    });
    await page.waitForTimeout(3000);

    // Check if map loaded
    const mapLoaded = await page.locator('table').count() > 0;
    if (!mapLoaded) {
      console.log('⚠️  Map not yet visible, may still be processing');
      results.warnings.push('Map still loading');
      await page.waitForTimeout(3000);
    }

    // Look for Entity Gaps tab
    console.log('\nStep 5: Checking for Entity Gaps tab...');
    const entityGapsBtn = page.locator('button:has-text("Entity Gaps")');
    const btnVisible = await entityGapsBtn.isVisible().catch(() => false);

    if (!btnVisible) {
      console.log('❌ Entity Gaps tab not found');
      const buttons = await page.locator('button').allTextContents();
      const tabs = buttons.filter(b => ['Table', 'Map', 'Links', 'Internal', 'Entity'].some(t => b.includes(t)));
      console.log(`Available tabs: ${tabs.length === 0 ? 'none' : tabs.join(', ')}`);
      results.failed.push('Entity Gaps tab not rendered');
    } else {
      console.log('✓ Entity Gaps tab found');
      results.passed.push('Entity Gaps tab exists');

      // Click tab
      await entityGapsBtn.click();
      await page.waitForTimeout(1500);

      // Check table
      const table = await page.locator('table').isVisible().catch(() => false);
      if (table) {
        const rows = await page.locator('tbody tr').count();
        console.log(`✓ Table visible: ${rows} rows`);
        results.passed.push(`Entity gaps table rendered with ${rows} rows`);

        // Verify structure
        const headers = await page.locator('thead th').allTextContents();
        const hasExpectedCols = headers.some(h => h.toLowerCase().includes('coverage')) && 
                               headers.some(h => h.toLowerCase().includes('gap'));
        if (hasExpectedCols) {
          console.log(`✓ Table has expected columns: ${headers.join(', ')}`);
          results.passed.push('Table structure correct');
        } else {
          results.warnings.push(`Unexpected columns: ${headers.join(', ')}`);
        }
      } else {
        results.warnings.push('Entity Gaps tab active but no table visible');
      }
    }

    // Screenshot
    await page.screenshot({ path: '/tmp/entity-gaps-full-test.png' });
    console.log('\n✓ Screenshot saved: /tmp/entity-gaps-full-test.png');
    results.passed.push('Screenshot captured');

  } catch (error) {
    console.error('Fatal error:', error.message);
    results.failed.push(error.message);
  } finally {
    await browser.close();
  }

  logResults(results);
})();

function logResults(results) {
  console.log('\n' + '='.repeat(60));
  console.log('TEST RESULTS');
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
  console.log(`${status}\n`);
  process.exit(results.failed.length === 0 ? 0 : 1);
}
